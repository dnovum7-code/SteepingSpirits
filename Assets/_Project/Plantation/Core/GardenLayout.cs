using System;
using System.Collections.Generic;

namespace SteepingSpirits.Plantation
{
    public enum GardenTile
    {
        Grass,
        Bed,
        Path,
        Wall,
        Well,
        Lantern,
        Start,
        Gate,
        Chest
    }

    /// <summary>
    /// A garden written as text (like the Jump'n'Run levels). Lines with '@' are
    /// settings, '//' comments, the rest the grid (top row = highest y). Legend:
    /// <code>
    ///   .  grass        B  bed cell (the only place to plant)    :  path
    ///   #  hedge/wall   W  well (refill the can)                 L  lantern (end the day)
    ///   P  start        G  gate (back to the meadow)             C  seed chest (start pack)
    /// </code>
    /// </summary>
    public sealed class GardenLayout
    {
        public const string Legend = ".B:#WLPGC";

        public string Name = "Teegarten";
        public string SourceText = "";
        public int Width;
        public int Height;
        public readonly Dictionary<string, string> Settings = new Dictionary<string, string>();
        private GardenTile[,] grid;

        public GardenTile At(GridPos p) => At(p.x, p.y);

        public GardenTile At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return GardenTile.Wall;
            return grid[x, y];
        }

        public bool IsBed(GridPos p) => At(p) == GardenTile.Bed;

        /// <summary>Can the player stand on this cell?</summary>
        public bool IsWalkable(GridPos p)
        {
            GardenTile t = At(p);
            return t == GardenTile.Grass || t == GardenTile.Bed || t == GardenTile.Path || t == GardenTile.Start || t == GardenTile.Gate;
        }

        public List<GridPos> All(GardenTile tile)
        {
            var list = new List<GridPos>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == tile) list.Add(new GridPos(x, y));
            }

            return list;
        }

        public string Setting(string key, string fallback = "") => Settings.TryGetValue(key, out string v) ? v : fallback;

        /// <summary>The starter pack from "@seeds plant:count ..." (plant id → count), in written order.</summary>
        public List<KeyValuePair<string, int>> SeedPack()
        {
            var pack = new List<KeyValuePair<string, int>>();
            foreach (string part in Setting("seeds").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = part.IndexOf(':');
                if (colon <= 0) continue;
                if (int.TryParse(part.Substring(colon + 1), out int n) && n > 0) pack.Add(new KeyValuePair<string, int>(part.Substring(0, colon), n));
            }

            return pack;
        }

        /// <summary>
        /// Connected groups of bed cells (4-neighbourhood), ordered like reading text:
        /// top rows first, then left to right.
        /// </summary>
        public List<List<GridPos>> BedBlocks()
        {
            var blocks = new List<List<GridPos>>();
            var seen = new HashSet<GridPos>();
            for (int y = Height - 1; y >= 0; y--)
            for (int x = 0; x < Width; x++)
            {
                var start = new GridPos(x, y);
                if (!IsBed(start) || seen.Contains(start)) continue;
                var block = new List<GridPos>();
                var open = new Queue<GridPos>();
                open.Enqueue(start);
                seen.Add(start);
                while (open.Count > 0)
                {
                    GridPos p = open.Dequeue();
                    block.Add(p);
                    foreach (GridPos d in new[] { GridPos.Up, GridPos.Down, GridPos.Left, GridPos.Right })
                    {
                        GridPos n = p + d;
                        if (IsBed(n) && seen.Add(n)) open.Enqueue(n);
                    }
                }

                blocks.Add(block);
            }

            return blocks;
        }

        /// <summary>Walking distance in cells from every walkable cell to target cells (BFS, 4-neighbourhood).</summary>
        public int[,] DistanceField(IEnumerable<GridPos> targets)
        {
            var dist = new int[Width, Height];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++) dist[x, y] = int.MaxValue;
            var open = new Queue<GridPos>();
            foreach (GridPos t in targets)
            {
                if (!IsWalkable(t) || dist[t.x, t.y] == 0) continue;
                dist[t.x, t.y] = 0;
                open.Enqueue(t);
            }

            while (open.Count > 0)
            {
                GridPos p = open.Dequeue();
                foreach (GridPos d in new[] { GridPos.Up, GridPos.Down, GridPos.Left, GridPos.Right })
                {
                    GridPos n = p + d;
                    if (!IsWalkable(n) || dist[n.x, n.y] != int.MaxValue) continue;
                    dist[n.x, n.y] = dist[p.x, p.y] + 1;
                    open.Enqueue(n);
                }
            }

            return dist;
        }

        public static GardenTile TileOf(char c)
        {
            switch (c)
            {
                case 'B': return GardenTile.Bed;
                case ':': return GardenTile.Path;
                case '#': return GardenTile.Wall;
                case 'W': return GardenTile.Well;
                case 'L': return GardenTile.Lantern;
                case 'P': return GardenTile.Start;
                case 'G': return GardenTile.Gate;
                case 'C': return GardenTile.Chest;
                default: return GardenTile.Grass;
            }
        }

        public static GardenLayout Parse(string text)
        {
            var layout = new GardenLayout { SourceText = text ?? "" };
            var rows = new List<string>();
            string[] lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (trimmed.StartsWith("//")) continue;
                if (trimmed.StartsWith("@"))
                {
                    int space = trimmed.IndexOf(' ');
                    layout.Settings[space < 0 ? trimmed.Substring(1) : trimmed.Substring(1, space - 1)] =
                        space < 0 ? "" : trimmed.Substring(space + 1).Trim();
                    continue;
                }

                if (trimmed.Length == 0 && rows.Count == 0) continue;
                string row = line.TrimEnd();
                for (int c = 0; c < row.Length; c++)
                {
                    if (row[c] != ' ' && Legend.IndexOf(row[c]) < 0)
                    {
                        throw new FormatException($"Line {i + 1}, column {c + 1}: unknown tile '{row[c]}'.");
                    }
                }

                rows.Add(row);
            }

            while (rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0) rows.RemoveAt(rows.Count - 1);
            layout.Name = layout.Setting("name", "Teegarten");
            layout.Height = rows.Count;
            foreach (string r in rows) layout.Width = Math.Max(layout.Width, r.Length);
            layout.grid = new GardenTile[layout.Width, layout.Height];
            for (int row = 0; row < rows.Count; row++)
            {
                int y = layout.Height - 1 - row;
                for (int x = 0; x < rows[row].Length; x++) layout.grid[x, y] = TileOf(rows[row][x]);
            }

            if (layout.All(GardenTile.Start).Count != 1) throw new FormatException("A garden needs exactly one start 'P'.");
            if (layout.All(GardenTile.Bed).Count == 0) throw new FormatException("A garden needs bed cells 'B'.");
            if (layout.All(GardenTile.Well).Count == 0) throw new FormatException("A garden needs a well 'W'.");
            if (layout.All(GardenTile.Lantern).Count == 0) throw new FormatException("A garden needs a lantern 'L' to end the day.");
            return layout;
        }
    }
}
