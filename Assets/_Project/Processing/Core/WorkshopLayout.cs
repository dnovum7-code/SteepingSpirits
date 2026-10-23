using System;
using System.Collections.Generic;

namespace SteepingSpirits.Processing
{
    public enum WorkshopTile
    {
        Floor,
        Wall,
        Start,
        Door,
        Shelf,
        Store,
        Rack,
        RollTable,
        Basket,
        Wok,
        Dryer,
        BlendTable
    }

    /// <summary>
    /// The workshop room as text (top row = north). Lines with '@' are settings, '//' comments.
    /// <code>
    ///   .  floor    #  wall     P  start        G  door to the garden
    ///   S  shelf    V  store    R  wither rack  T  rolling table
    ///   O  oxidation basket     K  wok          D  drying basket   M  blending table
    /// </code>
    /// </summary>
    public sealed class WorkshopLayout
    {
        public const string Legend = ".#PGSVRTOKDM";

        public string Name = "Teewerkstatt";
        public int Width;
        public int Height;
        public readonly Dictionary<string, string> Settings = new Dictionary<string, string>();
        private WorkshopTile[,] grid;

        public WorkshopTile At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? WorkshopTile.Wall : grid[x, y];

        public bool IsWalkable(int x, int y)
        {
            WorkshopTile t = At(x, y);
            return t == WorkshopTile.Floor || t == WorkshopTile.Start || t == WorkshopTile.Door;
        }

        public List<(int x, int y)> All(WorkshopTile tile)
        {
            var list = new List<(int, int)>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == tile) list.Add((x, y));
            }

            return list;
        }

        public string Setting(string key, string fallback = "") => Settings.TryGetValue(key, out string v) ? v : fallback;

        /// <summary>Starter pack from "@starter source:count:tier ..." (e.g. tea_leaf:8:3).</summary>
        public List<(string source, int count, int tier)> StarterPack()
        {
            var pack = new List<(string, int, int)>();
            foreach (string part in Setting("starter").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] f = part.Split(':');
                if (f.Length < 2 || !int.TryParse(f[1], out int n) || n <= 0) continue;
                int tier = f.Length > 2 && int.TryParse(f[2], out int t) ? Math.Max(1, Math.Min(3, t)) : 1;
                pack.Add((f[0], n, tier));
            }

            return pack;
        }

        public static WorkshopTile TileOf(char c)
        {
            switch (c)
            {
                case '#': return WorkshopTile.Wall;
                case 'P': return WorkshopTile.Start;
                case 'G': return WorkshopTile.Door;
                case 'S': return WorkshopTile.Shelf;
                case 'V': return WorkshopTile.Store;
                case 'R': return WorkshopTile.Rack;
                case 'T': return WorkshopTile.RollTable;
                case 'O': return WorkshopTile.Basket;
                case 'K': return WorkshopTile.Wok;
                case 'D': return WorkshopTile.Dryer;
                case 'M': return WorkshopTile.BlendTable;
                default: return WorkshopTile.Floor;
            }
        }

        public static WorkshopLayout Parse(string text)
        {
            var layout = new WorkshopLayout();
            var rows = new List<string>();
            string[] lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("//")) continue;
                if (trimmed.StartsWith("@"))
                {
                    int space = trimmed.IndexOf(' ');
                    layout.Settings[space < 0 ? trimmed.Substring(1) : trimmed.Substring(1, space - 1)] = space < 0 ? "" : trimmed.Substring(space + 1).Trim();
                    continue;
                }

                if (trimmed.Length == 0 && rows.Count == 0) continue;
                string row = lines[i].TrimEnd();
                for (int c = 0; c < row.Length; c++)
                {
                    if (row[c] != ' ' && Legend.IndexOf(row[c]) < 0) throw new FormatException($"Line {i + 1}, column {c + 1}: unknown tile '{row[c]}'.");
                }

                rows.Add(row);
            }

            while (rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0) rows.RemoveAt(rows.Count - 1);
            layout.Name = layout.Setting("name", "Teewerkstatt");
            layout.Height = rows.Count;
            foreach (string r in rows) layout.Width = Math.Max(layout.Width, r.Length);
            layout.grid = new WorkshopTile[layout.Width, layout.Height];
            for (int row = 0; row < rows.Count; row++)
            {
                int y = layout.Height - 1 - row;
                for (int x = 0; x < rows[row].Length; x++) layout.grid[x, y] = TileOf(rows[row][x]);
            }

            if (layout.All(WorkshopTile.Start).Count != 1) throw new FormatException("The workshop needs exactly one start 'P'.");
            foreach (WorkshopTile needed in new[] { WorkshopTile.Shelf, WorkshopTile.Store, WorkshopTile.Rack, WorkshopTile.RollTable, WorkshopTile.Basket, WorkshopTile.Wok, WorkshopTile.Dryer })
            {
                if (layout.All(needed).Count == 0) throw new FormatException("The workshop needs a " + needed + ".");
            }

            return layout;
        }
    }
}
