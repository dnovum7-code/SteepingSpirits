using NUnit.Framework;

namespace SteepingSpirits.Processing.Tests
{
    public class MinigameTests
    {
        private static readonly RollParams R = new RollParams();
        private static readonly WokParams W = new WokParams();

        private static RollSession Knead(float interval, int kneads)
        {
            var s = new RollSession(R);
            for (int i = 0; i < kneads; i++)
            {
                if (i > 0) s.Tick(interval);
                s.Knead();
            }

            return s;
        }

        [Test]
        public void EvenRhythm_GivesFullEvenness_AndAFullRollIn12Seconds()
        {
            RollSession s = Knead(R.beatSeconds, R.kneadsForFullRoll);
            Assert.IsTrue(s.Done);
            Assert.AreEqual(1f, s.Intensity);
            Assert.AreEqual(1f, s.Evenness, 1e-5);
            Assert.That(s.Time, Is.InRange(10f, 13f));
        }

        [Test]
        public void RhythmMistakes_OnlyMakeItABitUneven()
        {
            Assert.AreEqual(R.evennessFloor, Knead(0.2f, 10).Evenness, 1e-4, "hammering");
            float half = Knead(R.beatSeconds + 0.2f, 10).Evenness;
            Assert.That(half, Is.InRange(R.evennessFloor, 1f));
            Assert.GreaterOrEqual(Knead(3f, 5).Evenness, R.evennessFloor);
        }

        [Test]
        public void StoppingEarly_GivesALightRoll()
        {
            RollSession s = Knead(R.beatSeconds, 8);
            s.Stop();
            Assert.IsTrue(s.Done);
            Assert.AreEqual(0.5f, s.Outcome.intensity, 1e-5);
            Assert.Less(s.Outcome.intensity, R.lightRollMax);
        }

        [Test]
        public void ComfortHold_KneadsOnTheBeat()
        {
            var s = new RollSession(R, true);
            while (!s.Done) s.Tick(0.05f, true);
            Assert.AreEqual(1f, s.Intensity);
            Assert.AreEqual(R.comfortEvenness, s.Evenness, 1e-5);
            Assert.LessOrEqual(s.Time, R.maxSeconds);
        }

        [Test]
        public void Roll_EndsAtMaxSeconds()
        {
            var s = new RollSession(R);
            s.Knead();
            while (!s.Done) s.Tick(0.1f);
            Assert.AreEqual(R.maxSeconds, s.Time, 0.11f);
        }

        /// <summary>A calm player: stoke when below the window centre.</summary>
        public static WokOutcome Careful(int seed)
        {
            var s = new WokSession(W, seed);
            while (!s.Done) s.Tick(0.02f, s.Heat < s.WindowCenter - 0.02f);
            return s.Outcome;
        }

        [Test]
        public void CarefulStoking_StaysMostlyInTheWindow()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                WokOutcome o = Careful(seed);
                Assert.Greater(o.inside / o.seconds, 0.75f, "seed " + seed);
            }
        }

        [Test]
        public void NeverStoking_IsTooCold_AlwaysStoking_IsTooHot()
        {
            var cold = new WokSession(W, 1);
            var hot = new WokSession(W, 1);
            while (!cold.Done)
            {
                cold.Tick(0.02f, false);
                hot.Tick(0.02f, true);
            }

            Assert.Greater(cold.Outcome.under, cold.Outcome.inside);
            Assert.Greater(hot.Outcome.over, hot.Outcome.inside);
            Assert.AreEqual(W.seconds, cold.Outcome.seconds, 0.03f);
        }

        [Test]
        public void Wok_IsDeterministicPerSeed()
        {
            Assert.AreEqual(Careful(5).inside, Careful(5).inside);
            Assert.AreNotEqual(new WokSession(W, 1).CenterAt(3f), new WokSession(W, 2).CenterAt(3f));
        }
    }
}
