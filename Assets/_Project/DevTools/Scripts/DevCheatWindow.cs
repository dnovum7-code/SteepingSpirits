using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Core;
using SteepingSpirits.Economy;
using SteepingSpirits.Inventory;
using SteepingSpirits.Inventory.Integration;
using SteepingSpirits.Player;
using SteepingSpirits.Progression;
using SteepingSpirits.Quests;
using SteepingSpirits.Quests.Integration;
using SteepingSpirits.World;

namespace SteepingSpirits.DevTools
{
    /// <summary>
    /// In-Game Dev-/Cheat-Fenster (OnGUI, verschiebbar, mit Tabs) – aus Everdawn
    /// übernommen, ohne KI-Teil, auf 2D angepasst. Spricht direkt mit den
    /// Live-Systemen: QuestManager, PlayerInventory, Wallet, Health, GameClock.
    /// Ein-/Ausblenden mit F1 (oder ^ / Backquote).
    ///
    /// Einfach ein leeres GameObject anlegen und diese Komponente drauf.
    /// Für Release-Builds das Objekt entfernen oder die Komponente deaktivieren.
    /// </summary>
    public class DevCheatWindow : MonoBehaviour
    {
        private enum Tab { Quests, Inventar, Spieler, Welt, System }

        [SerializeField] private bool visibleOnStart;

        private bool open;
        private Tab tab = Tab.Quests;
        private Rect window = new Rect(24, 120, 440, 560);
        private Vector2 scroll;
        private float fps;
        private bool invincible;
        private bool oneHit;
        private Health cachedPlayerHealth;

        private const int WindowId = 731601;

        private void Start()
        {
            open = visibleOnStart;
        }

        private void Update()
        {
            // FPS (unskaliert, damit Zeitlupe/Pause den Wert nicht verfälscht).
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f), 0.1f);

            if (GameInput.CheatTogglePressed)
            {
                open = !open;
            }

