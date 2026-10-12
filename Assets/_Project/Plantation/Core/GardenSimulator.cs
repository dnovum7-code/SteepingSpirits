using System;
using System.Collections.Generic;

namespace SteepingSpirits.Plantation
{
    /// <summary>How a simulated player looks after the garden.</summary>
    public enum BotStyle
    {
        /// <summary>Comes daily, waters everything the garden help asks for, harvests and replants.</summary>
        Careful,

        /// <summary>Comes on about 70 % of the days and then waters only about half of what needs it.</summary>
        Sloppy,

        /// <summary>Comes daily, harvests and replants, but never waters.</summary>
        NeverWater
    }

    /// <summary>Time model for one garden round (seconds).</summary>
    [Serializable]
    public sealed class RoundCost
    {
        /// <summary>Player walk speed in cells per second (PlayerController2D: 4.5).</summary>
        public float walkSpeed = 4.5f;

        /// <summary>One tool use including its short animation.</summary>
        public float actionSeconds = 0.35f;

        /// <summary>Switching tools (number key).</summary>
        public float toolSwitchSeconds = 0.15f;

        public float refillSeconds = 0.8f;
        public float endDaySeconds = 1.5f;
    }

    /// <summary>Numbers of one simulated run.</summary>
    public sealed class SimResult
    {
        public BotStyle style;
        public int days;
        public int harvests;
        public int pieces;

        /// <summary>Pieces per quality tier (index 1..3).</summary>
        public readonly int[] piecesByQuality = new int[4];

        public readonly Dictionary<string, int> piecesByPlant = new Dictionary<string, int>();
        public int waterings;
        public int refills;
        public int actions;
        public int seedsFound;
        public int seedsFromChest;
        public int seedsLeft;

        /// <summary>Plants that vanished without being harvested or dug up (must stay 0).</summary>
        public int plantsLost;

        public int plantsAtEnd;
        public readonly List<float> roundSeconds = new List<float>();

        public float MaxRound
        {
            get
            {
                float m = 0f;
                foreach (float s in roundSeconds) m = Math.Max(m, s);
                return m;
            }
        }

        public float AverageRound
        {
            get
            {
                if (roundSeconds.Count == 0) return 0f;
                float sum = 0f;
                foreach (float s in roundSeconds) sum += s;
                return sum / roundSeconds.Count;
            }
        }

        public float QualityScore => pieces == 0 ? 0f : (piecesByQuality[1] + 2f * piecesByQuality[2] + 3f * piecesByQuality[3]) / pieces;

        public override string ToString() =>
            $"{style,-10} harvests {harvests,3}  pieces {pieces,4}  q1/q2/q3 {piecesByQuality[1]}/{piecesByQuality[2]}/{piecesByQuality[3]}" +
            $"  avgQ {QualityScore:0.00}  waterings {waterings,4}  seeds found {seedsFound,3} chest {seedsFromChest,3} left {seedsLeft,3}" +
            $"  planted {plantsAtEnd}  lost {plantsLost}  round avg {AverageRound:0}s max {MaxRound:0}s";
    }

    /// <summary>
    /// Plays the garden for a number of days with a bot, walking the real layout, to check
    /// tuning (care pays off, neglect never kills) and how long a daily round takes.
    /// </summary>
    public static class GardenSimulator
    {
        private const int SaltVisit = 0x56495354;
        private const int SaltWater = 0x57415452;

        public static SimResult Run(GardenLayout layout, GardenParams settings, PlantBook book, BotStyle style, int days,
            RoundCost cost = null, bool wideCan = false)
        {
            cost = cost ?? new RoundCost();
            var garden = Garden.FromLayout(layout, settings, book, 1);
            var result = new SimResult { style = style, days = days };
            var seeds = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> s in layout.SeedPack()) seeds[s.Key] = s.Value;
            Dictionary<GridPos, string> wish = Assign(layout);

            GridPos start = layout.All(GardenTile.Start)[0];
            List<GridPos> wells = layout.All(GardenTile.Well);
            List<GridPos> lanterns = layout.All(GardenTile.Lantern);
            List<GridPos> chests = layout.All(GardenTile.Chest);
            var inChest = new Dictionary<string, int>();
            var restock = new Dictionary<string, int>();
            int seed = settings.worldSeed;

