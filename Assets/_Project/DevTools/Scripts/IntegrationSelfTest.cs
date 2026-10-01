using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Economy;
using SteepingSpirits.Economy.Integration;
using SteepingSpirits.Inventory;
using SteepingSpirits.Inventory.Integration;
using SteepingSpirits.Progression;
using SteepingSpirits.Progression.Integration;
using SteepingSpirits.Quests;
using SteepingSpirits.Quests.Data;
using SteepingSpirits.Quests.Integration;
using SteepingSpirits.World;

namespace SteepingSpirits.DevTools
{
    /// <summary>
    /// In-Engine-Selbsttest: fährt beim Play ein komplettes Szenario durch,
    /// das Quests, Inventar, Wallet, XP, JSON-Quests, Spielzeit/Daily-Reset und
    /// Dev-Kit-Aktionen zusammen prüft, und zeigt oben links eine PASS/FAIL-Liste
    /// (auch in der Konsole).
    ///
    /// Benutzung: in eine LEERE Test-Szene ein GameObject legen, diese
    /// Komponente drauf, Play. (Erzeugt QuestManager/PlayerInventory/Wallet
    /// selbst — bitte NICHT zusammen mit der TestMeadow laufen lassen.)
    /// </summary>
    public class IntegrationSelfTest : MonoBehaviour
    {
        private const string DailyJson = @"{
  ""questID"": ""Q_DAILY_Test"", ""displayName"": ""Daily-Test"", ""category"": ""DAILY"", ""isRepeatable"": true,
  ""objectives"": [ { ""objectiveID"": ""d_talk"", ""stepType"": ""Talk"", ""targetID"": ""npc_test"", ""requiredAmount"": 1 } ],
  ""rewards"": [ { ""type"": ""Gold"", ""amount"": 1 } ]
}";

        private readonly List<(string name, bool ok)> results = new List<(string, bool)>();
        private Health testHealth;
        private bool visible = true;
        private bool allPass;
        private float autoCloseAt;
        private const float AutoCloseSeconds = 4f;

