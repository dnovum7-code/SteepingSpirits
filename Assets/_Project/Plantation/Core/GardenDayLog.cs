using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Telemetry of one garden day, written as one JSON line into garden_log.jsonl: what the player
    /// did, how many cells were wet, what was harvested and how long the round took.
    /// </summary>
    public sealed class GardenDayLog
    {
        public int day;
        public bool rainy;

        /// <summary>The night passed while the player was elsewhere (caught up later).</summary>
        public bool caughtUp;

        public readonly Dictionary<ActionEffect, int> actions = new Dictionary<ActionEffect, int>();
        public int wateredCells;
        public int harvests;
        public int pieces;
        public readonly int[] piecesByQuality = new int[4];
        public int seedsFound;
        public int growing;
        public int ripe;
        public int stageUps;

        /// <summary>Real seconds spent in the garden this day.</summary>
        public float secondsInGarden;

        /// <summary>Seconds from the first to the last tool use this day.</summary>
        public float activeSeconds;

        private float firstAction = -1f;

        public void Count(ActionOutcome o, float time)
        {
            if (!o.Done) return;
            actions.TryGetValue(o.check.effect, out int n);
            actions[o.check.effect] = n + 1;
            if (o.check.effect == ActionEffect.Harvest)
            {
                harvests++;
                pieces += o.harvest.count;
                piecesByQuality[(int)o.harvest.quality] += o.harvest.count;
                seedsFound += o.harvest.seeds;
            }

            if (firstAction < 0f) firstAction = time;
            activeSeconds = time - firstAction;
        }

        /// <summary>Takes the night's numbers (watered cells, growth) and the garden's state after it.</summary>
        public void Close(DayReport night, Garden garden)
        {
            wateredCells = night.wateredCells;
            stageUps = night.stageUps.Count;
            growing = 0;
            ripe = 0;
            foreach (GridPos p in garden.Beds)
            {
                Crop c = garden.PlotAt(p).crop;
                if (c == null) continue;
                if (c.ripe) ripe++;
                else growing++;
            }
        }

        public string ToJsonLine()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("{\"day\":").Append(day);
            sb.Append(",\"rainy\":").Append(rainy ? "true" : "false");
            sb.Append(",\"caughtUp\":").Append(caughtUp ? "true" : "false");
            sb.Append(",\"actions\":{");
            bool first = true;
            foreach (ActionEffect e in new[] { ActionEffect.Till, ActionEffect.Sow, ActionEffect.Water, ActionEffect.Harvest, ActionEffect.Flatten, ActionEffect.DigUp, ActionEffect.Compost })
            {
                if (!actions.TryGetValue(e, out int n) || n == 0) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append(MiniJson.Quote(e.ToString().ToLowerInvariant())).Append(':').Append(n);
            }

            sb.Append('}');
            sb.Append(",\"wateredCells\":").Append(wateredCells);
            sb.Append(",\"harvests\":").Append(harvests);
            sb.Append(",\"pieces\":").Append(pieces);
            sb.Append(",\"quality\":[").Append(piecesByQuality[1]).Append(',').Append(piecesByQuality[2]).Append(',').Append(piecesByQuality[3]).Append(']');
            sb.Append(",\"seedsFound\":").Append(seedsFound);
            sb.Append(",\"stageUps\":").Append(stageUps);
            sb.Append(",\"growing\":").Append(growing);
            sb.Append(",\"ripe\":").Append(ripe);
            sb.Append(",\"secondsInGarden\":").Append(secondsInGarden.ToString("0.0", inv));
            sb.Append(",\"activeSeconds\":").Append(activeSeconds.ToString("0.0", inv));
            return sb.Append('}').ToString();
        }
    }

    /// <summary>
    /// The seed chest refills a little: every few mornings it holds some seeds of each annual plant,
    /// so annual beds never run dry for good (seed drops alone return less than one seed per harvest).
    /// </summary>
    public static class SeedChest
    {
        /// <summary>Seeds (item id → count) the chest gets on the morning of "day"; empty on other days.</summary>
        public static void Restock(int day, GardenParams settings, PlantBook book, Dictionary<string, int> into)
        {
            into.Clear();
            if (settings.chestRestockDays <= 0 || settings.chestRestockAmount <= 0 || day <= 1 || day % settings.chestRestockDays != 0) return;
            foreach (PlantParams p in book.All)
            {
                if (!p.perennial) into[p.seedItemId] = settings.chestRestockAmount;
            }
        }
    }
}
