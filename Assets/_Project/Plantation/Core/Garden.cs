using System;
using System.Collections.Generic;

namespace SteepingSpirits.Plantation
{
    /// <summary>What one harvest gave.</summary>
    public struct HarvestResult
    {
        public string plantId;
        public string ingredientId;
        public int count;
        public HarvestQuality quality;

        /// <summary>Seeds that dropped (seed item id of the plant).</summary>
        public int seeds;

        public string seedItemId;
    }

    /// <summary>What applying an action did.</summary>
    public struct ActionOutcome
    {
        public ActionCheck check;
        public GridPos pos;

        /// <summary>Set when <see cref="ActionEffect.Harvest"/>.</summary>
        public HarvestResult harvest;

        /// <summary>Seed item that goes back to the player (digging up), else null.</summary>
        public string seedReturned;

        public bool Done => check.Valid;
    }

    /// <summary>What happened over one night (for feedback, telemetry and tests).</summary>
    public sealed class DayReport
    {
        /// <summary>The day that ended.</summary>
        public int day;

        /// <summary>Does it rain on the new day?</summary>
        public bool rainyNext;

        public int wateredCells;
        public readonly List<GridPos> grew = new List<GridPos>();
        public readonly List<GridPos> stageUps = new List<GridPos>();
        public readonly List<GridPos> newlyRipe = new List<GridPos>();
        public readonly List<GridPos> waited = new List<GridPos>();
        public readonly List<GridPos> reverted = new List<GridPos>();
    }

    /// <summary>
    /// The garden simulation: bed cells, the watering can and the nightly growth. Engine-free and
    /// deterministic – the same settings, seed and actions always produce the same garden.
    /// Nothing here ever kills a plant: neglect only costs growth days and quality.
    /// </summary>
    public sealed class Garden
    {
        private const int SaltYield = 0x59494c44;
        private const int SaltSeed = 0x53454544;

        public readonly GardenParams settings;
        public readonly PlantBook plants;
        private readonly IWeather weather;
        private readonly Dictionary<GridPos, Plot> plots = new Dictionary<GridPos, Plot>();
        private readonly List<GridPos> beds = new List<GridPos>();

        public int Day { get; private set; }
        public bool RainyToday { get; private set; }
        public int CanWater { get; set; }

        public IReadOnlyList<GridPos> Beds => beds;

        public Garden(IEnumerable<GridPos> bedCells, GardenParams settings, PlantBook plants, int startDay, IWeather weather = null)
        {
            this.settings = settings ?? new GardenParams();
            this.plants = plants ?? new PlantBook();
            this.weather = weather ?? new RainOracle(this.settings.worldSeed, this.settings.rainChance);
            foreach (GridPos p in bedCells)
            {
                if (plots.ContainsKey(p)) continue;
                plots[p] = new Plot { pos = p };
                beds.Add(p);
            }

            // Fixed processing order (row by row) keeps everything deterministic.
            beds.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            CanWater = this.settings.canCapacity;
            Day = startDay;
            StartMorning();
        }

        public static Garden FromLayout(GardenLayout layout, GardenParams settings, PlantBook plants, int startDay, IWeather weather = null) =>
            new Garden(layout.All(GardenTile.Bed), settings, plants, startDay, weather);

        /// <summary>The bed cell at p, or null if p is no bed.</summary>
        public Plot PlotAt(GridPos p) => plots.TryGetValue(p, out Plot plot) ? plot : null;

        public bool IsBed(GridPos p) => plots.ContainsKey(p);

        public ToolContext Context(string seedPlantId, int seedCount) =>
            new ToolContext { seedPlantId = plants.Has(seedPlantId) ? seedPlantId : null, seedCount = seedCount, canWater = CanWater };

        public ActionCheck Check(GridPos p, GardenAction action, string seedPlantId = null, int seedCount = 0) =>
            ActionRules.Check(PlotAt(p), action, Context(seedPlantId, seedCount));

