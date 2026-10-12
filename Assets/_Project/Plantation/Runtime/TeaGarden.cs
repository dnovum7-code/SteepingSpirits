using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SteepingSpirits.Core;
using SteepingSpirits.Inventory;
using SteepingSpirits.Player;
using SteepingSpirits.World;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Root of the tea garden scene. Builds the garden at runtime from the text layout (like the
    /// test meadow): hedges, paths, bed cells, well, lantern, seed chest, compost bin and the gate
    /// back to the meadow, plus player, camera, HUD and feedback. Owns the simulation, follows the
    /// game clock (nights that passed elsewhere are caught up) and saves to garden_save.json.
    /// Created by SteepingSpirits → Plantation → Build Garden.
    /// </summary>
    public class TeaGarden : MonoBehaviour
    {
        [SerializeField] private TextAsset layoutAsset;
        [SerializeField] private GardenSettings settings;
        [SerializeField] private PlantationAudioSet audioSet;

        [Tooltip("Scene the gate leads to when the garden was not entered through a portal")]
        [SerializeField] private string meadowScenePath = "Assets/Scenes/TestMeadow.unity";

        private static readonly Color GrassColor = new Color(0.43f, 0.64f, 0.34f);
        private static readonly Color GrassDark = new Color(0.38f, 0.58f, 0.30f);
        private static readonly Color PathColor = new Color(0.80f, 0.70f, 0.50f);
        private static readonly Color HedgeColor = new Color(0.20f, 0.42f, 0.24f);
        private static readonly Color Wood = new Color(0.52f, 0.35f, 0.20f);

        private readonly Dictionary<GridPos, GardenTile> objects = new Dictionary<GridPos, GardenTile>();
        private readonly Dictionary<string, int> restock = new Dictionary<string, int>();
        private IDayClock clock;
        private int lastClockDay;
        private bool dirty;
        private float nextSave;
        private bool busy;
        private bool leaving;
        private bool quitting;

        public Garden Garden { get; private set; }
        public GardenLayout Layout { get; private set; }
        public GardenSettings Settings => settings;
        public PlantBook Book { get; private set; }
        public GardenView View { get; private set; }
        public GardenHud Hud { get; private set; }
        public GardenFeedback Feedback { get; private set; }
        public GardenController Controller { get; private set; }
        public Transform Player { get; private set; }

        /// <summary>Chest, options and seed backup (everything saved besides the cells).</summary>
        public GardenSave State { get; private set; }

        /// <summary>True while the day changes (fade) – tools wait.</summary>
        public bool Busy => busy || leaving;

        /// <summary>Raised after every applied tool use (telemetry, debug).</summary>
        public event Action<ActionOutcome, GardenAction> ActionApplied;

        /// <summary>Raised after every night (telemetry).</summary>
        public event Action<DayReport> NightPassed;

        /// <summary>Raised when the player ends the day at the lantern (telemetry closes the round).</summary>
        public event Action DayEnding;

        public void Configure(TextAsset layout, GardenSettings gardenSettings, PlantationAudioSet audio, string meadowPath)
        {
            layoutAsset = layout;
            settings = gardenSettings;
            audioSet = audio;
            meadowScenePath = meadowPath;
        }

        private void Start()
        {
            EnsureSystems();
            if (settings == null) settings = GardenSettings.CreateDefault();
            Layout = ParseLayout();
            Book = settings.BuildBook();
            GardenItems.RegisterSeedItems(Book);

            clock = new GameClockDays(GameClock.Instance);
            Garden = Garden.FromLayout(Layout, settings.garden, Book, 1);
            LoadState();

            BuildWorld();
            Player = SetupPlayer();
            SetupCamera(Player);
            EnsureGlobalLight2D();

            Feedback = new GameObject("Rückmeldung").AddComponent<GardenFeedback>();
            Feedback.transform.SetParent(transform, false);
            Feedback.Configure(settings.feedback, audioSet, Player);

            View = new GameObject("Beete").AddComponent<GardenView>();
            View.transform.SetParent(transform, false);
            View.Build(Garden, settings);
            View.SetHelp(State.gardenHelp);

            Hud = gameObject.AddComponent<GardenHud>();
            Hud.Build();

            Controller = gameObject.AddComponent<GardenController>();
            Controller.Init(this, Player.GetComponent<PlayerController2D>());

            gameObject.AddComponent<GardenTelemetry>().Init(this);
            gameObject.AddComponent<GardenDebugOverlay>().Init(this);
            var spirit = new GameObject("Regengeist").AddComponent<RainSpirit>();
            spirit.transform.SetParent(transform, false);
            spirit.Init(this);

            RestoreSeedsIfLost();
            GameClock.OnNewDay += OnClockNewDay;
            SyncDays(true);
            Save();
        }

        private void OnDestroy()
        {
            GameClock.OnNewDay -= OnClockNewDay;

            // On quit the inventory may already be gone – the save from OnApplicationQuit stands.
            if (Garden != null && !leaving && !quitting) Save();
        }

        private void OnApplicationQuit()
        {
            if (Garden != null) Save();
            quitting = true;
        }

        private void LateUpdate()
        {
            if (dirty && Time.unscaledTime >= nextSave)
            {
                Save();
            }
        }

        // ---------------- Systems ----------------

        private static void EnsureSystems()
        {
            EnsureSingleton<PlayerInventory>("PlayerInventory");
            EnsureSingleton<GameClock>("GameClock");
        }

        private static void EnsureSingleton<T>(string name) where T : Component
        {
            if (FindAnyObjectByType<T>() == null) new GameObject(name).AddComponent<T>();
        }

        private GardenLayout ParseLayout()
        {
            if (layoutAsset != null)
            {
                try
                {
                    return GardenLayout.Parse(layoutAsset.text);
                }
                catch (FormatException e)
                {
                    Debug.LogWarning("[Plantation] Layout '" + layoutAsset.name + "' broken (" + e.Message + ") – using the small fallback garden.");
                }
            }

            return GardenLayout.Parse(FallbackLayout);
        }

        private const string FallbackLayout =
            "@seeds mint:6 lavender:3\n" +
            "##########\n" +
            "#W......C#\n" +
            "#.BBB.BBB#\n" +
            "#.BBB.BBB#\n" +
            "G:P...L..#\n" +
            "##########\n";

        // ---------------- Save / days ----------------

        private void LoadState()
        {
            GardenSave save = GardenSaveStore.Load();
            if (save == null)
            {
                State = new GardenSave
                {
                    wideCan = settings.comfort.wideCanDefault,
                    gardenHelp = settings.comfort.gardenHelpDefault,
                    clockDay = clock.Today
                };
                Garden.CanWater = settings.garden.canCapacity;
            }
            else
            {
                State = save;
                save.ApplyTo(Garden);
                if (State.clockDay <= 0) State.clockDay = clock.Today;
            }

            if (!settings.comfort.wideCanAvailable) State.wideCan = false;
            lastClockDay = State.clockDay;
        }

        public void MarkDirty()
        {
            if (!dirty) nextSave = Time.unscaledTime + 1f;
            dirty = true;
        }

        public void Save()
        {
            if (Garden == null || State == null) return;
            dirty = false;
            GardenSave s = GardenSave.From(Garden);
            s.clockDay = lastClockDay;
            s.chestOpened = State.chestOpened;
            s.wideCan = State.wideCan;
            s.gardenHelp = State.gardenHelp;
            foreach (KeyValuePair<string, int> kv in State.chestSeeds) s.chestSeeds[kv.Key] = kv.Value;
            if (PlayerInventory.Instance != null)
            {
                foreach (PlantParams p in Book.All)
                {
                    int n = GardenItems.Count(p.seedItemId);
                    if (n > 0) s.seedBackup[p.seedItemId] = n;
                }

                int compost = GardenItems.Count(GardenItems.CompostItemId);
                if (compost > 0) s.seedBackup[GardenItems.CompostItemId] = compost;
                State.seedBackup.Clear();
                foreach (KeyValuePair<string, int> kv in s.seedBackup) State.seedBackup[kv.Key] = kv.Value;
            }
            else
            {
                // No inventory to look at: keep the last known backup.
                foreach (KeyValuePair<string, int> kv in State.seedBackup) s.seedBackup[kv.Key] = kv.Value;
            }
            if (!GardenSaveStore.Save(s)) Hud?.Message(GardenTexts.SaveFailed);
        }

        /// <summary>
        /// The inventory is not always saved (it starts empty when nobody loads it). If it has no
        /// garden items at all but the garden remembers some, they come back – never twice, since
        /// the backup always mirrors what the player carried at the last garden save.
        /// </summary>
        private void RestoreSeedsIfLost()
        {
            if (State.seedBackup.Count == 0 || PlayerInventory.Instance == null) return;
            foreach (PlantParams p in Book.All)
            {
                if (GardenItems.Count(p.seedItemId) > 0) return;
            }

            if (GardenItems.Count(GardenItems.CompostItemId) > 0) return;
            foreach (KeyValuePair<string, int> kv in State.seedBackup)
            {
                int left = GardenItems.Add(kv.Key, kv.Value);
                if (left > 0) AddToChest(kv.Key, left);
            }

            Hud.Message(GardenTexts.SeedsRestored);
        }

        private void OnClockNewDay(GameClock c) => SyncDays(false);

        /// <summary>Catches up the nights the game clock moved on since the garden last looked.</summary>
        private void SyncDays(bool entering)
        {
            int today = clock.Today;
            int nights = today - lastClockDay;
            lastClockDay = today;
            if (nights <= 0)
            {
                if (entering) Hud.Message(GardenTexts.Morning(Garden.Day, Garden.RainyToday, CountRipe()));
                return;
            }

            List<DayReport> reports = Garden.CatchUpTo(Garden.Day + nights);
            bool grew = false;
            bool restocked = false;
            foreach (DayReport r in reports)
            {
                grew |= r.grew.Count > 0;
                NightPassed?.Invoke(r);
                SeedChest.Restock(r.day + 1, settings.garden, Book, restock);
                foreach (KeyValuePair<string, int> kv in restock)
                {
                    AddToChest(kv.Key, kv.Value);
                    restocked = true;
                }
            }

            if (reports.Count > 0) View.PlayNight(reports[reports.Count - 1]);
            else View.RefreshAll();
            if (grew) Feedback.Play(GardenSound.Grow, 0.8f);
            Hud.Message(GardenTexts.Morning(Garden.Day, Garden.RainyToday, CountRipe()) + (restocked ? " " + GardenTexts.ChestRestocked : ""));
            Save();
        }

        private int CountRipe()
        {
            int n = 0;
            foreach (GridPos p in Garden.Beds)
            {
                Crop c = Garden.PlotAt(p).crop;
                if (c != null && c.ripe) n++;
            }

            return n;
        }

        /// <summary>The lantern: soft dusk, the night passes, soft morning.</summary>
        public void EndDay(bool instant = false)
        {
            if (busy) return;
            StartCoroutine(EndDayRoutine(instant));
        }

        private IEnumerator EndDayRoutine(bool instant)
        {
            busy = true;
            DayEnding?.Invoke();
            var mover = Player != null ? Player.GetComponent<PlayerController2D>() : null;
            if (mover != null) mover.MovementLocked = true;
            float seconds = instant ? 0.15f : settings.feedback.dayFadeSeconds;
            Feedback.Play(GardenSound.EndDay);
            Hud.Fade(true, seconds);
            yield return new WaitForSecondsRealtime(seconds + 0.1f);

            clock.EndDay();
            SyncDays(false);
            yield return new WaitForSecondsRealtime(instant ? 0.05f : 0.35f);

            Hud.Fade(false, seconds);
            if (mover != null) mover.MovementLocked = false;
            busy = false;
        }

        // ---------------- Objects ----------------

        public bool ObjectAt(GridPos p, out GardenTile tile) => objects.TryGetValue(p, out tile);

        public void Leave()
        {
            if (leaving) return;
            string target = !string.IsNullOrEmpty(ScenePortal.PreviousScenePath) ? ScenePortal.PreviousScenePath : meadowScenePath;
            if (!CanLoad(target))
            {
                Hud.Message(GardenTexts.NoMeadow);
                return;
            }

            Save();
            leaving = true;
            ScenePortal.Load(target);
        }

        private static bool CanLoad(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
#if UNITY_EDITOR
            return System.IO.File.Exists(path);
#else
            return Application.CanStreamedLevelBeLoaded(System.IO.Path.GetFileNameWithoutExtension(path));
#endif
        }

        public void RefillCan(GridPos well)
        {
            if (Garden.CanWater >= settings.garden.canCapacity)
            {
                Hud.Message(GardenTexts.CanFull);
                return;
            }

            Garden.Refill();
            Feedback.Play(GardenSound.Refill);
            Feedback.Drops(GardenView.CellCenter(well));
            Hud.Message(GardenTexts.CanRefilled);
            View.RefreshAll();
            MarkDirty();
        }

        /// <summary>Seed chest: the starter pack once, afterwards seeds that did not fit into the bag.</summary>
        public void OpenChest(GridPos chest)
        {
            bool gave = false;
            if (!State.chestOpened)
            {
                State.chestOpened = true;
                foreach (KeyValuePair<string, int> kv in Layout.SeedPack())
                {
                    PlantParams p = Book.Get(kv.Key);
                    if (p != null) AddToChest(p.seedItemId, kv.Value);
                }

                Hud.Message(GardenTexts.ChestStarter);
            }

            var ids = new List<string>(State.chestSeeds.Keys);
            bool full = false;
            foreach (string id in ids)
            {
                int n = State.chestSeeds[id];
                int left = GardenItems.Add(id, n);
                gave |= left < n;
                full |= left > 0;
                if (left > 0) State.chestSeeds[id] = left;
                else State.chestSeeds.Remove(id);
            }

            if (gave) Feedback.Play(GardenSound.Chest);
            if (full) Hud.Message(GardenTexts.BagFull);
            else if (!gave) Hud.Message(GardenTexts.ChestEmpty);
            Feedback.Seeds(GardenView.CellCenter(chest));
            MarkDirty();
        }

        public void AddToChest(string itemId, int amount)
        {
            if (amount <= 0) return;
            State.chestSeeds.TryGetValue(itemId, out int have);
            State.chestSeeds[itemId] = have + amount;
        }

        /// <summary>Compost bin: tart tea (ids from the settings) becomes compost.</summary>
        public void UseCompostBin(GridPos bin)
        {
            string input = GardenItems.CarriedCompostInput(settings.compostInputIds);
            if (input == null)
            {
                Hud.Message(GardenTexts.CompostNothing);
                return;
            }

            if (!GardenItems.RemoveOne(input)) return;
            int left = GardenItems.Add(GardenItems.CompostItemId, 1);
            if (left > 0) AddToChest(GardenItems.CompostItemId, left);
            Feedback.Play(GardenSound.Dig);
            Feedback.Crumbs(GardenView.CellCenter(bin));
            Hud.Message(GardenTexts.CompostMade);
            MarkDirty();
        }

        internal void RaiseAction(ActionOutcome o, GardenAction a) => ActionApplied?.Invoke(o, a);

        // ---------------- World ----------------

        private void BuildWorld()
        {
            Transform world = new GameObject("Garten").transform;
            world.SetParent(transform, false);
            float w = Layout.Width, h = Layout.Height;
            Sprite(world, PlaceholderSprites.Square, GrassColor, new Vector2(w / 2f, h / 2f), new Vector2(w + 12f, h + 12f), -100, "Wiese");

            var rng = new System.Random(11);
            for (int i = 0; i < 24; i++)
            {
                var p = new Vector2((float)rng.NextDouble() * w, (float)rng.NextDouble() * h);
                float s = 0.5f + (float)rng.NextDouble();
                Sprite(world, PlaceholderSprites.Circle, GrassDark, p, new Vector2(s * 1.5f, s), -99, "Grasfleck");
            }

            // Invisible fence one cell outside the layout (the gate is an opening in the hedge).
            Fence(world, new Vector2(w / 2f, -0.5f), new Vector2(w + 2f, 1f));
            Fence(world, new Vector2(w / 2f, h + 0.5f), new Vector2(w + 2f, 1f));
            Fence(world, new Vector2(-0.5f, h / 2f), new Vector2(1f, h + 2f));
            Fence(world, new Vector2(w + 0.5f, h / 2f), new Vector2(1f, h + 2f));

            for (int y = 0; y < Layout.Height; y++)
            {
                int runStart = -1;
                for (int x = 0; x <= Layout.Width; x++)
                {
                    bool wall = x < Layout.Width && Layout.At(x, y) == GardenTile.Wall;
                    if (wall && runStart < 0) runStart = x;
                    if (!wall && runStart >= 0)
                    {
                        Hedge(world, runStart, x - 1, y);
                        runStart = -1;
                    }

                    if (x >= Layout.Width) continue;
                    var cell = new GridPos(x, y);
                    Vector2 c = GardenView.CellCenter(cell);
                    switch (Layout.At(x, y))
                    {
                        case GardenTile.Path:
                            Sprite(world, PlaceholderSprites.Square, PathColor, c, Vector2.one, -90, "Weg");
                            break;
                        case GardenTile.Well:
                            Well(world, c);
                            objects[cell] = GardenTile.Well;
                            break;
                        case GardenTile.Lantern:
                            Lantern(world, c);
                            objects[cell] = GardenTile.Lantern;
                            break;
                        case GardenTile.Chest:
                            Solid(world, PlaceholderSprites.Square, Wood, c, new Vector2(0.8f, 0.6f), "Samenkiste");
                            Sprite(world, PlaceholderSprites.Square, new Color(0.38f, 0.25f, 0.14f), c + new Vector2(0f, 0.12f), new Vector2(0.82f, 0.08f), 1, "Deckel");
                            objects[cell] = GardenTile.Chest;
                            break;
                        case GardenTile.Compost:
                            Solid(world, PlaceholderSprites.Square, new Color(0.40f, 0.30f, 0.18f), c, new Vector2(0.85f, 0.7f), "Komposter");
                            Sprite(world, PlaceholderSprites.Circle, new Color(0.25f, 0.18f, 0.1f), c + new Vector2(0f, 0.15f), new Vector2(0.6f, 0.3f), 1, "Kompost");
                            objects[cell] = GardenTile.Compost;
                            break;
                        case GardenTile.Gate:
                            Sprite(world, PlaceholderSprites.Square, PathColor, c, Vector2.one, -90, "Tor");
                            Sprite(world, PlaceholderSprites.Square, Wood, c + new Vector2(0f, 0.45f), new Vector2(0.2f, 0.9f), 2, "Torpfosten");
                            Sprite(world, PlaceholderSprites.Square, Wood, c + new Vector2(0f, -0.45f), new Vector2(0.2f, 0.9f), 2, "Torpfosten");
                            objects[cell] = GardenTile.Gate;
                            break;
                    }
                }
            }
        }

        private static void Fence(Transform parent, Vector2 center, Vector2 size)
        {
            var go = new GameObject("Zaun (unsichtbar)");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        private void Hedge(Transform parent, int x0, int x1, int y)
        {
            float cx = (x0 + x1 + 1) / 2f;
            var size = new Vector2(x1 - x0 + 1, 1f);
            SpriteRenderer sr = Sprite(parent, PlaceholderSprites.Square, HedgeColor, new Vector2(cx, y + 0.5f), size, -80, "Hecke");
            sr.gameObject.AddComponent<BoxCollider2D>();
        }

        private void Well(Transform parent, Vector2 c)
        {
            Solid(parent, PlaceholderSprites.Circle, new Color(0.62f, 0.62f, 0.6f), c, new Vector2(0.95f, 0.95f), "Brunnen");
            Sprite(parent, PlaceholderSprites.Circle, new Color(0.35f, 0.58f, 0.85f), c, new Vector2(0.6f, 0.6f), 1, "Wasser");
        }

        private void Lantern(Transform parent, Vector2 c)
        {
            Solid(parent, PlaceholderSprites.Square, Wood, c + new Vector2(0f, -0.1f), new Vector2(0.18f, 0.8f), "Laterne");
            Sprite(parent, PlaceholderSprites.Circle, new Color(1f, 0.85f, 0.5f, 0.18f), c + new Vector2(0f, 0.35f), new Vector2(1.6f, 1.6f), 0, "Laternenschein");
            Sprite(parent, PlaceholderSprites.Square, new Color(1f, 0.86f, 0.55f), c + new Vector2(0f, 0.35f), new Vector2(0.34f, 0.34f), 2, "Licht");
        }

        private static SpriteRenderer Solid(Transform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size, string name)
        {
            SpriteRenderer sr = Sprite(parent, sprite, color, pos, size, 0, name);
            sr.gameObject.AddComponent<SortingGroup>();
            sr.gameObject.AddComponent<BoxCollider2D>();
            return sr;
        }

        private static SpriteRenderer Sprite(Transform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size, int order, string name) =>
            PlaceholderSprites.CreateSpriteObject(name, sprite, color, pos, size, order, parent);

        // ---------------- Player & camera ----------------

        private Transform SetupPlayer()
        {
            Vector2 spawn = GardenView.CellCenter(Layout.All(GardenTile.Start)[0]);
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = new GameObject("Player") { tag = "Player" };
                var sr = player.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderSprites.Circle;
                sr.color = new Color(0.98f, 0.86f, 0.55f);
                player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            }

            player.transform.position = spawn;
            GetOrAdd<SortingGroup>(player);
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(player);
            body.bodyType = RigidbodyType2D.Dynamic;
            if (!player.TryGetComponent(out Collider2D _))
            {
                player.AddComponent<CircleCollider2D>().radius = 0.4f;
            }

            GetOrAdd<PlayerController2D>(player);
            PlaceholderSprites.CreateSpriteObject("Blickrichtung", PlaceholderSprites.Diamond, new Color(0.35f, 0.22f, 0.12f),
                player.transform.position, new Vector2(0.3f, 0.3f), 1, player.transform);
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

            if (FindAnyObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.transform.position = new Vector3(player.position.x, player.position.y, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.24f, 0.15f);

            CameraFollow2D follow = GetOrAdd<CameraFollow2D>(cam.gameObject);
            follow.Target = player;
            follow.OrthographicSize = 6.5f;
            follow.SetBounds(new Rect(-1f, -1f, Layout.Width + 2f, Layout.Height + 2f));
            follow.SnapToTarget();
        }

        /// <summary>URP 2D needs a light; add a global one if URP is there and none exists (by reflection).</summary>
        private void EnsureGlobalLight2D()
        {
            Type lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (lightType == null || FindAnyObjectByType(lightType) != null) return;
            try
            {
                var go = new GameObject("Global Light 2D");
                go.transform.SetParent(transform, false);
                Component light = go.AddComponent(lightType);
                var prop = lightType.GetProperty("lightType");
                if (prop != null && prop.CanWrite) prop.SetValue(light, Enum.Parse(prop.PropertyType, "Global"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Plantation] No Global Light 2D: " + e.Message);
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();
    }
}
