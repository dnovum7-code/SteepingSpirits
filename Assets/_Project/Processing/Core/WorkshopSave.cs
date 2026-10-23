using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Processing
{
    /// <summary>Extra data of a tea id (exact oxidation, origin) that the id itself does not carry.</summary>
    public sealed class TeaLedgerEntry
    {
        public string id = "";
        public int made;
        public float oxidationSum;
        public string lastSource = "";
        public string lastPath = "";

        public float AverageOxidation => made > 0 ? oxidationSum / made : 0f;
    }

    /// <summary>
    /// workshop_save.json (versioned): batches in work including running passive steps (place and
    /// game time, so withering/drying continue while away), the tea ledger and options.
    /// </summary>
    public sealed class WorkshopSave
    {
        public const int Version = 1;
        public const string FileName = "workshop_save.json";

        public int nextId = 1;
        public bool starterGiven;
        public bool showPercent;
        public bool evenRolling;
        public readonly List<LeafBatch> batches = new List<LeafBatch>();
        public readonly Dictionary<string, TeaLedgerEntry> ledger = new Dictionary<string, TeaLedgerEntry>();

        public static WorkshopSave From(Workshop w)
        {
            var s = new WorkshopSave { nextId = w.NextId };
            foreach (LeafBatch b in w.Batches) s.batches.Add(b.Clone());
            return s;
        }

        public void ApplyTo(Workshop w)
        {
            foreach (LeafBatch b in batches) w.Restore(b);
            w.NextId = Math.Max(w.NextId, nextId);
        }

        public void Record(ProcessedTea tea)
        {
            if (tea == null) return;
            if (!ledger.TryGetValue(tea.Id, out TeaLedgerEntry e))
            {
                e = new TeaLedgerEntry { id = tea.Id };
                ledger[tea.Id] = e;
            }

            e.made += tea.amount;
            e.oxidationSum += tea.oxidation * tea.amount;
            e.lastSource = tea.sourceId;
            e.lastPath = tea.path;
        }

        public string ToJson()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("{\"version\":").Append(Version);
            sb.Append(",\"nextId\":").Append(nextId);
            sb.Append(",\"starterGiven\":").Append(B(starterGiven));
            sb.Append(",\"showPercent\":").Append(B(showPercent));
            sb.Append(",\"evenRolling\":").Append(B(evenRolling));
            sb.Append(",\"batches\":[");
            for (int i = 0; i < batches.Count; i++)
            {
                LeafBatch b = batches[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n{\"id\":").Append(b.id);
                sb.Append(",\"amount\":").Append(b.amount);
                sb.Append(",\"source\":").Append(MiniJson.Quote(b.sourceId));
                sb.Append(",\"origin\":").Append(b.originTier);
                sb.Append(",\"firstFlush\":").Append(B(b.firstFlush));
                if (b.isHerb) sb.Append(",\"herb\":true");
                F(sb, "moisture", b.moisture);
                F(sb, "rolled", b.rolled);
                F(sb, "rollEvenness", b.rollEvenness);
                sb.Append(",\"rollCount\":").Append(b.rollCount);
                F(sb, "oxidation", b.oxidation);
                sb.Append(",\"oxStopped\":").Append(B(b.oxidationStopped));
                F(sb, "oxAtStop", b.oxidationAtStop);
                sb.Append(",\"fixed\":").Append(B(b.fixedByWok));
                F(sb, "wokCare", b.wokCare);
                F(sb, "fixResidual", b.fixResidual);
                F(sb, "roast", b.roast);
                F(sb, "roastFromDrying", b.roastFromDrying);
                F(sb, "dryness", b.dryness);
                F(sb, "workMoisture", b.workMoisture);
                sb.Append(",\"place\":").Append(MiniJson.Quote(b.place.ToString()));
                sb.Append(",\"lastMinutes\":").Append(b.lastMinutes.ToString("R", inv));
                F(sb, "activeSeconds", b.activeSeconds);
                sb.Append(",\"steps\":").Append(MiniJson.Quote(b.PathCode()));
                sb.Append('}');
            }

            sb.Append("],\"ledger\":[");
            bool first = true;
            foreach (TeaLedgerEntry e in ledger.Values)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("\n{\"id\":").Append(MiniJson.Quote(e.id)).Append(",\"made\":").Append(e.made);
                F(sb, "oxidationSum", e.oxidationSum);
                sb.Append(",\"lastSource\":").Append(MiniJson.Quote(e.lastSource)).Append(",\"lastPath\":").Append(MiniJson.Quote(e.lastPath)).Append('}');
            }

            return sb.Append("]}").ToString();
        }

        /// <summary>Reads a save; empty text gives a fresh save. Throws FormatException on broken or newer files.</summary>
        public static WorkshopSave FromJson(string json)
        {
            var s = new WorkshopSave();
            if (string.IsNullOrWhiteSpace(json)) return s;
            Dictionary<string, object> root = MiniJson.ParseObject(json);
            if (root == null) throw new FormatException("workshop: not a JSON object");
            int version = (int)Num(root, "version", 1);
            if (version > Version) throw new FormatException($"workshop: version {version} is newer than {Version}");

            s.nextId = (int)Num(root, "nextId", 1);
            s.starterGiven = Bool(root, "starterGiven");
            s.showPercent = Bool(root, "showPercent");
            s.evenRolling = Bool(root, "evenRolling");
            if (root.TryGetValue("batches", out object bo) && bo is List<object> list)
            {
                foreach (object o in list)
                {
                    if (!(o is Dictionary<string, object> d)) continue;
                    var b = new LeafBatch
                    {
                        id = (int)Num(d, "id", 0),
                        amount = Math.Max(1, (int)Num(d, "amount", 1)),
                        sourceId = Str(d, "source", "tea_leaf"),
                        originTier = Math.Max(1, Math.Min(3, (int)Num(d, "origin", 1))),
                        firstFlush = Bool(d, "firstFlush"),
                        isHerb = Bool(d, "herb"),
                        moisture = (float)Num(d, "moisture", 1),
                        rolled = (float)Num(d, "rolled", 0),
                        rollEvenness = (float)Num(d, "rollEvenness", 0),
                        rollCount = (int)Num(d, "rollCount", 0),
                        oxidation = (float)Num(d, "oxidation", 0),
                        oxidationStopped = Bool(d, "oxStopped"),
                        oxidationAtStop = (float)Num(d, "oxAtStop", -1),
                        fixedByWok = Bool(d, "fixed"),
                        wokCare = (float)Num(d, "wokCare", 0),
                        fixResidual = (float)Num(d, "fixResidual", 0),
                        roast = (float)Num(d, "roast", 0),
                        roastFromDrying = (float)Num(d, "roastFromDrying", 0),
                        dryness = (float)Num(d, "dryness", 0),
                        workMoisture = (float)Num(d, "workMoisture", -1),
                        lastMinutes = Num(d, "lastMinutes", 0),
                        activeSeconds = (float)Num(d, "activeSeconds", 0)
                    };
                    b.place = Enum.TryParse(Str(d, "place", "Shelf"), out BatchPlace place) ? place : BatchPlace.Shelf;
                    foreach (string code in Str(d, "steps", "").Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (StepOf(code[0], out ProcessStep step)) b.steps.Add(step);
                    }

                    s.batches.Add(b);
                }
            }

            if (root.TryGetValue("ledger", out object lo) && lo is List<object> entries)
            {
                foreach (object o in entries)
                {
                    if (!(o is Dictionary<string, object> d)) continue;
                    var e = new TeaLedgerEntry
                    {
                        id = Str(d, "id", ""),
                        made = (int)Num(d, "made", 0),
                        oxidationSum = (float)Num(d, "oxidationSum", 0),
                        lastSource = Str(d, "lastSource", ""),
                        lastPath = Str(d, "lastPath", "")
                    };
                    if (e.id.Length > 0) s.ledger[e.id] = e;
                }
            }

            return s;
        }

        private static bool StepOf(char c, out ProcessStep step)
        {
            foreach (ProcessStep s in (ProcessStep[])Enum.GetValues(typeof(ProcessStep)))
            {
                if (s.ToString()[0] == c)
                {
                    step = s;
                    return true;
                }
            }

            step = ProcessStep.Wither;
            return false;
        }

        private static string B(bool b) => b ? "true" : "false";

        private static void F(StringBuilder sb, string key, float v) =>
            sb.Append(",\"").Append(key).Append("\":").Append(v.ToString("R", CultureInfo.InvariantCulture));

        private static double Num(Dictionary<string, object> d, string key, double fallback) => d.TryGetValue(key, out object o) && o is double n ? n : fallback;

        private static bool Bool(Dictionary<string, object> d, string key) => d.TryGetValue(key, out object o) && o is bool b && b;

        private static string Str(Dictionary<string, object> d, string key, string fallback) => d.TryGetValue(key, out object o) && o is string s ? s : fallback;
    }

    /// <summary>One line in processing_log.jsonl per finished batch.</summary>
    public static class ProcessingLog
    {
        public static string Line(ProcessedTea tea, double gameMinutes)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("{");
            sb.Append("\"gameMinutes\":").Append(gameMinutes.ToString("0", inv));
            sb.Append(",\"path\":").Append(MiniJson.Quote(tea.path));
            sb.Append(",\"tea\":").Append(MiniJson.Quote(tea.variety.ToString()));
            sb.Append(",\"id\":").Append(MiniJson.Quote(tea.Id));
            sb.Append(",\"targetOxidation\":").Append(tea.oxidationAtStop < 0f ? "null" : tea.oxidationAtStop.ToString("0.000", inv));
            sb.Append(",\"reachedOxidation\":").Append(tea.oxidation.ToString("0.000", inv));
            sb.Append(",\"quality\":").Append(tea.tier);
            sb.Append(",\"notes\":[");
            for (int i = 0; i < tea.notes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(MiniJson.Quote(TeaNotes.Key(tea.notes[i])));
            }

            sb.Append("],\"activeSeconds\":").Append(tea.activeSeconds.ToString("0.0", inv));
            sb.Append(",\"amount\":").Append(tea.amount);
            sb.Append(",\"source\":").Append(MiniJson.Quote(tea.sourceId));
            sb.Append(",\"originTier\":").Append(tea.originTier);
            sb.Append(",\"firstFlush\":").Append(tea.firstFlush ? "true" : "false");
            return sb.Append('}').ToString();
        }
    }
}
