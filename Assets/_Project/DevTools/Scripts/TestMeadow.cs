using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SteepingSpirits.Combat;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Dialogue;
using SteepingSpirits.Economy;
using SteepingSpirits.Economy.Integration;
using SteepingSpirits.HUD;
using SteepingSpirits.Interaction;
using SteepingSpirits.Inventory;
using SteepingSpirits.Inventory.Integration;
using SteepingSpirits.Inventory.UI;
using SteepingSpirits.Player;
using SteepingSpirits.Progression;
using SteepingSpirits.Progression.Integration;
using SteepingSpirits.Quests;
using SteepingSpirits.Quests.Integration;
using SteepingSpirits.Quests.Steps;
using SteepingSpirits.Quests.UI;
using SteepingSpirits.World;

namespace SteepingSpirits.DevTools
{
    /// <summary>
    /// Baut beim Play eine komplette, spielbare 2D-Testwiese (2D-Gegenstück zur
    /// QuestTestArena aus Everdawn):
    ///  - Wiese mit Weg, Teich, Bäumen, Zaun und Haus mit Bett,
    ///  - Spieler (Top-Down-Steuerung, Schwert, Herzen) + folgende Kamera,
    ///  - 6 NPCs, je eine Quest-Art: Sammeln, Reden, Bringen, Besuchen,
    ///    Besiegen + eine Daily-Quest aus JSON (jeden Morgen neu),
    ///  - alle Systeme: Quests, Inventar, Gold, XP, Uhr/Tag-Nacht, HUDs, Cheats.
    /// Alles aus Platzhalter-Grafiken – kein einziger Import nötig.
    ///
    /// Benutzung: LEERE Szene → leeres GameObject → diese Komponente → Play.
    /// Hilfe mit Taste N. Liegt schon ein Spieler (Tag „Player") in der Szene,
    /// wird er benutzt und nur um fehlende Komponenten ergänzt.
    /// </summary>
    public class TestMeadow : MonoBehaviour
    {
        [SerializeField] private Vector2 worldSize = new Vector2(40f, 28f);
        [SerializeField] private int slimeCount = 3;
        [SerializeField] private float slimeRespawnSeconds = 8f;

        [Tooltip("Pfad der Jump'n'Run-Szene (Portal links auf der Wiese)")]
        [SerializeField] private string jumpAndRunScenePath = "Assets/Scenes/JumpAndRun.unity";

        private string JumpAndRunScenePath => jumpAndRunScenePath;

        // Daily-Quest im JSON-Format – zeigt den JSON-Weg ohne Datei.
        private const string DailyQuestJson = @"{
  ""questID"": ""Q_DAILY_Schleimjagd"",
  ""displayName"": ""Tägliche Schleimjagd"",
  ""description"": ""Das Anschlagbrett sucht jeden Tag Freiwillige: 2 Schleime vertreiben."",
  ""category"": ""DAILY"",
  ""isRepeatable"": true,
  ""objectives"": [
    { ""objectiveID"": ""daily_slimes"", ""stepType"": ""Defeat"", ""description"": ""Schleime besiegen"", ""targetID"": ""enemy_slime"", ""requiredAmount"": 2 }
  ],
  ""rewards"": [
    { ""type"": ""Gold"", ""amount"": 15 },
    { ""type"": ""XP"", ""amount"": 10 }
  ]
}";

        private static readonly Color Grass = new Color(0.42f, 0.66f, 0.33f);
        private static readonly Color GrassDark = new Color(0.36f, 0.58f, 0.29f);
        private static readonly Color Path = new Color(0.78f, 0.66f, 0.45f);
        private static readonly Color Water = new Color(0.32f, 0.58f, 0.86f);
        private static readonly Color Wood = new Color(0.50f, 0.33f, 0.18f);

        private bool showHelp = true;
        private readonly GuiDraggablePanel helpPanel = new GuiDraggablePanel();
        private readonly List<string> help = new List<string>();
        private readonly List<(Transform t, string label)> labels = new List<(Transform, string)>();
        private readonly List<GameObject> slimes = new List<GameObject>();

