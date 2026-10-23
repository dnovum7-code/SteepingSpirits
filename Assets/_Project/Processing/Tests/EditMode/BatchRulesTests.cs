using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class BatchRulesTests
    {
        public enum S
        {
            Fresh,
            Withered,
            Rolled,
            RolledTwice,
            Fixed,
            Drying,
            Dry,
            OnRack
        }

        private static LeafBatch Make(S s)
        {
            switch (s)
            {
                case S.Fresh: return Fresh();
                case S.Withered: return Withered();
                case S.Rolled: return BatchRules.Roll(Withered(), Roll(0.5f), P);
                case S.RolledTwice: return BatchRules.Roll(BatchRules.Roll(Withered(), Roll(0.5f), P), Roll(1f), P);
                case S.Fixed: return BatchRules.Fix(Withered(), Wok(0f, 1f, 0f), P);
                case S.Drying:
                    LeafBatch d = BatchRules.Place(Withered(), BatchAction.Dry, P);
                    return BatchRules.Place(BatchRules.Elapse(d, 60, P), BatchAction.TakeOut, P);
                case S.Dry: return White();
                default: return BatchRules.Place(Fresh(), BatchAction.Wither, P);
            }
        }

        // The full truth table of what each action allows in each state.
        [TestCase(S.Fresh, BatchAction.Wither, BatchReason.Ok)]
        [TestCase(S.Fresh, BatchAction.Roll, BatchReason.NotWithered)]
        [TestCase(S.Fresh, BatchAction.Oxidize, BatchReason.NotRolled)]
        [TestCase(S.Fresh, BatchAction.Fix, BatchReason.NotWithered)]
        [TestCase(S.Fresh, BatchAction.Dry, BatchReason.NotWithered)]
        [TestCase(S.Fresh, BatchAction.TakeOut, BatchReason.AlreadyOnShelf)]
        [TestCase(S.Fresh, BatchAction.Finish, BatchReason.NotDry)]
        [TestCase(S.Withered, BatchAction.Wither, BatchReason.Ok)]
        [TestCase(S.Withered, BatchAction.Roll, BatchReason.Ok)]
        [TestCase(S.Withered, BatchAction.Oxidize, BatchReason.NotRolled)]
        [TestCase(S.Withered, BatchAction.Fix, BatchReason.Ok)]
        [TestCase(S.Withered, BatchAction.Dry, BatchReason.Ok)]
        [TestCase(S.Withered, BatchAction.Finish, BatchReason.NotDry)]
        [TestCase(S.Rolled, BatchAction.Wither, BatchReason.AlreadyRolled)]
        [TestCase(S.Rolled, BatchAction.Roll, BatchReason.Ok)]
        [TestCase(S.Rolled, BatchAction.Oxidize, BatchReason.Ok)]
        [TestCase(S.Rolled, BatchAction.Fix, BatchReason.Ok)]
        [TestCase(S.Rolled, BatchAction.Dry, BatchReason.Ok)]
        [TestCase(S.RolledTwice, BatchAction.Roll, BatchReason.RolledEnough)]
        [TestCase(S.RolledTwice, BatchAction.Oxidize, BatchReason.Ok)]
        [TestCase(S.Fixed, BatchAction.Wither, BatchReason.AlreadyFixed)]
        [TestCase(S.Fixed, BatchAction.Roll, BatchReason.Ok)]
        [TestCase(S.Fixed, BatchAction.Oxidize, BatchReason.AlreadyFixed)]
        [TestCase(S.Fixed, BatchAction.Fix, BatchReason.AlreadyFixed)]
        [TestCase(S.Fixed, BatchAction.Dry, BatchReason.Ok)]
        [TestCase(S.Drying, BatchAction.Wither, BatchReason.AlreadyDry)]
        [TestCase(S.Drying, BatchAction.Roll, BatchReason.TooDry)]
        [TestCase(S.Drying, BatchAction.Oxidize, BatchReason.TooDry)]
        [TestCase(S.Drying, BatchAction.Fix, BatchReason.TooDry)]
        [TestCase(S.Drying, BatchAction.Dry, BatchReason.Ok)]
        [TestCase(S.Drying, BatchAction.Finish, BatchReason.NotDry)]
        [TestCase(S.Dry, BatchAction.Dry, BatchReason.AlreadyDry)]
        [TestCase(S.Dry, BatchAction.Finish, BatchReason.Ok)]
        [TestCase(S.OnRack, BatchAction.Roll, BatchReason.NotOnShelf)]
        [TestCase(S.OnRack, BatchAction.Dry, BatchReason.NotOnShelf)]
        [TestCase(S.OnRack, BatchAction.TakeOut, BatchReason.Ok)]
        public void TruthTable(S state, BatchAction action, BatchReason expected)
        {
            BatchCheck c = BatchRules.Check(Make(state), action, P);
            Assert.AreEqual(expected, c.reason, c.ToString());
            Assert.AreEqual(expected == BatchReason.Ok, c.ok);
        }

        [Test]
        public void Transitions_NeverChangeTheirInput()
        {
            LeafBatch w = Withered();
            float m = w.moisture;
            BatchRules.Roll(w, Roll(1f), P);
            BatchRules.Fix(w, Wok(0f, 1f, 0f), P);
            BatchRules.Elapse(w, 500, P);
            BatchRules.Place(w, BatchAction.Dry, P);
            Assert.AreEqual(m, w.moisture);
            Assert.AreEqual(0, w.rollCount);
            Assert.IsFalse(w.fixedByWok);
            Assert.AreEqual(BatchPlace.Shelf, w.place);
        }

        [Test]
        public void InvalidTransitions_ReturnTheSameBatch()
        {
            LeafBatch f = Fresh();
            Assert.AreSame(f, BatchRules.Roll(f, Roll(1f), P));
            Assert.AreSame(f, BatchRules.Fix(f, Wok(0f, 1f, 0f), P));
            Assert.AreSame(f, BatchRules.Place(f, BatchAction.Dry, P));
        }

        [Test]
        public void Withering_FollowsTheCurve_AndIsSlowerOffTheRack()
        {
            LeafBatch onRack = BatchRules.Elapse(BatchRules.Place(Fresh(), BatchAction.Wither, P), 240, P);
            Assert.That(onRack.moisture, Is.InRange(0.66f, 0.70f), "about 4 game hours to the ideal window");
            LeafBatch onShelf = BatchRules.Elapse(Fresh(), 240, P);
            Assert.Greater(onShelf.moisture, 0.88f, "off the rack the leaves stay almost fresh");
            LeafBatch forever = BatchRules.Elapse(BatchRules.Place(Fresh(), BatchAction.Wither, P), 100000, P);
            Assert.GreaterOrEqual(forever.moisture, P.wither.floor - 0.001f, "never below the floor");
        }

        [Test]
        public void StepSize_DoesNotChangeTheResult()
        {
            LeafBatch a = BatchRules.Place(BatchRules.Roll(Withered(), Roll(0.7f), P), BatchAction.Oxidize, P);
            LeafBatch big = BatchRules.Elapse(a, 60, P);
            LeafBatch small = a;
            for (int i = 0; i < 60; i++) small = BatchRules.Elapse(small, 1, P);
            Assert.AreEqual(big.oxidation, small.oxidation, 1e-4);
            Assert.AreEqual(big.moisture, small.moisture, 1e-4);
        }

        [Test]
        public void Rolling_StartsOxidation_AndAveragesEvenness()
        {
            LeafBatch r = BatchRules.Roll(Withered(), Roll(1f, 0.6f), P);
            Assert.Greater(r.oxidation, 0.03f);
            r = BatchRules.Roll(r, Roll(0.5f, 1f), P);
            Assert.AreEqual(0.8f, r.rollEvenness, 1e-5);
            Assert.AreEqual(1f, r.rolled, "strongest roll counts");
            Assert.AreEqual(2, r.rollCount);
            Assert.AreEqual(24f, r.activeSeconds, 1e-4);
        }

        [Test]
        public void RolledLeaves_OxidizeFastInTheBasket_SlowSetAside_AndUnrolledBarely()
        {
            LeafBatch rolled = BatchRules.Roll(Withered(), Roll(1f), P);
            float start = rolled.oxidation;
            LeafBatch basket = BatchRules.Elapse(BatchRules.Place(rolled, BatchAction.Oxidize, P), 60, P);
            Assert.AreEqual(start + 0.72f, basket.oxidation, 0.01f, "1.2 % per game minute");
            LeafBatch aside = BatchRules.Elapse(BatchRules.Place(basket, BatchAction.TakeOut, P), 60, P);
            Assert.AreEqual(basket.oxidation + 0.72f * P.oxidation.setAsideFactor, aside.oxidation, 0.01f);
            Assert.AreEqual(basket.oxidation, aside.oxidationAtStop, 1e-6);
            LeafBatch unrolled = BatchRules.Elapse(Withered(), 600, P);
            Assert.Less(unrolled.oxidation, 0.1f, "white tea leaves stay pale over a long day");
        }

        [Test]
        public void Wok_StopsOxidation_WeakFixLetsItCreep_TooHotRoasts()
        {
            LeafBatch rolled = BatchRules.Roll(Withered(), Roll(1f), P);
            LeafBatch good = BatchRules.Elapse(BatchRules.Fix(rolled, Wok(0f, 1f, 0f), P), 120, P);
            Assert.AreEqual(rolled.oxidation, good.oxidation, 1e-5);
            Assert.AreEqual(0f, good.roast);
            LeafBatch weak = BatchRules.Elapse(BatchRules.Fix(rolled, Wok(1f, 0f, 0f), P), 120, P);
            Assert.Greater(weak.oxidation, rolled.oxidation + 0.1f);
            LeafBatch hot = BatchRules.Fix(rolled, Wok(0f, 0.5f, 0.5f), P);
            Assert.AreEqual(0.3f, hot.roast, 1e-5);
            Assert.AreEqual(0.5f, hot.wokCare, 1e-5);
        }

        [Test]
        public void Drying_FinishesTheTea_AndOverDryingRoastsGently()
        {
            LeafBatch b = BatchRules.Place(Withered(), BatchAction.Dry, P);
            b = BatchRules.Elapse(b, P.dry.minutesToDry * P.dry.doneAt + 1, P);
            Assert.IsTrue(b.IsDry(P.dry));
            Assert.AreEqual(0f, b.roast);
            LeafBatch longer = BatchRules.Elapse(b, 1000, P);
            Assert.AreEqual(P.dry.overDryRoastCap, longer.roast, 1e-4, "roast is capped – never burnt");
            LeafBatch taken = BatchRules.Place(longer, BatchAction.TakeOut, P);
            Assert.AreEqual("W-D", taken.PathCode());
        }

        [Test]
        public void ClockGoingBackwards_PassesNoTime()
        {
            LeafBatch b = BatchRules.Place(Fresh(), BatchAction.Wither, P);
            b = BatchRules.AdvanceTo(b, 100, P);
            float m = b.moisture;
            b = BatchRules.AdvanceTo(b, 20, P);
            Assert.AreEqual(m, b.moisture);
            Assert.AreEqual(20.0, b.lastMinutes);
            b = BatchRules.AdvanceTo(b, 30, P);
            Assert.Less(b.moisture, m);
        }

        [Test]
        public void Paths_RecordTheirSteps()
        {
            Assert.AreEqual("W-D", White().PathCode());
            Assert.AreEqual("W-F-R-D", Green().PathCode());
            Assert.AreEqual("W-R-O-F-R-D", Oolong(0.2f).PathCode());
            Assert.AreEqual("W-R-O-D", Black().PathCode());
        }
    }
}
