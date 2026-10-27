using System.Text;
using NUnit.Framework;

namespace SteepingSpirits.Processing.Tests
{
    /// <summary>
    /// The tea-loop evaluation (E12): results per bot by leaf origin and by tea, how much the garden care counts,
    /// and options for the quality rule. Prints the tables (see Docs/TEALOOP_PROGRESS.md) and checks the shape.
    /// </summary>
    public class WorkshopEvaluationTests
    {
        private static readonly WorkshopBot[] Bots = { WorkshopBot.Careful, WorkshopBot.Sloppy, WorkshopBot.Random };

        [Test]
        public void Breakdowns_AddUpToTheBatches()
        {
            foreach (WorkshopBot bot in Bots)
            {
                WorkshopSimResult r = WorkshopSimulator.Run(new ProcessingParams(), bot, 100);
                TestContext.WriteLine(r.OriginLine());
                TestContext.WriteLine(r.VarietyLine());
                int origin = 0, variety = 0, premiumOnly = 0;
                for (int t = 1; t <= 3; t++)
                {
                    for (int o = 0; o < 4; o++) origin += r.byOriginTier[o, t];
                    for (int v = 0; v < 5; v++) variety += r.byVarietyTier[v, t];
                    premiumOnly += r.byTierPremiumOnly[t];
                }

                Assert.AreEqual(100, origin, bot.ToString());
                Assert.AreEqual(100, variety, bot.ToString());
                Assert.AreEqual(100, premiumOnly, bot.ToString());
            }
        }

        [Test]
        public void OrdinaryLeaves_StopAtFine_PremiumReachesExquisite()
        {
            var p = new ProcessingParams();
            for (int origin = 0; origin < 4; origin++)
            {
                WorkshopSimResult careful = WorkshopSimulator.Run(p, WorkshopBot.Careful, 200, 1, origin);
                TestContext.WriteLine(WorkshopSimResult.OriginNames[origin] + ": " + careful.OriginLine());
                if (origin < 2) Assert.AreEqual(0, careful.byTier[3], "garden gut/fein never gives erlesen");
                else Assert.Greater(careful.byTier[3], 150, "careful work on top leaves is nearly always erlesen");
            }
        }

        /// <summary>The tables of the evaluation: fixed origin per bot, and the three options for the quality rule.</summary>
        [Test]
        public void Evaluation_Tables()
        {
            var sb = new StringBuilder("\nTiers gut/fein/erlesen (avg) per bot and leaf origin, 200 batches each\n");
            foreach (WorkshopBot bot in Bots)
            {
                sb.Append($"{bot,-8}");
                for (int origin = 0; origin < 4; origin++)
                {
                    WorkshopSimResult r = WorkshopSimulator.Run(new ProcessingParams(), bot, 200, 1, origin);
                    sb.Append($" | {WorkshopSimResult.OriginNames[origin]} {r.byTier[1]}/{r.byTier[2]}/{r.byTier[3]} ({r.AverageTier:0.00})");
                }

                sb.Append('\n');
            }

            sb.Append("\nMixed leaves (50 % erlesen / 30 % fein / 20 % gut, 5 % first flush), 100 batches\n");
            foreach (WorkshopBot bot in Bots)
            {
                WorkshopSimResult r = WorkshopSimulator.Run(new ProcessingParams(), bot, 100);
                sb.Append(r.OriginLine()).Append('\n').Append(r.VarietyLine()).Append('\n');
            }

            sb.Append("\nOptions (mixed leaves, 100 batches): erlesen per bot  A now | B only premium lifts | C origin weight 0.45\n");
            ProcessingParams c = new ProcessingParams().Clone();
            c.quality.originWeight = 0.45f;
            foreach (WorkshopBot bot in Bots)
            {
                WorkshopSimResult a = WorkshopSimulator.Run(new ProcessingParams(), bot, 100);
                WorkshopSimResult cr = WorkshopSimulator.Run(c, bot, 100);
                sb.Append($"{bot,-8} A {a.byTier[1]}/{a.byTier[2]}/{a.byTier[3]} ({a.AverageTier:0.00})");
                sb.Append($" | B {a.byTierPremiumOnly[1]}/{a.byTierPremiumOnly[2]}/{a.byTierPremiumOnly[3]}");
                sb.Append($" | C {cr.byTier[1]}/{cr.byTier[2]}/{cr.byTier[3]} ({cr.AverageTier:0.00})\n");
            }

            TestContext.WriteLine(sb.ToString());
            Assert.Pass();
        }
    }
}
