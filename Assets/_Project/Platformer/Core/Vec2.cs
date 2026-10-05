using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>Engine-free 2D vector. Converted to UnityEngine.Vector2 at the edge.</summary>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public float x;
        public float y;

        public Vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vec2 Zero => new Vec2(0f, 0f);
        public static Vec2 Up => new Vec2(0f, 1f);

        public float SqrMagnitude => x * x + y * y;
        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public Vec2 Normalized
        {
            get
            {
                float m = Magnitude;
                return m > 1e-6f ? new Vec2(x / m, y / m) : Zero;
            }
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.x, -a.y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.x * s, a.y * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.x * s, a.y * s);

        public static float Dot(Vec2 a, Vec2 b) => a.x * b.x + a.y * b.y;
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;

        public bool Equals(Vec2 other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => (x.GetHashCode() * 397) ^ y.GetHashCode();
        public override string ToString() => $"({x:0.###}, {y:0.###})";
    }

    /// <summary>Small float helpers so Core does not need UnityEngine.Mathf.</summary>
    public static class PMath
    {
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Abs(float v) => v < 0f ? -v : v;
        public static float Sign(float v) => v < 0f ? -1f : 1f;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Abs(target - current) <= maxDelta)
            {
                return target;
            }

            return current + Sign(target - current) * maxDelta;
        }

        /// <summary>Frame-rate independent exponential approach (1 − e^(−rate·dt)).</summary>
        public static float Damp(float current, float target, float rate, float dt)
        {
            return current + (target - current) * (1f - (float)Math.Exp(-rate * dt));
        }
    }
}
