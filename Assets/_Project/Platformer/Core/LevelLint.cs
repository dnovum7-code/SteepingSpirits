using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Design warnings for a level text, shown by the builder: unreachable
    /// markers, lanterns whose numbering does not follow the way through the
    /// level, swings without room, and settings that point nowhere.
    /// </summary>
    public static class LevelLint
    {
        public static List<string> Check(LevelLayout layout, MovementParams move = null, SpiritElementParams elements = null)
        {
            var warnings = new List<string>();
            var reach = new LevelReachability(layout, move, elements);
            warnings.AddRange(reach.Problems());

            if (!layout.IsHub)
            {
                CheckLanternOrder(layout, reach, warnings);
            }

            foreach (LevelMarker n in layout.All(TileKind.Npc))
            {
                if (string.IsNullOrEmpty(layout.Setting("npc" + n.index)))
                {
                    warnings.Add($"spirit NPC #{n.index} at ({n.x},{n.y}) has no line (@npc{n.index})");
                }
            }

            foreach (LevelMarker o in layout.All(TileKind.Swing))
            {
                float rope = reach.RopeLength(o);
                int seatRow = (int)Math.Floor(o.y + 0.5f - rope);
                for (int y = seatRow; y <= o.y; y++)
                {
                    if (reach.IsBlocking(o.x, y))
                    {
                        warnings.Add($"swing #{o.index} at ({o.x},{o.y}): rope passes through solid tiles at y={y}");
                        break;
                    }
                }
            }

            return warnings;
        }

        /// <summary>
        /// Lanterns are numbered left to right; checkpoints only move forward.
        /// Warn when the way through the level reaches a higher-numbered lantern
        /// before a lower-numbered one (respawn could then jump back).
        /// </summary>
        private static void CheckLanternOrder(LevelLayout layout, LevelReachability reach, List<string> warnings)
        {
            List<LevelMarker> lanterns = layout.All(TileKind.Lantern);
            if (lanterns.Count < 2)
            {
                return;
            }

            LevelMarker s = layout.Start;
            var start = new LevelReachability.Cell(s.x, s.y);
            var order = new List<KeyValuePair<float, LevelMarker>>();
            foreach (LevelMarker l in lanterns)
            {
                List<RouteStep> route = RoutePlanner.Plan(reach, layout, start, c => c.y == l.y && c.x == l.x);
                if (route == null)
                {
                    continue; // unreachable lanterns are already reported
                }

                float length = 0f;
                foreach (RouteStep step in route) length += Math.Abs(step.to.x - step.from.x) + Math.Abs(step.to.y - step.from.y);
                order.Add(new KeyValuePair<float, LevelMarker>(length, l));
            }

            order.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int i = 1; i < order.Count; i++)
            {
                if (order[i].Value.index < order[i - 1].Value.index)
                {
                    warnings.Add($"lantern #{order[i - 1].Value.index} at ({order[i - 1].Value.x},{order[i - 1].Value.y}) is reached before " +
                                 $"lantern #{order[i].Value.index} at ({order[i].Value.x},{order[i].Value.y}) – lantern order does not follow the way");
                }
            }
        }
    }
}
