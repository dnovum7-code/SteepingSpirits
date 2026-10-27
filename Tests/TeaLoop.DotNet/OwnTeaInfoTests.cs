using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Integration;
using SteepingSpirits.Processing;

namespace SteepingSpirits.TeaLoop.Tests
{
    public class OwnTeaInfoTests
    {
        private static readonly ProcessedBrewParams K = new ProcessedBrewParams();
        private static readonly ExtractionParams Shape = new ExtractionParams();
        private static readonly QualityParams Quality = new QualityParams();

        [Test]
        public void WorkshopTea_IsReadWithLedgerOxidation()
        {
            Assert.IsTrue(OwnTeaInfo.TryRead("tea_oolong:dark/honeyed.roasty+q3", id => 0.56f, null, out OwnTea t));
            Assert.AreEqual(BrewBase.Oolong, t.info.baseTea);
            Assert.AreEqual(0.56f, t.info.oxidation, 1e-6);
            Assert.AreEqual(3, t.info.tier);
            Assert.AreEqual(BrewNotes.Honeyed | BrewNotes.Roasty, t.info.notes);
            Assert.AreEqual("Oolong (dunkel; honigsüß, röstig; erlesen)", t.displayName);
            Assert.IsFalse(t.isBlend);
        }

        [Test]
        public void WithoutLedger_TheSweetSpotIsUsed()
        {
            Assert.IsTrue(OwnTeaInfo.TryRead("tea_oolong:light/floral+q2", id => null, null, out OwnTea t));
            Assert.AreEqual(0.20f, t.info.oxidation, 1e-6);
            Assert.IsTrue(OwnTeaInfo.TryRead("tea_black/malty", null, null, out OwnTea b));
            Assert.AreEqual(0.92f, b.info.oxidation, 1e-6);
            Assert.AreEqual(1, b.info.tier);
        }

        [Test]
        public void Blends_BrewAsTheirBaseTea_WithTheirNotes()
        {
            Assert.IsTrue(OwnTeaInfo.TryRead("tea_blend:green~jasmine/fresh.floral+q2", null, null, out OwnTea t));
            Assert.IsTrue(t.isBlend);
            Assert.AreEqual(BrewBase.Green, t.info.baseTea);
            Assert.AreEqual(BrewNotes.Fresh | BrewNotes.Floral, t.info.notes);
            Assert.AreEqual("Jasmin-Grüntee (frisch, blumig; fein)", t.displayName);
        }

        [Test]
        public void ForeignIds_AreNoOwnTea()
        {
            Assert.IsFalse(OwnTeaInfo.TryRead("tea_tart", null, null, out _));
            Assert.IsFalse(OwnTeaInfo.TryRead("mint_dried+q2", null, null, out _));
            Assert.IsFalse(OwnTeaInfo.TryRead("tea_leaf+q3", null, null, out _));
        }

        [Test]
        public void LightAndDarkOolong_BrewNoticeablyDifferently()
        {
            OwnTeaInfo.TryRead("tea_oolong:light/floral+q2", null, null, out OwnTea light);
            OwnTeaInfo.TryRead("tea_oolong:dark/honeyed+q2", null, null, out OwnTea dark);
            TeaParams l = OwnTeaInfo.Brewable(TeaPresets.Oolong(), light, K);
            TeaParams d = OwnTeaInfo.Brewable(TeaPresets.Oolong(), dark, K);
            Assert.Greater(d.IdealMid - l.IdealMid, 4f, "dark oolong wants clearly hotter water");
            CalibrationSeries sl = BrewCalibration.CalibrateSeries(l, Shape, Quality, 20f);
            CalibrationSeries sd = BrewCalibration.CalibrateSeries(d, Shape, Quality, 20f);
            Assert.Greater(System.Math.Abs(sl.For(0).OptimalSeconds - sd.For(0).OptimalSeconds), 0.05f, "and a different steeping time");
            Assert.AreEqual("own:tea_oolong:light/floral+q2", l.id);
        }

        [Test]
        public void EveryWorkshopTeaId_TranslatesToABrewableTea()
        {
            foreach (TeaVariety v in System.Enum.GetValues(typeof(TeaVariety)))
            for (int tier = 1; tier <= 3; tier++)
            {
                string id = ProcessedTea.MakeId(v, new List<TeaNote> { TeaNote.Floral, TeaNote.Roasty }, tier);
                Assert.IsTrue(OwnTeaInfo.TryRead(id, null, null, out OwnTea t), id);
                Assert.AreEqual(OwnTeaInfo.BaseOf(v), t.info.baseTea);
            }
        }
    }
}
