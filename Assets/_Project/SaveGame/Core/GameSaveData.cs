using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.SaveGame
{
    /// <summary>One inventory entry: item id and amount (slot order is kept).</summary>
    public struct ItemEntry
    {
        public string id;
        public int amount;

        public ItemEntry(string id, int amount)
        {
            this.id = id;
            this.amount = amount;
        }
    }

    /// <summary>The game clock at the moment of saving.</summary>
    public struct ClockEntry
    {
        public int day;
        public int season;
        public int year;
        public int minuteOfDay;
    }

    /// <summary>
    /// game_save.json – the shared save of the systems every module uses: the player inventory
    /// (including items created at runtime: teas, blends, dried herbs, leaf variants, seeds) and the
    /// game clock. Versioned; version 1 is the old inventory_save.json of PlayerInventory, which is
    /// migrated. Entries whose item cannot be rebuilt yet stay in <see cref="pending"/> and are written
    /// again on every save – nothing is lost (like the pantry).
    /// Module saves (garden, workshop, jump'n'run, pantry) keep their own files.
    /// </summary>
    public sealed class GameSaveData
    {
        public const int Version = 2;
        public const string FileName = "game_save.json";
        public const string LegacyInventoryFileName = "inventory_save.json";

        public readonly List<ItemEntry> inventory = new List<ItemEntry>();

        /// <summary>Items that could not be put back (unknown id, bag full) – kept for later.</summary>
        public readonly List<ItemEntry> pending = new List<ItemEntry>();

        public bool hasClock;
        public ClockEntry clock;

        /// <summary>Version the data was read from (1 = migrated legacy inventory file).</summary>
        public int loadedVersion = Version;

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":").Append(Version);
            sb.Append(",\"inventory\":");
            Entries(sb, inventory);
            sb.Append(",\"pending\":");
            Entries(sb, pending);
            if (hasClock)
            {
                sb.Append(",\"clock\":{\"day\":").Append(clock.day).Append(",\"season\":").Append(clock.season)
                    .Append(",\"year\":").Append(clock.year).Append(",\"minuteOfDay\":").Append(clock.minuteOfDay).Append('}');
            }

            return sb.Append('}').ToString();
        }

        private static void Entries(StringBuilder sb, List<ItemEntry> list)
        {
            sb.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("\n{\"id\":").Append(MiniJson.Quote(list[i].id)).Append(",\"amount\":").Append(list[i].amount.ToString(CultureInfo.InvariantCulture)).Append('}');
            }

            sb.Append(']');
        }

        /// <summary>Reads a save. Empty text → empty save; newer versions or broken JSON throw FormatException.</summary>
        public static GameSaveData FromJson(string json)
        {
            var s = new GameSaveData();
            if (string.IsNullOrWhiteSpace(json)) return s;
            Dictionary<string, object> root = MiniJson.ParseObject(json);
            if (root == null) throw new FormatException("game save: not a JSON object");
            int version = root.TryGetValue("version", out object v) && v is double d ? (int)d : 1;
            if (version > Version) throw new FormatException($"game save: version {version} is newer than {Version}");

            // Version 1 (or no version) in this file: the old PlayerInventory layout.
            if (!root.ContainsKey("inventory") && root.ContainsKey("itemIDs")) return FromLegacyInventory(root);

            s.loadedVersion = version;
            ReadEntries(root, "inventory", s.inventory);
            ReadEntries(root, "pending", s.pending);
            if (root.TryGetValue("clock", out object co) && co is Dictionary<string, object> c)
            {
                s.hasClock = true;
                s.clock = new ClockEntry { day = Int(c, "day", 1), season = Int(c, "season", 0), year = Int(c, "year", 1), minuteOfDay = Int(c, "minuteOfDay", 360) };
            }

            return s;
        }

        /// <summary>The old inventory_save.json ({"itemIDs":[…],"amounts":[…]}) → a version-2 save without clock.</summary>
        public static GameSaveData FromLegacyInventoryJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new GameSaveData { loadedVersion = 1 };
            Dictionary<string, object> root = MiniJson.ParseObject(json);
            if (root == null) throw new FormatException("inventory save: not a JSON object");
            return FromLegacyInventory(root);
        }

        private static GameSaveData FromLegacyInventory(Dictionary<string, object> root)
        {
            var s = new GameSaveData { loadedVersion = 1 };
            var ids = root.TryGetValue("itemIDs", out object io) ? io as List<object> : null;
            var amounts = root.TryGetValue("amounts", out object ao) ? ao as List<object> : null;
            if (ids == null || amounts == null) return s;
            for (int i = 0; i < ids.Count && i < amounts.Count; i++)
            {
                if (ids[i] is string id && amounts[i] is double n && n > 0) s.inventory.Add(new ItemEntry(id, (int)n));
            }

            return s;
        }

        private static void ReadEntries(Dictionary<string, object> root, string key, List<ItemEntry> into)
        {
            if (!root.TryGetValue(key, out object o) || !(o is List<object> list)) return;
            foreach (object e in list)
            {
                if (!(e is Dictionary<string, object> d)) continue;
                if (!(d.TryGetValue("id", out object io) && io is string id) || id.Length == 0) continue;
                int n = Int(d, "amount", 0);
                if (n > 0) into.Add(new ItemEntry(id, n));
            }
        }

        private static int Int(Dictionary<string, object> d, string key, int fallback) => d.TryGetValue(key, out object o) && o is double n ? (int)n : fallback;
    }
}
