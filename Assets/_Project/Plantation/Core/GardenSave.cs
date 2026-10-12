using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Everything of the garden that is saved in garden_save.json (versioned): the cells, the can,
    /// the day, the seed chest and a backup of the player's seeds (restored only when the
    /// inventory comes up without any seeds, e.g. after a restart without inventory save).
    /// </summary>
    public sealed class GardenSave
    {
        public const int Version = 1;
        public const string FileName = "garden_save.json";

        public int day = 1;
        public int canWater;

        /// <summary>GameClock.TotalDays at the last save (0 = unknown). Nights passed on the clock since then are caught up.</summary>
        public int clockDay;
        public readonly List<Plot> plots = new List<Plot>();

        /// <summary>The starter pack was taken from the chest.</summary>
        public bool chestOpened;

        /// <summary>Seeds kept in the chest (e.g. when the bag was full), item id → count.</summary>
        public readonly Dictionary<string, int> chestSeeds = new Dictionary<string, int>();

        /// <summary>Seeds the player carried at the last save, item id → count.</summary>
        public readonly Dictionary<string, int> seedBackup = new Dictionary<string, int>();

        public bool wideCan;
        public bool gardenHelp = true;

        public static GardenSave From(Garden g)
        {
            var s = new GardenSave { day = g.Day, canWater = g.CanWater };
            foreach (GridPos p in g.Beds) s.plots.Add(g.PlotAt(p).Clone());
            return s;
        }

        public void ApplyTo(Garden g) => g.Restore(day, canWater, plots);

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":").Append(Version);
            sb.Append(",\"day\":").Append(day);
            sb.Append(",\"can\":").Append(canWater);
            sb.Append(",\"clockDay\":").Append(clockDay);
            sb.Append(",\"chestOpened\":").Append(B(chestOpened));
            sb.Append(",\"wideCan\":").Append(B(wideCan));
            sb.Append(",\"gardenHelp\":").Append(B(gardenHelp));
            sb.Append(",\"chestSeeds\":");
            Counts(sb, chestSeeds);
            sb.Append(",\"seedBackup\":");
            Counts(sb, seedBackup);
            sb.Append(",\"plots\":[");
            for (int i = 0; i < plots.Count; i++)
            {
                Plot p = plots[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n{\"x\":").Append(p.pos.x).Append(",\"y\":").Append(p.pos.y);
                sb.Append(",\"soil\":").Append((int)p.soil).Append(",\"wet\":").Append(B(p.watered));
                sb.Append(",\"idle\":").Append(p.idleDays).Append(",\"compost\":").Append(B(p.compost));
                if (p.crop != null)
                {
                    Crop c = p.crop;
                    sb.Append(",\"crop\":{\"plant\":").Append(MiniJson.Quote(c.plantId));
                    sb.Append(",\"stage\":").Append(c.stage).Append(",\"progress\":").Append(c.progress);
                    sb.Append(",\"ripe\":").Append(B(c.ripe)).Append(",\"harvests\":").Append(c.harvests);
                    sb.Append(",\"regrowing\":").Append(B(c.regrowing)).Append(",\"forgiven\":").Append(B(c.forgivenUsed));
                    sb.Append(",\"cycleDays\":").Append(c.cycleDays).Append(",\"cyclePoints\":").Append(c.cyclePoints);
                    sb.Append(",\"wetYesterday\":").Append(B(c.wateredYesterday)).Append(",\"history\":").Append(c.waterHistory);
                    sb.Append(",\"compost\":").Append(B(c.compost)).Append('}');
                }

                sb.Append('}');
            }

            return sb.Append("]}").ToString();
        }

        /// <summary>Reads a save; empty text gives a fresh save. Throws FormatException on broken or newer files.</summary>
        public static GardenSave FromJson(string json)
        {
            var s = new GardenSave();
            if (string.IsNullOrWhiteSpace(json)) return s;
            Dictionary<string, object> root = MiniJson.ParseObject(json);
            if (root == null) throw new FormatException("garden: not a JSON object");
            int version = Int(root, "version", 1);
            if (version > Version) throw new FormatException($"garden: version {version} is newer than {Version}");

            s.day = Int(root, "day", 1);
            s.canWater = Int(root, "can", 0);
            s.clockDay = Int(root, "clockDay", 0);
            s.chestOpened = Bool(root, "chestOpened");
            s.wideCan = Bool(root, "wideCan");
            s.gardenHelp = !root.ContainsKey("gardenHelp") || Bool(root, "gardenHelp");
            ReadCounts(root, "chestSeeds", s.chestSeeds);
            ReadCounts(root, "seedBackup", s.seedBackup);
            if (root.TryGetValue("plots", out object list) && list is List<object> items)
            {
                foreach (object o in items)
                {
                    if (!(o is Dictionary<string, object> d)) continue;
                    var p = new Plot
                    {
                        pos = new GridPos(Int(d, "x", 0), Int(d, "y", 0)),
                        soil = Int(d, "soil", 0) == 1 ? Soil.Tilled : Soil.Untouched,
                        watered = Bool(d, "wet"),
                        idleDays = Int(d, "idle", 0),
                        compost = Bool(d, "compost")
                    };
                    if (d.TryGetValue("crop", out object co) && co is Dictionary<string, object> c)
                    {
                        p.crop = new Crop
                        {
                            plantId = c.TryGetValue("plant", out object id) ? id as string ?? "" : "",
                            stage = Int(c, "stage", 0),
                            progress = Int(c, "progress", 0),
                            ripe = Bool(c, "ripe"),
                            harvests = Int(c, "harvests", 0),
                            regrowing = Bool(c, "regrowing"),
                            forgivenUsed = Bool(c, "forgiven"),
                            cycleDays = Int(c, "cycleDays", 0),
                            cyclePoints = Int(c, "cyclePoints", 0),
                            wateredYesterday = Bool(c, "wetYesterday"),
                            waterHistory = Int(c, "history", 0),
                            compost = Bool(c, "compost")
                        };
                    }

                    s.plots.Add(p);
                }
            }

            return s;
        }

        private static string B(bool b) => b ? "true" : "false";

        private static void Counts(StringBuilder sb, Dictionary<string, int> counts)
        {
            sb.Append('{');
            bool first = true;
            foreach (KeyValuePair<string, int> kv in counts)
            {
                if (kv.Value <= 0) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append(MiniJson.Quote(kv.Key)).Append(':').Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append('}');
        }

        private static void ReadCounts(Dictionary<string, object> root, string key, Dictionary<string, int> into)
        {
            if (!root.TryGetValue(key, out object o) || !(o is Dictionary<string, object> d)) return;
            foreach (KeyValuePair<string, object> kv in d)
            {
                if (kv.Value is double n && n > 0) into[kv.Key] = (int)n;
            }
        }

        private static int Int(Dictionary<string, object> d, string key, int fallback) =>
            d.TryGetValue(key, out object o) && o is double n ? (int)n : fallback;

        private static bool Bool(Dictionary<string, object> d, string key) => d.TryGetValue(key, out object o) && o is bool b && b;
    }
}