        private Transform root;
        private Transform facingDot;
        private PlayerController2D playerController;
        private Vector2 slimeHome;

        private void Start()
        {
            root = new GameObject("Wiese").transform;
            root.SetParent(transform, false);

            EnsureSystems();
            RegisterItems();
            BuildWorld();
            Transform player = SetupPlayer(Vector2.zero);
            SetupCamera(player);
            EnsureGlobalLight2D();
            BuildQuests();
        }

        // ---------------- Systeme ----------------

        private void EnsureSystems()
        {
            EnsureSingleton<QuestManager>("QuestManager");
            EnsureSingleton<PlayerInventory>("PlayerInventory");
            EnsureSingleton<Wallet>("Wallet");
            EnsureSingleton<PlayerProgression>("PlayerProgression");
            EnsureSingleton<GameClock>("GameClock");

            AddOnce<InventoryPickupBridge>();
            AddOnce<QuestRewardCollector>();
            AddOnce<GoldRewardCollector>();
            AddOnce<XpRewardCollector>();
            AddOnce<CollectQuestInventoryTracker>();
            AddOnce<ItemUseEffects>();
            AddOnce<DailyQuestReset>();

            AddOnce<DayNightTint>();
            AddOnce<GameHUD>();
            AddOnce<InventoryHUD>();
            AddOnce<QuestTrackerHUD>();
            QuestToastHUD toast = AddOnce<QuestToastHUD>();
            toast.ShowTracker = false; // der Tracker links (QuestTrackerHUD) reicht
            AddOnce<QuestJournalCanvas>();
            AddOnce<QuestMarkerHUD>();
            AddOnce<QuestAudioHooks>();
            AddOnce<DevCheatWindow>();
        }

        private void RegisterItems()
        {
            PlayerInventory inv = PlayerInventory.Instance;
            inv.RegisterItem(MakeItem("item_teeblatt", "Teeblatt", ItemCategory.Material, 99, 2, new Color(0.45f, 0.85f, 0.35f)));
            inv.RegisterItem(MakeItem("item_coin", "Münze", ItemCategory.Currency, 999, 1, new Color(1f, 0.84f, 0.2f)));
            ItemData potion = MakeItem("potion_small", "Kleiner Heiltrank", ItemCategory.Consumable, 20, 15, new Color(0.95f, 0.35f, 0.45f));
            potion.usable = true;
            potion.healAmount = 4f; // 2 Herzen
            inv.RegisterItem(potion);
        }

        // ---------------- Welt ----------------

        private void BuildWorld()
        {
            float w = worldSize.x, h = worldSize.y;

            Sprite(PlaceholderSprites.Square, Grass, Vector2.zero, worldSize, -100, "Boden");

            // Ein paar dunklere Grasflecken, damit man Bewegung sieht.
            var rng = new System.Random(7);
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector2((float)(rng.NextDouble() - 0.5) * (w - 2f), (float)(rng.NextDouble() - 0.5) * (h - 2f));
                float s = 0.6f + (float)rng.NextDouble() * 1.4f;
                Sprite(PlaceholderSprites.Circle, GrassDark, p, new Vector2(s * 1.6f, s), -99, "Grasfleck");
            }

            // Wege (Kreuz durch die Mitte).
            Sprite(PlaceholderSprites.Square, Path, new Vector2(0f, 0f), new Vector2(w - 4f, 1.4f), -98, "Weg_OW");
            Sprite(PlaceholderSprites.Square, Path, new Vector2(0f, 0f), new Vector2(1.4f, h - 4f), -98, "Weg_NS");

            // Teich (nicht begehbar).
            SpriteRenderer pond = Sprite(PlaceholderSprites.Circle, Water, new Vector2(9f, 6.5f), new Vector2(5f, 3.2f), -97, "Teich");
            var pondCol = pond.gameObject.AddComponent<CapsuleCollider2D>(); // ellipsenähnlich
            pondCol.direction = CapsuleDirection2D.Horizontal;
            pondCol.size = new Vector2(0.95f, 0.9f);

