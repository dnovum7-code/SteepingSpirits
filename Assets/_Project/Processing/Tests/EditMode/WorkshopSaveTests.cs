using System;
using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Ingredients;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class WorkshopSaveTests
    {
        private static string Snap(LeafBatch b) =>
            $"{b.id}|{b.amount}|{b.sourceId}|{b.originTier}|{b.firstFlush}|{b.moisture:R}|{b.rolled:R}|{b.rollEvenness:R}|{b.rollCount}|{b.oxidation:R}|" +
            $"{b.oxidationStopped}|{b.oxidationAtStop:R}|{b.fixedByWok}|{b.wokCare:R}|{b.fixResidual:R}|{b.roast:R}|{b.roastFromDrying:R}|{b.dryness:R}|" +
            $"{b.workMoisture:R}|{b.place}|{b.lastMinutes:R}|{b.activeSeconds:R}|{b.PathCode()}";

        private static Workshop Busy(ManualGameTime clock)
        {
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch a = w.Add("tea_leaf", 3, 4, false);
            LeafBatch b = w.Add("tea_leaf_first", 1, 2, true);
            LeafBatch c = w.Add("golden_tip", 2, 1, true);
            w.Do(a.id, BatchAction.Wither);
            w.Do(b.id, BatchAction.Wither);
            clock.Advance(250);
            w.Do(b.id, BatchAction.TakeOut);
            w.Roll(b.id, Roll(0.5f, 0.8f));
            w.Do(b.id, BatchAction.Oxidize);
            clock.Advance(17.25);
            w.Sync();
            return w;
        }

        [Test]
        public void RoundTrip_KeepsEveryBatchExactly()
        {
            var clock = new ManualGameTime(3000);
            Workshop w = Busy(clock);
            WorkshopSave s = WorkshopSave.From(w);
            s.starterGiven = true;
            s.showPercent = true;
            s.evenRolling = true;
            s.Record(ProcessedTea.From(Oolong(0.2f), P));
            s.Record(ProcessedTea.From(Oolong(0.22f), P));
            string json = s.ToJson();

            WorkshopSave back = WorkshopSave.FromJson(json);
            var w2 = new Workshop(new ProcessingParams(), clock);
            back.ApplyTo(w2);
            Assert.AreEqual(w.Batches.Count, w2.Batches.Count);
            for (int i = 0; i < w.Batches.Count; i++) Assert.AreEqual(Snap(w.Batches[i]), Snap(w2.Batches[i]));
            Assert.AreEqual(w.NextId, w2.NextId);
            Assert.IsTrue(back.starterGiven && back.showPercent && back.evenRolling);
            TeaLedgerEntry e = back.ledger[ProcessedTea.From(Oolong(0.2f), P).Id];
            Assert.AreEqual(10, e.made);
            Assert.AreEqual(0.21f, e.AverageOxidation, 0.01f);
            Assert.AreEqual(json, back.ToJson(), "stable output");
        }

        [Test]
        public void RunningPassiveSteps_ContinueAfterLoading()
        {
            var clock = new ManualGameTime(3000);
            Workshop w = Busy(clock);
            string json = WorkshopSave.From(w).ToJson();
            clock.Advance(120);
            w.Sync();

            var w2 = new Workshop(new ProcessingParams(), clock);
            WorkshopSave.FromJson(json).ApplyTo(w2);
            w2.Sync();
            for (int i = 0; i < w.Batches.Count; i++) Assert.AreEqual(Snap(w.Batches[i]), Snap(w2.Batches[i]), "loaded later = as if it had run");
        }

        [Test]
        public void EmptyText_IsFresh_NewerOrBrokenIsRefused()
        {
            Assert.AreEqual(0, WorkshopSave.FromJson("").batches.Count);
            Assert.Throws<FormatException>(() => WorkshopSave.FromJson("{\"version\":2}"));
            Assert.Throws<FormatException>(() => WorkshopSave.FromJson("[]"));
        }

        [Test]
        public void LogLine_IsOneParsableLine()
        {
            ProcessedTea tea = ProcessedTea.From(Oolong(0.5f), P);
            string line = ProcessingLog.Line(tea, 4321);
            StringAssert.DoesNotContain("\n", line);
            Dictionary<string, object> d = MiniJson.ParseObject(line);
            Assert.AreEqual("W-R-O-F-R-D", d["path"]);
            Assert.AreEqual("DarkOolong", d["tea"]);
            Assert.AreEqual(0.5, (double)d["targetOxidation"], 0.02);
            Assert.AreEqual(3.0, d["quality"]);
            Assert.IsInstanceOf<List<object>>(d["notes"]);
            Assert.AreEqual(36.0, (double)d["activeSeconds"], 0.1, "two rolls and a wok, 12 s each");
            StringAssert.Contains("\"targetOxidation\":null", ProcessingLog.Line(ProcessedTea.From(White(), P), 1));
        }
    }
}
