using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>One move of a planned route.</summary>
    public struct RouteStep
    {
        public LevelReachability.Cell from;
        public LevelReachability.Cell to;
        public LevelReachability.MoveKind kind;

        /// <summary>Wind column bottom centre or swing pivot (world units), for Wind/Swing.</summary>
        public Vec2 anchor;

        /// <summary>Rope length for swings.</summary>
        public float rope;

        /// <summary>SwingWind: bottom centre of the wind column.</summary>
        public Vec2 anchor2;

        public Vec2 Target => new Vec2(to.x + 0.5f, to.y);

        /// <summary>True when the target is a swing seat (kind SwingMount).</summary>
        public bool ToSeat => kind == LevelReachability.MoveKind.SwingMount;

        public override string ToString() => $"{kind} {from}→{to}";
    }

    /// <summary>
    /// Cheapest route over the reachability graph (Dijkstra). Costs prefer
    /// walking and short jumps so the route hugs the ground like a player would;
    /// jumps need head room above everything they pass over.
    /// </summary>
    public static class RoutePlanner
    {
        public static List<RouteStep> Plan(LevelReachability r, LevelLayout layout, LevelReachability.Cell start,
            Func<LevelReachability.Cell, bool> isGoal)
        {
            List<LevelReachability.Cell> standing = r.StandingCells();
            if (!standing.Contains(start))
            {
                standing.Add(start);
            }

            var dist = new Dictionary<LevelReachability.Cell, float> { [start] = 0f };
            var prev = new Dictionary<LevelReachability.Cell, RouteStep>();
            var done = new HashSet<LevelReachability.Cell>();
            var open = new HashSet<LevelReachability.Cell> { start };

            while (open.Count > 0)
            {
                LevelReachability.Cell c = default;
                float best = float.MaxValue;
                foreach (LevelReachability.Cell candidate in open)
                {
                    if (dist[candidate] < best)
                    {
                        best = dist[candidate];
                        c = candidate;
                    }
                }

                open.Remove(c);
                done.Add(c);

                if (isGoal(c))
                {
                    return Unwind(prev, start, c);
                }

                foreach (LevelReachability.Edge e in r.Edges(c, standing))
                {
                    if (done.Contains(e.to) || !Clear(r, layout, c, e))
                    {
                        continue;
                    }

                    float cost = dist[c] + Cost(r, c, e);
                    if (!dist.TryGetValue(e.to, out float old) || cost < old)
                    {
                        dist[e.to] = cost;
                        prev[e.to] = MakeStep(r, c, e);
                        open.Add(e.to);
                    }
                }
            }

            return null;
        }

        public static LevelReachability.Cell? NearestStanding(LevelReachability r, Vec2 feet)
        {
            LevelReachability.Cell? best = null;
            float bestD = float.MaxValue;
            foreach (LevelReachability.Cell c in r.StandingCells())
            {
                float d = Math.Abs(c.x + 0.5f - feet.x) + Math.Abs(c.y - feet.y) * 2f;
                if (d < bestD)
                {
                    bestD = d;
                    best = c;
                }
            }

            return best;
        }

        /// <summary>Standing cell for a marker that sits on the ground (goal, door).</summary>
        public static bool IsAt(LevelReachability.Cell c, LevelMarker m) => c.y == m.y && Math.Abs(c.x - m.x) <= 0;

        private static RouteStep MakeStep(LevelReachability r, LevelReachability.Cell from, LevelReachability.Edge e)
        {
            var s = new RouteStep { from = from, to = e.to, kind = e.kind };
            if (e.kind == LevelReachability.MoveKind.Wind)
            {
                s.anchor = new Vec2(e.anchor.x + 0.5f, e.anchor.y);
            }
            else if (e.kind == LevelReachability.MoveKind.Swing || e.kind == LevelReachability.MoveKind.SwingMount
                     || e.kind == LevelReachability.MoveKind.SwingWind)
            {
                s.anchor = new Vec2(e.anchor.x + 0.5f, e.anchor.y + 0.5f);
                s.rope = r.RopeLength(e.anchor);
                if (e.kind == LevelReachability.MoveKind.SwingWind)
                {
                    s.anchor2 = new Vec2(e.anchor2.x + 0.5f, e.anchor2.y);
                }
            }

            return s;
        }

        private static float Cost(LevelReachability r, LevelReachability.Cell from, LevelReachability.Edge e)
        {
            float dx = Math.Abs(e.to.x - from.x);
            float dy = e.to.y - from.y;
            switch (e.kind)
            {
                case LevelReachability.MoveKind.Walk:
                    return dx;
                case LevelReachability.MoveKind.Jump:
                {
                    // Prefer small hops; jumps close to the limit are risky.
                    float reach = Math.Max(0.1f, r.Reach((int)dy));
                    float ratio = (Math.Max(0f, dx - 1f) + r.Margin) / reach;
                    return 2f + dx + Math.Max(0f, dy) * Math.Max(0f, dy) + 0.3f * Math.Max(0f, -dy)
                           + 30f * Math.Max(0f, ratio - 0.55f);
                }
                case LevelReachability.MoveKind.Dew: return 3f + dx + 0.5f * Math.Abs(dy);
                case LevelReachability.MoveKind.Wind: return 4f + dx + 0.3f * Math.Abs(dy);
                case LevelReachability.MoveKind.SwingMount: return 5f + Math.Min(dx, 20f);
                case LevelReachability.MoveKind.SwingWind: return 6f + Math.Min(dx, 20f);
                default: return 3f + Math.Min(dx, 20f);
            }
        }

        /// <summary>Jumps need two free tiles above the higher end over every tile they cross.</summary>
        private static bool Clear(LevelReachability r, LevelLayout layout, LevelReachability.Cell from, LevelReachability.Edge e)
        {
            if (e.kind != LevelReachability.MoveKind.Jump && e.kind != LevelReachability.MoveKind.Dew)
            {
                return true;
            }

            int top = Math.Max(from.y, e.to.y);
            int lo = Math.Min(from.x, e.to.x), hi = Math.Max(from.x, e.to.x);
            for (int x = lo + 1; x < hi; x++)
            {
                for (int y = top; y <= top + 1; y++)
                {
                    if (r.IsBlocking(x, y))
                    {
                        return false;
                    }
                }
            }

            // Head room above the take-off for upward jumps.
            for (int y = from.y + 1; y <= Math.Min(from.y + 3, e.to.y + 1); y++)
            {
                if (r.IsBlocking(from.x, y))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<RouteStep> Unwind(Dictionary<LevelReachability.Cell, RouteStep> prev,
            LevelReachability.Cell start, LevelReachability.Cell end)
        {
            var list = new List<RouteStep>();
            LevelReachability.Cell c = end;
            while (!c.Equals(start))
            {
                RouteStep s = prev[c];
                list.Add(s);
                c = s.from;
            }

            list.Reverse();
            return Compress(list);
        }

        /// <summary>Merges consecutive walks into one.</summary>
        private static List<RouteStep> Compress(List<RouteStep> steps)
        {
            var result = new List<RouteStep>();
            foreach (RouteStep s in steps)
            {
                if (result.Count > 0 && s.kind == LevelReachability.MoveKind.Walk
                    && result[result.Count - 1].kind == LevelReachability.MoveKind.Walk)
                {
                    RouteStep last = result[result.Count - 1];
                    last.to = s.to;
                    result[result.Count - 1] = last;
                    continue;
                }

                result.Add(s);
            }

            return result;
        }
    }
}
