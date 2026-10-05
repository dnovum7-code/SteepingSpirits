using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class SquashStretchTests
    {
        [Test]
        public void Landing_SquashesWithSpeedUpToCap()
        {
            var p = new FeedbackParams();
            var s = new SquashStretch(p);
            s.Land(5f);
            Assert.AreEqual(-5f * p.landSquashPerSpeed, s.Amount, 1e-5f);
            Assert.Less(s.Scale.y, 1f);
            Assert.Greater(s.Scale.x, 1f);

            s.Land(500f);
            Assert.AreEqual(-p.landSquashMax, s.Amount, 1e-5f);
        }

        [Test]
        public void Jump_StretchesAndRecoversQuickly()
        {
            var s = new SquashStretch(new FeedbackParams());
            s.Jump();
            Assert.Greater(s.Scale.y, 1f);
            for (int i = 0; i < 18; i++) s.Update(1f / 60f);
            Assert.Less(System.Math.Abs(s.Amount), 0.01f, "back to normal within 0.3 s");
        }
    }
}
