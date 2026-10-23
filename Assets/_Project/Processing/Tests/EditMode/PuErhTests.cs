using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class PuErhTests
    {
        private static LeafBatch Maocha(float under = 0.3f)
        {
            LeafBatch b = BatchRules.Fix(Withered(), Wok(under, 1f - under, 0f), P);
            b = BatchRules.Roll(b, Roll(1f), P);
            return DryFully(b);
        }

        [Test]
        public void GentlyFixedRolledDriedLeaves_AreMaocha()
        {
            LeafBatch m = Maocha();
            Assert.AreEqual(MaochaReason.Ok, PuErh.CheckMaocha(m, P));
            Assert.IsTrue(PuErh.TryPress(m, P, 1234, out TeaBrick brick));
            Assert.AreEqual(0, brick.maturationDays, "no maturation yet");
            Assert.AreEqual(1234.0, brick.pressedAtMinutes);
            StringAssert.StartsWith("tea_brick:raw", brick.Id);
        }

        [Test]
        public void OtherTeas_SayWhyNot()
        {
            Assert.AreEqual(MaochaReason.FixedTooHot, PuErh.CheckMaocha(Maocha(0f), P));
            Assert.AreEqual(MaochaReason.NotFixed, PuErh.CheckMaocha(White(), P));
            Assert.AreEqual(MaochaReason.NotFixed, PuErh.CheckMaocha(Black(), P));
            Assert.AreEqual(MaochaReason.NotDry, PuErh.CheckMaocha(Withered(), P));
            LeafBatch flat = DryFully(BatchRules.Fix(Withered(), Wok(0.3f, 0.7f, 0f), P));
            Assert.AreEqual(MaochaReason.NotRolledEnough, PuErh.CheckMaocha(flat, P));
            Assert.IsFalse(PuErh.TryPress(flat, P, 0, out _));
        }

        [Test]
        public void MaochaIsStillAValidTea()
        {
            Assert.AreEqual(TeaVariety.Green, TeaClassifier.Classify(Maocha(), P.variety), "the path ends in a valid tea even without pressing");
        }
    }
}