        /// <summary>
        /// Does the action (if valid). Seeds are handled by the caller: remove one on Sow,
        /// give <see cref="ActionOutcome.seedReturned"/> back on DigUp and harvest seeds on Harvest.
        /// </summary>
        public ActionOutcome Apply(GridPos p, GardenAction action, string seedPlantId = null, int seedCount = 0)
        {
            var outcome = new ActionOutcome { pos = p, check = Check(p, action, seedPlantId, seedCount) };
            if (!outcome.check.Valid) return outcome;

            Plot plot = plots[p];
            switch (outcome.check.effect)
            {
                case ActionEffect.Till:
                    plot.soil = Soil.Tilled;
                    plot.idleDays = 0;
                    plot.watered = RainyToday;
                    break;

                case ActionEffect.Sow:
                    plot.crop = new Crop { plantId = seedPlantId, compost = plot.compost };
                    plot.compost = false;
                    plot.idleDays = 0;
                    break;

                case ActionEffect.Water:
                    plot.watered = true;
                    CanWater = Math.Max(0, CanWater - 1);
                    break;

                case ActionEffect.Harvest:
                    outcome.harvest = Harvest(plot);
                    break;

                case ActionEffect.Flatten:
                    plot.soil = Soil.Untouched;
                    plot.watered = false;
                    plot.idleDays = 0;
                    break;

                case ActionEffect.DigUp:
                    PlantParams dug = plants.Get(plot.crop.plantId);
                    outcome.seedReturned = dug != null ? dug.seedItemId : null;
                    plot.compost = plot.compost || (plot.crop.compost && plot.crop.harvests == 0);
                    plot.crop = null;
                    plot.idleDays = 0;
                    break;
            }

            return outcome;
        }

        /// <summary>Fill the can at the well. Returns the water added.</summary>
        public int Refill()
        {
            int added = settings.canCapacity - CanWater;
            CanWater = settings.canCapacity;
            return Math.Max(0, added);
        }

        /// <summary>Quality the crop would have if harvested now.</summary>
        public HarvestQuality ExpectedQuality(Crop crop)
        {
            PlantParams pp = crop != null ? plants.Get(crop.plantId) : null;
            if (pp == null) return HarvestQuality.Good;
            float ratio = crop.CareRatio;
            int tier = ratio >= settings.excellentCare ? 3 : ratio >= settings.fineCare ? 2 : 1;
            if (crop.harvests == 0)
            {
                tier += pp.firstHarvestQualityBonus;
                if (crop.compost) tier += settings.compostQualityBonus;
            }

            return (HarvestQuality)Math.Max(1, Math.Min(3, tier));
        }

        /// <summary>
        /// Garden help: should this cell be watered today? True when watering today still changes
        /// growth or quality (frugal plants watered yesterday don't need it).
        /// </summary>
        public bool WantsWater(GridPos p)
        {
            Plot plot = PlotAt(p);
            if (plot == null || plot.watered || plot.crop == null || plot.crop.ripe) return false;
            PlantParams pp = plants.Get(plot.crop.plantId);
            if (pp == null) return false;
            return pp.water != WaterNeed.Frugal || !plot.crop.wateredYesterday;
        }

        /// <summary>The night: plants grow, soil dries, the next morning starts (with rain or not).</summary>
        public DayReport EndDay()
        {
            var report = new DayReport { day = Day };
            foreach (GridPos p in beds)
            {
                Plot plot = plots[p];
                if (plot.watered) report.wateredCells++;
                if (plot.crop != null)
                {
                    plot.idleDays = 0;
                    Night(plot, report);
                }
                else if (plot.soil == Soil.Tilled)
                {
                    plot.idleDays++;
                    if (plot.idleDays >= Math.Max(1, settings.tilledDecayDays))
                    {
                        plot.soil = Soil.Untouched;
                        plot.idleDays = 0;
                        report.reverted.Add(p);
                    }
                }

                plot.watered = false;
            }

            Day++;
            StartMorning();
            report.rainyNext = RainyToday;
            return report;
        }

