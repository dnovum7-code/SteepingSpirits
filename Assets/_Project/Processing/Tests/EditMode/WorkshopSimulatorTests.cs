using NUnit.Framework;

namespace SteepingSpirits.Processing.Tests
{
    /// <summary>Three bots process 100 batches each through the real rules and minigame models.</summary>
    public class WorkshopSimulatorTests
    {
        private static WorkshopSimResult Run(WorkshopBot bot)
        {
            WorkshopSimResult r = WorkshopSimulator.Run(new ProcessingParams(), bot, 100);
            TestContext.WriteLine(r.ToString());
            TestContext.WriteLine(r.DurationLine());
            return r;
        }

        [Test]
        public void Careful_HitsTheWantedTea_AndTheBestQuality()
        {
            WorkshopSimResult careful = Run(WorkshopBot.Careful);
            WorkshopSimResult sloppy = Run(WorkshopBot.Sloppy);
            WorkshopSimResult random = Run(WorkshopBot.Random);
            Assert.GreaterOrEqual(careful.HitRate, 0.95f);
            Assert.Greater(careful.HitRate, sloppy.HitRate);
            Assert.Greater(careful.AverageTier, sloppy.AverageTier);
            Assert.Greater(careful.AverageTier, random.AverageTier);
            Assert.AreEqual(0, careful.byTier[1], "careful work never gives the lowest tier");
        }

        [Test]
        public void EveryBot_AlwaysGetsAUsableTea()
        {
            foreach (WorkshopBot bot in new[] { WorkshopBot.Careful, WorkshopBot.Sloppy, WorkshopBot.Random })
            {
                WorkshopSimResult r = Run(bot);
                Assert.AreEqual(0, r.invalid, bot.ToString());
                Assert.AreEqual(100, r.byTier[1] + r.byTier[2] + r.byTier[3], bot.ToString());
            }
        }

        [Test]
        public void ActiveTime_OolongAtMost90Seconds_WhiteOnlyAFewSeconds()
        {
            WorkshopSimResult careful = Run(WorkshopBot.Careful);
            Assert.LessOrEqual(careful.maxActiveByWanted[(int)TeaVariety.LightOolong], 90f);
            Assert.LessOrEqual(careful.maxActiveByWanted[(int)TeaVariety.DarkOolong], 90f);
            Assert.LessOrEqual(careful.maxActiveByWanted[(int)TeaVariety.White], 8f);
            Assert.Less(careful.AverageActive(TeaVariety.White), careful.AverageActive(TeaVariety.Green));
            Assert.Less(careful.AverageActive(TeaVariety.Green), careful.AverageActive(TeaVariety.DarkOolong), "oolong is the master class");
        }

        [Test]
        public void Simulation_IsDeterministic()
        {
            Assert.AreEqual(Run(WorkshopBot.Sloppy).ToString(), Run(WorkshopBot.Sloppy).ToString());
        }
    }
}