            for (int d = 0; d < days; d++)
            {
                var before = new Dictionary<GridPos, Crop>();
                foreach (GridPos b in garden.Beds) before[b] = garden.PlotAt(b).crop;

                bool visits = style != BotStyle.Sloppy || d == 0 || GardenRandom.Unit(seed ^ SaltVisit, garden.Day) < 0.7f;
                var removedOnPurpose = new HashSet<GridPos>();
                if (visits)
                {
                    var round = new Round(layout, garden, cost, start);
                    if (inChest.Count > 0 && chests.Count > 0)
                    {
                        round.WalkToUse(chests);
                        foreach (KeyValuePair<string, int> kv in inChest)
                        {
                            string plantOf = book.BySeed(kv.Key)?.id;
                            if (plantOf == null) continue;
                            seeds.TryGetValue(plantOf, out int had);
                            seeds[plantOf] = had + kv.Value;
                            result.seedsFromChest += kv.Value;
                        }

                        inChest.Clear();
                    }
                    foreach (GridPos b in Order(layout, garden.Beds, start))
                    {
                        Plot plot = garden.PlotAt(b);
                        var todo = new List<GardenAction>();
                        if (plot.crop != null && plot.crop.ripe) todo.Add(GardenAction.Harvest);
                        bool willBeEmpty = plot.crop == null || (plot.crop.ripe && !(book.Get(plot.crop.plantId)?.perennial ?? false));
                        string plant = wish[b];
                        if (willBeEmpty && seeds.TryGetValue(plant, out int have) && have > 0)
                        {
                            if (plot.soil == Soil.Untouched) todo.Add(GardenAction.Hoe);
                            todo.Add(GardenAction.Sow);
                        }

                        foreach (GardenAction a in todo)
                        {
                            round.Act(b, a);
                            seeds.TryGetValue(plant, out int count);
                            ActionOutcome o = garden.Apply(b, a, plant, count);
                            if (!o.Done) continue;
                            result.actions++;
                            if (a == GardenAction.Sow) seeds[plant] = count - 1;
                            if (a == GardenAction.Harvest)
                            {
                                result.harvests++;
                                result.pieces += o.harvest.count;
                                result.piecesByQuality[(int)o.harvest.quality] += o.harvest.count;
                                result.piecesByPlant.TryGetValue(o.harvest.plantId, out int pp);
                                result.piecesByPlant[o.harvest.plantId] = pp + o.harvest.count;
                                if (o.harvest.seeds > 0)
                                {
                                    result.seedsFound += o.harvest.seeds;
                                    seeds.TryGetValue(o.harvest.plantId, out int sc);
                                    seeds[o.harvest.plantId] = sc + o.harvest.seeds;
                                }

                                if (garden.PlotAt(b).crop == null) removedOnPurpose.Add(b);
                            }
                        }
                    }

                    if (style != BotStyle.NeverWater)
                    {
                        var thirsty = new List<GridPos>();
                        foreach (GridPos b in garden.Beds)
                        {
                            if (!garden.WantsWater(b)) continue;
                            if (style == BotStyle.Sloppy && GardenRandom.Unit(seed ^ SaltWater, garden.Day, b.x, b.y) >= 0.5f) continue;
                            thirsty.Add(b);
                        }

                        var done = new HashSet<GridPos>();
                        foreach (GridPos b in Order(layout, thirsty, round.Position))
                        {
                            if (done.Contains(b)) continue;
                            if (garden.CanWater == 0)
                            {
                                round.WalkToUse(wells);
                                round.Spend(cost.refillSeconds);
                                garden.Refill();
                                result.refills++;
                            }

                            GridPos dir = round.Act(b, GardenAction.Water);
                            int reach = wideCan ? 3 : 1;
                            for (int i = 0; i < reach && garden.CanWater > 0; i++)
                            {
                                GridPos c = b + dir * i;
                                if (i > 0 && (!thirsty.Contains(c) || done.Contains(c))) break;
                                if (garden.Apply(c, GardenAction.Water).Done)
                                {
                                    result.waterings++;
                                    done.Add(c);
                                }
                            }

                            result.actions++;
                        }
                    }

                    round.WalkToUse(lanterns);
                    round.Spend(cost.endDaySeconds);
                    result.roundSeconds.Add(round.Seconds);
                }

                garden.EndDay();
                SeedChest.Restock(garden.Day, settings, book, restock);
                foreach (KeyValuePair<string, int> kv in restock)
                {
                    inChest.TryGetValue(kv.Key, out int c);
                    inChest[kv.Key] = c + kv.Value;
                }

                foreach (KeyValuePair<GridPos, Crop> kv in before)
                {
                    if (kv.Value != null && garden.PlotAt(kv.Key).crop == null && !removedOnPurpose.Contains(kv.Key)) result.plantsLost++;
                }
            }