        /// <summary>Catch up nights that passed elsewhere (e.g. slept on the meadow) – they count as dry nights.</summary>
        public List<DayReport> CatchUpTo(int today)
        {
            var reports = new List<DayReport>();
            int nights = today - Day;
            if (nights <= 0) return reports;
            int processed = Math.Min(nights, Math.Max(1, settings.maxCatchUpDays));
            int skip = nights - processed;
            if (skip > 0)
            {
                Day += skip;
            }

            for (int i = 0; i < processed; i++) reports.Add(EndDay());
            return reports;
        }

        private void StartMorning()
        {
            RainyToday = weather.IsRainy(Day);
            if (!RainyToday) return;
            foreach (GridPos p in beds)
            {
                Plot plot = plots[p];
                if (plot.soil == Soil.Tilled) plot.watered = true;
            }
        }

        private void Night(Plot plot, DayReport report)
        {
            Crop c = plot.crop;
            bool wet = plot.watered;
            PlantParams pp = plants.Get(c.plantId);
            if (pp != null && !c.ripe)
            {
                bool grows;
                int points;
                switch (pp.water)
                {
                    case WaterNeed.Thirsty:
                        grows = wet;
                        points = wet ? 2 : 0;
                        break;

                    case WaterNeed.Frugal:
                        grows = true;
                        points = wet || c.wateredYesterday ? 2 : 0;
                        break;

                    default:
                        if (wet)
                        {
                            grows = true;
                            points = 2;
                        }
                        else if (!c.forgivenUsed)
                        {
                            grows = true;
                            points = 1;
                            c.forgivenUsed = true;
                        }
                        else
                        {
                            grows = false;
                            points = 0;
                        }

                        break;
                }

                c.cycleDays++;
                c.cyclePoints += points;
                if (grows)
                {
                    report.grew.Add(plot.pos);
                    c.progress++;
                    int needed = c.regrowing ? Math.Max(1, pp.regrowDays) : pp.StageDays(c.stage);
                    if (c.progress >= needed)
                    {
                        c.progress = 0;
                        c.forgivenUsed = false;
                        c.stage = c.regrowing ? pp.RipeStage : c.stage + 1;
                        c.regrowing = false;
                        report.stageUps.Add(plot.pos);
                        if (c.stage >= pp.RipeStage)
                        {
                            c.stage = pp.RipeStage;
                            c.ripe = true;
                            report.newlyRipe.Add(plot.pos);
                        }
                    }
                }
                else
                {
                    report.waited.Add(plot.pos);
                }
            }

            c.wateredYesterday = wet;
            c.waterHistory = ((c.waterHistory << 1) | (wet ? 1 : 0)) & 0xFF;
        }

        private HarvestResult Harvest(Plot plot)
        {
            Crop c = plot.crop;
            PlantParams pp = plants.Get(c.plantId);
            var result = new HarvestResult { plantId = c.plantId, quality = ExpectedQuality(c) };
            if (pp != null)
            {
                int seed = settings.worldSeed;
                result.ingredientId = pp.ingredientId;
                result.count = GardenRandom.Range(pp.yieldMin, pp.yieldMax, seed ^ SaltYield, Day, plot.pos.x, plot.pos.y, c.harvests);
                if (c.harvests == 0 && c.compost) result.count += settings.compostYieldBonus;
                result.seedItemId = pp.seedItemId;
                result.seeds = GardenRandom.Unit(seed ^ SaltSeed, Day, plot.pos.x, plot.pos.y, c.harvests) < pp.seedDropChance ? 1 : 0;
            }

            if (pp != null && pp.perennial)
            {
                c.harvests++;
                c.ripe = false;
                c.regrowing = true;
                c.stage = Math.Max(0, pp.RipeStage - 1);
                c.progress = 0;
                c.forgivenUsed = false;
                c.cycleDays = 0;
                c.cyclePoints = 0;
            }
            else
            {
                plot.crop = null;
                plot.idleDays = 0;
            }

            return result;
        }
    }
}
