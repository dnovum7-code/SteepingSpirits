using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class JumpMathTests
    {
        [Test]
        public void GravityAndVelocity_ReachHeightAtApexTime()
        {
            float h = 3.2f, t = 0.38f;
            float g = JumpMath.Gravity(h, t);
            float v0 = JumpMath.LaunchVelocity(h, t);

            Assert.AreEqual(0f, v0 - g * t, 1e-4f, "vy must be zero at the apex");
            Assert.AreEqual(h, v0 * t - 0.5f * g * t * t, 1e-4f);
            Assert.AreEqual(h, JumpMath.PeakHeight(v0, g), 1e-4f);
        }

        [Test]
        public void VelocityForHeight_IsInverseOfPeakHeight()
        {
            float g = 40f;
            Assert.AreEqual(2.5f, JumpMath.PeakHeight(JumpMath.VelocityForHeight(2.5f, g), g), 1e-4f);
        }

        [Test]
        public void FlatJumpDistance_ShorterWithStrongerFallGravity()
        {
            float slow = JumpMath.FlatJumpDistance(3f, 0.4f, 1f, 8f);
            float fast = JumpMath.FlatJumpDistance(3f, 0.4f, 1.8f, 8f);
            Assert.AreEqual(8f * 0.8f, slow, 1e-3f);
            Assert.Less(fast, slow);
        }

        [Test]
        public void MovementParams_DerivedValuesMatchFormula()
        {
            var p = new MovementParams();
            Assert.AreEqual(2f * p.jumpHeight / (p.timeToApex * p.timeToApex), p.Gravity, 1e-3f);
            Assert.AreEqual(p.JumpVelocity * 2.5f, p.MaxFallSpeed, 1e-3f);
            Assert.AreEqual(0.65f, p.airControl, 1e-6f);
        }

        [Test]
        public void PMath_DampIsFrameRateIndependent()
        {
            float once = PMath.Damp(0f, 1f, 5f, 0.2f);
            float twice = PMath.Damp(PMath.Damp(0f, 1f, 5f, 0.1f), 1f, 5f, 0.1f);
            Assert.AreEqual(once, twice, 1e-5f);
        }
    }
}
