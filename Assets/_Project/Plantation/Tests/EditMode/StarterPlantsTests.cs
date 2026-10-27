using NUnit.Framework;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Plantation.Tests
{
    public class StarterPlantsTests
    {
        [Test]
        public void SixPlants_AllHarvestCatalogIngredients()
        {
            PlantBook book = StarterPlants.Book();
            Assert.AreEqual(6, book.All.Count);
            foreach (PlantParams p in book.All)
            {
                Assert.IsTrue(IngredientCatalog.IsKnown(p.ingredientId), p.id);
                Assert.AreEqual(p.ingredientId, IngredientCatalog.Normalize(p.ingredientId), "catalogue id, not an alias");
                Assert.IsFalse(IngredientCatalog.IsRare(p.ingredientId), p.id);
                Assert.AreEqual("seed_" + p.id, p.seedItemId);
                Assert.AreSame(p, book.BySeed(p.seedItemId));
                Assert.LessOrEqual(p.yieldMin, p.yieldMax, p.id);
                Assert.Greater(p.yieldMin, 0, p.id);
                Assert.Greater(p.stageDays.Length, 1, p.id);
                Assert.IsNotEmpty(p.seasons, p.id);
                if (p.perennial) Assert.Greater(p.regrowDays, 0, p.id);
            }
        }

        [Test]
        public void ThePlantsDiffer_AsDesigned()
        {
            PlantBook b = StarterPlants.Book();
            PlantParams tea = b.Get(StarterPlants.Tea), mint = b.Get(StarterPlants.Mint), chamomile = b.Get(StarterPlants.Chamomile);
            PlantParams jasmine = b.Get(StarterPlants.Jasmine), lavender = b.Get(StarterPlants.Lavender), balm = b.Get(StarterPlants.LemonBalm);

            Assert.AreEqual(WaterNeed.Thirsty, mint.water);
            Assert.AreEqual(WaterNeed.Thirsty, jasmine.water);
            Assert.AreEqual(WaterNeed.Frugal, lavender.water);
            Assert.AreEqual(WaterNeed.Normal, tea.water);
            Assert.AreEqual(WaterNeed.Normal, chamomile.water);
            Assert.AreEqual(WaterNeed.Normal, balm.water);

            Assert.IsFalse(mint.perennial);
            Assert.IsFalse(chamomile.perennial);
            Assert.IsTrue(tea.perennial && jasmine.perennial && lavender.perennial && balm.perennial);

            Assert.Less(mint.DaysToFirstHarvest, chamomile.DaysToFirstHarvest, "mint fast, chamomile medium");
            Assert.Less(chamomile.DaysToFirstHarvest, tea.DaysToFirstHarvest, "tea slow to the first harvest");
            Assert.GreaterOrEqual(jasmine.DaysToFirstHarvest, tea.DaysToFirstHarvest, "jasmine slow");
            Assert.Less(tea.regrowDays, tea.DaysToFirstHarvest / 3, "then tea gives leaves every few days");
            Assert.AreEqual(IngredientCatalog.FirstFlushLeaf, tea.firstHarvestIngredientId, "erster Austrieb");
            Assert.AreEqual(0, tea.firstHarvestQualityBonus, "no extra tier – the first flush is its own ingredient");
            Assert.Greater(jasmine.regrowDays, balm.regrowDays, "jasmine valuable: slow to regrow");
        }

        [Test]
        public void TeaShrub_FirstHarvestIsTheFirstFlush_ThenOrdinaryLeaves()
        {
            var g = new Garden(new[] { new GridPos(0, 0) }, new GardenParams(), StarterPlants.Book(), 1, new FixedWeather());
            var p = new GridPos(0, 0);
            g.Apply(p, GardenAction.Hoe);
            g.Apply(p, GardenAction.Sow, StarterPlants.Tea, 1);
            for (int i = 0; i < 40 && !g.PlotAt(p).crop.ripe; i++)
            {
                g.Refill();
                g.Apply(p, GardenAction.Water);
                g.EndDay();
            }

            HarvestResult first = g.Apply(p, GardenAction.Harvest).harvest;
            Assert.AreEqual(IngredientCatalog.FirstFlushLeaf, first.ingredientId);
            Assert.AreEqual(HarvestQuality.Excellent, first.quality, "careful work, no bonus needed");
            for (int i = 0; i < 40 && !g.PlotAt(p).crop.ripe; i++)
            {
                g.Refill();
                g.Apply(p, GardenAction.Water);
                g.EndDay();
            }

            Assert.AreEqual(IngredientCatalog.TeaLeaf, g.Apply(p, GardenAction.Harvest).harvest.ingredientId);
            Assert.AreEqual("Erster Austrieb (erlesen)", IngredientQuality.DisplayName(IngredientQuality.With(first.ingredientId, (int)first.quality)));
        }

        [Test]
        public void Create_UnknownId_IsNull()
        {
            Assert.IsNull(StarterPlants.Create("ghost_pepper"));
        }
    }
}
