using NUnit.Framework;
using SteepingSpirits.Ingredients;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class HerbDryingTests
    {
        [Test]
        public void Herbs_CanOnlyBeDried()
        {
            LeafBatch h = LeafBatch.Herb(1, "mint", 2, 3, 0);
            foreach (BatchAction a in new[] { BatchAction.Wither, BatchAction.Roll, BatchAction.Oxidize, BatchAction.Fix })
            {
                Assert.AreEqual(BatchReason.HerbOnlyDries, BatchRules.Check(h, a, P).reason, a.ToString());
            }

            Assert.IsTrue(BatchRules.Check(h, BatchAction.Dry, P).ok, "fresh herbs go straight into the dryer");
            Assert.AreEqual(BatchReason.NotDry, BatchRules.Check(h, BatchAction.Finish, P).reason);
        }

        [Test]
        public void DriedHerbs_AreTheirOwnIngredient_WithGardenQuality()
        {
            var clock = new ManualGameTime();
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch h = w.AddHerb("jasmine", 3, 2);
            Assert.IsTrue(w.Do(h.id, BatchAction.Dry));
            clock.Advance(130);
            w.Do(h.id, BatchAction.TakeOut);
            Assert.IsFalse(w.Finish(h.id, out _), "a herb is no tea");
            Assert.IsTrue(w.FinishHerb(h.id, out DriedHerb d));
            Assert.AreEqual("jasmine_dried+q3", d.Id);
            Assert.AreEqual(2, d.amount);
            Assert.IsTrue(IngredientCatalog.IsKnown(DriedHerb.DriedId("jasmine")));
            Assert.AreEqual("Getrocknete Jasminblüten", IngredientTexts.Name("jasmine_dried"));
        }

        [Test]
        public void EveryGardenHerb_HasADriedIngredient()
        {
            foreach (string herb in DriedHerb.Herbs)
            {
                Assert.IsTrue(IngredientCatalog.IsKnown(herb), herb);
                Assert.IsTrue(IngredientCatalog.IsKnown(DriedHerb.DriedId(herb)), herb);
            }

            Assert.IsTrue(DriedHerb.IsHerb("lavender+q2"));
            Assert.IsFalse(DriedHerb.IsHerb("tea_leaf"));
        }

        [Test]
        public void HerbBatches_SurviveSaving()
        {
            var w = new Workshop(new ProcessingParams(), new ManualGameTime());
            w.AddHerb("lemon_balm", 1, 4);
            WorkshopSave back = WorkshopSave.FromJson(WorkshopSave.From(w).ToJson());
            Assert.IsTrue(back.batches[0].isHerb);
            Assert.AreEqual("lemon_balm", back.batches[0].sourceId);
        }
    }
}
