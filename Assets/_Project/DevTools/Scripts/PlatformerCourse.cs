using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Economy;
using SteepingSpirits.Economy.Integration;
using SteepingSpirits.Interaction;
using SteepingSpirits.Inventory;
using SteepingSpirits.Inventory.Integration;
using SteepingSpirits.Platformer;
using SteepingSpirits.Player;
using SteepingSpirits.Progression;
using SteepingSpirits.Progression.Integration;
using SteepingSpirits.Quests;
using SteepingSpirits.Quests.Integration;
using SteepingSpirits.Quests.UI;
using SteepingSpirits.World;

namespace SteepingSpirits.DevTools
{
    /// <summary>
    /// Baut beim Play den kompletten Jump'n'Run-Testparcours (Seitenansicht)
    /// aus Platzhalter-Grafiken. Liegt fertig in der Szene
    /// Assets/Scenes/JumpAndRun.unity – oder: leere Szene → leeres GameObject →
    /// diese Komponente → Play.
    ///
    /// Abschnitte (links → rechts, jeweils mit Checkpoint):
    ///  1. Start      – Laufen, Springen
    ///  2. Dash       – Lücke nur mit Dash, Stufe nur mit Sprung + Dash nach oben
    ///  3. Wand       – Schacht per Wandsprung hoch
    ///  4. Halten     – Haltepunkte über Stacheln (greifen, abspringen)
    ///  5. Schwingen  – Seilpunkte: hin und her Schwung aufbauen, weit fliegen
    ///  6. Lianen     – Physik-Lianen schaukeln, klettern, abspringen → Ziel
    ///
    /// Sternfrüchte unterwegs landen im Inventar (gleiches Quest-/Inventar-System
    /// wie auf der Wiese). Das Ziel erfüllt die Quest „Der Kletterpfad".
    /// </summary>
    public class PlatformerCourse : MonoBehaviour
    {
        private static readonly Color Rock = new Color(0.42f, 0.36f, 0.33f);
        private static readonly Color Grass = new Color(0.38f, 0.65f, 0.3f);
        private static readonly Color Spike = new Color(0.85f, 0.85f, 0.9f);

        private Transform root;
        private readonly List<(Vector2 pos, string text)> signs = new List<(Vector2, string)>();

        private void Start()
        {
            root = new GameObject("Parcours").transform;
            root.SetParent(transform, false);

            EnsureSystems();
            BuildCourse();
            PlatformerController2D player = BuildPlayer(new Vector2(-3f, 1f));
            SetupCamera(player.transform);
        }

        // ---------------- Systeme ----------------

        private void EnsureSystems()
        {
            EnsureSingleton<QuestManager>("QuestManager");
            EnsureSingleton<PlayerInventory>("PlayerInventory");
            EnsureSingleton<Wallet>("Wallet");
            EnsureSingleton<PlayerProgression>("PlayerProgression");

            AddOnce<InventoryPickupBridge>();
            AddOnce<QuestRewardCollector>();
            AddOnce<GoldRewardCollector>();
            AddOnce<XpRewardCollector>();
            AddOnce<QuestToastHUD>();
            AddOnce<QuestJournalCanvas>();
            AddOnce<QuestAudioHooks>();
            AddOnce<DevCheatWindow>();
            AddOnce<PlatformerHUD>();

            var fruit = ScriptableObject.CreateInstance<ItemData>();
            fruit.name = fruit.itemID = "item_sternfrucht";
            fruit.displayName = "Sternfrucht";
            fruit.category = ItemCategory.Material;
            fruit.goldValue = 25;
            fruit.tint = new Color(1f, 0.85f, 0.3f);
            PlayerInventory.Instance.RegisterItem(fruit);
        }

        // ---------------- Parcours ----------------