            if (invincible)
            {
                Health h = PlayerHealth();
                if (h != null && !h.IsDead && h.CurrentHealth < h.MaxHealth)
                {
                    h.Heal(h.MaxHealth);
                }
            }
        }

        private void OnGUI()
        {
            var fpsStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 12 };
            fpsStyle.normal.textColor = new Color(1, 1, 1, .7f);
            GUI.Label(new Rect(Screen.width - 230f, Screen.height - 26f, 220f, 20f),
                $"{fps:0} FPS   ·   Cheats: {(open ? "F1 schliesst" : "F1")}", fpsStyle);

            if (!open)
            {
                return;
            }

            window = GUI.Window(WindowId, window, DrawWindow, "Steeping Spirits · Dev-Cheats");
            GameInput.ClaimGuiArea(window); // Klicks im Fenster lösen keinen Schwertschlag aus
        }

        private void DrawWindow(int id)
        {
            tab = (Tab)GUILayout.Toolbar((int)tab, new[] { "Quests", "Inventar", "Spieler", "Welt", "System" });

            GUILayout.Space(6);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(window.height - 92f));

            switch (tab)
            {
                case Tab.Quests: DrawQuests(); break;
                case Tab.Inventar: DrawInventory(); break;
                case Tab.Spieler: DrawPlayer(); break;
                case Tab.Welt: DrawWorld(); break;
                case Tab.System: DrawSystem(); break;
            }

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        // ---------------------------------------------------------------
        // QUESTS
        // ---------------------------------------------------------------
        private void DrawQuests()
        {
            QuestManager qm = QuestManager.Instance;
            if (qm == null)
            {
                Info("Kein QuestManager in der Szene.");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Alle zurücksetzen (verfügbar)"))
            {
                foreach (QuestInstance rq in new List<QuestInstance>(qm.GetAllQuests()))
                {
                    qm.ResetQuest(rq.Data.questID);
                }
            }
            if (GUILayout.Button("Alle Quests weg", GUILayout.Width(140f)))
            {
                foreach (QuestInstance rq in new List<QuestInstance>(qm.GetAllQuests()))
                {
                    qm.RemoveQuest(rq.Data.questID);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            var quests = new List<QuestInstance>(qm.GetAllQuests());
            if (quests.Count == 0)
            {
                Info("Keine Quests registriert.");
                return;
            }

            // Kurze Legende, damit die Farben sofort verständlich sind.
            GUILayout.Label(
                $"Status:  {StateLabel(QuestState.AVAILABLE)}  {StateLabel(QuestState.ACTIVE)}  " +
                $"{StateLabel(QuestState.COMPLETED)}  {StateLabel(QuestState.FAILED)}",
                Rich(11));
            GUILayout.Space(2);

            foreach (QuestInstance q in quests)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.Label($"<b>{q.Data.displayName}</b>   {StateLabel(q.State)}   <color=#9aa0ac>{q.Data.category}</color>", Rich());

                // Fortschritt IMMER anzeigen (nicht nur bei aktiven Quests).
                if (q.Data.objectives != null)
                {
                    bool active = q.State == QuestState.ACTIVE;
                    foreach (ObjectiveData obj in q.Data.objectives)
                    {
                        int cur = q.GetProgress(obj.objectiveID);
                        bool done = cur >= obj.requiredAmount;
                        string tick = done ? "<color=#7fe07f>✔</color>" : "<color=#9aa0ac>•</color>";

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"  {tick} {obj.description}: {cur}/{obj.requiredAmount}", Rich(12));
                        GUILayout.FlexibleSpace();
                        // +1 nur wenn die Quest aktiv ist (sonst tut QuestContinue nichts).
                        if (active && GUILayout.Button("+1", GUILayout.Width(40f)))
                        {
                            qm.QuestContinue(q.Data.questID, obj.objectiveID, 1);
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(2);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Start")) qm.QuestStart(q.Data.questID);
                if (GUILayout.Button("Fertig")) ForceComplete(qm, q);
                if (GUILayout.Button("Fehlschlag")) qm.QuestFail(q.Data.questID);
                if (GUILayout.Button("Abbruch")) qm.AbandonQuest(q.Data.questID);
                if (GUILayout.Button("Reset")) qm.ResetQuest(q.Data.questID);
                if (GUILayout.Button("✕")) qm.RemoveQuest(q.Data.questID);
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(4);
            }
        }

        /// <summary>Farbiges [Zustand]-Etikett (grün = fertig, gold = aktiv, ...).</summary>
        private static string StateLabel(QuestState state)
        {
            string hex;
            switch (state)
            {
                case QuestState.ACTIVE: hex = "#ffcf40"; break;      // gold
                case QuestState.COMPLETED: hex = "#7fe07f"; break;   // grün
                case QuestState.AVAILABLE: hex = "#7fc8ff"; break;   // blau
                case QuestState.FAILED: hex = "#ff6a5a"; break;      // rot
                default: hex = "#9aa0ac"; break;                     // grau
            }

            return $"<color={hex}><b>[{state}]</b></color>";
        }

        private static void ForceComplete(QuestManager qm, QuestInstance q)
        {
            if (q.State != QuestState.ACTIVE)
            {
                qm.QuestStart(q.Data.questID);
            }

            if (q.Data.objectives == null)
            {
                return;
            }

            // Alle Ziele auffüllen → der Manager schliesst die Quest automatisch ab.
            foreach (ObjectiveData obj in q.Data.objectives)
            {
                qm.QuestContinue(q.Data.questID, obj.objectiveID, obj.requiredAmount);
            }
        }

        // ---------------------------------------------------------------
        // INVENTAR + GOLD
        // ---------------------------------------------------------------
        private void DrawInventory()
        {
            Wallet wallet = EnsureWallet();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"<b>Gold:</b> {wallet.Gold}", Rich());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10")) wallet.Add(10);
            if (GUILayout.Button("+100")) wallet.Add(100);
            if (GUILayout.Button("+1000")) wallet.Add(1000);
            if (GUILayout.Button("Auf 0")) wallet.Set(0);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            PlayerInventory inv = PlayerInventory.Instance;
            if (inv == null)
            {
                Info("Kein PlayerInventory in der Szene.");
                return;
            }

            var catalog = new List<ItemData>(inv.CatalogItems);
            GUILayout.Space(4);
            GUILayout.Label("<b>Item geben</b>", Rich());
            if (catalog.Count == 0)
            {
                Info("Katalog leer – Items am PlayerInventory eintragen oder TestMeadow starten.");
            }
            GUILayout.Label("<size=11><i>Zählt wie echtes Aufsammeln – auch für Collect-Quests.</i></size>", Rich(11));
            foreach (ItemData item in catalog)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.displayName, Rich(12));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+1", GUILayout.Width(40f))) GiveItem(inv, item, 1);
                if (GUILayout.Button("+10", GUILayout.Width(45f))) GiveItem(inv, item, 10);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label($"<b>Inhalt</b>  ({inv.Slots.Count}/{inv.Capacity})", Rich());
            var slots = new List<ItemStack>(inv.Slots);
            string removeId = null;
            string useId = null;
            foreach (ItemStack s in slots)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{s.Item.displayName} x{s.Amount}", Rich(12));
                GUILayout.FlexibleSpace();
                if (s.Item.usable && GUILayout.Button("Benutzen", GUILayout.Width(80f))) useId = s.Item.itemID;
                if (GUILayout.Button("−1", GUILayout.Width(40f))) removeId = s.Item.itemID;
                GUILayout.EndHorizontal();
            }
            if (useId != null) inv.Use(useId);
            if (removeId != null) inv.Remove(removeId, 1);

            GUILayout.Space(4);
            if (GUILayout.Button("Alle Katalog-Items geben"))
            {
                foreach (ItemData item in catalog) GiveItem(inv, item, 1);
            }
        }

        /// <summary>
        /// Gibt ein Item so, als hätte man es aufgesammelt: meldet
        /// GameplayEvents.ItemCollected (damit Collect-Quests mitzählen). Die
        /// InventoryPickupBridge legt es dabei ins Inventar. Ist keine Bridge in
        /// der Szene, fügen wir es selbst hinzu – so bleibt beides konsistent.
        /// </summary>
        private static void GiveItem(PlayerInventory inv, ItemData item, int count)
        {
            if (item == null || count <= 0)
            {
                return;
            }

            Transform player = PlayerLocator.Find();
            GameObject source = player != null ? player.gameObject : null;
            for (int i = 0; i < count; i++)
            {
                GameplayEvents.ItemCollected(item.itemID, source);
            }

            if (!InventoryPickupBridge.IsActive && inv != null)
            {
                inv.Add(item, count);
            }
        }

        // ---------------------------------------------------------------
        // SPIELER
        // ---------------------------------------------------------------
        private void DrawPlayer()
        {
            Health h = PlayerHealth();
            if (h == null)
            {
                Info("Kein Spieler mit Tag 'Player' + Health gefunden.");
            }
            else
            {
                GUILayout.Label($"<b>Leben:</b> {h.CurrentHealth:0.#} / {h.MaxHealth:0.#}  ({h.MaxHealth / 2f:0.#} Herzen)", Rich());
                Rect bar = GUILayoutUtility.GetRect(100, 14);
                GUI.Box(bar, GUIContent.none);
                float frac = h.MaxHealth > 0 ? Mathf.Clamp01(h.CurrentHealth / h.MaxHealth) : 0f;
                GUI.color = Color.Lerp(new Color(.8f, .2f, .2f), new Color(.3f, .8f, .3f), frac);
                GUI.Box(new Rect(bar.x, bar.y, bar.width * frac, bar.height), GUIContent.none);
                GUI.color = Color.white;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Voll heilen")) h.Heal(h.MaxHealth);
                if (GUILayout.Button("−½ Herz")) h.TakeDamage(1f);
                if (GUILayout.Button("Töten")) h.TakeDamage(999999f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+1 Herzcontainer")) h.SetMaxHealth(h.MaxHealth + 2f, true);
                if (GUILayout.Button("−1 Herzcontainer") && h.MaxHealth > 2f) h.SetMaxHealth(h.MaxHealth - 2f, false);
                GUILayout.EndHorizontal();

                invincible = GUILayout.Toggle(invincible, " Unsterblich (heilt jeden Frame)");
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Fortschritt (XP)</b>", Rich());
            PlayerProgression prog = EnsureProgression();
            GUILayout.Label($"Level {prog.Level}   ·   XP {prog.Xp} / {prog.XpToNext}", Rich(12));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+50 XP")) prog.AddXp(50);
            if (GUILayout.Button("+500 XP")) prog.AddXp(500);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>Bewegung & Kampf</b>", Rich());
            Transform player = PlayerLocator.Find();
            PlayerController2D controller = player != null ? player.GetComponent<PlayerController2D>() : null;
            if (controller != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Tempo ({controller.SpeedMultiplier:0.#}x):", Rich(12), GUILayout.Width(100f));
                if (GUILayout.Button("1x")) controller.SpeedMultiplier = 1f;
                if (GUILayout.Button("2x")) controller.SpeedMultiplier = 2f;
                if (GUILayout.Button("4x")) controller.SpeedMultiplier = 4f;
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Zum Startpunkt (0,0)"))
                {
                    controller.Teleport(Vector2.zero);
                }
            }
            else
            {
                Info("Kein PlayerController2D am Spieler.");
            }

            PlayerAttack2D attack = player != null ? player.GetComponent<PlayerAttack2D>() : null;
            if (attack != null)
            {
                bool newOneHit = GUILayout.Toggle(oneHit, " One-Hit (Schwert macht 99x Schaden)");
                if (newOneHit != oneHit)
                {
                    oneHit = newOneHit;
                    attack.DamageMultiplier = oneHit ? 99f : 1f;
                }
            }
        }

        // ---------------------------------------------------------------
        // WELT
        // ---------------------------------------------------------------
        private void DrawWorld()
        {
            GUILayout.Label($"<b>Zeit-Tempo (Engine):</b> {Time.timeScale:0.00}x", Rich());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause")) Time.timeScale = 0f;
            if (GUILayout.Button("0.25x")) Time.timeScale = 0.25f;
            if (GUILayout.Button("0.5x")) Time.timeScale = 0.5f;
            if (GUILayout.Button("1x")) Time.timeScale = 1f;
            if (GUILayout.Button("2x")) Time.timeScale = 2f;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GameClock clock = GameClock.Instance;
            if (clock == null)
            {
                Info("Keine GameClock in der Szene.");
            }
            else
            {
                GUILayout.Label($"<b>Spielzeit:</b> {clock.DateLabel}, Jahr {clock.Year}  ·  {clock.TimeLabel}", Rich());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("06:00")) clock.SetTime(6);
                if (GUILayout.Button("12:00")) clock.SetTime(12);
                if (GUILayout.Button("18:00")) clock.SetTime(18);
                if (GUILayout.Button("21:00")) clock.SetTime(21);
                if (GUILayout.Button("01:00")) clock.SetTime(25);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Uhr-Tempo ({clock.SpeedMultiplier:0}x):", Rich(12), GUILayout.Width(120f));
                if (GUILayout.Button("Stopp")) clock.SpeedMultiplier = 0f;
                if (GUILayout.Button("1x")) clock.SpeedMultiplier = 1f;
                if (GUILayout.Button("10x")) clock.SpeedMultiplier = 10f;
                if (GUILayout.Button("60x")) clock.SpeedMultiplier = 60f;
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Nächster Tag (schlafen)"))
                {
                    clock.AdvanceToNextDay();
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Gegner</b>", Rich());
            if (GUILayout.Button("Alle Gegner besiegen"))
            {
                KillAllEnemies();
            }
        }

        // ---------------------------------------------------------------
        // SYSTEM
        // ---------------------------------------------------------------
        private void DrawSystem()
        {
            GUILayout.Label($"<b>FPS:</b> {fps:0}", Rich());

            Transform player = PlayerLocator.Find();
            if (player != null)
            {
                GUILayout.Label($"Position: {player.position.x:0.0}, {player.position.y:0.0}", Rich(12));
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Sound</b>", Rich());
            if (QuestAudioHooks.HasInstance)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Quest-SFX: {QuestAudioHooks.MasterVolume * 100f:0}%", Rich(12), GUILayout.Width(150f));
                QuestAudioHooks.MasterVolume = GUILayout.HorizontalSlider(QuestAudioHooks.MasterVolume, 0f, 1f);
                GUILayout.EndHorizontal();
            }
            else
            {
                Info("Kein QuestAudioHooks in der Szene.");
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Speichern / Laden</b> (Quests + Inventar)", Rich());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Alles speichern"))
            {
                QuestManager.Instance?.SaveToDisk();
                PlayerInventory.Instance?.SaveToDisk();
            }
            if (GUILayout.Button("Alles laden"))
            {
                QuestManager.Instance?.LoadFromDisk();
                PlayerInventory.Instance?.LoadFromDisk();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("Fenster schliessen (F1)")) open = false;
        }

        // ---------------------------------------------------------------
        // Helfer
        // ---------------------------------------------------------------
        private static Wallet EnsureWallet()
        {
            if (Wallet.Instance == null)
            {
                new GameObject("Wallet").AddComponent<Wallet>();
            }

            return Wallet.Instance;
        }

        private static PlayerProgression EnsureProgression()
        {
            if (PlayerProgression.Instance == null)
            {
                new GameObject("PlayerProgression").AddComponent<PlayerProgression>();
            }

            return PlayerProgression.Instance;
        }

        private Health PlayerHealth()
        {
            if (cachedPlayerHealth == null)
            {
                Transform p = PlayerLocator.Find();
                if (p != null)
                {
                    cachedPlayerHealth = p.GetComponent<Health>();
                }
            }

            return cachedPlayerHealth;
        }

        private static void KillAllEnemies()
        {
            foreach (EnemyAI2D enemy in Object.FindObjectsByType<EnemyAI2D>())
            {
                var h = enemy.GetComponent<Health>();
                if (h != null)
                {
                    h.TakeDamage(999999f);
                }
            }
        }

        private static GUIStyle Rich(int size = 13)
        {
            return new GUIStyle(GUI.skin.label) { richText = true, fontSize = size, wordWrap = true };
        }

        private static void Info(string text)
        {
            GUILayout.Label(text, new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 });
        }
    }
}
