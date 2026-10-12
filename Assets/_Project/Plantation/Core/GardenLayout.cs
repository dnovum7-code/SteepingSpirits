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