        private void Start()
        {
            // --- Systeme aufsetzen ---
            EnsureSingleton<QuestManager>("QuestManager");
            EnsureSingleton<PlayerInventory>("PlayerInventory");
            EnsureSingleton<Wallet>("Wallet");
            EnsureSingleton<PlayerProgression>("PlayerProgression");
            EnsureSingleton<GameClock>("GameClock");

            gameObject.AddComponent<InventoryPickupBridge>();   // Aufsammeln -> Inventar
            gameObject.AddComponent<QuestRewardCollector>();    // Item-Rewards -> Inventar
            gameObject.AddComponent<GoldRewardCollector>();     // Gold-Rewards -> Wallet
            gameObject.AddComponent<XpRewardCollector>();       // XP-Rewards -> Progression
            gameObject.AddComponent<DailyQuestReset>();         // neuer Tag -> Dailies wieder verfügbar
            InventoryEvents.OnItemUsed += HandleItemUsed;       // Trank -> heilen

            var mgr = QuestManager.Instance;
            var inv = PlayerInventory.Instance;

            inv.RegisterItem(MakeItem("item_test_coin", 999, false));
            inv.RegisterItem(MakeItem("item_test_flower", 50, false));
            inv.RegisterItem(MakeItem("potion_test", 20, true));

            mgr.RegisterQuest(MakeQuest("Q_TEST_coins", "c_coins", "item_test_coin", 5,
                new Reward { type = RewardType.Gold, amount = 50 },
                new Reward { type = RewardType.Item, itemID = "potion_test", amount = 1 }));
            mgr.RegisterQuest(MakeQuest("Q_TEST_flowers", "c_flowers", "item_test_flower", 3,
                new Reward { type = RewardType.XP, amount = 20 }));

            // Test-Spieler für die Heilung – bewusst OHNE Player-Tag, damit ein
            // echter Spieler in der Szene den Test nicht stört.
            var player = new GameObject("TestPlayer");
            var hp = player.AddComponent<Health>();
            hp.Configure(100f, false);
            testHealth = hp;

            // ============ SZENARIO ============

            // 1) Start
            mgr.QuestStart("Q_TEST_coins");
            mgr.QuestStart("Q_TEST_flowers");
            Check("1 · beide Quests ACTIVE",
                mgr.GetQuest("Q_TEST_coins").State == QuestState.ACTIVE &&
                mgr.GetQuest("Q_TEST_flowers").State == QuestState.ACTIVE);

            // 2) Broadcast-Filter: 2 Münzen + 1 Blume
            GameplayEvents.ItemCollected("item_test_coin");
            GameplayEvents.ItemCollected("item_test_coin");
            GameplayEvents.ItemCollected("item_test_flower");
            Check("2 · Münz-Quest 2/5 (Blume zählt nicht)", Progress("Q_TEST_coins", "c_coins") == 2);
            Check("2 · Blumen-Quest 1/3", Progress("Q_TEST_flowers", "c_flowers") == 1);
            Check("2 · Inventar: 2 Münzen via Bridge", inv.Count("item_test_coin") == 2);
            Check("2 · Inventar: 1 Blume via Bridge", inv.Count("item_test_flower") == 1);

            // 3) Münz-Quest fertigsammeln (+3 Münzen) -> Rewards
            for (int i = 0; i < 3; i++) GameplayEvents.ItemCollected("item_test_coin");
            Check("3 · Münz-Quest COMPLETED", mgr.GetQuest("Q_TEST_coins").State == QuestState.COMPLETED);
            Check("3 · Reward: +50 Gold im Wallet", Wallet.Instance.Gold == 50);
            Check("3 · Reward: Trank im Inventar", inv.Count("potion_test") == 1);

            // 4) Nach Abschluss weitere Münze -> kein Overflow, Inventar zählt weiter
            GameplayEvents.ItemCollected("item_test_coin");
            Check("4 · Quest bleibt COMPLETED (kein Overflow)", mgr.GetQuest("Q_TEST_coins").State == QuestState.COMPLETED);
            Check("4 · Inventar jetzt 6 Münzen", inv.Count("item_test_coin") == 6);

            // 5) Blumen-Quest fertig
            GameplayEvents.ItemCollected("item_test_flower");
            GameplayEvents.ItemCollected("item_test_flower");
            Check("5 · Blumen-Quest COMPLETED", mgr.GetQuest("Q_TEST_flowers").State == QuestState.COMPLETED);
            Check("5 · XP-Reward +20 angekommen", PlayerProgression.Instance.Xp == 20 && PlayerProgression.Instance.Level == 1);

            // 6) Trank benutzen -> Spieler geheilt
            hp.TakeDamage(30f); // 100 -> 70
            bool used = inv.Use("potion_test");
            Check("6 · Trank benutzt (true)", used);
            Check("6 · Trank aus Inventar entfernt", inv.Count("potion_test") == 0);
            Check("6 · Spieler geheilt (70 -> 95)", Mathf.Abs(hp.CurrentHealth - 95f) < 0.1f);

            // 7) Dev-Kit: Gold geben, Item geben, Quest per Cheat abschliessen
            Wallet.Instance.Add(1000);
            Check("7 · Cheat +1000 Gold -> 1050", Wallet.Instance.Gold == 1050);
            inv.AddById("item_test_flower", 10);
            Check("7 · Cheat +10 Blumen (3+10) -> 13", inv.Count("item_test_flower") == 13);
            mgr.RegisterQuest(MakeQuest("Q_TEST_cheat", "c_x", "item_none", 7,
                new Reward { type = RewardType.Gold, amount = 5 }));
            ForceComplete("Q_TEST_cheat");
            Check("7 · Force-Complete -> COMPLETED", mgr.GetQuest("Q_TEST_cheat").State == QuestState.COMPLETED);
            Check("7 · Force-Complete Gold-Reward (1050 -> 1055)", Wallet.Instance.Gold == 1055);

            // 8) Quest aus JSON (ohne Datei, ohne Asset)
            QuestInstance jsonQuest = mgr.RegisterQuestFromJson(DailyJson, "SelfTest");
            Check("8 · JSON-Quest registriert (AVAILABLE)", jsonQuest != null && jsonQuest.State == QuestState.AVAILABLE);
            Check("8 · Kaputtes JSON wird abgelehnt", QuestJsonLoader.CreateFromJson("{ \"questID\": \"falsch\" }", out List<string> jsonErrors) == null && jsonErrors.Count > 0);

            // 9) Daily: abschliessen, neuer Tag -> wieder verfügbar
            ForceComplete("Q_DAILY_Test");
            Check("9 · Daily COMPLETED", mgr.GetQuest("Q_DAILY_Test").State == QuestState.COMPLETED);
            int dayBefore = GameClock.Instance.TotalDays;
            GameClock.Instance.AdvanceToNextDay();
            Check("9 · Neuer Tag (+1)", GameClock.Instance.TotalDays == dayBefore + 1 && GameClock.Instance.Hour == 6);
            Check("9 · Daily nach neuem Tag wieder AVAILABLE", mgr.GetQuest("Q_DAILY_Test").State == QuestState.AVAILABLE);
            Check("9 · Daily-Fortschritt zurückgesetzt", Progress("Q_DAILY_Test", "d_talk") == 0);
            Check("9 · Nicht-Daily bleibt COMPLETED", mgr.GetQuest("Q_TEST_coins").State == QuestState.COMPLETED);

            // 10) Schaden mit i-Frames
            hp.Configure(6f, false, 1f);
            hp.TakeDamage(1f);
            hp.TakeDamage(1f); // innerhalb der i-Frames -> ignoriert
            Check("10 · i-Frames: zweiter Treffer ignoriert (6 -> 5)", Mathf.Abs(hp.CurrentHealth - 5f) < 0.01f);

            // Ergebnis
            int pass = 0;
            foreach (var r in results) if (r.ok) pass++;
            bool all = pass == results.Count;
            Debug.Log($"[SelfTest] {pass}/{results.Count} Checks bestanden — {(all ? "ALLE GRÜN" : "FEHLSCHLÄGE!")}");

            allPass = all;
            if (allPass)
            {
                autoCloseAt = Time.unscaledTime + AutoCloseSeconds; // schliesst sich selbst
            }
        }