            // Zaun rundherum (Wände).
            Wall(new Vector2(0f, h / 2f), new Vector2(w, 0.6f));
            Wall(new Vector2(0f, -h / 2f), new Vector2(w, 0.6f));
            Wall(new Vector2(-w / 2f, 0f), new Vector2(0.6f, h));
            Wall(new Vector2(w / 2f, 0f), new Vector2(0.6f, h));

            // Bäume (Stamm kollidiert).
            Vector2[] trees =
            {
                new Vector2(-16f, 10f), new Vector2(-12f, 11f), new Vector2(-17f, 4f), new Vector2(16f, 11f),
                new Vector2(17f, 3f), new Vector2(-16f, -11f), new Vector2(5f, 11f), new Vector2(-4f, -11f),
                new Vector2(16f, -11f), new Vector2(3f, -9f), new Vector2(-11.5f, 7.5f), new Vector2(12f, 1.5f)
            };
            foreach (Vector2 t in trees)
            {
                Tree(t);
            }

            // Haus mit Bett (Schlafen = nächster Tag). SortingGroup an der
            // Hauswand-Unterkante → läuft man hinterm Haus, verdeckt es einen.
            var house = new GameObject("Haus");
            house.transform.SetParent(root, false);
            house.transform.position = new Vector2(-7f, 5.5f);
            house.AddComponent<SortingGroup>();
            var houseCol = house.AddComponent<BoxCollider2D>();
            houseCol.offset = new Vector2(0f, 1.5f);
            houseCol.size = new Vector2(5f, 3f);
            PlaceholderSprites.CreateSpriteObject("Wand", PlaceholderSprites.Square, new Color(0.72f, 0.55f, 0.38f),
                new Vector2(-7f, 7f), new Vector2(5f, 3f), 0, house.transform);
            PlaceholderSprites.CreateSpriteObject("Dach", PlaceholderSprites.Triangle, new Color(0.62f, 0.25f, 0.2f),
                new Vector2(-7f, 9.6f), new Vector2(6f, 2.4f), 1, house.transform);
            PlaceholderSprites.CreateSpriteObject("Tür", PlaceholderSprites.Square, Wood,
                new Vector2(-7f, 6.1f), new Vector2(0.9f, 1.2f), 2, house.transform);
            SpriteRenderer bed = Sprite(PlaceholderSprites.Square, new Color(0.85f, 0.85f, 0.95f), new Vector2(-5.2f, 4.9f), new Vector2(1.1f, 0.8f), 0, "Bett");
            bed.gameObject.AddComponent<BoxCollider2D>();
            bed.gameObject.AddComponent<Bed>();
            labels.Add((bed.transform, "Bett"));

