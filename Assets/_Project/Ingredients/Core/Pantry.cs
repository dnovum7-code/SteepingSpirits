using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SteepingSpirits.Ingredients
{
    /// <summary>
    /// The tea house's pantry ("Vorratsschrank"): ingredient ids and amounts,
    /// stored as small versioned JSON so the brewing system can read it later.
    /// <code>
    /// {"version":1,"updated":"2026-10-05T10:00:00Z","items":{"herb":2,"tea_leaf":5}}
    /// </code>
    /// Unknown ids are kept (forward compatible); aliases are normalised.
    /// </summary>
    public sealed class Pantry
    {
        public const int Version = 1;
        public const string FileName = "pantry.json";

        private readonly SortedDictionary<string, int> items = new SortedDictionary<string, int>(StringComparer.Ordinal);

        public string Updated = "";

        public IEnumerable<KeyValuePair<string, int>> Items => items;

        public int Count(string id)
        {
            string key = IngredientCatalog.Normalize(id) ?? id;
            return key != null && items.TryGetValue(key, out int n) ? n : 0;
        }

        public int Total
        {
            get
            {
                int t = 0;
                foreach (int n in items.Values) t += n;
                return t;
            }
        }

        public void Add(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            string key = IngredientCatalog.Normalize(id) ?? id;
            items.TryGetValue(key, out int n);
            items[key] = n + amount;
        }

        /// <summary>Takes up to amount; returns how many were taken.</summary>
        public int Take(string id, int amount)
        {
            string key = IngredientCatalog.Normalize(id) ?? id;
            if (key == null || amount <= 0 || !items.TryGetValue(key, out int n)) return 0;
            int taken = Math.Min(n, amount);
            if (n - taken == 0) items.Remove(key);
            else items[key] = n - taken;
            return taken;
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":").Append(Version).Append(",\"updated\":\"").Append(Escape(Updated)).Append("\",\"items\":{");
            bool first = true;
            foreach (var kv in items)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(Escape(kv.Key)).Append("\":").Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            }

            return sb.Append("}}").ToString();
        }

        /// <summary>Reads a pantry file; an empty or broken file gives an empty pantry (nothing is lost on disk).</summary>
        public static Pantry FromJson(string json)
        {
            var p = new Pantry();
            if (string.IsNullOrWhiteSpace(json)) return p;

            var reader = new MiniJson(json);
            Dictionary<string, object> root = reader.ReadObject();
            if (root == null) throw new FormatException("pantry: not a JSON object");

            int version = root.TryGetValue("version", out object v) && v is double d ? (int)d : 1;
            if (version > Version) throw new FormatException($"pantry: version {version} is newer than {Version}");
            if (root.TryGetValue("updated", out object u) && u is string s) p.Updated = s;
            if (root.TryGetValue("items", out object it) && it is Dictionary<string, object> dict)
            {
                foreach (var kv in dict)
                {
                    if (kv.Value is double amount && amount > 0) p.Add(kv.Key, (int)amount);
                }
            }

            return p;
        }

        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>Tiny JSON reader for flat objects (strings, numbers, nested objects, bools, null).</summary>
    internal sealed class MiniJson
    {
        private readonly string s;
        private int i;

        public MiniJson(string text)
        {
            s = text;
        }

        public Dictionary<string, object> ReadObject()
        {
            Skip();
            if (i >= s.Length || s[i] != '{') return null;
            i++;
            var d = new Dictionary<string, object>();
            Skip();
            if (Peek() == '}') { i++; return d; }
            while (i < s.Length)
            {
                Skip();
                string key = ReadString();
                Skip();
                Expect(':');
                d[key] = ReadValue();
                Skip();
                if (Peek() == ',') { i++; continue; }
                Expect('}');
                return d;
            }

            throw new FormatException("unterminated object");
        }

        private object ReadValue()
        {
            Skip();
            char c = Peek();
            if (c == '{') return ReadObject();
            if (c == '"') return ReadString();
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw new FormatException($"unexpected '{c}' at {i}");
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        private string ReadString()
        {
            Expect('"');
            var sb = new StringBuilder();
            while (i < s.Length && s[i] != '"')
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    i++;
                    char e = s[i];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'u' && i + 4 < s.Length)
                    {
                        sb.Append((char)int.Parse(s.Substring(i + 1, 4), NumberStyles.HexNumber));
                        i += 4;
                    }
                    else sb.Append(e);
                }
                else
                {
                    sb.Append(s[i]);
                }

                i++;
            }

            Expect('"');
            return sb.ToString();
        }

        private void Skip()
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private char Peek() => i < s.Length ? s[i] : '\0';

        private void Expect(char c)
        {
            Skip();
            if (Peek() != c) throw new FormatException($"expected '{c}' at {i}");
            i++;
        }
    }
}
