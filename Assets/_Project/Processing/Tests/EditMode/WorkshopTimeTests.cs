using NUnit.Framework;

namespace SteepingSpirits.Processing.Tests
{
    public class WorkshopTimeTests
    {
        [Test]
        public void PassiveSteps_RunOnGameTime_WhileThePlayerIsAway()
        {
            var clock = new ManualGameTime(1000);
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch b = w.Add("tea_leaf", 2, 4, false);
            Assert.IsTrue(w.Do(b.id, BatchAction.Wither));
            clock.Advance(240);
            w.Sync();
            Assert.That(w.Get(b.id).moisture, Is.InRange(0.66f, 0.70f));
        }

        [Test]
        public void DayAndMinute_IsOneContinuousTime()
        {
            int day = 3;
            double minute = 600;
            var t = new DayAndMinuteTime(() => day, () => minute);
            Assert.AreEqual(3 * 1440 + 600, t.Minutes);
            day = 4;
            minute = 360;
            Assert.AreEqual(4 * 1440 + 360, t.Minutes);
        }

        [Test]
        public void FastForward_AddsAnOffset()
        {
            var inner = new ManualGameTime(50);
            var t = new SkippableTime(inner);
            t.FastForward(120);
            inner.Advance(10);
            Assert.AreEqual(180, t.Minutes);
            t.FastForward(-5);
            Assert.AreEqual(120, t.Offset);
        }

        [Test]
        public void FastForward_ThroughTheWorkshop_DriesTheTea()
        {
            var t = new SkippableTime(new ManualGameTime());
            var w = new Workshop(new ProcessingParams(), t);
            LeafBatch b = w.Add("tea_leaf", 3, 2, false);
            w.Do(b.id, BatchAction.Wither);
            t.FastForward(240);
            w.Do(b.id, BatchAction.TakeOut);
            w.Do(b.id, BatchAction.Dry);
            t.FastForward(200);
            w.Sync();
            Assert.IsTrue(w.Get(b.id).IsDry(w.settings.dry));
        }

        [Test]
        public void ClockReset_PassesNoTime_ThenContinues()
        {
            var clock = new ManualGameTime(5000);
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch saved = w.Add("tea_leaf", 1, 1, false);
            w.Do(saved.id, BatchAction.Wither);

            // New session: the game clock starts at day 1 again.
            var fresh = new ManualGameTime(100);
            var w2 = new Workshop(new ProcessingParams(), fresh);
            w2.Restore(w.Get(saved.id));
            w2.Sync();
            float m = w2.Get(saved.id).moisture;
            Assert.AreEqual(1f, m, 1e-6);
            fresh.Advance(240);
            w2.Sync();
            Assert.Less(w2.Get(saved.id).moisture, 0.7f);
            Assert.AreEqual(saved.id + 1, w2.NextId);
        }

        [Test]
        public void InvalidActions_ReportFalse_AndChangeNothing()
        {
            var w = new Workshop(new ProcessingParams(), new ManualGameTime());
            LeafBatch b = w.Add("tea_leaf", 1, 1, false);
            Assert.IsFalse(w.Roll(b.id, new RollOutcome { intensity = 1f, evenness = 1f }));
            Assert.IsFalse(w.Do(99, BatchAction.Wither));
            Assert.AreEqual(BatchReason.NotWithered, w.Check(b.id, BatchAction.Fix).reason);
        }
    }
}
