using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class OxidationForecastTests
    {
        [Test]
        public void Forecast_MatchesWhatActuallyHappens()
        {
            LeafBatch b = BatchRules.Place(BatchRules.Roll(Withered(), Roll(0.5f), P), BatchAction.Oxidize, P);
            TeaVariety start = TeaClassifier.Classify(b, P.variety);
            double minutes = OxidationForecast.MinutesUntilChange(b, P);
            Assert.That(minutes, Is.GreaterThan(1).And.LessThan(60));
            LeafBatch before = BatchRules.Elapse(b, minutes - 1, P);
            LeafBatch after = BatchRules.Elapse(b, minutes + 1, P);
            Assert.AreEqual(start, TeaClassifier.Classify(before, P.variety));
            Assert.AreNotEqual(start, TeaClassifier.Classify(after, P.variety));
        }

        [Test]
        public void NoChangeAhead_OutsideTheBasket_OrWhenBlack()
        {
            Assert.IsTrue(double.IsPositiveInfinity(OxidationForecast.MinutesUntilChange(Withered(), P)));
            LeafBatch black = BatchRules.Place(BatchRules.Roll(Withered(), Roll(1f), P), BatchAction.Oxidize, P);
            black = BatchRules.Elapse(black, 200, P);
            Assert.IsTrue(double.IsPositiveInfinity(OxidationForecast.MinutesUntilChange(black, P)));
        }
    }
}
