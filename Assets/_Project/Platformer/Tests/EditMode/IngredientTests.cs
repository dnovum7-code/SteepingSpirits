using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class IngredientTests
    {
        [Test]
        public void Symbols_MapToCommonAndRareIds()
        {
            Assert.AreEqual(IngredientIds.TeaLeaf, IngredientIds.FromSymbol('t'));
            Assert.AreEqual(IngredientIds.GoldenTip, IngredientIds.FromSymbol('T'));
            Assert.AreEqual(IngredientIds.SpringWater, IngredientIds.FromSymbol('q'));
            Assert.IsNull(IngredientIds.FromSymbol('#'));
            Assert.IsTrue(IngredientIds.IsRare(IngredientIds.StarDew));
            Assert.AreEqual(IngredientIds.MorningDew, IngredientIds.BaseOf(IngredientIds.StarDew));
        }

        [Test]
        public void Bag_CountsAndKeepsOrder()
        {
            var bag = new IngredientBag();
            int events = 0;
            bag.Added += (id, n) => events++;
            bag.Add(IngredientIds.Herb);
            bag.Add(IngredientIds.TeaLeaf, 2);
            bag.Add(IngredientIds.Herb);
            bag.Add(IngredientIds.Herb, 0);
            bag.Add(null);

            Assert.AreEqual(2, bag.Count(IngredientIds.Herb));
            Assert.AreEqual(4, bag.Total);
            Assert.AreEqual(IngredientIds.Herb, bag.Ids[0]);
            Assert.AreEqual(3, events);
        }

        [Test]
        public void Bag_SerializeParseAndMerge()
        {
            var bag = new IngredientBag();
            bag.Add(IngredientIds.TeaLeaf, 3);
            bag.Add(IngredientIds.MoonHerb);
            string s = bag.Serialize();
            Assert.AreEqual("tea_leaf:3,moon_herb:1", s);

            IngredientBag back = IngredientBag.Parse(s);
            back.Merge(bag);
            Assert.AreEqual(6, back.Count(IngredientIds.TeaLeaf));
            Assert.AreEqual(0, IngredientBag.Parse("garbage,x:y").Total);
        }

        [Test]
        public void Tally_ListsFoundAgainstAvailable()
        {
            LevelLayout l = LevelLayout.Parse("P.tt.k.T.E\n##########");
            var avail = IngredientTally.Available(l);
            Assert.AreEqual(2, avail[IngredientIds.TeaLeaf]);

            var bag = new IngredientBag();
            bag.Add(IngredientIds.TeaLeaf);
            var lines = IngredientTally.Lines(bag, avail);
            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual(IngredientIds.TeaLeaf, lines[0].id);
            Assert.AreEqual(1, lines[0].found);
            Assert.AreEqual(2, lines[0].available);
            Assert.IsTrue(lines[2].rare);
        }
    }
}
