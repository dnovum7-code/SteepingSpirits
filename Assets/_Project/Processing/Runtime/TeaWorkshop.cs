using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SteepingSpirits.Core;
using SteepingSpirits.Inventory;
using SteepingSpirits.Player;
using SteepingSpirits.World;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// Root of the tea workshop scene. Builds the room from the text layout (stations, shelf, store,
    /// door to the garden), player, camera, UI and feedback; owns the batches (Core Workshop) on game
    /// time; saves to workshop_save.json and writes processing_log.jsonl.
    /// Created by SteepingSpirits → Processing → Build Workshop.
    /// </summary>
    public class TeaWorkshop : MonoBehaviour
    {
        [SerializeField] private TextAsset layoutAsset;
        [SerializeField] private ProcessingSettings settings;
        [SerializeField] private ProcessingAudioSet audioSet;
        [SerializeField] private string gardenScenePath = "Assets/Scenes/Teegarten.unity";

        private readonly Dictionary<(int, int), StationView> stations = new Dictionary<(int, int), StationView>();
        private readonly Dictionary<BatchPlace, List<LeafBatch>> byPlace = new Dictionary<BatchPlace, List<LeafBatch>>();
        private readonly List<LeafBatch> scratch = new List<LeafBatch>();
        private float syncAt;
        private float sparkAt;
        private bool dirty;
        private float saveAt;
        private bool leaving;
        private bool quitting;
        private int wokSeed;
        private readonly Dictionary<int, TeaVariety> lastHeading = new Dictionary<int, TeaVariety>();
        private readonly HashSet<int> toldWithered = new HashSet<int>();
        private readonly HashSet<int> toldDry = new HashSet<int>();

        public WorkshopLayout Layout { get; private set; }
        public ProcessingSettings Settings => settings;
        public Workshop Model { get; private set; }
        public SkippableTime Time { get; private set; }
        public WorkshopSave State { get; private set; }
        public WorkshopUi Ui { get; private set; }
        public WorkshopFeedback Feedback { get; private set; }
        public WorkshopController Controller { get; private set; }
        public BlendStation Blending { get; set; }
        public SpiritView Spirit { get; private set; }

        /// <summary>Raised for every finished tea (telemetry, extras).</summary>
        public event Action<ProcessedTea> TeaFinished;

        public void Configure(TextAsset layout, ProcessingSettings s, ProcessingAudioSet audio, string gardenPath)
        {
            layoutAsset = layout;
            settings = s;
            audioSet = audio;
            gardenScenePath = gardenPath;
        }

        public int NextWokSeed() => ++wokSeed;

        private void Start()
        {
            if (FindAnyObjectByType<PlayerInventory>() == null) new GameObject("PlayerInventory").AddComponent<PlayerInventory>();
            if (FindAnyObjectByType<GameClock>() == null) new GameObject("GameClock").AddComponent<GameClock>();
            if (settings == null) settings = ProcessingSettings.CreateDefault();
            Layout = ParseLayout();

            Time = WorkshopClock.Create(GameClock.Instance);
            Model = new Workshop(settings.processing, Time);
            State = WorkshopSaveStore.Load() ?? new WorkshopSave
            {
                showPercent = settings.comfort.showPercentDefault,
                evenRolling = settings.comfort.evenRollingDefault
            };
            State.ApplyTo(Model);
            State.batches.Clear();
            wokSeed = State.nextId * 17;

            BuildRoom();
            Transform player = SetupPlayer();
            SetupCamera(player);
            EnsureGlobalLight2D();

            Feedback = new GameObject("Rückmeldung").AddComponent<WorkshopFeedback>();
            Feedback.transform.SetParent(transform, false);
            Feedback.Configure(settings.feedback, audioSet);
            Ui = gameObject.AddComponent<WorkshopUi>();
            Ui.Build();
            if (Blending == null) Blending = new BlendStation(this);
            Controller = gameObject.AddComponent<WorkshopController>();
            Controller.Init(this, player.GetComponent<PlayerController2D>());
            gameObject.AddComponent<WorkshopDebugOverlay>().Init(this);
            Spirit = new GameObject("Werkstattgeist").AddComponent<SpiritView>();
            Spirit.transform.SetParent(transform, false);
            Spirit.Init(StationCenter(WorkshopTile.Shelf) + Vector2.up * 0.9f);
            Spirit.SetAwake(SpiritAwakeNow());

            Model.Sync();
            RefreshViews();
            Save();
        }

        private void Update()
        {
            if (Model == null) return;
            if (UnityEngine.Time.unscaledTime >= syncAt)
            {
                syncAt = UnityEngine.Time.unscaledTime + Mathf.Max(0.05f, settings.comfort.syncSeconds);
                Model.Sync();
                TendSpirit();
                Notice();
                RefreshViews();
            }

            if (dirty && UnityEngine.Time.unscaledTime >= saveAt) Save();
        }

        private void OnApplicationQuit()
        {
            if (Model != null) Save();
            quitting = true;
        }

        private void OnDestroy()
        {
            if (Model != null && !leaving && !quitting) Save();
        }

        private WorkshopLayout ParseLayout()
        {
            if (layoutAsset != null)
            {
                try
                {
                    return WorkshopLayout.Parse(layoutAsset.text);
                }
                catch (FormatException e)
                {
                    Debug.LogWarning("[Processing] Layout broken (" + e.Message + ") – using the small fallback room.");
                }
            }

            return WorkshopLayout.Parse("@starter tea_leaf:6:3 tea_leaf_first:2:3\n#########\n#V.S.R.O#\n#.......#\n#.T.K.D.#\n#G.P....#\n#########\n");
        }

        // ---------------- Actions (called by the controller) ----------------

        public void StartBatch(LeafSource source, int amount)
        {
            int taken = WorkshopItems.TakeLeaves(source, amount);
            if (taken <= 0) return;
            LeafBatch b = Model.Add(source.sourceId, source.tier, taken, source.FirstFlush);
            Feedback.Play(WorkshopSound.Place);
            Ui.Message(ProcessingTexts.BatchStarted(b.id));
            MarkDirty();
        }

        public void StartHerbBatch(LeafSource source, int amount)
        {
            int taken = WorkshopItems.TakeLeaves(source, amount);
            if (taken <= 0) return;
            LeafBatch b = Model.AddHerb(source.sourceId, source.tier, taken);
            Feedback.Play(WorkshopSound.Place);
            Ui.Message(ProcessingTexts.BatchStarted(b.id));
            MarkDirty();
        }

        public void FinishHerb(int id)
        {
            if (!Model.FinishHerb(id, out DriedHerb h)) return;
            WorkshopDelivery where = WorkshopItems.DeliverHerb(h);
            Feedback.Play(WorkshopSound.Pack);
            Ui.Message(ProcessingTexts.HerbDone(h) + (where == WorkshopDelivery.Pantry ? ProcessingTexts.ToPantry : ""));
            RefreshViews();
            Save();
        }

        public void GiveStarterPack()
        {
            if (State.starterGiven) return;
            State.starterGiven = true;
            foreach ((string source, int count, int tier) in Layout.StarterPack()) WorkshopItems.GiveLeaves(source, tier, count);
            Feedback.Play(WorkshopSound.Rustle);
            Ui.Message(ProcessingTexts.StarterTaken);
            MarkDirty();
        }

        public void Apply(int id, BatchAction action)
        {
            BatchPlace before = Model.Get(id)?.place ?? BatchPlace.Shelf;
            if (!Model.Do(id, action)) return;
            LeafBatch b = Model.Get(id);
            if (action == BatchAction.Wither) Feedback.Play(WorkshopSound.Rustle);
            else if (action == BatchAction.TakeOut && before == BatchPlace.OxidationBasket)
            {
                Feedback.Play(WorkshopSound.Chime, 0.7f);
                Ui.Message(ProcessingTexts.Stopped + " " + ProcessingTexts.Heading(b, settings.processing));
            }
            else Feedback.Play(WorkshopSound.Place);
            Feedback.LeafBits(StationCenterOf(b.place == BatchPlace.Shelf ? before : b.place), LeafLook.Colour(b), 3);
            RefreshViews();
            MarkDirty();
        }

        public void ApplyRoll(int id, RollOutcome o)
        {
            if (Model.Roll(id, o)) MarkDirty();
            RefreshViews();
        }

        public void ApplyWok(int id, WokOutcome o)
        {
            if (Model.Fix(id, o)) MarkDirty();
            RefreshViews();
        }

        private bool SpiritAwakeNow()
        {
            SpiritParams p = settings.processing.spirit;
            return State.mastery.Wither(p) || State.mastery.Dry(p);
        }

        /// <summary>The workshop spirit takes over mastered passive steps.</summary>
        private void TendSpirit()
        {
            if (!SpiritAwakeNow()) return;
            List<SpiritDeed> deeds = WorkshopSpirit.Tend(Model, State.mastery, State.spiritWhite);
            foreach (SpiritDeed d in deeds)
            {
                WorkshopTile at = d.kind == SpiritDeedKind.TookOutOfDryer ? WorkshopTile.Dryer : WorkshopTile.Rack;
                Spirit.Visit(StationCenter(at), d.kind == SpiritDeedKind.MovedToDryer ? StationCenter(WorkshopTile.Dryer) : StationCenter(WorkshopTile.Shelf));
                Feedback.Play(WorkshopSound.Place, 0.6f);
                Ui.Message(ProcessingTexts.SpiritDid(d));
            }

            if (deeds.Count > 0) MarkDirty();
        }

        /// <summary>
        /// Passive steps call the player back once (leaves supple on the rack, tea dry in the basket),
        /// and an oxidizing batch says softly when its colour turns into the next tea.
        /// </summary>
        private void Notice()
        {
            ProcessingParams p = settings.processing;
            foreach (LeafBatch b in Model.Batches)
            {
                if (b.place == BatchPlace.OxidationBasket)
                {
                    TeaVariety now = TeaClassifier.Classify(b, p.variety);
                    if (lastHeading.TryGetValue(b.id, out TeaVariety was) && was != now)
                    {
                        Feedback.Play(WorkshopSound.Chime, 0.45f);
                        Ui.Message(ProcessingTexts.Turned(b, now));
                    }

                    lastHeading[b.id] = now;
                }
                else
                {
                    lastHeading.Remove(b.id);
                }

                if (b.place == BatchPlace.WitherRack && b.moisture <= p.wither.idealMax && toldWithered.Add(b.id))
                {
                    Feedback.Play(WorkshopSound.Rustle, 0.5f);
                    Ui.Message(ProcessingTexts.WitherReady(b));
                }

                if (b.place == BatchPlace.DryBasket && b.IsDry(p.dry) && toldDry.Add(b.id))
                {
                    Feedback.Play(WorkshopSound.Chime, 0.4f);
                    Ui.Message(ProcessingTexts.DryReady(b));
                }
            }
        }

        /// <summary>The player hands a batch on the rack to the spirit for the white-tea way.</summary>
        public void HandToSpirit(int id)
        {
            if (!State.spiritWhite.Contains(id)) State.spiritWhite.Add(id);
            MarkDirty();
        }

        public void FinishBatch(int id)
        {
            LeafBatch before = Model.Get(id);
            CareReport care = before != null ? TeaQuality.Evaluate(BatchRules.AdvanceTo(before, Model.Now, settings.processing), settings.processing) : default;
            bool wasAwake = SpiritAwakeNow();
            if (!Model.Finish(id, out ProcessedTea tea)) return;
            State.mastery.Record(tea, care, settings.processing.spirit);
            State.spiritWhite.Remove(id);
            if (!wasAwake && SpiritAwakeNow())
            {
                Spirit.SetAwake(true);
                Ui.Message(ProcessingTexts.SpiritAwake);
            }
            WorkshopDelivery where = WorkshopItems.DeliverTea(tea);
            State.Record(tea);
            WorkshopSaveStore.Log(tea, Model.Now);
            Feedback.Play(WorkshopSound.Pack);
            Feedback.LeafBits(StationCenter(WorkshopTile.Shelf), LeafLook.TeaColor(tea.variety), 6);
            if (wasAwake || !SpiritAwakeNow()) Ui.Message(ProcessingTexts.Finished(tea) + (where == WorkshopDelivery.Pantry ? ProcessingTexts.ToPantry : ""));
            TeaFinished?.Invoke(tea);
            RefreshViews();
            Save();
        }

        public void Leave()
        {
            if (leaving) return;
            if (!WorkshopSceneLoader.CanLoad(gardenScenePath))
            {
                Ui.Message(ProcessingTexts.NoGarden);
                return;
            }

            Save();
            leaving = true;
            WorkshopSceneLoader.Load(gardenScenePath);
        }

        // ---------------- Save ----------------

        public void MarkDirty()
        {
            if (!dirty) saveAt = UnityEngine.Time.unscaledTime + 1f;
            dirty = true;
        }

        public void Save()
        {
            if (Model == null || State == null) return;
            dirty = false;
            WorkshopSave s = WorkshopSave.From(Model);
            s.starterGiven = State.starterGiven;
            s.showPercent = State.showPercent;
            s.evenRolling = State.evenRolling;
            foreach (KeyValuePair<string, TeaLedgerEntry> kv in State.ledger) s.ledger[kv.Key] = kv.Value;
            s.mastery.goodWithers = State.mastery.goodWithers;
            s.mastery.dried = State.mastery.dried;
            s.mastery.goodWhites = State.mastery.goodWhites;
            foreach (int i in State.spiritWhite)
            {
                if (Model.Get(i) != null) s.spiritWhite.Add(i);
            }
            if (!WorkshopSaveStore.Save(s)) Ui?.Message(ProcessingTexts.SaveFailed);
        }

        // ---------------- Room ----------------

        public StationView StationAt(int x, int y) => stations.TryGetValue((x, y), out StationView s) ? s : null;

        public Vector2 StationCenter(WorkshopTile tile)
        {
            foreach (StationView s in stations.Values)
            {
                if (s.Tile == tile) return s.Center;
            }

            return Vector2.zero;
        }

        public void StationGlow(WorkshopTile tile, float amount)
        {
            foreach (StationView s in stations.Values)
            {
                if (s.Tile == tile) s.SetGlow(amount);
            }
        }

        private Vector2 StationCenterOf(BatchPlace place)
        {
            switch (place)
            {
                case BatchPlace.WitherRack: return StationCenter(WorkshopTile.Rack);
                case BatchPlace.OxidationBasket: return StationCenter(WorkshopTile.Basket);
                case BatchPlace.DryBasket: return StationCenter(WorkshopTile.Dryer);
                default: return StationCenter(WorkshopTile.Shelf);
            }
        }

        /// <summary>Leaf heaps on the stations follow the batches; embers glow while something dries.</summary>
        private void RefreshViews()
        {
            foreach (BatchPlace p in (BatchPlace[])Enum.GetValues(typeof(BatchPlace)))
            {
                if (!byPlace.TryGetValue(p, out List<LeafBatch> l)) byPlace[p] = l = new List<LeafBatch>();
                l.Clear();
            }

            foreach (LeafBatch b in Model.Batches) byPlace[b.place].Add(b);
            bool drying = byPlace[BatchPlace.DryBasket].Count > 0;
            Feedback.SetCrackle(drying);
            if (drying && UnityEngine.Time.time >= sparkAt)
            {
                sparkAt = UnityEngine.Time.time + 1.1f;
                Feedback.Spark(StationCenter(WorkshopTile.Dryer) + Vector2.down * 0.25f);
            }

            int rackIndex = 0;
            foreach (StationView s in stations.Values)
            {
                switch (s.Tile)
                {
                    case WorkshopTile.Shelf: s.Show(byPlace[BatchPlace.Shelf]); break;
                    case WorkshopTile.Basket: s.Show(byPlace[BatchPlace.OxidationBasket]); break;
                    case WorkshopTile.Dryer:
                        s.Show(byPlace[BatchPlace.DryBasket]);
                        s.SetGlow(drying ? 0.8f : 0.15f);
                        break;
                    case WorkshopTile.Rack:
                        // Several racks share the withering batches.
                        scratch.Clear();
                        List<LeafBatch> rack = byPlace[BatchPlace.WitherRack];
                        int racks = Math.Max(1, Layout.All(WorkshopTile.Rack).Count);
                        for (int i = rackIndex; i < rack.Count; i += racks) scratch.Add(rack[i]);
                        s.Show(scratch);
                        rackIndex++;
                        break;
                }
            }
        }

        private void BuildRoom()
        {
            Transform room = new GameObject("Werkstatt").transform;
            room.SetParent(transform, false);
            float w = Layout.Width, h = Layout.Height;
            Sprite(room, new Color(0.62f, 0.50f, 0.36f), new Vector2(w / 2f, h / 2f), new Vector2(w, h), -100, "Dielen");
            for (int i = 0; i < Layout.Height; i++)
            {
                Sprite(room, new Color(0.55f, 0.44f, 0.31f), new Vector2(w / 2f, i + 0.5f), new Vector2(w, 0.04f), -99, "Fuge");
            }

            for (int y = 0; y < Layout.Height; y++)
            {
                for (int x = 0; x < Layout.Width; x++)
                {
                    WorkshopTile t = Layout.At(x, y);
                    var c = new Vector2(x + 0.5f, y + 0.5f);
                    if (t == WorkshopTile.Wall)
                    {
                        SpriteRenderer wall = Sprite(room, new Color(0.40f, 0.30f, 0.22f), c, Vector2.one, -80, "Wand");
                        wall.gameObject.AddComponent<BoxCollider2D>();
                        continue;
                    }

                    if (t == WorkshopTile.Floor || t == WorkshopTile.Start) continue;
                    var go = new GameObject(t.ToString());
                    go.transform.SetParent(room, false);
                    go.transform.position = c;
                    StationView view = go.AddComponent<StationView>();
                    view.Build(t);
                    stations[(x, y)] = view;
                }
            }

            // Invisible fence one cell outside the room.
            Fence(room, new Vector2(w / 2f, -0.5f), new Vector2(w + 2f, 1f));
            Fence(room, new Vector2(w / 2f, h + 0.5f), new Vector2(w + 2f, 1f));
            Fence(room, new Vector2(-0.5f, h / 2f), new Vector2(1f, h + 2f));
            Fence(room, new Vector2(w + 0.5f, h / 2f), new Vector2(1f, h + 2f));
        }

        private static void Fence(Transform parent, Vector2 c, Vector2 size)
        {
            var go = new GameObject("Zaun (unsichtbar)");
            go.transform.SetParent(parent, false);
            go.transform.position = c;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        private static SpriteRenderer Sprite(Transform parent, Color color, Vector2 pos, Vector2 size, int order, string name) =>
            PlaceholderSprites.CreateSpriteObject(name, PlaceholderSprites.Square, color, pos, size, order, parent);

        private Transform SetupPlayer()
        {
            (int sx, int sy) = Layout.All(WorkshopTile.Start)[0];
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = new GameObject("Player") { tag = "Player" };
                var sr = player.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderSprites.Circle;
                sr.color = new Color(0.98f, 0.86f, 0.55f);
                player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            }

            player.transform.position = new Vector2(sx + 0.5f, sy + 0.5f);
            if (!player.TryGetComponent(out SortingGroup _)) player.AddComponent<SortingGroup>();
            Rigidbody2D body = player.TryGetComponent(out Rigidbody2D rb) ? rb : player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            if (!player.TryGetComponent(out Collider2D _)) player.AddComponent<CircleCollider2D>().radius = 0.38f;
            if (!player.TryGetComponent(out PlayerController2D _)) player.AddComponent<PlayerController2D>();
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
            cam.backgroundColor = new Color(0.14f, 0.10f, 0.08f);
            CameraFollow2D follow = cam.TryGetComponent(out CameraFollow2D f) ? f : cam.gameObject.AddComponent<CameraFollow2D>();
            follow.Target = player;
            follow.OrthographicSize = 5.5f;
            follow.SetBounds(new Rect(-1f, -1f, Layout.Width + 2f, Layout.Height + 2f));
            follow.SnapToTarget();
        }

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
                Debug.LogWarning("[Processing] No Global Light 2D: " + e.Message);
            }
        }
    }
}
