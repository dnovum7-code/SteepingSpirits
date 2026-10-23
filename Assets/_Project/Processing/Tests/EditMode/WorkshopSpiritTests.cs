using System.Collections.Generic;
using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class WorkshopSpiritTests
    {
        private static SpiritMastery Master() => new SpiritMastery { goodWithers = 3, dried = 3, goodWhites = 2 };

        [Test]
        public void Untrained_SpiritDoesNothing()
        {
            var clock = new ManualGameTime();
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch b = w.Add("tea_leaf", 3, 4, false);
            w.Do(b.id, BatchAction.Wither);
            clock.Advance(600);
            Assert.AreEqual(0, WorkshopSpirit.Tend(w, new SpiritMastery(), null).Count);
            Assert.AreEqual(BatchPlace.WitherRack, w.Get(b.id).place);
        }

        [Test]
        public void Mastered_TakesLeavesOffTheRack_AtTheRightMoment()
        {
            var clock = new ManualGameTime();
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch b = w.Add("tea_leaf", 3, 4, false);
            w.Do(b.id, BatchAction.Wither);
            clock.Advance(60);
            Assert.AreEqual(0, WorkshopSpirit.Tend(w, Master(), null).Count, "too early");
            List<SpiritDeed> deeds = new List<SpiritDeed>();
            for (int i = 0; i < 100 && deeds.Count == 0; i++)
            {
                clock.Advance(5);
                deeds = WorkshopSpirit.Tend(w, Master(), null);
            }

            Assert.AreEqual(SpiritDeedKind.TookOffRack, deeds[0].kind);
            Assert.AreEqual(1f, TeaQuality.WitherCare(BatchRules.Place(w.Get(b.id), BatchAction.Dry, P), P.wither), "in the ideal window");
        }

        [Test]
        public void HandedOverWhiteTea_GoesToTheDryer_AndComesOutDry()
        {
            var clock = new ManualGameTime();
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch b = w.Add("tea_leaf", 3, 4, false);
            w.Do(b.id, BatchAction.Wither);
            var intent = new HashSet<int> { b.id };
            for (int i = 0; i < 200; i++)
            {
                clock.Advance(5);
                WorkshopSpirit.Tend(w, Master(), intent);
            }

            LeafBatch done = w.Get(b.id);
            Assert.AreEqual(BatchPlace.Shelf, done.place);
            Assert.IsTrue(done.IsDry(P.dry));
            Assert.Less(done.roast, P.notes.roastyFrom, "taken out before it roasts");
            Assert.IsTrue(w.Finish(b.id, out ProcessedTea tea));
            Assert.AreEqual(TeaVariety.White, tea.variety);
            Assert.AreEqual(0, intent.Count);
        }

        [Test]
        public void Mastery_GrowsFromFinishedTeas_AndIsSaved()
        {
            var m = new SpiritMastery();
            for (int i = 0; i < 3; i++)
            {
                LeafBatch white = White();
                m.Record(ProcessedTea.From(white, P), TeaQuality.Evaluate(white, P), P.spirit);
            }

            Assert.IsTrue(m.Wither(P.spirit) && m.Dry(P.spirit) && m.White(P.spirit));
            var save = new WorkshopSave();
            save.mastery.goodWithers = 5;
            save.mastery.dried = 7;
            save.spiritWhite.Add(12);
            WorkshopSave back = WorkshopSave.FromJson(save.ToJson());
            Assert.AreEqual(5, back.mastery.goodWithers);
            Assert.AreEqual(7, back.mastery.dried);
            CollectionAssert.AreEqual(new[] { 12 }, back.spiritWhite);
        }

        [Test]
        public void TheSpirit_NeverTouchesActiveSteps()
        {
            var clock = new ManualGameTime();
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch b = w.Add("tea_leaf", 3, 4, false);
            w.Do(b.id, BatchAction.Wither);
            clock.Advance(240);
            w.Do(b.id, BatchAction.TakeOut);
            w.Roll(b.id, Roll(1f));
            w.Do(b.id, BatchAction.Oxidize);
            clock.Advance(500);
            Assert.AreEqual(0, WorkshopSpirit.Tend(w, Master(), new HashSet<int> { b.id }).Count);
            Assert.AreEqual(BatchPlace.OxidationBasket, w.Get(b.id).place, "stopping oxidation stays with the player");
        }
    }
}
