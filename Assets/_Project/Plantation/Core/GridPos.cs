using System;

namespace SteepingSpirits.Plantation
{
    /// <summary>A cell of the garden grid (x right, y up). Perspective-free.</summary>
    public struct GridPos : IEquatable<GridPos>
    {
        public int x;
        public int y;

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.x + b.x, a.y + b.y);
        public static GridPos operator *(GridPos a, int k) => new GridPos(a.x * k, a.y * k);

        public static readonly GridPos Up = new GridPos(0, 1);
        public static readonly GridPos Down = new GridPos(0, -1);
        public static readonly GridPos Left = new GridPos(-1, 0);
        public static readonly GridPos Right = new GridPos(1, 0);

        public int Manhattan(GridPos o) => Math.Abs(x - o.x) + Math.Abs(y - o.y);

        public bool Equals(GridPos o) => x == o.x && y == o.y;
        public override bool Equals(object obj) => obj is GridPos p && Equals(p);
        public override int GetHashCode() => x * 73856093 ^ y * 19349663;
        public override string ToString() => $"({x},{y})";
    }
}
