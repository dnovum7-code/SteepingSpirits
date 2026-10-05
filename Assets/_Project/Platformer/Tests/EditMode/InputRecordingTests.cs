using System;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class InputRecordingTests
    {
        private static InputRecording Sample()
        {
            var r = new InputRecording { levelId = "Level1", start = new Vec2(2.5f, 2f), assists = "speed=1.0;air=0;fall=0" };
            r.Record(new RecordedInput { time = 0f });
            r.Record(new RecordedInput { time = 0.1f, moveX = 1f });
            r.Record(new RecordedInput { time = 0.2f, moveX = 1f });
            r.Record(new RecordedInput { time = 0.3f, moveX = 1f, jumpHeld = true, jumpPressed = true });
            r.Record(new RecordedInput { time = 0.4f, moveX = 1f, jumpHeld = true });
            r.Record(new RecordedInput { time = 0.6f, moveX = 0.333f });
            r.End(1.5f);
            return r;
        }

        [Test]
        public void OnlyChangesAndPressesAreStored()
        {
            InputRecording r = Sample();
            Assert.AreEqual(5, r.entries.Count, "repeated identical samples are skipped, end marker added");
            Assert.AreEqual(1.5f, r.Duration, 1e-5f);
            Assert.AreEqual(0.33f, r.entries[3].moveX, 1e-5f, "quantized to 0.01");
        }

        [Test]
        public void SerializeParse_RoundTrip()
        {
            InputRecording r = Sample();
            InputRecording back = InputRecording.Parse(r.Serialize());
            Assert.AreEqual("Level1", back.levelId);
            Assert.AreEqual(2.5f, back.start.x, 1e-4f);
            Assert.AreEqual(r.assists, back.assists);
            Assert.AreEqual(r.entries.Count, back.entries.Count);
            for (int i = 0; i < r.entries.Count; i++)
            {
                Assert.AreEqual(r.entries[i].time, back.entries[i].time, 1e-3f);
                Assert.AreEqual(r.entries[i].moveX, back.entries[i].moveX, 1e-4f);
                Assert.AreEqual(r.entries[i].jumpPressed, back.entries[i].jumpPressed);
            }
        }

        [Test]
        public void Parse_RejectsForeignFilesAndNewerVersions()
        {
            Assert.Throws<FormatException>(() => InputRecording.Parse("hello"));
            Assert.Throws<FormatException>(() => InputRecording.Parse("jnrrec 99\n"));
        }

        [Test]
        public void Playback_DeliversPressesEvenWhenFramesAreSkipped()
        {
            var p = new InputPlayback(Sample());
            Assert.AreEqual(0f, p.Sample(0.05f).moveX);
            RecordedInput big = p.Sample(0.5f); // jumps over the press at 0.3
            Assert.IsTrue(big.jumpPressed);
            Assert.IsTrue(big.jumpHeld);
            Assert.IsFalse(p.Sample(0.55f).jumpPressed, "a press is delivered once");
            Assert.AreEqual(0.33f, p.Sample(0.7f).moveX, 1e-5f);
            Assert.IsFalse(p.Finished);
            p.Sample(2f);
            Assert.IsTrue(p.Finished);
        }

        [Test]
        public void ReplayedInputs_GiveTheSameRunInTheSimulation()
        {
            LevelLayout l = LevelLayout.Parse(LevelFiles.Read("Level1"));

            // Record a bot run …
            var sim = new TileWorldSim(l);
            var route = RoutePlanner.Plan(sim.Reachability, l, RoutePlanner.NearestStanding(sim.Reachability, sim.Feet).Value,
                c => c.y == l.All(TileKind.Goal)[0].y && c.x == l.All(TileKind.Goal)[0].x);
            var bot = new RouteFollower(route, sim.Reachability, sim.Motor.Params) { SeatOf = sim.SeatNear };
            var rec = new InputRecording { levelId = "Level1", start = sim.Feet };
            const float dt = 1f / 60f;
            int frame = 0;
            while (!sim.Finished && sim.Time < 60f)
            {
                MotorInput mi = bot.Tick(dt, sim.Observe());
                mi.moveX = (float)Math.Round(mi.moveX * 100f) / 100f; // what a recording keeps
                mi.moveY = (float)Math.Round(mi.moveY * 100f) / 100f;
                rec.Record(new RecordedInput { time = frame * dt, moveX = mi.moveX, moveY = mi.moveY, jumpHeld = mi.jumpHeld, jumpPressed = mi.jumpPressed });
                sim.Step(dt, mi);
                frame++;
            }

            Assert.IsTrue(sim.Finished);
            rec.End(frame * dt);

            // … and play it back from the text file.
            var replaySim = new TileWorldSim(l);
            var playback = new InputPlayback(InputRecording.Parse(rec.Serialize()));
            for (int i = 0; i < frame && !replaySim.Finished; i++)
            {
                RecordedInput s = playback.Sample(i * dt + 1e-4f);
                replaySim.Step(dt, new MotorInput { moveX = s.moveX, moveY = s.moveY, jumpHeld = s.jumpHeld, jumpPressed = s.jumpPressed });
            }

            Assert.IsTrue(replaySim.Finished, "the replay reaches the goal like the original");
            Assert.AreEqual(sim.Catches, replaySim.Catches);
        }
    }
}