        private void Update()
        {
            if (visible && allPass && Time.unscaledTime >= autoCloseAt)
            {
                visible = false;
            }
        }

        private void OnDestroy()
        {
            InventoryEvents.OnItemUsed -= HandleItemUsed;
        }

        // ---------------- Helfer ----------------

        private void HandleItemUsed(ItemData item)
        {
            // Gezielt den Test-Spieler heilen – nicht irgendein 'Player' in der Szene.
            if (item.itemID == "potion_test" && testHealth != null)
            {
                testHealth.Heal(25f);
            }
        }

        private void Check(string name, bool ok)
        {
            results.Add((name, ok));
            Debug.Log($"[SelfTest] {(ok ? "PASS" : "FAIL")}  {name}");
        }

        private static int Progress(string questID, string objectiveID)
        {
            var q = QuestManager.Instance.GetQuest(questID);
            return q != null ? q.GetProgress(objectiveID) : -1;
        }

        private static void ForceComplete(string questID)
        {
            var mgr = QuestManager.Instance;
            var q = mgr.GetQuest(questID);
            if (q == null) return;
            if (q.State != QuestState.ACTIVE) mgr.QuestStart(questID);
            foreach (ObjectiveData o in q.Data.objectives)
            {
                mgr.QuestContinue(questID, o.objectiveID, o.requiredAmount);
            }
        }

        private static void EnsureSingleton<T>(string name) where T : Component
        {
            if (Object.FindAnyObjectByType<T>() == null)
            {
                new GameObject(name).AddComponent<T>();
            }
        }

        private static ItemData MakeItem(string id, int maxStack, bool usable)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = id;
            item.itemID = id;
            item.displayName = id;
            item.maxStack = maxStack;
            item.usable = usable;
            return item;
        }

        private static QuestData MakeQuest(string id, string objId, string target, int req, params Reward[] rewards)
        {
            var d = ScriptableObject.CreateInstance<QuestData>();
            d.name = id;
            d.questID = id;
            d.displayName = id;
            d.category = QuestCategory.SIDE;
            d.objectives = new[]
            {
                new ObjectiveData
                {
                    objectiveID = objId,
                    stepType = QuestStepType.Collect,
                    targetID = target,
                    requiredAmount = req,
                    description = objId
                }
            };
            d.rewards = rewards;
            return d;
        }

        // ---------------- Anzeige ----------------

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            int pass = 0;
            foreach (var r in results) if (r.ok) pass++;

            var box = new Rect(16, 16, 470, 54 + results.Count * 20);
            GUI.Box(box, GUIContent.none);

            // Manuell schliessen.
            if (GUI.Button(new Rect(box.xMax - 26, box.y + 6, 20, 20), "✕"))
            {
                visible = false;
                return;
            }

            GUILayout.BeginArea(new Rect(box.x + 12, box.y + 10, box.width - 42, box.height - 20));

            var head = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            GUILayout.Label($"<b>Integrations-Selbsttest</b>   {pass}/{results.Count}   " +
                            (allPass ? "<color=#6fbf73>ALLE GRÜN</color>" : "<color=#e8a24a>läuft/fehler</color>"), head);

            if (allPass)
            {
                int rest = Mathf.Max(0, Mathf.CeilToInt(autoCloseAt - Time.unscaledTime));
                var hint = new GUIStyle(GUI.skin.label) { fontSize = 11 };
                hint.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
                GUILayout.Label($"schliesst automatisch in {rest}s  ·  ✕ zum sofort schliessen", hint);
            }

            foreach (var r in results)
            {
                var s = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
                string mark = r.ok ? "<color=#6fbf73>PASS</color>" : "<color=#e05a5a>FAIL</color>";
                GUILayout.Label($"{mark}  {r.name}", s);
            }
            GUILayout.EndArea();
        }
    }
}
