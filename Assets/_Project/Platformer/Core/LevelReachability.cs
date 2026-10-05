using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Coarse "can this level be finished?" check on the tile grid, using the
    /// real jump curve from <see cref="PlayerMotor"/>. It is a design aid, not
    /// a proof: head bumps on overhangs are ignored, ghost platforms count as
    /// lit, and elements are approximated (wind lifts to the column top, dew
    /// leaves bounce to their held height, swings throw up to a few tiles).
    /// Used by tests to keep the level files finishable.
    /// </summary>
    public sealed class LevelReachability
    {
        public struct Cell : IEquatable<Cell>
        {
            public int x;
            public int y;

            public Cell(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public bool Equals(Cell o) => x == o.x && y == o.y;
            public override bool Equals(object obj) => obj is Cell c && Equals(c);
            public override int GetHashCode() => x * 7919 + y;
            public override string ToString() => $"({x},{y})";
        }

        /// <summary>Distance in tiles that a player standing at a tile edge must still cover (safety margin).</summary>
        public float Margin = 0.35f;

        /// <summary>Player height in tiles (to grab things above the head while jumping).</summary>
        public float BodyHeight = 1.4f;

        private readonly LevelLayout layout;
        private readonly MovementParams move;
        private readonly SpiritElementParams elements;
        private readonly Dictionary<int, float> reachCache = new Dictionary<int, float>();

        public HashSet<Cell> Reached { get; } = new HashSet<Cell>();

        public LevelReachability(LevelLayout layout, MovementParams move = null, SpiritElementParams elements = null)
        {
            this.layout = layout;
            this.move = move ?? new MovementParams();
            this.elements = elements ?? new SpiritElementParams();
        }

        /// <summary>A tile the player can stand in (free tile with ground directly below).</summary>
        public bool IsStanding(int x, int y)
        {
            // Brambles are not dangerous, but nobody stands in them on purpose: route over them.
            return !IsBlocked(x, y) && !IsBlocked(x, y + 1) && layout.At(x, y) != TileKind.Bramble
                   && (layout.IsStandable(x, y - 1) || layout.At(x, y - 1) == TileKind.DewLeaf);
        }

        /// <summary>Solid for a body passing through (ground, ghost, leaf).</summary>
        public bool IsBlocking(int x, int y) => IsBlocked(x, y);

        private bool IsBlocked(int x, int y)
        {
            TileKind k = layout.At(x, y);
            return k == TileKind.Solid || k == TileKind.GhostPlatform || k == TileKind.LeafPlatform;
        }

        /// <summary>
        /// Horizontal distance (tiles) a running full jump covers while landing
        /// dy tiles higher (negative = lower). -1 if the height is out of reach.
        /// </summary>
        public float Reach(int dy, float riseBonusVelocity = 0f)
        {
            int key = dy * 1000 + (int)(riseBonusVelocity * 10f);
            if (reachCache.TryGetValue(key, out float cached))
            {
                return cached;
            }

            var motor = new PlayerMotor(move);
            const float dt = 1f / 120f;
            float x = 0f, y = 0f, best = -1f;
            var input = new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true };
            motor.Velocity = new Vec2(move.runSpeed, 0f);
            motor.Step(dt, input, new MotorContacts { grounded = true });
            if (riseBonusVelocity > 0f)
            {
                motor.Launch(new Vec2(move.runSpeed, riseBonusVelocity), false);
            }

            input.jumpPressed = false;
            for (int i = 0; i < 2400; i++)
            {
                x += motor.Velocity.x * dt;
                y += motor.Velocity.y * dt;
                if (y >= dy)
                {
                    best = x;
                }

                if (motor.Velocity.y < 0f && y < dy - 0.01f)
                {
                    break;
                }

                motor.Step(dt, input, new MotorContacts());
            }

            reachCache[key] = best;
            return best;
        }

        public float PeakHeight(float riseBonusVelocity = 0f)
        {
            float v = riseBonusVelocity > 0f ? riseBonusVelocity : move.JumpVelocity;
            // Apex hang adds a little; use the plain formula as the safe value.
            return JumpMath.PeakHeight(v, move.Gravity);
        }

        public void Run()
        {
            Reached.Clear();
            LevelMarker start = layout.Start;
            var queue = new Queue<Cell>();
            var first = new Cell(start.x, start.y);
            Reached.Add(first);
            queue.Enqueue(first);

            List<Cell> standing = AllStanding();
            while (queue.Count > 0)
            {
                Cell c = queue.Dequeue();
                foreach (Cell n in Neighbours(c, standing))
                {
                    if (Reached.Add(n))
                    {
                        queue.Enqueue(n);
                    }
                }
            }
        }

        private List<Cell> AllStanding()
        {
            var list = new List<Cell>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (IsStanding(x, y)) list.Add(new Cell(x, y));
                }
            }

            return list;
        }

        private IEnumerable<Cell> Neighbours(Cell from, List<Cell> standing)
        {
            foreach (Edge e in Edges(from, standing))
            {
                yield return e.to;
            }
        }

        /// <summary>How the player gets from one standing cell to the next.</summary>
        public enum MoveKind
        {
            Walk,
            Jump,
            Dew,
            Wind,

            /// <summary>Seat → standing cell: pump and let go towards it.</summary>
            Swing,

            /// <summary>Standing cell or another seat → onto a swing's seat.</summary>
            SwingMount,

            /// <summary>Seat → through a wind column → standing cell above.</summary>
            SwingWind
        }

        public struct Edge
        {
            public Cell to;
            public MoveKind kind;

            /// <summary>Marker of the wind spirit or swing used (kind Wind/Swing/SwingMount: the target swing).</summary>
            public LevelMarker anchor;

            /// <summary>SwingWind: the wind spirit flown into.</summary>
            public LevelMarker anchor2;
        }

        public List<Cell> StandingCells() => AllStanding();

        /// <summary>All moves from a standing cell (coarse, see class summary).</summary>
        public IEnumerable<Edge> Edges(Cell from, List<Cell> standing)
        {
            if (IsSeat(from, out LevelMarker seatOf))
            {
                foreach (Edge e in SeatEdges(seatOf, standing)) yield return e;
                yield break;
            }

            bool onDew = layout.At(from.x, from.y - 1) == TileKind.DewLeaf;
            float bonus = onDew ? SpiritMath.DewBounceVelocity(elements.dew, move.Gravity, true) : 0f;

            foreach (Cell to in standing)
            {
                if (to.Equals(from))
                {
                    continue;
                }

                int dy = to.y - from.y;
                float dist = Math.Max(0f, Math.Abs(to.x - from.x) - 1f) + Margin;
                if (Reach(dy, bonus) >= dist)
                {
                    MoveKind kind = onDew ? MoveKind.Dew : (IsWalk(from, to) ? MoveKind.Walk : MoveKind.Jump);
                    yield return new Edge { to = to, kind = kind };
                }
            }

            // Wind: standing in or next to a column → lifted to its top, then jump from there.
            foreach (LevelMarker w in layout.All(TileKind.WindSpirit))
            {
                if (Math.Abs(w.x - from.x) > 1 || from.y < w.y || from.y > w.y + 1)
                {
                    continue;
                }

                int top = w.y + ColumnHeight(w) - 1;
                foreach (Cell to in standing)
                {
                    float dist = Math.Max(0f, Math.Abs(to.x - w.x) - 1f) + Margin;
                    if (to.y <= top + 1 && Reach(to.y - top) >= dist)
                    {
                        yield return new Edge { to = to, kind = MoveKind.Wind, anchor = w };
                    }
                }
            }

            // Swing: jump onto the seat (the seat is its own node, see SeatCell).
            foreach (LevelMarker o in layout.All(TileKind.Swing))
            {
                float seatY = SeatY(o);
                float toSeat = Math.Max(0f, Math.Abs(o.x - from.x) - 0.5f) + Margin;
                int rise = (int)Math.Ceiling(seatY - from.y);
                if (Reach(rise) >= toSeat)
                {
                    yield return new Edge { to = SeatCell(o), kind = MoveKind.SwingMount, anchor = o };
                }
            }
        }

        /// <summary>Moves from a swing seat: fly to ground, to another swing, or through a wind column.</summary>
        private IEnumerable<Edge> SeatEdges(LevelMarker o, List<Cell> standing)
        {
            float rope = RopeLength(o);
            float seatY = SeatY(o);
            float range = rope + 3f;

            foreach (Cell to in standing)
            {
                if (to.y <= seatY + 2f && Math.Abs(to.x - o.x) <= range)
                {
                    yield return new Edge { to = to, kind = MoveKind.Swing, anchor = o };
                }
            }

            foreach (LevelMarker other in layout.All(TileKind.Swing))
            {
                if (other.index == o.index) continue;
                float otherSeat = SeatY(other);
                if (otherSeat <= seatY + 2f && Math.Abs(other.x - o.x) <= range + RopeLength(other) * 0.5f)
                {
                    yield return new Edge { to = SeatCell(other), kind = MoveKind.SwingMount, anchor = other };
                }
            }

            foreach (LevelMarker w in layout.All(TileKind.WindSpirit))
            {
                int top = w.y + ColumnHeight(w) - 1;
                if (Math.Abs(w.x - o.x) > range || w.y > seatY + 1f || top < seatY)
                {
                    continue;
                }

                foreach (Cell to in standing)
                {
                    float dist = Math.Max(0f, Math.Abs(to.x - w.x) - 1f) + Margin;
                    if (to.y > seatY + 2f && to.y <= top + 1 && Reach(to.y - top) >= dist)
                    {
                        yield return new Edge { to = to, kind = MoveKind.SwingWind, anchor = o, anchor2 = w };
                    }
                }
            }
        }

        /// <summary>World height of a swing's seat when it hangs still.</summary>
        public float SeatY(LevelMarker swing) => swing.y + 0.5f - RopeLength(swing);

        /// <summary>The graph node standing for "sitting on this swing".</summary>
        public Cell SeatCell(LevelMarker swing) => new Cell(swing.x, (int)Math.Floor(SeatY(swing)) - 10000);

        /// <summary>Is this node a swing seat (see SeatCell)? Returns the swing.</summary>
        public bool IsSeat(Cell c, out LevelMarker swing)
        {
            swing = default;
            if (c.y > -5000) return false;
            foreach (LevelMarker o in layout.All(TileKind.Swing))
            {
                if (SeatCell(o).Equals(c))
                {
                    swing = o;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Same height and solid footing all the way: just walk.</summary>
        public bool IsWalk(Cell from, Cell to)
        {
            if (from.y != to.y)
            {
                return false;
            }

            int step = to.x > from.x ? 1 : -1;
            for (int x = from.x; x != to.x; x += step)
            {
                if (!IsStanding(x, from.y))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Highest swing angle of a swing marker ("@angle&lt;index&gt; 70", else the tuning value), degrees.</summary>
        public float MaxAngleDeg(LevelMarker swing)
        {
            string s = layout.Setting("angle" + swing.index);
            return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out float v) && v >= 20f && v <= 89f ? v : elements.swing.maxAngleDeg;
        }

        /// <summary>Rope length of a swing marker (per-swing "@rope&lt;index&gt; 3.5", else the tuning value).</summary>
        public float RopeLength(LevelMarker swing)
        {
            string s = layout.Setting("rope" + swing.index);
            return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out float v) && v > 0.5f ? v : elements.swing.ropeLength;
        }

        public int ColumnHeight(LevelMarker w)
        {
            int h = 1;
            while (h < elements.wind.maxHeight && !layout.IsStandable(w.x, w.y + h))
            {
                h++;
            }

            return h;
        }

        /// <summary>Can the marker's tile be touched from some reached standing cell?</summary>
        public bool CanTouch(int x, int y)
        {
            foreach (Cell s in Reached)
            {
                if (IsSeat(s, out LevelMarker swing))
                {
                    // Things along the swing's arc and its flights.
                    float rope = RopeLength(swing);
                    if (Math.Abs(x - swing.x) <= rope + 3f && y <= swing.y + 1 && y >= SeatY(swing) - 3f)
                    {
                        return true;
                    }

                    continue;
                }

                if (s.x == x && s.y == y)
                {
                    return true;
                }

                // Tiles touched by the body during a jump: need height (y − feet) ≤ peak + body.
                int dy = y - s.y;
                float needHeight = Math.Max(0f, dy - BodyHeight + 0.5f);
                if (needHeight > PeakHeight())
                {
                    continue;
                }

                float dist = Math.Max(0f, Math.Abs(x - s.x) - 0.5f);
                float reach = Reach((int)Math.Ceiling(needHeight));
                if (reach >= dist)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanTouch(LevelMarker m) => CanTouch(m.x, m.y);

        /// <summary>Human-readable list of problems (empty = fine).</summary>
        public List<string> Problems()
        {
            Run();
            var problems = new List<string>();
            foreach (LevelMarker m in layout.Markers)
            {
                bool required = m.kind == TileKind.Goal || m.kind == TileKind.Lantern || m.kind == TileKind.Ingredient
                                || m.kind == TileKind.PathLantern || m.kind == TileKind.Door;
                if (required && !CanTouch(m))
                {
                    problems.Add($"{m.kind} '{m.symbol}' at ({m.x},{m.y}) is not reachable");
                }
            }

            return problems;
        }
    }
}