        private void BuildCourse()
        {
            // 1) START --------------------------------------------------------
            Block(-7f, -6f, 0f, 20f);                // linke Wand
            Ground(-6f, 20f, 0f);
            Ground(6f, 9f, 1.8f, 0.5f);
            Ground(11f, 14f, 3.4f, 0.5f);
            Sign(-3f, 2.5f, "Laufen: WASD · Springen: Leertaste (länger halten = höher)");
            Sign(12.5f, 5.2f, "Sprung kurz nach der Kante klappt noch (Coyote-Time)");

            // 2) DASH ---------------------------------------------------------
            Checkpoint(17f, 0f);
            Pit(20f, 28f, -3f);
            Sign(19f, 3.5f, "Zu weit? Im Sprung DASH: Shift/X + Richtung");
            Fruit(24f, 3f);
            Ground(28f, 34f, 0f);
            Ground(34f, 44f, 5f, 5f);                // hohe Stufe (geht unter dem Schacht weiter)
            Sign(31f, 3f, "Hoch hinaus: Springen, dann Dash nach oben (↑ + Shift)");

            // 3) WAND ---------------------------------------------------------
            Checkpoint(38f, 5f);
            Block(40.5f, 41.5f, 7.5f, 17f);          // linke Schachtwand (unten offen)
            Block(44f, 45f, 5f, 17f);                // rechte Schachtwand
            Ground(44f, 56f, 17f, 1f);               // Plattform oben
            Sign(42.7f, 9.5f, "Wandsprung: an die Wand, dann springen");
            Fruit(42.7f, 15f);

            // 4) HALTEN -------------------------------------------------------
            Checkpoint(53f, 17f);
            Pit(56f, 77f, 3f);
            Sign(55f, 20.5f, "Greifen: K / Strg HALTEN · Sprung dosieren (kurz drücken) · Dash geht auch");
            foreach (float x in new[] { 60f, 64.5f, 69f, 73.5f })
            {
                Point(GrabPoint.Mode.Hold, new Vector2(x, 18.8f), 1.3f, 0f);
            }
            Fruit(66.75f, 21f);
            Ground(77f, 86f, 17f, 1f);

            // 5) SCHWINGEN ----------------------------------------------------
            Checkpoint(82f, 17f);
            Pit(86f, 113f, 3f);
            Sign(84f, 21f, "Schwingen: greifen, dann IM TAKT links/rechts → immer mehr Schwung");
            Point(GrabPoint.Mode.Swing, new Vector2(90f, 22f), 2.6f, 3.5f);
            Point(GrabPoint.Mode.Swing, new Vector2(99f, 23f), 2.6f, 3.5f);
            Point(GrabPoint.Mode.Swing, new Vector2(108f, 22f), 2.6f, 3.5f);
            Fruit(103.5f, 25.5f);
            Ground(113f, 122f, 17f, 1f);

            // 6) LIANEN -------------------------------------------------------
            Checkpoint(116f, 17f);
            Pit(122f, 151f, 3f);
            Sign(119f, 21f, "Lianen: greifen, schaukeln (links/rechts), klettern (hoch/runter)");
            foreach (float x in new[] { 126f, 133f, 140f, 147f })
            {
                VineAt(new Vector2(x, 26f));
            }
            Fruit(136.5f, 22f);
            Ground(151f, 168f, 17f, 1f);
            Block(168f, 169f, 17f, 32f);             // rechte Wand

            // ZIEL ------------------------------------------------------------
            SpriteRenderer flag = Sprite(PlaceholderSprites.Triangle, new Color(1f, 0.85f, 0.3f), new Vector2(161f, 18.6f), new Vector2(1f, 1.2f), 2, "Ziel");
            var goalCol = flag.gameObject.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            goalCol.size = new Vector2(2f, 3f);
            flag.gameObject.AddComponent<Goal2D>().Configure("loc_kletterpfad_ziel");
            Sign(161f, 20.5f, "ZIEL");

            SpriteRenderer portal = Sprite(PlaceholderSprites.Diamond, new Color(0.7f, 0.45f, 1f), new Vector2(165f, 18f), new Vector2(1.2f, 1.6f), 2, "Portal zurück");
            portal.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            portal.gameObject.AddComponent<ScenePortal>().Configure(null, "Zurück", true);
            Sign(165f, 19.6f, "Portal [E]");
        }

        // ---------------- Bau-Helfer ----------------

        private SpriteRenderer Sprite(Sprite sprite, Color color, Vector2 pos, Vector2 size, int order, string name)
        {
            return PlaceholderSprites.CreateSpriteObject(name, sprite, color, pos, size, order, root);
        }

        /// <summary>Fester Block von x0..x1, y0..y1.</summary>
        private void Block(float x0, float x1, float y0, float y1)
        {
            SpriteRenderer sr = Sprite(PlaceholderSprites.Square, Rock,
                new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), new Vector2(x1 - x0, y1 - y0), 0, "Fels");
            sr.gameObject.AddComponent<BoxCollider2D>();
        }

        /// <summary>Boden mit Gras-Oberkante bei top.</summary>
        private void Ground(float x0, float x1, float top, float thickness = 6f)
        {
            Block(x0, x1, top - thickness, top);
            Sprite(PlaceholderSprites.Square, Grass, new Vector2((x0 + x1) * 0.5f, top - 0.1f), new Vector2(x1 - x0, 0.2f), 1, "Gras");
        }

        /// <summary>Grube mit Stacheln am Boden (tödlich).</summary>
        private void Pit(float x0, float x1, float floorTop)
        {
            Block(x0, x1, floorTop - 3f, floorTop);
            for (float x = x0 + 0.25f; x < x1; x += 0.5f)
            {
                Sprite(PlaceholderSprites.Triangle, Spike, new Vector2(x, floorTop + 0.25f), new Vector2(0.5f, 0.5f), 1, "Stachel");
            }

            var zone = new GameObject("Stacheln");
            zone.transform.SetParent(root, false);
            zone.transform.position = new Vector2((x0 + x1) * 0.5f, floorTop + 0.25f);
            var col = zone.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(x1 - x0, 0.5f);
            zone.AddComponent<Hazard2D>();
        }

