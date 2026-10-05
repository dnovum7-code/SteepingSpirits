using System;
using System.Collections.Generic;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Jump'n'Run progress, saved as small versioned JSON (jumpnrun_save.json):
    /// unlocked and finished levels, rare finds (per spot), finished lantern
    /// challenges and the comfort options.
    /// <code>
    /// {"version":1,"unlocked":["Level1"],"completed":[],"rareSpots":["Level2@61,16"],
    ///  "rareIds":["star_dew"],"challenges":[],"assists":"speed=1.0;air=0;fall=0"}
    /// </code>
    /// </summary>
    public sealed class JumpNRunSave
    {
        public const int Version = 1;
        public const string FileName = "jumpnrun_save.json";

        /// <summary>Levels that are open without playing anything.</summary>
        public static readonly string[] InitiallyUnlocked = { "Level1" };

        private readonly SortedSet<string> unlocked = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> completed = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> rareSpots = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> rareIds = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> challenges = new SortedSet<string>(StringComparer.Ordinal);

        public string Assists = "";

        /// <summary>Custom keyboard bindings (KeyBindings text); empty = defaults.</summary>
        public string Keys = "";

        public JumpNRunSave()
        {
            foreach (string id in InitiallyUnlocked) unlocked.Add(id);
        }

        public IEnumerable<string> Completed => completed;
        public IEnumerable<string> RareIds => rareIds;
        public int CompletedCount => completed.Count;
        public int RareCount => rareIds.Count;
        public int ChallengeCount => challenges.Count;

        public bool IsUnlocked(string levelId) => !string.IsNullOrEmpty(levelId) && unlocked.Contains(levelId);
        public bool IsCompleted(string levelId) => completed.Contains(levelId ?? "");
        public bool IsChallengeDone(string levelId) => challenges.Contains(levelId ?? "");

        public void Unlock(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId)) unlocked.Add(levelId);
        }

        /// <summary>Marks a level as finished and opens the next one. Returns true the first time.</summary>
        public bool Complete(string levelId, string nextLevelId)
        {
            Unlock(levelId);
            Unlock(nextLevelId);
            return !string.IsNullOrEmpty(levelId) && completed.Add(levelId);
        }

        public static string SpotKey(string levelId, int x, int y) => $"{levelId}@{x},{y}";

        public bool HasFoundRareAt(string levelId, int x, int y) => rareSpots.Contains(SpotKey(levelId, x, y));
        public bool HasEverFound(string ingredientId) => rareIds.Contains(ingredientId ?? "");

        /// <summary>Remembers a rare find. Returns true if this spot was new.</summary>
        public bool FoundRare(string levelId, int x, int y, string ingredientId)
        {
            if (!string.IsNullOrEmpty(ingredientId)) rareIds.Add(ingredientId);
            return rareSpots.Add(SpotKey(levelId, x, y));
        }

        public bool CompleteChallenge(string levelId) => !string.IsNullOrEmpty(levelId) && challenges.Add(levelId);

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":").Append(Version);
            Array(sb, "unlocked", unlocked);
            Array(sb, "completed", completed);
            Array(sb, "rareSpots", rareSpots);
            Array(sb, "rareIds", rareIds);
            Array(sb, "challenges", challenges);
            sb.Append(",\"assists\":").Append(MiniJson.Quote(Assists));
            if (!string.IsNullOrEmpty(Keys)) sb.Append(",\"keys\":").Append(MiniJson.Quote(Keys));
            return sb.Append('}').ToString();
        }

        private static void Array(StringBuilder sb, string name, IEnumerable<string> values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            bool first = true;
            foreach (string v in values)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(MiniJson.Quote(v));
            }

            sb.Append(']');
        }

        /// <summary>Reads a save; empty text gives a fresh save. Newer versions are rejected (the file stays untouched).</summary>
        public static JumpNRunSave FromJson(string json)
        {
            var save = new JumpNRunSave();
            if (string.IsNullOrWhiteSpace(json)) return save;

            Dictionary<string, object> root = MiniJson.ParseObject(json);
            if (root == null) throw new FormatException("save: not a JSON object");
            int version = root.TryGetValue("version", out object v) && v is double d ? (int)d : 1;
            if (version > Version) throw new FormatException($"save: version {version} is newer than {Version}");

            Read(root, "unlocked", save.unlocked);
            Read(root, "completed", save.completed);
            Read(root, "rareSpots", save.rareSpots);
            Read(root, "rareIds", save.rareIds);
            Read(root, "challenges", save.challenges);
            if (root.TryGetValue("assists", out object a) && a is string s) save.Assists = s;
            if (root.TryGetValue("keys", out object k) && k is string ks) save.Keys = ks;
            return save;
        }

        private static void Read(Dictionary<string, object> root, string name, SortedSet<string> target)
        {
            if (!root.TryGetValue(name, out object o) || !(o is List<object> list)) return;
            foreach (object item in list)
            {
                if (item is string s && s.Length > 0) target.Add(s);
            }
        }
    }
}
