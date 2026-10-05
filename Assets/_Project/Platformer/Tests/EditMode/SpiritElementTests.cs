using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class SpiritElementTests
    {
        [Test]
        public void Updraft_LiftsFallingPlayerToSteadyRise()
        {
            var move = new MovementParams();
            var wind = new WindParams();
            float vy = -10f, y = 4f, column = 8f;
            const float dt = 1f / 120f;
            float maxVy = vy;
            for (int i = 0; i < 240 && y < column - 2f; i++)
            {
                float g = move.Gravity * (vy <= 0f ? move.fallGravityFactor : 1f);
                vy += (SpiritMath.Updraft(wind, column, y, vy) - g) * dt;
                y += vy * dt;
                maxVy = System.Math.Max(maxVy, vy);
            }

            Assert.Greater(y, 5.5f, "the column catches the fall and carries the player up");
            Assert.LessOrEqual(maxVy, wind.maxRiseSpeed + 0.5f);
        }

        [Test]
        public void Updraft_FadesAtTopAndOutside()
        {
            var wind = new WindParams();
            Assert.AreEqual(0f, SpiritMath.Updraft(wind, 8f, 8f, 0f), 1e-5f);
            Assert.AreEqual(0f, SpiritMath.Updraft(wind, 8f, -0.1f, 0f), 1e-5f);
            Assert.Less(SpiritMath.Updraft(wind, 8f, 7.5f, 0f), SpiritMath.Updraft(wind, 8f, 2f, 0f));
            Assert.AreEqual(0f, SpiritMath.Updraft(wind, 8f, 2f, wind.maxRiseSpeed), 1e-5f);
        }

        [Test]
        public void DewBounce_ReachesConfiguredHeights()
        {
            var move = new MovementParams();
            var dew = new DewParams();
            float g = move.Gravity;
            Assert.AreEqual(dew.bounceHeight, JumpMath.PeakHeight(SpiritMath.DewBounceVelocity(dew, g, false), g), 1e-3f);
            Assert.Greater(SpiritMath.DewBounceVelocity(dew, g, true), SpiritMath.DewBounceVelocity(dew, g, false));
        }

        [Test]
        public void GhostPlatform_LitOnlyNearALight()
        {
            var lights = new List<Vec2> { new Vec2(0f, 0f) };
            Assert.IsTrue(SpiritMath.IsLit(lights, 3f, 2f, -0.5f, 5f, 0.5f));
            Assert.IsFalse(SpiritMath.IsLit(lights, 1.5f, 2f, -0.5f, 5f, 0.5f));
            Assert.IsFalse(SpiritMath.IsLit(new List<Vec2>(), 10f, 0f, 0f, 1f, 1f));
            Assert.AreEqual(0f, SpiritMath.DistanceToRect(new Vec2(3f, 0f), 2f, -1f, 5f, 1f), 1e-5f);
        }

        [Test]
        public void LeafSpring_SinksSlowlyStopsAtLimitAndReturns()
        {
            var p = new LeafParams();
            var leaf = new LeafSpring(p);
            for (int i = 0; i < 30; i++) leaf.Step(1f / 60f, true);
            Assert.Less(leaf.Offset, 0f);
            Assert.Greater(leaf.Offset, -p.sinkSpeed * 0.5f - 0.01f, "slow start, no drop");

            for (int i = 0; i < 600; i++) leaf.Step(1f / 60f, true);
            Assert.AreEqual(-p.maxSink, leaf.Offset, 1e-4f);
            Assert.AreEqual(0f, leaf.Velocity, 1e-3f);

            for (int i = 0; i < 600; i++) leaf.Step(1f / 60f, false);
            Assert.AreEqual(0f, leaf.Offset, 0.01f);
        }
    }
}