        private void Checkpoint(float x, float groundTop)
        {
            Sprite(PlaceholderSprites.Square, new Color(0.45f, 0.3f, 0.18f), new Vector2(x, groundTop + 0.75f), new Vector2(0.1f, 1.5f), 1, "Fahnenstange");
            SpriteRenderer flag = Sprite(PlaceholderSprites.Square, new Color(0.8f, 0.3f, 0.3f), new Vector2(x + 0.3f, groundTop + 1.3f), new Vector2(0.55f, 0.35f), 2, "Checkpoint");

            var zone = new GameObject("CheckpointZone");
            zone.transform.SetParent(root, false);
            zone.transform.position = new Vector2(x, groundTop + 1f);
            var col = zone.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 2f);
            zone.AddComponent<Checkpoint2D>().Configure(flag);
        }

        private void Point(GrabPoint.Mode mode, Vector2 pos, float catchRadius, float ropeLength)
        {
            Color c = mode == GrabPoint.Mode.Hold ? new Color(1f, 0.82f, 0.3f) : new Color(0.45f, 0.85f, 1f);
            SpriteRenderer sr = Sprite(PlaceholderSprites.Circle, c, pos, new Vector2(0.45f, 0.45f), 3,
                mode == GrabPoint.Mode.Hold ? "Haltepunkt" : "Schwingpunkt");
            sr.gameObject.AddComponent<GrabPoint>().Configure(mode, catchRadius, ropeLength);

            // Dezenter Ring zeigt die Greif-Reichweite.
            Sprite(PlaceholderSprites.Circle, new Color(c.r, c.g, c.b, 0.12f), pos, Vector2.one * catchRadius * 2f, -5, "Reichweite");
        }

        private void VineAt(Vector2 anchor)
        {
            var go = new GameObject("Liane");
            go.transform.SetParent(root, false);
            go.transform.position = anchor;
            go.AddComponent<Vine>().Configure(12, 0.6f);
        }

        private void Fruit(float x, float y)
        {
            SpriteRenderer sr = Sprite(PlaceholderSprites.Diamond, new Color(1f, 0.85f, 0.3f), new Vector2(x, y), new Vector2(0.55f, 0.55f), 4, "Sternfrucht");
            var col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            sr.gameObject.AddComponent<QuestItemPickup>().Configure("item_sternfrucht", "Player", 0f);
        }

        private void Sign(float x, float y, string text)
        {
            signs.Add((new Vector2(x, y), text));
        }

        // ---------------- Spieler & Kamera ----------------

        private PlatformerController2D BuildPlayer(Vector2 spawn)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = spawn;

            var body = player.AddComponent<Rigidbody2D>();
            body.mass = 1f;
            var col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.65f, 0.95f);

            // Grafik als Kind (damit Strecken/Stauchen den Collider nicht verändert).
            SpriteRenderer sr = PlaceholderSprites.CreateSpriteObject("Körper", PlaceholderSprites.Square,
                Color.white, spawn, new Vector2(0.65f, 0.95f), 10, player.transform);
            sr.transform.localPosition = Vector3.zero;
            PlaceholderSprites.CreateSpriteObject("Auge", PlaceholderSprites.Circle, new Color(0.1f, 0.1f, 0.15f),
                spawn + new Vector2(0.15f, 0.2f), new Vector2(0.18f, 0.18f), 11, sr.transform);

            PlatformerController2D controller = player.AddComponent<PlatformerController2D>();
            player.AddComponent<Interactor2D>();
            return controller;
        }

        private void SetupCamera(Transform player)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }

            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            cam.transform.position = new Vector3(player.position.x, player.position.y, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);

            CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();
            follow.Target = player;
            follow.OrthographicSize = 7.5f;
            follow.SetBounds(new Rect(-7f, -6f, 176f, 38f));
            follow.SnapToTarget();

            EnsureGlobalLight2D();
        }

        /// <summary>Wie in der TestMeadow: URP-2D braucht ein Licht, sonst sind Sprites dunkel.</summary>
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
                Debug.LogWarning("[PlatformerCourse] Konnte kein Global Light 2D anlegen: " + e.Message);
            }
        }

        // ---------------- Kleinkram ----------------

        private static void EnsureSingleton<T>(string name) where T : Component
        {
            if (Object.FindAnyObjectByType<T>() == null)
            {
                new GameObject(name).AddComponent<T>();
            }
        }

        private void AddOnce<T>() where T : Component
        {
            if (Object.FindAnyObjectByType<T>() == null)
            {
                gameObject.AddComponent<T>();
            }
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null || QuestJournalCanvas.IsOpen)
            {
                return;
            }

            GUIStyle style = GuiDraw.Rich(13, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            style.wordWrap = false;

            foreach (var sign in signs)
            {
                Vector3 sp = cam.WorldToScreenPoint(sign.pos);
                if (sp.x < -400f || sp.x > Screen.width + 400f)
                {
                    continue;
                }

                var content = new GUIContent(sign.text);
                Vector2 size = style.CalcSize(content);
                var rect = new Rect(sp.x - size.x / 2f - 8f, Screen.height - sp.y - size.y / 2f, size.x + 16f, size.y + 6f);
                GuiDraw.Solid(rect, new Color(0.15f, 0.1f, 0.05f, 0.7f));
                GUI.Label(rect, content, style);
            }
        }
    }
}
