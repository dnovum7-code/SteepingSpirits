using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SteepingSpirits.Core;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Shows the bed cells: soil (untouched / tilled / darker when watered, compost specks),
    /// the plant per stage (definition sprite or soft placeholder shapes), a slow sparkle on
    /// ripe plants, the garden-help drops and the target-cell preview. Plants grow with a small
    /// "pop" after each night. Everything is gentle; nothing flashes.
    /// </summary>
    public class GardenView : MonoBehaviour
    {
        private static readonly Color SoilUntouched = new Color(0.56f, 0.47f, 0.32f);
        private static readonly Color SoilTilled = new Color(0.44f, 0.31f, 0.19f);
        private static readonly Color SoilWet = new Color(0.29f, 0.20f, 0.13f);
        private static readonly Color Furrow = new Color(0f, 0f, 0f, 0.16f);
        private static readonly Color CompostColor = new Color(0.20f, 0.14f, 0.08f, 0.85f);
        private static readonly Color HelpDrop = new Color(0.55f, 0.78f, 1f, 0.85f);
        private static readonly Color CursorValid = new Color(1f, 0.95f, 0.8f, 0.75f);
        private static readonly Color CursorIdle = new Color(0.85f, 0.85f, 0.85f, 0.3f);

        private sealed class Cell
        {
            public GridPos pos;
            public SpriteRenderer soil;
            public SpriteRenderer furrowA;
            public SpriteRenderer furrowB;
            public SpriteRenderer compost;
            public Transform plantRoot;
            public SpriteRenderer stem;
            public SpriteRenderer crown;
            public SpriteRenderer sparkle;
            public SpriteRenderer help;
            public float pop;
            public float phase;
            public bool ripe;
        }

        private Garden garden;
        private GardenSettings settings;
        private GardenFeedbackParams tuning;
        private readonly List<Cell> cells = new List<Cell>();
        private readonly Dictionary<GridPos, int> index = new Dictionary<GridPos, int>();
        private readonly SpriteRenderer[] cursorBars = new SpriteRenderer[12];
        private bool helpOn;

        public static Vector2 CellCenter(GridPos p) => new Vector2(p.x + 0.5f, p.y + 0.5f);

        public static GridPos CellAt(Vector2 world) => new GridPos(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));

        public void Build(Garden garden, GardenSettings settings)
        {
            this.garden = garden;
            this.settings = settings;
            tuning = settings.feedback ?? new GardenFeedbackParams();
            foreach (GridPos p in garden.Beds)
            {
                Vector2 c = CellCenter(p);
                var cell = new Cell { pos = p, phase = (p.x * 0.37f + p.y * 0.61f) % 1f };
                cell.soil = PlaceholderSprites.CreateSpriteObject("Beet " + p, PlaceholderSprites.Square, SoilUntouched, c, new Vector2(0.94f, 0.94f), -40, transform);
                cell.furrowA = PlaceholderSprites.CreateSpriteObject("Furche", PlaceholderSprites.Square, Furrow, c + new Vector2(0f, 0.2f), new Vector2(0.8f, 0.07f), -39, transform);
                cell.furrowB = PlaceholderSprites.CreateSpriteObject("Furche", PlaceholderSprites.Square, Furrow, c + new Vector2(0f, -0.18f), new Vector2(0.8f, 0.07f), -39, transform);
                cell.compost = PlaceholderSprites.CreateSpriteObject("Kompost", PlaceholderSprites.Diamond, CompostColor, c + new Vector2(-0.22f, -0.25f), new Vector2(0.22f, 0.16f), -38, transform);

                var root = new GameObject("Pflanze " + p);
                root.transform.SetParent(transform, false);
                root.transform.position = c + new Vector2(0f, -0.3f);
                root.AddComponent<SortingGroup>();
                cell.plantRoot = root.transform;
                cell.stem = PlaceholderSprites.CreateSpriteObject("Blatt", PlaceholderSprites.Triangle, Color.green, Vector2.zero, Vector2.one, 0, root.transform);
                cell.crown = PlaceholderSprites.CreateSpriteObject("Blüte", PlaceholderSprites.Circle, Color.white, Vector2.zero, Vector2.one, 1, root.transform);
                cell.sparkle = PlaceholderSprites.CreateSpriteObject("Glanz", PlaceholderSprites.Diamond, new Color(1f, 1f, 0.9f, 0f), Vector2.zero, new Vector2(0.12f, 0.12f), 2, root.transform);
                cell.help = PlaceholderSprites.CreateSpriteObject("Gartenhilfe", PlaceholderSprites.Circle, HelpDrop, c + new Vector2(0.3f, 0.32f), new Vector2(0.14f, 0.18f), 30, transform);

                index[p] = cells.Count;
                cells.Add(cell);
            }

            for (int i = 0; i < cursorBars.Length; i++)
            {
                cursorBars[i] = PlaceholderSprites.CreateSpriteObject("Zielfeld", PlaceholderSprites.Square, CursorValid, Vector2.zero, Vector2.one, 35, transform);
                cursorBars[i].enabled = false;
            }

            RefreshAll();
        }

        public void SetHelp(bool on)
        {
            helpOn = on;
            for (int i = 0; i < cells.Count; i++) RefreshHelp(cells[i]);
        }

        public void RefreshAll()
        {
            for (int i = 0; i < cells.Count; i++) Refresh(cells[i]);
        }

        public void Refresh(GridPos p)
        {
            if (index.TryGetValue(p, out int i)) Refresh(cells[i]);
        }

        /// <summary>After a night: everything that grew pops softly to its new size.</summary>
        public void PlayNight(DayReport report)
        {
            RefreshAll();
            foreach (GridPos p in report.grew)
            {
                if (index.TryGetValue(p, out int i)) cells[i].pop = Mathf.Max(0.01f, tuning.growPopSeconds);
            }
        }

        /// <summary>Target preview: up to three cells, warm when the tool does something, faint grey when not.</summary>
        public void SetCursor(IList<GridPos> targets, int count, bool valid)
        {
            Color color = valid ? CursorValid : CursorIdle;
            float pulse = valid ? 0.85f + 0.15f * Mathf.Sin(Time.time * 4f) : 1f;
            color.a *= pulse;
            for (int k = 0; k < 3; k++)
            {
                bool on = k < count;
                Vector2 c = on ? CellCenter(targets[k]) : Vector2.zero;
                for (int b = 0; b < 4; b++)
                {
                    SpriteRenderer bar = cursorBars[k * 4 + b];
                    bar.enabled = on;
                    if (!on) continue;
                    bar.color = color;
                    bool horizontal = b < 2;
                    float sign = b % 2 == 0 ? 1f : -1f;
                    bar.transform.position = c + (horizontal ? new Vector2(0f, 0.47f * sign) : new Vector2(0.47f * sign, 0f));
                    bar.transform.localScale = horizontal ? new Vector3(1f, 0.06f, 1f) : new Vector3(0.06f, 1f, 1f);
                }
            }
        }

        private void Refresh(Cell cell)
        {
            Plot plot = garden.PlotAt(cell.pos);
            if (plot == null) return;
            bool tilled = plot.soil == Soil.Tilled;
            cell.soil.color = !tilled ? SoilUntouched : plot.watered ? SoilWet : SoilTilled;
            cell.furrowA.enabled = tilled;
            cell.furrowB.enabled = tilled;
            cell.compost.enabled = plot.compost || (plot.crop != null && plot.crop.compost && plot.crop.harvests == 0);

            Crop crop = plot.crop;
            PlantParams pp = crop != null ? garden.plants.Get(crop.plantId) : null;
            cell.plantRoot.gameObject.SetActive(pp != null);
            cell.ripe = pp != null && crop.ripe;
            if (pp != null) ShowPlant(cell, pp, crop);
            RefreshHelp(cell);
        }

        private void RefreshHelp(Cell cell) => cell.help.enabled = helpOn && garden.WantsWater(cell.pos);

        private void ShowPlant(Cell cell, PlantParams pp, Crop crop)
        {
            PlantDefinition def = settings.Definition(pp.id);
            Color leaf = def != null ? def.leafColor : PlantPalette.Leaf(pp.id);
            Color ripe = def != null ? def.ripeColor : PlantPalette.Ripe(pp.id);
            int stage = crop.ripe ? pp.RipeStage : crop.stage;
            Sprite custom = def != null ? def.SpriteFor(stage) : null;

            if (custom != null)
            {
                cell.stem.sprite = custom;
                cell.stem.color = Color.white;
                cell.stem.transform.localScale = Vector3.one;
                cell.stem.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                cell.crown.enabled = false;
                cell.sparkle.transform.localPosition = new Vector3(0.18f, 0.6f, 0f);
                return;
            }

            float grown = pp.RipeStage <= 0 ? 1f : Mathf.Clamp01(stage / (float)pp.RipeStage);
            if (stage == 0)
            {
                // A seed: a small light mound.
                cell.stem.sprite = PlaceholderSprites.Circle;
                cell.stem.color = new Color(0.62f, 0.5f, 0.33f);
                cell.stem.transform.localScale = new Vector3(0.22f, 0.14f, 1f);
                cell.stem.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                cell.crown.enabled = false;
                return;
            }

            float h = Mathf.Lerp(0.28f, 0.82f, grown);
            Vector2 shape = ShapeOf(pp.id, out Sprite sprite);
            cell.stem.sprite = sprite;
            cell.stem.color = leaf;
            cell.stem.transform.localScale = new Vector3(h * shape.x, h * shape.y, 1f);
            cell.stem.transform.localPosition = new Vector3(0f, h * shape.y * 0.5f + 0.05f, 0f);

            bool showCrown = crop.ripe;
            cell.crown.enabled = showCrown;
            if (showCrown)
            {
                float top = h * shape.y + 0.05f;
                cell.crown.color = ripe;
                cell.crown.transform.localScale = new Vector3(0.3f, 0.26f, 1f);
                cell.crown.transform.localPosition = new Vector3(0f, top - 0.08f, 0f);
            }

            cell.sparkle.transform.localPosition = new Vector3(0.2f, h * shape.y, 0f);
        }

        /// <summary>Placeholder silhouette per plant (width, height factors).</summary>
        private static Vector2 ShapeOf(string plantId, out Sprite sprite)
        {
            switch (plantId)
            {
                case StarterPlants.Tea:
                case StarterPlants.LemonBalm:
                    sprite = PlaceholderSprites.Circle;
                    return new Vector2(1f, 0.85f);
                case StarterPlants.Jasmine:
                    sprite = PlaceholderSprites.Diamond;
                    return new Vector2(0.8f, 1.05f);
                case StarterPlants.Lavender:
                    sprite = PlaceholderSprites.Triangle;
                    return new Vector2(0.45f, 1.15f);
                default:
                    sprite = PlaceholderSprites.Triangle;
                    return new Vector2(0.8f, 0.9f);
            }
        }

        private void Update()
        {
            float now = Time.time;
            float dt = Time.deltaTime;
            float period = Mathf.Max(0.5f, tuning != null ? tuning.sparklePeriod : 2.6f);
            float alpha = tuning != null ? tuning.sparkleAlpha : 0.5f;
            float popFrom = tuning != null ? tuning.growPopFrom : 0.82f;
            float popSeconds = tuning != null ? Mathf.Max(0.01f, tuning.growPopSeconds) : 0.5f;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell.pop > 0f)
                {
                    cell.pop = Mathf.Max(0f, cell.pop - dt);
                    float t = 1f - cell.pop / popSeconds;
                    float s = Mathf.Lerp(popFrom, 1f, 1f - (1f - t) * (1f - t)) + Mathf.Sin(t * Mathf.PI) * 0.06f;
                    cell.plantRoot.localScale = new Vector3(s, s, 1f);
                }

                if (cell.ripe)
                {
                    float w = Mathf.Sin(((now / period) + cell.phase) * Mathf.PI * 2f);
                    Color c = cell.sparkle.color;
                    c.a = alpha * Mathf.Clamp01(w);
                    cell.sparkle.color = c;
                    cell.sparkle.transform.localRotation = Quaternion.Euler(0f, 0f, now * 40f + cell.phase * 360f);
                }
                else if (cell.sparkle.color.a > 0f)
                {
                    Color c = cell.sparkle.color;
                    c.a = 0f;
                    cell.sparkle.color = c;
                }

                if (cell.help.enabled)
                {
                    Vector2 c = CellCenter(cell.pos);
                    cell.help.transform.position = c + new Vector2(0.3f, 0.32f + Mathf.Sin(now * 2f + cell.phase * 6f) * 0.04f);
                }
            }
        }
    }
}
