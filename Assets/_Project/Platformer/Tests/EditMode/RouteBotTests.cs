using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    /// <summary>The route bot plays every level in the engine-free tile simulation.</summary>
    public class RouteBotTests
    {
        public struct BotResult
        {
            public bool finished;
            public float seconds;
            public int catches;
            public int replans;
            public string log;
        }

        public static BotResult Play(LevelLayout layout, float maxSeconds = 240f)
        {
            var sim = new TileWorldSim(layout);
            var log = new System.Text.StringBuilder();
            LevelMarker goal = layout.All(TileKind.Goal)[0];
            int replans = 0;
            RouteFollower bot = Replan(sim, layout, goal, log);
            int lastCatches = 0;
            const float dt = 1f / 60f;
            while (sim.Time < maxSeconds && !sim.Finished)
            {
                if (bot == null) break;
                MotorInput input = bot.Tick(dt, sim.Observe());
                sim.Step(dt, input);
                if (sim.Catches != lastCatches || bot.Failed || (bot.Done && !sim.Finished))
                {
                    log.AppendLine(bot.Failed ? "  fail: " + bot.FailReason : sim.Catches != lastCatches
                        ? $"  caught at t={sim.Time:0.0}s during step {bot.Index} ({bot.Current})" : "  route done but goal not touched");
                    lastCatches = sim.Catches;
                    if (++replans > 12) break;
                    bot = Replan(sim, layout, goal, log);
                }
            }

            return new BotResult { finished = sim.Finished, seconds = sim.Time, catches = sim.Catches, replans = replans, log = log.ToString() };
        }

        private static RouteFollower Replan(TileWorldSim sim, LevelLayout layout, LevelMarker goal, System.Text.StringBuilder log)
        {
            LevelReachability r = sim.Reachability;
            LevelReachability.Cell? start = RoutePlanner.NearestStanding(r, sim.Feet);
            if (start == null) return null;
            List<RouteStep> route = RoutePlanner.Plan(r, layout, start.Value, c => c.y == goal.y && Math.Abs(c.x - goal.x) <= 0);
            if (route == null)
            {
                log.AppendLine("  no route from " + start.Value);
                return null;
            }

            log.AppendLine($"  route from {start.Value}: " + string.Join(", ", route));
            return new RouteFollower(route, r, sim.Motor.Params) { SeatOf = sim.SeatNear };
        }

        [Test]
        public void Bot_FinishesAFlatLevelWithAGap()
        {
            BotResult r = Play(LevelLayout.Parse("P.........E\n####...####"));
            Assert.IsTrue(r.finished, r.log);
            Assert.AreEqual(0, r.catches, r.log);
        }

        [Test]
        public void Bot_ClimbsSteps()
        {
            const string text =
                ".........E\n" +
                "......####\n" +
                "...###....\n" +
                "P.........\n" +
                "##########\n";
            BotResult r = Play(LevelLayout.Parse(text));
            Assert.IsTrue(r.finished, r.log);
        }

        [Test]
        public void Bot_RidesTheWind()
        {
            const string text =
                ".......E\n" +
                ".......#\n" +
                ".......#\n" +
                ".......#\n" +
                ".......#\n" +
                "P...W..#\n" +
                "########\n";
            BotResult r = Play(LevelLayout.Parse(text));
            Assert.IsTrue(r.finished, r.log);
        }

        [Test]
        public void Bot_UsesTheSwingOverAWideGap()
        {
            const string text =
                "...........O..............\n" +
                "..........................\n" +
                "..........................\n" +
                "..........................\n" +
                "P.......................E.\n" +
                "#######.........##########\n";
            BotResult r = Play(LevelLayout.Parse(text));
            Assert.IsTrue(r.finished, r.log);
        }

        [Test]
        public void Bot_TakesTheUpperWayInLevel2WhenTheSwingIsGone()
        {
            string text = LevelFiles.Read("Level2");
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("@") && !lines[i].StartsWith("//")) lines[i] = lines[i].Replace('O', '.');
            }

            BotResult r = Play(LevelLayout.Parse(string.Join("\n", lines)));
            Assert.IsTrue(r.finished, r.log);
        }

        [Test]
        public void Bot_FinishesEveryLevelFile()
        {
            foreach (string f in LevelFiles.All())
            {
                LevelLayout l = LevelLayout.Parse(File.ReadAllText(f));
                if (l.IsHub) continue;
                BotResult r = Play(l);
                TestContext.WriteLine($"{Path.GetFileName(f)}: finished={r.finished} t={r.seconds:0.0}s catches={r.catches}\n{r.log}");
                Assert.IsTrue(r.finished, Path.GetFileName(f) + "\n" + r.log);
            }
        }
    }
}
