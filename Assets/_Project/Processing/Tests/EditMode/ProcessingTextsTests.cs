using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class ProcessingTextsTests
    {
        [Test]
        public void StateIsTold_WithoutNumbers_UnlessAskedFor()
        {
            string plain = ProcessingTexts.State(Withered(), P, false);
            StringAssert.DoesNotContain("%", plain);
            StringAssert.Contains("%", ProcessingTexts.State(Withered(), P, true));
        }

        [Test]
        public void Colour_AndScent_FollowOxidation()
        {
            Assert.AreEqual("sattgrün", ProcessingTexts.Colour(Fresh()));
            Assert.AreEqual("grasig", ProcessingTexts.Scent(Fresh()));
            LeafBatch black = Black();
            Assert.AreEqual("dunkelbraun", ProcessingTexts.Colour(black));
            StringAssert.StartsWith("malzig", ProcessingTexts.Scent(black));
            StringAssert.StartsWith("blumig", ProcessingTexts.Scent(Oolong(0.2f)));
        }

        [Test]
        public void Feel_TellsWithering()
        {
            StringAssert.Contains("prall", ProcessingTexts.Feel(Fresh(), P));
            StringAssert.Contains("geschmeidig", ProcessingTexts.Feel(Withered(), P));
            StringAssert.Contains("schlaff", ProcessingTexts.Feel(Withered(3, 1500), P));
            StringAssert.Contains("knisternd", ProcessingTexts.Feel(White(), P));
        }

        [Test]
        public void TeaNames_ReadLikeAShopLabel()
        {
            Assert.AreEqual("Oolong (dunkel; honigsüß, röstig; erlesen)",
                ProcessingTexts.TeaName(TeaVariety.DarkOolong, new[] { TeaNote.Honeyed, TeaNote.Roasty }, 3));
            Assert.AreEqual("Weißtee", ProcessingTexts.TeaName(TeaVariety.White, null, 1));
            Assert.AreEqual("Grüntee (frisch; fein)", ProcessingTexts.TeaNameOfId("tea_green/fresh+q2"));
        }

        [Test]
        public void EveryReasonAndVariety_HasAText()
        {
            foreach (BatchReason r in System.Enum.GetValues(typeof(BatchReason)))
            {
                if (r != BatchReason.Ok) Assert.IsNotEmpty(ProcessingTexts.Reason(r), r.ToString());
            }

            foreach (TeaNote n in System.Enum.GetValues(typeof(TeaNote))) Assert.IsNotEmpty(ProcessingTexts.NoteName(n));
            foreach (TeaVariety v in System.Enum.GetValues(typeof(TeaVariety))) Assert.IsNotEmpty(ProcessingTexts.VarietyName(v));
        }

        [Test]
        public void Heading_SaysWhereThePathLeads()
        {
            StringAssert.Contains("Weißtee", ProcessingTexts.Heading(Withered(), P));
            StringAssert.StartsWith("Fertig", ProcessingTexts.Heading(Black(), P));
        }
    }
}
