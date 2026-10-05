using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    public class MemorySparkTests
    {
        private static MemorySpark NewSpark(out SparkParams p, bool allowed = true)
        {
            p = new SparkParams();
            return new MemorySpark(p, 1f, allowed);
        }

        [Test]
        public void AppearsOnceWhenQualityRisesPastThreshold()
        {
            MemorySpark spark = NewSpark(out SparkParams p);
            int appeared = 0;
            spark.StateChanged += s => { if (s == SparkState.Visible) appeared++; };

            spark.Tick(0.1f, 0.5f);
            Assert.AreEqual(SparkState.None, spark.State);
            spark.Tick(0.1f, p.qShareThreshold + 0.01f);
            Assert.AreEqual(SparkState.Visible, spark.State);

            // Dropping and rising again does not spawn a second spark.
            spark.TryCatch(1f);
            spark.Tick(0.1f, 0.2f);
            spark.Tick(0.1f, 0.99f);
            Assert.AreEqual(1, appeared);
        }

        [Test]
        public void DoesNotAppearWhenStartingAboveThreshold()
        {
            MemorySpark spark = NewSpark(out _);
            spark.Tick(0.1f, 0.99f);
            spark.Tick(0.1f, 0.99f);
            Assert.AreEqual(SparkState.None, spark.State, "needs an upward crossing");
        }

        [Test]
        public void MissedAfterWindowWithoutPenalty()
        {
            MemorySpark spark = NewSpark(out SparkParams p);
            spark.Tick(0.1f, 0f);
            spark.Tick(0.1f, 1f);
            for (float t = 0f; t <= p.catchWindowSeconds + 0.2f; t += 0.1f) spark.Tick(0.1f, 1f);

            Assert.AreEqual(SparkState.Missed, spark.State);
            Assert.AreEqual(0f, spark.Bonus(20f), 1e-6f);
            Assert.IsFalse(spark.TryCatch(20f));
        }

        [Test]
        public void BonusGrowsWhileFollowingAndCapsAtMax()
        {
            MemorySpark spark = NewSpark(out SparkParams p);
            spark.Tick(0.1f, 0f);
            spark.Tick(0.1f, 1f);
            Assert.IsTrue(spark.TryCatch(5f));

            Assert.AreEqual(p.bonusOnCatch, spark.Bonus(5f), 1e-5f);
            Assert.That(spark.Bonus(5f + p.followSeconds * 0.5f), Is.InRange(p.bonusOnCatch, p.bonusMax));
            Assert.AreEqual(p.bonusMax, spark.Bonus(5f + p.followSeconds), 1e-5f);
            Assert.AreEqual(p.bonusMax, spark.Bonus(99f), 1e-5f);
            Assert.AreEqual(1f, spark.Depth(99f), 1e-5f);
        }

        [Test]
        public void DisabledOrNotAllowedSparksNeverAppear()
        {
            MemorySpark notAllowed = NewSpark(out _, allowed: false);
            var off = new SparkParams { enabled = false };
            var disabled = new MemorySpark(off, 1f, true);

            foreach (MemorySpark s in new[] { notAllowed, disabled })
            {
                s.Tick(0.1f, 0f);
                s.Tick(0.1f, 1f);
                Assert.AreEqual(SparkState.None, s.State);
            }
        }
    }
}
