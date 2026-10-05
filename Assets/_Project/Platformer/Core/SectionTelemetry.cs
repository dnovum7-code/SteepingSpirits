using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>One finished (or abandoned) section of a level run.</summary>
    public sealed class SectionRecord
    {
        public string runId = "";
        public string levelId = "";
        public int section;
        public float seconds;
        public int falls;
        public readonly Dictionary<string, int> ingredients = new Dictionary<string, int>();
        public string assists = "";

        /// <summary>"lantern", "goal" or "left" (level closed mid-section).</summary>
        public string end = "";
    }

    /// <summary>
    /// Splits a level run into sections (start → lantern → … → goal) and
    /// counts time, falls and ingredients per section. Lighting an earlier
    /// lantern again does not start a new section. Produces one JSON line per
    /// section for jumpnrun_log.jsonl (no external JSON library needed).
    /// </summary>
    public sealed class SectionTelemetry
    {
        private SectionRecord current;
        private float sectionStart;
        private int highestLantern = -1;

        public string RunId { get; }
        public string LevelId { get; }
        public List<SectionRecord> Finished { get; } = new List<SectionRecord>();
        public SectionRecord Current => current;

        public SectionTelemetry(string runId, string levelId, float now, string assists)
        {
            RunId = runId ?? "";
            LevelId = levelId ?? "";
            Begin(0, now, assists);
        }

        public void Fall()
        {
            if (current != null) current.falls++;
        }

        public void Ingredient(string id)
        {
            if (current == null || string.IsNullOrEmpty(id)) return;
            current.ingredients.TryGetValue(id, out int n);
            current.ingredients[id] = n + 1;
        }

        /// <summary>Assists can change mid-section; the record keeps the latest setting.</summary>
        public void Assists(string assists)
        {
            if (current != null) current.assists = assists ?? "";
        }

        /// <summary>Returns the finished record if this lantern starts a new section, else null.</summary>
        public SectionRecord LanternLit(int lantern, float now)
        {
            if (lantern <= highestLantern || current == null)
            {
                return null;
            }

            highestLantern = lantern;
            SectionRecord done = Close(now, "lantern");
            Begin(done.section + 1, now, done.assists);
            return done;
        }

        public SectionRecord Goal(float now) => current == null ? null : Close(now, "goal", false);

        public SectionRecord Left(float now) => current == null ? null : Close(now, "left", false);

        private void Begin(int section, float now, string assists)
        {
            current = new SectionRecord { runId = RunId, levelId = LevelId, section = section, assists = assists ?? "" };
            sectionStart = now;
        }

        private SectionRecord Close(float now, string end, bool keepGoing = true)
        {
            SectionRecord r = current;
            r.seconds = Math.Max(0f, now - sectionStart);
            r.end = end;
            Finished.Add(r);
            if (!keepGoing)
            {
                current = null;
            }

            return r;
        }

        public static string ToJson(SectionRecord r, string timestampUtc)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            Field(sb, "time", timestampUtc).Append(',');
            Field(sb, "run", r.runId).Append(',');
            Field(sb, "level", r.levelId).Append(',');
            sb.Append("\"section\":").Append(r.section).Append(',');
            sb.Append("\"seconds\":").Append(r.seconds.ToString("0.00", CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"falls\":").Append(r.falls).Append(',');
            sb.Append("\"ingredients\":{");
            bool first = true;
            var keys = new List<string>(r.ingredients.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (string k in keys)
            {
                if (!first) sb.Append(',');
                first = false;
                Quote(sb, k).Append(':').Append(r.ingredients[k]);
            }

            sb.Append("},");
            Field(sb, "assists", r.assists).Append(',');
            Field(sb, "end", r.end);
            sb.Append('}');
            return sb.ToString();
        }

        private static StringBuilder Field(StringBuilder sb, string name, string value)
        {
            Quote(sb, name).Append(':');
            return Quote(sb, value ?? "");
        }

        private static StringBuilder Quote(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }

            return sb.Append('"');
        }
    }
}
