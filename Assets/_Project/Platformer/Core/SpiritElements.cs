using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>Wind spirit: a column of rising air.</summary>
    [Serializable]
    public class WindParams
    {
        /// <summary>Upward acceleration in the column (beats fall gravity).</summary>
        public float acceleration = 120f;

        /// <summary>No more push above this rising speed.</summary>
        public float maxRiseSpeed = 9f;

        /// <summary>The push fades out over this many units below the column top.</summary>
        public float topFade = 1.5f;

        /// <summary>Highest column if nothing blocks it (tiles).</summary>
        public int maxHeight = 9;

        public float width = 1.6f;
    }

    /// <summary>Lantern spirit: lights ghost platforms around it.</summary>
    [Serializable]
    public class LanternSpiritParams
    {
        public float lightRadius = 4f;

        /// <summary>How fast ghost platforms appear / fade (1/s).</summary>
        public float fadeRate = 6f;

        /// <summary>Follow speed once it joined the player (1/s).</summary>
        public float followRate = 3f;

        /// <summary>Where it floats relative to the player.</summary>
        public float followOffsetX = -0.9f;
        public float followOffsetY = 1.6f;
    }

    /// <summary>Leaf platform: sinks slowly under the player, comes back up when free.</summary>
    [Serializable]
    public class LeafParams
    {
        public float sinkSpeed = 0.9f;
        public float maxSink = 1.6f;
        public float returnSpeed = 1.4f;

        /// <summary>How quickly the speed follows the target (1/s) – softens start and stop.</summary>
        public float response = 6f;
    }

    /// <summary>Dew leaf: bounces the player up.</summary>
    [Serializable]
    public class DewParams
    {
        /// <summary>Bounce height without holding jump (units).</summary>
        public float bounceHeight = 4.5f;

        /// <summary>Bounce height while holding jump.</summary>
        public float bounceHeightHeld = 6.5f;

        /// <summary>Seconds before the same leaf bounces again.</summary>
        public float cooldown = 0.2f;
    }

    [Serializable]
    public class SpiritElementParams
    {
        public WindParams wind = new WindParams();
        public LanternSpiritParams lanternSpirit = new LanternSpiritParams();
        public LeafParams leaf = new LeafParams();
        public DewParams dew = new DewParams();
        public SwingParams swing = new SwingParams();
    }

    public static class SpiritMath
    {
        /// <summary>
        /// Upward acceleration of an updraft for a body at heightInColumn
        /// (0 = bottom) with vertical speed vy.
        /// </summary>
        public static float Updraft(WindParams p, float columnHeight, float heightInColumn, float vy)
        {
            if (heightInColumn < 0f || heightInColumn > columnHeight || vy >= p.maxRiseSpeed)
            {
                return 0f;
            }

            float toTop = columnHeight - heightInColumn;
            float fade = p.topFade > 0f ? PMath.Clamp01(toTop / p.topFade) : 1f;

            // Ease in near the speed cap so the rise settles softly.
            float speedRoom = PMath.Clamp01((p.maxRiseSpeed - vy) / 2f);
            return p.acceleration * fade * speedRoom;
        }

        /// <summary>Velocity for a dew bounce to the configured height.</summary>
        public static float DewBounceVelocity(DewParams p, float gravity, bool jumpHeld)
        {
            return JumpMath.VelocityForHeight(jumpHeld ? p.bounceHeightHeld : p.bounceHeight, gravity);
        }

        /// <summary>Distance from a point to a rect (0 inside).</summary>
        public static float DistanceToRect(Vec2 point, float minX, float minY, float maxX, float maxY)
        {
            float dx = Math.Max(Math.Max(minX - point.x, 0f), point.x - maxX);
            float dy = Math.Max(Math.Max(minY - point.y, 0f), point.y - maxY);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Is any light within radius of the rect?</summary>
        public static bool IsLit(IReadOnlyList<Vec2> lights, float radius, float minX, float minY, float maxX, float maxY)
        {
            for (int i = 0; i < lights.Count; i++)
            {
                if (DistanceToRect(lights[i], minX, minY, maxX, maxY) <= radius)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>State of one leaf platform (offset below its rest height).</summary>
    public sealed class LeafSpring
    {
        public LeafParams Params;
        public float Offset { get; private set; }
        public float Velocity { get; private set; }

        public LeafSpring(LeafParams p)
        {
            Params = p ?? new LeafParams();
        }

        /// <summary>Returns the vertical velocity for this step (negative = sinking).</summary>
        public float Step(float dt, bool loaded)
        {
            LeafParams p = Params;
            float target;
            if (loaded)
            {
                target = Offset > -p.maxSink ? -p.sinkSpeed : 0f;
            }
            else
            {
                target = Offset < 0f ? Math.Min(p.returnSpeed, -Offset * 4f) : 0f;
            }

            Velocity = PMath.Damp(Velocity, target, p.response, dt);
            float next = Offset + Velocity * dt;
            if (next < -p.maxSink)
            {
                next = -p.maxSink;
                Velocity = Math.Max(Velocity, 0f);
            }

            if (next > 0f)
            {
                next = 0f;
                Velocity = Math.Min(Velocity, 0f);
            }

            Offset = next;
            return Velocity;
        }
    }
}