            // Willkommens-Schild.
            SpriteRenderer sign = Sprite(PlaceholderSprites.Square, Wood, new Vector2(1.6f, 1.6f), new Vector2(0.8f, 0.6f), 0, "Schild");
            sign.gameObject.AddComponent<BoxCollider2D>();
            sign.gameObject.AddComponent<InteractableDialogue>().Configure("Schild", new[]
            {
                "Willkommen auf der Testwiese von Steeping Spirits!",
                "Laufen: WASD · Schwert: Leertaste/J/Maus · Reden: E · Inventar: I · Questlog: B",
                "Leute mit einem gelben „!“ haben Arbeit für dich. Viel Spass!"
            });
            labels.Add((sign.transform, "Schild"));
        }

        private SpriteRenderer Sprite(Sprite sprite, Color color, Vector2 pos, Vector2 size, int order, string name)
        {
            return PlaceholderSprites.CreateSpriteObject(name, sprite, color, pos, size, order, root);
        }

        private void Wall(Vector2 pos, Vector2 size)
        {
            SpriteRenderer wall = Sprite(PlaceholderSprites.Square, Wood, pos, size, -90, "Zaun");
            wall.gameObject.AddComponent<BoxCollider2D>();
        }

        private void Tree(Vector2 pos)
        {
            // Wurzel-Objekt mit SortingGroup: Stamm + Krone sortieren gemeinsam
            // nach der Fussposition (Y-Sortierung) – man kann hinter Bäumen laufen.
            var tree = new GameObject("Baum");
            tree.transform.SetParent(root, false);
            tree.transform.position = pos;
            tree.AddComponent<SortingGroup>();
            tree.AddComponent<CircleCollider2D>().radius = 0.25f;

            PlaceholderSprites.CreateSpriteObject("Stamm", PlaceholderSprites.Square, new Color(0.45f, 0.3f, 0.16f),
                pos + new Vector2(0f, 0.3f), new Vector2(0.45f, 0.7f), 0, tree.transform);
            SpriteRenderer crown = PlaceholderSprites.CreateSpriteObject("Krone", PlaceholderSprites.Circle,
                new Color(0.2f, 0.5f, 0.22f), pos + new Vector2(0f, 1.2f), new Vector2(1.8f, 1.8f), 1, tree.transform);
            crown.transform.position = new Vector3(pos.x, pos.y + 1.2f, 0f);
        }

        // ---------------- Spieler & Kamera ----------------

        private Transform SetupPlayer(Vector2 spawn)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = new GameObject("Player") { tag = "Player" };
                player.transform.position = spawn;
                var sr = player.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderSprites.Circle;
                sr.color = new Color(0.98f, 0.86f, 0.55f);
                player.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            }

            // Figur + Kinder (Blickrichtungs-Punkt) sortieren gemeinsam nach Y.
            GetOrAdd<SortingGroup>(player);

            Rigidbody2D body = GetOrAdd<Rigidbody2D>(player);
            body.bodyType = RigidbodyType2D.Dynamic;
            if (player.GetComponent<Collider2D>() == null)
            {
                player.AddComponent<CircleCollider2D>().radius = 0.42f;
            }

            if (player.GetComponent<Health>() == null)
            {
                player.AddComponent<Health>().Configure(6f, false, 1f); // 3 Herzen, 1s i-Frames
            }

            GetOrAdd<Knockback2D>(player);
            GetOrAdd<DamageFlash2D>(player);
            playerController = GetOrAdd<PlayerController2D>(player);
            GetOrAdd<PlayerAttack2D>(player);
            GetOrAdd<Interactor2D>(player);
            GetOrAdd<PlayerRespawn2D>(player);

            // Kleiner Punkt zeigt die Blickrichtung (der Platzhalter-Kreis hat keine).
            facingDot = PlaceholderSprites.CreateSpriteObject("Blickrichtung", PlaceholderSprites.Diamond,
                new Color(0.35f, 0.22f, 0.12f), player.transform.position, new Vector2(0.3f, 0.3f), 1, player.transform).transform;

            return player.transform;
        }

        private void SetupCamera(Transform player)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }

            // Genau EIN AudioListener (QuestAudioHooks legt evtl. schon einen an).
            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            cam.transform.position = new Vector3(player.position.x, player.position.y, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.1f);

            CameraFollow2D follow = GetOrAdd<CameraFollow2D>(cam.gameObject);
            follow.Target = player;
            follow.SetBounds(new Rect(-worldSize.x / 2f, -worldSize.y / 2f, worldSize.x, worldSize.y));
            follow.SnapToTarget();
        }

        /// <summary>
        /// Mit dem URP-2D-Renderer sind Sprites ohne Licht dunkel. Gibt es das
        /// Light2D (URP) im Projekt, aber keins in der Szene, legen wir ein
        /// globales an. Per Reflection, damit das Projekt auch ohne URP kompiliert.
        /// </summary>
        private void EnsureGlobalLight2D()
        {
            System.Type lightType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (lightType == null || Object.FindAnyObjectByType(lightType) != null)
            {
                return;
            }

            try
            {
                var go = new GameObject("Global Light 2D");
                go.transform.SetParent(transform, false);
                Component light = go.AddComponent(lightType);
                var prop = lightType.GetProperty("lightType");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(light, System.Enum.Parse(prop.PropertyType, "Global"));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TestMeadow] Konnte kein Global Light 2D anlegen: " + e.Message);
            }
        }

        // ---------------- Quests ----------------

        private void BuildQuests()
        {
            QuestManager qm = QuestManager.Instance;

            // 1) SAMMELN ------------------------------------------------------
            QuestData collect = MakeQuest("Q_MAIN_Teeblaetter", "Teeblätter für Oma Hilde", QuestCategory.MAIN,
                "Oma Hilde braucht frische Teeblätter für ihren Geister-Tee.",
                Objective("collect_tea", QuestStepType.Collect, "Teeblätter sammeln", "item_teeblatt", 5),
                Xp(30), Gold(20));
            collect.objectives[0].countExistingInventory = true; // schon gesammelte zählen mit
            Register(qm, collect);
            Npc("Oma Hilde", new Vector2(-9f, -1.5f), new Color(0.85f, 0.6f, 0.9f), "npc_hilde", collect.questID,
                new[] { "Ach, Kindchen, gut dass du da bist!", "Bring mir doch 5 Teeblätter von der Wiese im Südwesten." },
                new[] { "Ein guter Tee braucht Geduld … und gute Blätter." });
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f;
                Pickup("item_teeblatt", new Vector2(-12f, -5.5f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (1.5f + i * 0.35f),
                    new Color(0.45f, 0.85f, 0.35f));
            }
            help.Add("SAMMELN – Oma Hilde (links): 5 Teeblätter im Südwesten aufsammeln.");

            // 2) REDEN --------------------------------------------------------
            QuestData talk = MakeQuest("Q_SIDE_Botengang", "Botengang", QuestCategory.SIDE,
                "Finn hat eine Nachricht für die Wächterin Ida.",
                Objective("talk_ida", QuestStepType.Talk, "Mit Wächterin Ida reden", "npc_ida", 1),
                Xp(20), Gold(10));
            Register(qm, talk);
            Npc("Finn", new Vector2(2.5f, 5f), new Color(0.45f, 0.75f, 1f), "npc_finn", talk.questID,
                new[] { "Hey! Kannst du Ida im Nordosten Bescheid geben?", "Sag ihr, der Tee ist fertig." });
            Npc("Wächterin Ida", new Vector2(14f, 9f), new Color(0.75f, 0.75f, 0.4f), "npc_ida", null,
                new[] { "Der Tee ist fertig? Endlich! Danke für die Nachricht." },
                new[] { "Alles ruhig an der Grenze. Zieh weiter." });
            help.Add("REDEN – Finn (oben): Nachricht an Wächterin Ida (Nordosten).");

            // 3) BRINGEN ------------------------------------------------------
            QuestData bring = MakeQuest("Q_WORLD_Paketdienst", "Paketdienst", QuestCategory.WORLD,
                "Postbote Bruno hat ein Paket für Mira.",
                Objective("bring_mira", QuestStepType.Bring, "Paket zu Mira bringen", "npc_mira", 1),
                Xp(25), Gold(15), Item("potion_small", 1));
            Register(qm, bring);
            Npc("Postbote Bruno", new Vector2(4.5f, -2.5f), new Color(1f, 0.6f, 0.3f), "npc_bruno", bring.questID,
                new[] { "Puh, so viele Pakete heute …", "Bringst du das hier zu Mira? Sie wohnt ganz im Osten." });
            Npc("Mira", new Vector2(17f, -3f), new Color(1f, 0.5f, 0.7f), "npc_mira", null,
                new[] { "Oh, ein Paket für mich? Das sind meine neuen Teetassen! Danke!" },
                new[] { "Hallo! Ich warte noch auf eine Lieferung." });
            help.Add("BRINGEN – Postbote Bruno: Paket zu Mira (ganz im Osten) bringen.");

            // 4) BESUCHEN -----------------------------------------------------
            QuestData visit = MakeQuest("Q_SIDE_AlterTurm", "Der alte Turm", QuestCategory.SIDE,
                "Späherin Lia will wissen, ob am alten Turm Geister spuken.",
                Objective("visit_tower", QuestStepType.Visit, "Zum alten Turm gehen", "loc_alter_turm", 1),
                Xp(20));
            Register(qm, visit);
            Npc("Späherin Lia", new Vector2(-2.5f, -5.5f), new Color(0.6f, 0.85f, 0.9f), "npc_lia", visit.questID,
                new[] { "Am alten Turm im Süden soll es nachts leuchten …", "Gehst du mal nachsehen?" });
            Tower(new Vector2(-14f, -10f), "loc_alter_turm");
            help.Add("BESUCHEN – Späherin Lia: zum alten Turm (Südwesten) laufen.");

            // 5) BESIEGEN -----------------------------------------------------
            QuestData defeat = MakeQuest("Q_EVENT_Schleimplage", "Schleimplage", QuestCategory.EVENT,
                "Hauptmann Rolf braucht Hilfe gegen die Schleime auf der Ostwiese.",
                Objective("defeat_slimes", QuestStepType.Defeat, "Schleime besiegen", "enemy_slime", 3),
                Xp(40), Gold(30));
            Register(qm, defeat);
            Npc("Hauptmann Rolf", new Vector2(7.5f, -6f), new Color(0.9f, 0.4f, 0.4f), "npc_rolf", defeat.questID,
                new[] { "Die Schleime fressen uns die Teepflanzen weg!", "Besiege 3 von ihnen – Schwert mit Leertaste." });
            slimeHome = new Vector2(13f, -8.5f);
            for (int i = 0; i < slimeCount; i++)
            {
                SpawnSlime();
            }
            StartCoroutine(RespawnSlimes());
            help.Add("BESIEGEN – Hauptmann Rolf: 3 Schleime (Südosten) mit dem Schwert besiegen.");

            // 6) DAILY aus JSON -----------------------------------------------
            QuestInstance daily = qm.GetQuest("Q_DAILY_Schleimjagd")
                                  ?? qm.RegisterQuestFromJson(DailyQuestJson, "TestMeadow.DailyQuestJson");
            SpriteRenderer board = Sprite(PlaceholderSprites.Square, Wood, new Vector2(-2.6f, 2.2f), new Vector2(1.2f, 0.9f), 0, "Anschlagbrett");
            board.gameObject.AddComponent<BoxCollider2D>();
            board.gameObject.AddComponent<QuestNpc>().Configure("npc_brett", daily != null ? daily.Data.questID : null,
                new[] { "AUSHANG: Freiwillige gesucht! Vertreibe heute 2 Schleime.", "(Jeden Morgen gibt es einen neuen Aushang.)" },
                "Anschlagbrett");
            help.Add("DAILY – Anschlagbrett: jeden Tag neu (Bett im Haus = schlafen = nächster Tag).");

            // 7) JUMP'N'RUN (eigene Szene) ------------------------------------
            QuestData climb = MakeQuest("Q_SIDE_Kletterpfad", "Der Kletterpfad", QuestCategory.SIDE,
                "Kletter-Kai wettet, dass du es nicht bis ans Ende des Kletterpfads schaffst.",
                Objective("reach_goal", QuestStepType.Visit, "Ziel des Kletterpfads erreichen", "loc_kletterpfad_ziel", 1),
                Xp(50), Gold(40));
            Register(qm, climb);
            Npc("Kletter-Kai", new Vector2(-14.5f, 1.5f), new Color(1f, 0.75f, 0.35f), "npc_kai", climb.questID,
                new[] { "Siehst du das Portal? Dahinter liegt der Kletterpfad.", "Dashen, Festhalten, Schwingen, Lianen … wetten, du schaffst es nicht bis ans Ziel?" },
                new[] { "Respekt! Bist echt geklettert wie ein Bergspatz." });
            SpriteRenderer portal = Sprite(PlaceholderSprites.Diamond, new Color(0.7f, 0.45f, 1f), new Vector2(-17f, 0f), new Vector2(1.2f, 1.6f), 0, "Portal");
            portal.gameObject.AddComponent<BoxCollider2D>();
            portal.gameObject.AddComponent<ScenePortal>().Configure(JumpAndRunScenePath, "Kletterpfad betreten");
            labels.Add((portal.transform, "Portal: Kletterpfad"));
            help.Add("JUMP'N'RUN – Portal ganz links: Kletterpfad (eigene Szene). Kletter-Kai hat eine Quest dazu.");
        }

        /// <summary>Registriert nur, wenn noch nicht vorhanden (beim Zurückkehren aus einer anderen Szene).</summary>
        private static void Register(QuestManager qm, QuestData data)
        {
            if (qm.GetQuest(data.questID) == null)
            {
                qm.RegisterQuest(data);
            }
        }

        private void Npc(string name, Vector2 pos, Color color, string npcID, string questID,
            string[] dialogue, string[] idle = null)
        {
            SpriteRenderer sr = Sprite(PlaceholderSprites.Circle, color, pos, new Vector2(0.9f, 0.9f), 0, "NPC_" + name);
            sr.gameObject.AddComponent<SortingGroup>();
            sr.gameObject.AddComponent<CircleCollider2D>().radius = 0.45f;
            // „Nase" zeigt, wohin der NPC schaut (facePlayer spiegelt das ganze Objekt).
            PlaceholderSprites.CreateSpriteObject("Nase", PlaceholderSprites.Diamond, new Color(0.2f, 0.15f, 0.12f),
                pos + new Vector2(0.3f, 0.1f), new Vector2(0.25f, 0.25f), 1, sr.transform);

            QuestNpc npc = sr.gameObject.AddComponent<QuestNpc>();
            npc.Configure(npcID, questID, dialogue, name, idle);
            npc.FlipWholeObject = true;
        }

        private void Pickup(string itemID, Vector2 pos, Color color)
        {
            SpriteRenderer sr = Sprite(PlaceholderSprites.Diamond, color, pos, new Vector2(0.55f, 0.55f), 0, "Pickup_" + itemID);
            var col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            // Wächst nach 6 s nach – so kann man beliebig oft testen.
            sr.gameObject.AddComponent<QuestItemPickup>().Configure(itemID, "Player", 6f);
        }

        private void Tower(Vector2 pos, string locationID)
        {
            SpriteRenderer tower = Sprite(PlaceholderSprites.Square, new Color(0.6f, 0.56f, 0.5f), pos, new Vector2(1.6f, 2.6f), 0, "Alter Turm");
            tower.gameObject.AddComponent<BoxCollider2D>();
            labels.Add((tower.transform, "Alter Turm"));

            var zone = new GameObject("Besuchszone");
            zone.transform.SetParent(root, false);
            zone.transform.position = pos;
            var trigger = zone.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 2.8f;
            zone.AddComponent<QuestLocationTrigger>().Configure(locationID);
        }

        private void SpawnSlime()
        {
            Vector2 pos = slimeHome + Random.insideUnitCircle * 2.5f;
            SpriteRenderer sr = Sprite(PlaceholderSprites.Circle, new Color(0.45f, 0.9f, 0.55f), pos, new Vector2(0.8f, 0.65f), 0, "Schleim");

            var body = sr.gameObject.AddComponent<Rigidbody2D>();
            body.mass = 0.6f;
            sr.gameObject.AddComponent<CircleCollider2D>().radius = 0.5f;
            sr.gameObject.AddComponent<Health>().Configure(3f, true);
            sr.gameObject.AddComponent<Knockback2D>();
            sr.gameObject.AddComponent<DamageFlash2D>();
            sr.gameObject.AddComponent<EnemyAI2D>();
            sr.gameObject.AddComponent<QuestEnemyReporter>().Configure("enemy_slime");
            slimes.Add(sr.gameObject);
        }

        private IEnumerator RespawnSlimes()
        {
            var wait = new WaitForSeconds(slimeRespawnSeconds);
            while (true)
            {
                yield return wait;
                slimes.RemoveAll(s => s == null);
                if (slimes.Count < slimeCount)
                {
                    SpawnSlime();
                }
            }
        }

        // ---------------- Daten-Helfer ----------------

        private static ItemData MakeItem(string id, string displayName, ItemCategory category, int maxStack, int gold, Color tint)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = id;
            item.itemID = id;
            item.displayName = displayName;
            item.category = category;
            item.maxStack = maxStack;
            item.goldValue = gold;
            item.tint = tint;
            return item;
        }

        private static QuestData MakeQuest(string id, string title, QuestCategory category, string description,
            ObjectiveData objective, params Reward[] rewards)
        {
            var d = ScriptableObject.CreateInstance<QuestData>();
            d.name = id;
            d.questID = id;
            d.displayName = title;
            d.description = description;
            d.category = category;
            d.objectives = new[] { objective };
            d.rewards = rewards;
            return d;
        }

        private static ObjectiveData Objective(string id, QuestStepType type, string description, string target, int amount)
        {
            return new ObjectiveData
            {
                objectiveID = id,
                stepType = type,
                description = description,
                targetID = target,
                requiredAmount = amount
            };
        }

        private static Reward Xp(int a) => new Reward { type = RewardType.XP, amount = a };
        private static Reward Gold(int a) => new Reward { type = RewardType.Gold, amount = a };
        private static Reward Item(string id, int a) => new Reward { type = RewardType.Item, itemID = id, amount = a };

        // ---------------- Kleinkram ----------------

        private static void EnsureSingleton<T>(string name) where T : Component
        {
            if (Object.FindAnyObjectByType<T>() == null)
            {
                new GameObject(name).AddComponent<T>();
            }
        }

        private T AddOnce<T>() where T : Component
        {
            T existing = Object.FindAnyObjectByType<T>();
            return existing != null ? existing : gameObject.AddComponent<T>();
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private void Update()
        {
            if (GameInput.HelpTogglePressed)
            {
                showHelp = !showHelp;
            }

            // Blickrichtungs-Punkt am Spieler nachführen.
            if (facingDot != null && playerController != null)
            {
                facingDot.localPosition = playerController.Facing * 0.5f;
            }
        }

        private void OnGUI()
        {
            if (QuestJournalCanvas.IsOpen)
            {
                return;
            }

            DrawLabels();

            if (DialogueHUD.IsActive)
            {
                return;
            }

            if (!showHelp)
            {
                GuiDraw.ShadowLabel(new Rect(14f, Screen.height - 54f, 360f, 20f), "Hilfe: N   ·   Questlog: B   ·   Cheats: F1",
                    GuiDraw.Rich(12));
                return;
            }

            float height = 52f + help.Count * 22f + 70f;
            var area = helpPanel.Apply(new Rect(Screen.width - 492f, Screen.height - height - 60f, 480f, height), 26f, 34f);
            GuiDraw.Panel(area, 0.9f);
            GuiDraggablePanel.DrawGrip(area);
            GameInput.ClaimGuiArea(area);

            if (GUI.Button(new Rect(area.xMax - 26f, area.y + 6f, 20f, 20f), "✕"))
            {
                showHelp = false;
            }

            GUILayout.BeginArea(new Rect(area.x + 24f, area.y + 8f, area.width - 46f, area.height - 16f));
            GUILayout.Label("<b>Testwiese</b>   (Hilfe aus: N oder ✕)", GuiDraw.Rich(15, new Color(1f, 0.85f, 0.5f)));
            GUILayout.Space(2);
            foreach (string line in help)
            {
                GUILayout.Label("• " + line, GuiDraw.Rich(12));
            }

            GUILayout.Space(4);
            GUILayout.Label("<b>Steuerung:</b> WASD laufen · Leertaste/J/Maus Schwert · E reden", GuiDraw.Rich(12));
            GUILayout.Label("I Inventar · B Questlog · F1 Cheats (Zeit, Quests, Gold …)", GuiDraw.Rich(12));
            GUILayout.EndArea();
        }

        private void DrawLabels()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            GUIStyle style = GuiDraw.Rich(12, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            style.wordWrap = false;

            foreach (var entry in labels)
            {
                if (entry.t == null)
                {
                    continue;
                }

                Vector3 sp = cam.WorldToScreenPoint(entry.t.position + Vector3.up * 0.9f);
                var content = new GUIContent(entry.label);
                Vector2 size = style.CalcSize(content);
                var rect = new Rect(sp.x - size.x / 2f - 5f, Screen.height - sp.y - size.y / 2f, size.x + 10f, size.y + 2f);
                GuiDraw.Solid(rect, new Color(0f, 0f, 0f, 0.4f));
                GUI.Label(rect, content, style);
            }
        }
    }
}