            foreach (GridPos b in garden.Beds)
            {
                if (garden.PlotAt(b).crop != null) result.plantsAtEnd++;
            }

            foreach (int s in seeds.Values) result.seedsLeft += s;
            return result;
        }

        /// <summary>The plant the layout suggests for each bed cell (by block, "@blocks").</summary>
        public static Dictionary<GridPos, string> Assign(GardenLayout layout)
        {
            string[] names = layout.Setting("blocks").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var map = new Dictionary<GridPos, string>();
            List<List<GridPos>> blocks = layout.BedBlocks();
            for (int i = 0; i < blocks.Count; i++)
            {
                string plant = names.Length > 0 ? names[i % names.Length] : StarterPlants.Ids[i % StarterPlants.Ids.Length];
                foreach (GridPos p in blocks[i]) map[p] = plant;
            }

            return map;
        }

        /// <summary>Greedy nearest-next order through the cells, starting near "from".</summary>
        private static List<GridPos> Order(GardenLayout layout, IEnumerable<GridPos> cells, GridPos from)
        {
            var left = new List<GridPos>(cells);
            var order = new List<GridPos>();
            GridPos at = from;
            while (left.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < left.Count; i++)
                {
                    int a = left[i].Manhattan(at), b = left[best].Manhattan(at);
                    if (a < b || (a == b && (left[i].y > left[best].y || (left[i].y == left[best].y && left[i].x < left[best].x)))) best = i;
                }

                at = left[best];
                order.Add(at);
                left.RemoveAt(best);
            }

            return order;
        }

        /// <summary>Walks the player through one day and adds up the time.</summary>
        private sealed class Round
        {
            private readonly GardenLayout layout;
            private readonly Garden garden;
            private readonly RoundCost cost;
            private GardenAction? tool;

            public GridPos Position { get; private set; }
            public float Seconds { get; private set; }

            public Round(GardenLayout layout, Garden garden, RoundCost cost, GridPos start)
            {
                this.layout = layout;
                this.garden = garden;
                this.cost = cost;
                Position = start;
            }

            public void Spend(float s) => Seconds += s;

            /// <summary>Walk next to the target, face it, use the tool. Returns the facing direction.</summary>
            public GridPos Act(GridPos target, GardenAction action)
            {
                GridPos dir = WalkNextTo(new[] { target });
                if (tool != action)
                {
                    if (tool != null) Seconds += cost.toolSwitchSeconds;
                    tool = action;
                }

                Seconds += cost.actionSeconds;
                return dir;
            }

            public void WalkToUse(List<GridPos> objects)
            {
                WalkNextTo(objects);
                Seconds += cost.actionSeconds;
            }

            private GridPos WalkNextTo(IList<GridPos> targets)
            {
                // Stand on a walkable neighbour of a target, facing it.
                var stands = new List<GridPos>();
                var facing = new Dictionary<GridPos, GridPos>();
                foreach (GridPos t in targets)
                {
                    foreach (GridPos d in new[] { GridPos.Down, GridPos.Left, GridPos.Right, GridPos.Up })
                    {
                        GridPos s = t + d * -1;
                        if (!layout.IsWalkable(s) || facing.ContainsKey(s)) continue;
                        stands.Add(s);
                        facing[s] = d;
                    }
                }

                int[,] dist = layout.DistanceField(stands);
                int steps = dist[Position.x, Position.y];
                if (steps == int.MaxValue) return GridPos.Up;

                // Follow the gradient to find which stand cell we arrive at.
                GridPos at = Position;
                while (dist[at.x, at.y] > 0)
                {
                    foreach (GridPos d in new[] { GridPos.Up, GridPos.Down, GridPos.Left, GridPos.Right })
                    {
                        GridPos n = at + d;
                        if (layout.IsWalkable(n) && dist[n.x, n.y] == dist[at.x, at.y] - 1)
                        {
                            at = n;
                            break;
                        }
                    }
                }

                Seconds += steps / Math.Max(0.1f, cost.walkSpeed);
                Position = at;
                return facing[at];
            }
        }
    }
}
