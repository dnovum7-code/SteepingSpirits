using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>What a single character of a level text stands for.</summary>
    public enum TileKind
    {
        Empty,
        Solid,
        OneWay,
        Start,
        Lantern,
        Goal,
        Ingredient,
        WindSpirit,
        LanternSpirit,
        GhostPlatform,
        LeafPlatform,
        DewLeaf,
        Swing,
        Npc,
        Bramble
    }

    /// <summary>An axis-aligned block of tiles in world units (x right, y up, 1 tile = 1 unit).</summary>
    public struct TileRect
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public TileRect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float CenterX => x + width * 0.5f;
        public float CenterY => y + height * 0.5f;

        public override string ToString() => $"[{x},{y} {width}x{height}]";
    }

    /// <summary>A single placed object (start, lantern, ingredient, spirit element …).</summary>
    public struct LevelMarker
    {
        public TileKind kind;
        public char symbol;
        public int x;
        public int y;

        /// <summary>Running number per kind from left to right (bottom first on equal x), so "higher index = further on".</summary>
        public int index;

        public override string ToString() => $"{kind} '{symbol}' ({x},{y}) #{index}";
    }

    /// <summary>
    /// A level written as text. Lines starting with '@' are settings
    /// ("@name Morgenwiese"), lines starting with "//" are comments, the rest is
    /// the tile grid (top row = highest). Legend:
    ///
    ///   #  solid ground           =  thin one-way platform
    ///   P  start                  L  lantern (checkpoint)      E  goal
    ///   t k b m q  ingredient (tea leaf, herb, blossom, morning dew, spring water)
    ///   T K B M Q  rare variant of the same ingredient
    ///   W  wind spirit (updraft)  S  lantern spirit (light)
    ///   G  ghost platform (solid only in a lantern spirit's light)
    ///   F  leaf platform (sinks)  D  dew leaf (bounces)
    ///   O  swing anchor           N  spirit NPC   ^  bramble (gentle catch)
    ///   .  or space: empty
    ///
    /// Runs of '#', '=', 'G', 'F' are merged into rectangles.
    /// </summary>
    public sealed class LevelLayout
    {
        public const string Legend = "#=PLEtkbmqTKBMQWSGFDON^";

        public string Name = "";
        public int Width;
        public int Height;
        public readonly Dictionary<string, string> Settings = new Dictionary<string, string>();
        public readonly List<TileRect> Solids = new List<TileRect>();
        public readonly List<TileRect> OneWays = new List<TileRect>();
        public readonly List<TileRect> GhostPlatforms = new List<TileRect>();
        public readonly List<TileRect> LeafPlatforms = new List<TileRect>();
        public readonly List<LevelMarker> Markers = new List<LevelMarker>();

        private TileKind[,] grid;

        public TileKind At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return TileKind.Empty;
            }

            return grid[x, y];
        }

        /// <summary>Solid for standing (ground, one-way, ghost, leaf).</summary>
        public bool IsStandable(int x, int y)
        {
            TileKind k = At(x, y);
            return k == TileKind.Solid || k == TileKind.OneWay || k == TileKind.GhostPlatform || k == TileKind.LeafPlatform;
        }

        public LevelMarker Start => Markers.Find(m => m.kind == TileKind.Start);

        public List<LevelMarker> All(TileKind kind) => Markers.FindAll(m => m.kind == kind);

        public string Setting(string key, string fallback = "")
        {
            return Settings.TryGetValue(key, out string v) ? v : fallback;
        }

        public static TileKind KindOf(char c)
        {
            switch (c)
            {
                case '#': return TileKind.Solid;
                case '=': return TileKind.OneWay;
                case 'P': return TileKind.Start;
                case 'L': return TileKind.Lantern;
                case 'E': return TileKind.Goal;
                case 't': case 'k': case 'b': case 'm': case 'q':
                case 'T': case 'K': case 'B': case 'M': case 'Q':
                    return TileKind.Ingredient;
                case 'W': return TileKind.WindSpirit;
                case 'S': return TileKind.LanternSpirit;
                case 'G': return TileKind.GhostPlatform;
                case 'F': return TileKind.LeafPlatform;
                case 'D': return TileKind.DewLeaf;
                case 'O': return TileKind.Swing;
                case 'N': return TileKind.Npc;
                case '^': return TileKind.Bramble;
                default: return TileKind.Empty;
            }
        }

        public static bool IsKnown(char c) => c == '.' || c == ' ' || Legend.IndexOf(c) >= 0;

        /// <summary>Parses a level text. Throws FormatException with line/column on errors.</summary>
        public static LevelLayout Parse(string text)
        {
            var layout = new LevelLayout();
            var rows = new List<string>();
            string[] lines = (text ?? "").Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (trimmed.StartsWith("//"))
                {
                    continue;
                }

                if (trimmed.StartsWith("@"))
                {
                    int space = trimmed.IndexOf(' ');
                    string key = space < 0 ? trimmed.Substring(1) : trimmed.Substring(1, space - 1);
                    string value = space < 0 ? "" : trimmed.Substring(space + 1).Trim();
                    layout.Settings[key] = value;
                    continue;
                }

                if (trimmed.Length == 0 && rows.Count == 0)
                {
                    continue;
                }

                string row = line.TrimEnd();
                for (int c = 0; c < row.Length; c++)
                {
                    if (!IsKnown(row[c]))
                    {
                        throw new FormatException($"Line {i + 1}, column {c + 1}: unknown tile '{row[c]}'.");
                    }
                }

                rows.Add(row);
            }

            while (rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            layout.Name = layout.Setting("name", "Level");
            layout.Height = rows.Count;
            foreach (string r in rows)
            {
                layout.Width = Math.Max(layout.Width, r.Length);
            }

            layout.grid = new TileKind[layout.Width, layout.Height];

            for (int row = 0; row < rows.Count; row++)
            {
                int y = layout.Height - 1 - row;
                for (int x = 0; x < rows[row].Length; x++)
                {
                    char c = rows[row][x];
                    TileKind kind = KindOf(c);
                    layout.grid[x, y] = kind;
                    if (kind == TileKind.Empty || IsMerged(kind))
                    {
                        continue;
                    }

                    layout.Markers.Add(new LevelMarker { kind = kind, symbol = c, x = x, y = y });
                }
            }

            // Number markers per kind along the level (x), so lanterns further right count as further on.
            layout.Markers.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var counters = new Dictionary<TileKind, int>();
            for (int i = 0; i < layout.Markers.Count; i++)
            {
                LevelMarker m = layout.Markers[i];
                counters.TryGetValue(m.kind, out int n);
                counters[m.kind] = n + 1;
                m.index = n;
                layout.Markers[i] = m;
            }

            MergeRects(layout, TileKind.Solid, layout.Solids);
            MergeRects(layout, TileKind.OneWay, layout.OneWays);
            MergeRects(layout, TileKind.GhostPlatform, layout.GhostPlatforms);
            MergeRunsOnly(layout, TileKind.LeafPlatform, layout.LeafPlatforms);
            Validate(layout);
            return layout;
        }

        private static bool IsMerged(TileKind k)
        {
            return k == TileKind.Solid || k == TileKind.OneWay || k == TileKind.GhostPlatform || k == TileKind.LeafPlatform;
        }

        /// <summary>Greedy merge: horizontal runs first, then stack equal runs vertically.</summary>
        private static void MergeRects(LevelLayout layout, TileKind kind, List<TileRect> output)
        {
            var used = new bool[layout.Width, layout.Height];
            for (int y = layout.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (used[x, y] || layout.grid[x, y] != kind)
                    {
                        continue;
                    }

                    int w = 1;
                    while (x + w < layout.Width && layout.grid[x + w, y] == kind && !used[x + w, y])
                    {
                        w++;
                    }

                    int bottom = y;
                    while (bottom - 1 >= 0 && RowMatches(layout, used, kind, x, w, bottom - 1))
                    {
                        bottom--;
                    }

                    for (int yy = bottom; yy <= y; yy++)
                    {
                        for (int xx = x; xx < x + w; xx++)
                        {
                            used[xx, yy] = true;
                        }
                    }

                    output.Add(new TileRect(x, bottom, w, y - bottom + 1));
                }
            }
        }

        /// <summary>Elements that act per row (leaf platforms sink as one piece per run).</summary>
        private static void MergeRunsOnly(LevelLayout layout, TileKind kind, List<TileRect> output)
        {
            for (int y = layout.Height - 1; y >= 0; y--)
            {
                int x = 0;
                while (x < layout.Width)
                {
                    if (layout.grid[x, y] != kind)
                    {
                        x++;
                        continue;
                    }

                    int w = 1;
                    while (x + w < layout.Width && layout.grid[x + w, y] == kind)
                    {
                        w++;
                    }

                    output.Add(new TileRect(x, y, w, 1));
                    x += w;
                }
            }
        }

        private static bool RowMatches(LevelLayout layout, bool[,] used, TileKind kind, int x, int w, int y)
        {
            for (int xx = x; xx < x + w; xx++)
            {
                if (layout.grid[xx, y] != kind || used[xx, y])
                {
                    return false;
                }
            }

            // Only merge if the run is exactly as wide (no overhang continuing sideways).
            bool leftContinues = x - 1 >= 0 && layout.grid[x - 1, y] == kind && !used[x - 1, y];
            bool rightContinues = x + w < layout.Width && layout.grid[x + w, y] == kind && !used[x + w, y];
            return !leftContinues && !rightContinues;
        }

        private static void Validate(LevelLayout layout)
        {
            int starts = layout.All(TileKind.Start).Count;
            if (starts != 1)
            {
                throw new FormatException($"Level '{layout.Name}' needs exactly one start 'P' (found {starts}).");
            }

            if (layout.All(TileKind.Goal).Count == 0)
            {
                throw new FormatException($"Level '{layout.Name}' needs a goal 'E'.");
            }
        }
    }
}
