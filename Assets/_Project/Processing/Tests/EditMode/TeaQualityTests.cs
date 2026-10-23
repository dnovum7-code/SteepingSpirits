using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class TeaQualityTests
    {
        [Test]
        public void CarefulWork_WithErlesenLeaves_IsErlesen_ForEveryTea()
        {
            Assert.AreEqual(3, TeaQuality.Tier(White(), P));
            Assert.AreEqual(3, TeaQuality.Tier(Green(), P));
            Assert.AreEqual(3, TeaQuality.Tier(Oolong(0.2f), P));
            Assert.AreEqual(3, TeaQuality.Tier(Oolong(0.5f), P));
            Assert.AreEqual(3, TeaQuality.Tier(Black(), P));
        }

        [Test]
        public void OrdinaryLeaves_StopAtFein_FirstFlushLiftsTheCeiling()
        {
            Assert.AreEqual(2, TeaQuality.Tier(Oolong(0.2f, 1), P));
            Assert.AreEqual(2, TeaQuality.Tier(Oolong(0.2f, 2), P));
            CareReport first = TeaQuality.Evaluate(DryFully(Withered(1, 240, true)), P);
            Assert.AreEqual(3, first.ceiling);
            Assert.AreEqual(3, first.tier);
        }

        [Test]
        public void UnevenRolling_OnlyLowersQualityABit()
        {
            LeafBatch even = BatchRules.Roll(Withered(), Roll(1f, 1f), P);
            LeafBatch uneven = BatchRules.Roll(Withered(), Roll(1f, 0.4f), P);
            CareReport a = TeaQuality.Evaluate(DryFully(OxidizeTo(even, 0.92f)), P);
            CareReport b = TeaQuality.Evaluate(DryFully(OxidizeTo(uneven, 0.92f)), P);
            Assert.Greater(a.score, b.score);
            Assert.GreaterOrEqual(b.tier, 2, "a sloppy roll alone never drops a careful batch to the lowest tier");
        }

        [Test]
        public void HittingTheSweetSpot_IsBest()
        {
            float near = TeaQuality.OxidationCare(TeaVariety.DarkOolong, 0.5f, P);
            float off = TeaQuality.OxidationCare(TeaVariety.DarkOolong, 0.65f, P);
            Assert.AreEqual(1f, near);
            Assert.Less(off, near);
            Assert.AreEqual(1f, TeaQuality.OxidationCare(TeaVariety.Black, 1f, P), "black can't be too oxidized");
            Assert.AreEqual(1f, TeaQuality.OxidationCare(TeaVariety.Green, 0f, P));
        }

        [Test]
        public void WokHeat_InTheWindow_Counts()
        {
            LeafBatch hot = BatchRules.Roll(BatchRules.Fix(Withered(), Wok(0f, 0.3f, 0.7f), P), Roll(1f), P);
            CareReport r = TeaQuality.Evaluate(DryFully(hot), P);
            Assert.AreEqual(0.3f, r.wok, 1e-5);
            Assert.Less(r.score, TeaQuality.Evaluate(Green(), P).score);
        }

        [Test]
        public void WitherWindow_Counts()
        {
            Assert.AreEqual(1f, TeaQuality.WitherCare(Withered(3, 240), P.wither));
            Assert.Less(TeaQuality.WitherCare(Withered(3, 60), P.wither), 1f, "too fresh");
            Assert.Less(TeaQuality.WitherCare(Withered(3, 1200), P.wither), 1f, "too long");
        }

        [Test]
        public void WhiteTea_IsTheEasyWay()
        {
            // Hardly any skill involved: withered roughly right and dried is already "fein" with fine leaves.
            Assert.GreaterOrEqual(TeaQuality.Tier(White(2, 200), P), 2);
            Assert.GreaterOrEqual(TeaQuality.Tier(White(2, 320), P), 2);
        }

        [Test]
        public void QualityLimits_AreTunable()
        {
            ProcessingParams strict = P.Clone();
            strict.quality.excellentScore = 1.01f;
            Assert.AreEqual(2, TeaQuality.Tier(White(), strict));
            ProcessingParams generous = P.Clone();
            generous.quality.ordinaryCeiling = 3;
            Assert.AreEqual(3, TeaQuality.Evaluate(Oolong(0.2f, 2), generous).ceiling);
        }
    }
}
