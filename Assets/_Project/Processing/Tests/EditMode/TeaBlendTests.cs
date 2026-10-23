using NUnit.Framework;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Processing.Tests
{
    public class TeaBlendTests
    {
        [Test]
        public void JasmineGreenTea()
        {
            Assert.IsTrue(TeaBlend.TryMake("tea_green/fresh+q3", "jasmine_dried+q2", 3, out TeaBlend b));
            Assert.AreEqual(TeaVariety.Green, b.tea);
            Assert.AreEqual(2, b.tier, "the lower quality counts");
            CollectionAssert.AreEqual(new[] { TeaNote.Fresh, TeaNote.Floral }, b.notes);
            Assert.AreEqual("tea_blend:green~jasmine/fresh.floral+q2", b.Id);
            Assert.AreEqual("Jasmin-Grüntee (frisch, blumig; fein)", ProcessingTexts.BlendName(b));
            Assert.IsTrue(IngredientCatalog.IsKnown(ProcessedTea.CatalogIdOf(b.Id)));
        }

        [Test]
        public void BlendIds_RoundTrip()
        {
            foreach (string tea in new[] { "tea_white", "tea_oolong:dark/honeyed.roasty+q3", "tea_black/malty+q2", "tea_oolong:light/floral" })
            foreach (string herb in DriedHerb.Herbs)
            {
                Assert.IsTrue(TeaBlend.TryMake(tea, DriedHerb.DriedId(herb) + "+q3", 3, out TeaBlend b), tea + " " + herb);
                Assert.IsTrue(TeaBlend.TryParseId(b.Id, out TeaBlend back), b.Id);
                Assert.AreEqual(b.Id, back.Id);
                Assert.LessOrEqual(b.notes.Count, 3);
            }
        }

        [Test]
        public void TheHerbNote_AlwaysStays_WhenNotesAreCut()
        {
            Assert.IsTrue(TeaBlend.TryMake("tea_oolong:dark/honeyed.malty.roasty+q3", "mint_dried", 3, out TeaBlend b));
            Assert.AreEqual(3, b.notes.Count);
            CollectionAssert.Contains(b.notes, TeaNote.Fresh);
            Assert.AreEqual(1, b.tier);
        }

        [Test]
        public void OnlyWorkshopTeaAndDriedHerbs_Blend()
        {
            Assert.IsFalse(TeaBlend.TryMake("mint+q2", "jasmine_dried", 3, out _));
            Assert.IsFalse(TeaBlend.TryMake("tea_green", "jasmine", 3, out _), "fresh blossoms must be dried first");
            Assert.IsFalse(TeaBlend.TryMake("tea_green", "spring_water_dried", 3, out _));
            Assert.IsFalse(TeaBlend.TryParseId("tea_green+q2", out _));
        }
    }
}
