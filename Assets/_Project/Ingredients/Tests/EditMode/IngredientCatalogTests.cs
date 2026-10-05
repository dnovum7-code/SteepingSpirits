using System.Collections.Generic;
using NUnit.Framework;

namespace SteepingSpirits.Ingredients.Tests
{
    public class IngredientCatalogTests
    {
        [Test]
        public void TenIngredients_FiveCommonFiveRare_UniqueIds()
        {
            Assert.AreEqual(10, IngredientCatalog.All.Count);
            var ids = new HashSet<string>();
            foreach (IngredientInfo i in IngredientCatalog.All) Assert.IsTrue(ids.Add(i.Id), i.Id);
            Assert.AreEqual(5, new List<string>(IngredientCatalog.Ids(Rarity.Rare)).Count);
        }

        [Test]
        public void EveryRareHasACommonOfTheSameFamily()
        {
            foreach (IngredientInfo i in IngredientCatalog.All)
            {
                IngredientInfo common = IngredientCatalog.Get(i.CommonId);
                Assert.IsNotNull(common, i.Id);
                Assert.AreEqual(Rarity.Common, common.Rarity, i.Id);
                Assert.AreEqual(i.Family, common.Family, i.Id);
            }
        }

        [Test]
        public void EveryIngredientHasNameAndDescription()
        {
            foreach (IngredientInfo i in IngredientCatalog.All)
            {
                Assert.AreNotEqual(i.Id, IngredientTexts.Name(i.Id), i.Id);
                Assert.IsNotEmpty(IngredientTexts.Description(i.Id), i.Id);
            }
        }

        [Test]
        public void MeadowTeaLeafAlias_MapsToTheCatalogue()
        {
            Assert.AreEqual(IngredientCatalog.TeaLeaf, IngredientCatalog.Normalize("item_teeblatt"));
            Assert.AreEqual("Teeblatt", IngredientTexts.Name("item_teeblatt"));
            Assert.IsNull(IngredientCatalog.Normalize("potion_small"));
            Assert.IsNull(IngredientCatalog.Get(null));
        }
    }
}
