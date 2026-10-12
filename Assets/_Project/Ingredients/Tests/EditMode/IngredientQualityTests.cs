using NUnit.Framework;

namespace SteepingSpirits.Ingredients.Tests
{
    public class IngredientQualityTests
    {
        [Test]
        public void TierOne_IsThePlainId()
        {
            Assert.AreEqual("mint", IngredientQuality.With("mint", 1));
            Assert.AreEqual("mint+q3", IngredientQuality.With("mint", 3));
            Assert.AreEqual("mint+q3", IngredientQuality.With("mint+q2", 9));
        }

        [Test]
        public void RoundTrip()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                string id = IngredientQuality.With(IngredientCatalog.LemonBalm, tier);
                Assert.AreEqual(tier, IngredientQuality.Of(id));
                Assert.AreEqual(IngredientCatalog.LemonBalm, IngredientQuality.Plain(id));
            }

            Assert.AreEqual(1, IngredientQuality.Of("mint+qx"));
            Assert.AreEqual(1, IngredientQuality.Of(null));
        }

        [Test]
        public void Names()
        {
            Assert.AreEqual("Minze", IngredientQuality.DisplayName("mint"));
            Assert.AreEqual("Jasmin (erlesen)", IngredientQuality.DisplayName("jasmine+q3"));
        }

        [Test]
        public void Pantry_KeepsQualitiesApart()
        {
            var p = new Pantry();
            p.Add("mint", 2);
            p.Add("mint+q3", 1);
            Assert.AreEqual(2, p.Count("mint"));
            Assert.AreEqual(1, p.Count("mint+q3"));
            Pantry back = Pantry.FromJson(p.ToJson());
            Assert.AreEqual(1, back.Count("mint+q3"));
        }
    }
}
