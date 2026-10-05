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
            return !IsBlocked(x, y) && !IsBlocked(x, y + 1) && (layout.IsStandable(x, y - 1) || layout.At(x, y - 1) == TileKind.DewLeaf);
        }

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
            float bonus = 0f;
            if (layout.At(from.x, from.y - 1) == TileKind.DewLeaf)
            {
                bonus = SpiritMath.DewBounceVelocity(elements.dew, move.Gravity, true);
            }

            foreach (Cell to in standing)
            {
                if (to.Equals(from))
                {
                    continue;
                }

                int dy = to.y - from.y;
                float dist = Math.Max(0f, Math.Abs(to.x - from.x) - 1f) + Margin;
                float reach = Reach(dy, bonus);
                if (reach >= dist)
                {
                    yield return to;
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
                        yield return to;
                    }
                }
            }

            // Swing: jump onto the seat, then fly off either way.
            foreach (LevelMarker o in layout.All(TileKind.Swing))
            {
                float seatY = o.y + 0.5f - elements.swing.ropeLength;
                float toSeat = Math.Max(0f, Math.Abs(o.x - from.x) - 0.5f) + Margin;
                int rise = (int)Math.Ceiling(seatY - from.y);
                if (Reach(rise) < toSeat)
                {
                    continue;
                }

                foreach (Cell to in standing)
                {
                    float dx = Math.Abs(to.x - o.x);
                    if (to.y <= seatY + 2f && dx <= elements.swing.ropeLength + 3f)
                    {
                        yield return to;
                    }
                }
            }
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
