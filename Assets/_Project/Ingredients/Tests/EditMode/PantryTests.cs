using System;
using NUnit.Framework;

namespace SteepingSpirits.Ingredients.Tests
{
    public class PantryTests
    {
        [Test]
        public void AddTakeAndAliases()
        {
            var p = new Pantry();
            p.Add(IngredientCatalog.TeaLeaf, 3);
            p.Add("item_teeblatt", 2);
            p.Add(IngredientCatalog.MoonHerb, 1);
            p.Add(IngredientCatalog.Herb, 0);
            Assert.AreEqual(5, p.Count(IngredientCatalog.TeaLeaf));
            Assert.AreEqual(6, p.Total);
            Assert.AreEqual(2, p.Take(IngredientCatalog.TeaLeaf, 2));
            Assert.AreEqual(3, p.Take(IngredientCatalog.TeaLeaf, 9));
            Assert.AreEqual(0, p.Count(IngredientCatalog.TeaLeaf));
        }

        [Test]
        public void Json_RoundTripIsVersionedAndSorted()
        {
            var p = new Pantry { Updated = "2026-10-05T10:00:00Z" };
            p.Add(IngredientCatalog.TeaLeaf, 5);
            p.Add(IngredientCatalog.Herb, 2);
            string json = p.ToJson();
            Assert.AreEqual("{\"version\":1,\"updated\":\"2026-10-05T10:00:00Z\",\"items\":{\"herb\":2,\"tea_leaf\":5}}", json);

            Pantry back = Pantry.FromJson(json);
            Assert.AreEqual(5, back.Count(IngredientCatalog.TeaLeaf));
            Assert.AreEqual("2026-10-05T10:00:00Z", back.Updated);
        }

        [Test]
        public void Json_ToleratesWhitespaceUnknownIdsAndExtraFields()
        {
            Pantry p = Pantry.FromJson("{ \"version\": 1, \"note\": \"hi\", \"flag\": true,\n \"items\": { \"tea_leaf\": 2, \"future_root\": 4 } }");
            Assert.AreEqual(2, p.Count(IngredientCatalog.TeaLeaf));
            Assert.AreEqual(4, p.Count("future_root"), "unknown ids are kept");
            Assert.AreEqual(0, Pantry.FromJson("").Total);
        }

        [Test]
        public void Json_RejectsNewerVersionsAndGarbage()
        {
            Assert.Throws<FormatException>(() => Pantry.FromJson("{\"version\":7,\"items\":{}}"));
            Assert.Throws<FormatException>(() => Pantry.FromJson("[1,2]"));
        }
    }
}
