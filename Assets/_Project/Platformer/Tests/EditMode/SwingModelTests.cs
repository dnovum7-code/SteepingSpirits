using System;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class SwingModelTests
    {
        private const float Dt = 1f / 120f;

        private static float Deg(float rad) => rad * 180f / (float)Math.PI;

        /// <summary>Presses in the direction the swing is moving (the right rhythm).</summary>
        private static void PumpInRhythm(SwingModel s, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                float input = s.AngularVelocity >= 0f ? 1f : -1f;
                s.Step(Dt, input);
            }
        }

        private static SwingModel Swinging(float amplitudeDeg)
        {
            var s = new SwingModel(new SwingParams());
            s.Angle = amplitudeDeg * (float)Math.PI / 180f;
            return s;
        }

        [Test]
        public void PumpingInRhythm_BuildsUpTheSwing()
        {
            var s = new SwingModel(new SwingParams());
            PumpInRhythm(s, 2f);
            float early = s.Amplitude;
            PumpInRhythm(s, 8f);
            Assert.Greater(Deg(early), 5f, "starts moving from rest");
            Assert.Greater(Deg(s.Amplitude), 55f);
            Assert.Greater(s.Amplitude, early);
        }

        [Test]
        public void HoldingOneKey_SlowsTheSwingDown()
        {
            SwingModel s = Swinging(45f);
            float before = s.Amplitude;
            for (float t = 0f; t < 6f; t += Dt) s.Step(Dt, 1f);
            Assert.Less(s.Amplitude, before * 0.7f);
        }

        [Test]
        public void WrongRhythm_BrakesFast()
        {
            SwingModel s = Swinging(45f);
            for (float t = 0f; t < 3f; t += Dt)
            {
                float input = s.AngularVelocity >= 0f ? -1f : 1f;
                s.Step(Dt, input);
            }

            Assert.Less(Deg(s.Amplitude), 10f);
        }

        [Test]
        public void NoInput_DecaysOnlySlowly()
        {
            SwingModel s = Swinging(45f);
            for (float t = 0f; t < 5f; t += Dt) s.Step(Dt, 0f);
            Assert.Greater(Deg(s.Amplitude), 45f * 0.7f);
            Assert.Less(Deg(s.Amplitude), 45f);
        }

        [Test]
        public void NeverPassesTheMaximumAngle()
        {
            var p = new SwingParams { pumpAcceleration = 40f };
            var s = new SwingModel(p);
            float maxSeen = 0f;
            for (float t = 0f; t < 20f; t += Dt)
            {
                s.Step(Dt, s.AngularVelocity >= 0f ? 1f : -1f);
                maxSeen = Math.Max(maxSeen, Math.Abs(s.Angle));
            }

            Assert.LessOrEqual(Deg(maxSeen), p.maxAngleDeg + 1e-3f);
        }

        [Test]
        public void SmallSwing_HasPendulumPeriod()
        {
            var p = new SwingParams { damping = 0f };
            var s = new SwingModel(p) { Angle = 0.1f };
            float t = 0f;
            int crossings = 0;
            float firstCross = -1f, lastCross = 0f;
            float prev = s.Angle;
            while (t < 10f)
            {
                s.Step(Dt, 0f);
                t += Dt;
                if (prev > 0f && s.Angle <= 0f)
                {
                    crossings++;
                    if (firstCross < 0f) firstCross = t;
                    lastCross = t;
                }

                prev = s.Angle;
            }

            float period = (lastCross - firstCross) / (crossings - 1);
            float expected = 2f * (float)Math.PI * (float)Math.Sqrt(p.ropeLength / p.gravity);
            Assert.AreEqual(expected, period, 0.03f);
        }

        [Test]
        public void Release_FollowsTheSeatAndAddsLift()
        {
            var p = new SwingParams();
            var s = new SwingModel(p) { Angle = 0f, AngularVelocity = 2f };
            Vec2 v = s.ReleaseVelocity;
            Assert.AreEqual(2f * p.ropeLength * p.releaseBoost, v.x, 1e-4f, "at the bottom the seat moves sideways");
            Assert.AreEqual(p.releaseUpBonus, v.y, 1e-4f);

            s.Angle = 0.6f;
            Assert.Greater(s.ReleaseVelocity.y, p.releaseUpBonus, "forward-up on the way up");
        }

        [Test]
        public void BiggerSwing_FliesFurtherInAnArc()
        {
            float small = FlightDistance(25f);
            float big = FlightDistance(60f);
            Assert.Greater(big, small * 2f);
            Assert.Greater(big, 2.5f, "a big swing carries over a gap of a few tiles");
            Assert.Greater(small, 0.3f);
        }

        /// <summary>Swing with amplitude a, jump off on the way up at 30°, fly with player gravity until back at seat height.</summary>
        private static float FlightDistance(float amplitudeDeg)
        {
            var p = new SwingParams { damping = 0f };
            var s = new SwingModel(p) { Angle = -amplitudeDeg * (float)Math.PI / 180f };
            float release = Math.Min(30f, amplitudeDeg * 0.8f) * (float)Math.PI / 180f;
            for (int i = 0; i < 2000 && !(s.Angle >= release && s.AngularVelocity > 0f); i++) s.Step(Dt, 0f);

            var move = new MovementParams();
            Vec2 v = s.ReleaseVelocity;
            Vec2 pos = s.SeatOffset;
            float startY = pos.y;
            for (int i = 0; i < 2000; i++)
            {
                float g = move.Gravity * (v.y <= 0f ? move.fallGravityFactor : 1f);
                v.y = Math.Max(v.y - g * Dt, -move.MaxFallSpeed);
                pos += v * Dt;
                if (v.y < 0f && pos.y <= startY) break;
            }

            return pos.x - s.SeatOffset.x;
        }

        [Test]
        public void Mount_TakesOverSidewaysSpeed()
        {
            var s = new SwingModel(new SwingParams());
            s.Mount(new Vec2(0f, -3f), new Vec2(6f, -2f));
            Assert.AreEqual(0f, s.Angle, 1e-5f);
            Assert.Greater(s.AngularVelocity, 0f);
        }
    }
}
