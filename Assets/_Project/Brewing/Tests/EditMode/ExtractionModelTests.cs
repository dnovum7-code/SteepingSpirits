using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    public class ExtractionModelTests
    {
        private static ExtractionModel NewModel(TeaParams tea, float start, float aromaMax = 1f) =>
            new ExtractionModel(tea, BrewTestUtil.Shape, start, BrewTestUtil.Room, aromaMax);

        [Test]
        public void AromaFactorIsOneInWindowAndFallsToFloorBelow()
        {
            TeaParams tea = TeaPresets.White(); // window 75–85
            ExtractionModel m = NewModel(tea, 80f);

            Assert.AreEqual(1f, m.AromaFactor(80f), 1e-5f);
            Assert.AreEqual(1f, m.AromaFactor(99f), 1e-5f);
            Assert.AreEqual(0.3f, m.AromaFactor(60f), 1e-5f);   // 75 − 15
            Assert.AreEqual(0.3f, m.AromaFactor(30f), 1e-5f);
            Assert.AreEqual(0.65f, m.AromaFactor(67.5f), 1e-5f); // halfway
        }

        [Test]
        public void BitterFactorRisesWithHeatAboveWindowAndDropsBelow()
        {
            TeaParams tea = TeaPresets.Green(); // window 70–80, sensitivity 2
            ExtractionModel m = NewModel(tea, 75f);

            Assert.AreEqual(1f, m.BitterFactor(75f), 1e-5f);
            Assert.AreEqual(0.6f, m.BitterFactor(60f), 1e-5f);
            Assert.AreEqual(1f + 2f * 20f / 10f, m.BitterFactor(100f), 1e-4f);
        }

        [Test]
        public void AromaRisesMonotonicallyAndStaysBelowMax()
        {
            ExtractionModel m = NewModel(TeaPresets.Black(), 95f, 0.85f);
            float last = 0f;
            for (int i = 0; i < 3000; i++)
            {
                m.Tick(0.02f);
                Assert.GreaterOrEqual(m.Aroma, last);
                last = m.Aroma;
            }

            Assert.LessOrEqual(m.Aroma, 0.85f);
            Assert.Greater(m.Aroma, 0.84f);
        }

        [Test]
        public void NoBitternessBeforeOnset()
        {
            TeaParams tea = TeaPresets.Black();
            ExtractionModel m = NewModel(tea, tea.IdealMid);
            while (m.ElapsedSeconds < tea.bitterOnsetSeconds * 0.9f)
            {
                m.Tick(0.02f);
            }

            Assert.AreEqual(0f, m.Bitterness, 1e-6f);
            Assert.Less(m.BitterOnset, 1f);
        }

        [Test]
        public void HotterWaterTurnsBitterEarlier()
        {
            TeaParams tea = TeaPresets.Green();
            ExtractionModel ideal = NewModel(tea, 75f);
            ExtractionModel hot = NewModel(tea, 97f);
            for (int i = 0; i < 300; i++)
            {
                ideal.Tick(0.02f);
                hot.Tick(0.02f);
            }

            Assert.Greater(hot.Bitterness, ideal.Bitterness * 3f);
        }

        [Test]
        public void VesselCoolsDuringSteep()
        {
            ExtractionModel m = NewModel(TeaPresets.Black(), 95f);
            for (int i = 0; i < 500; i++) m.Tick(0.02f);

            Assert.Less(m.Temperature, 95f);
            Assert.Greater(m.Temperature, 85f); // 0.01/s is slow: ~7 °C in 10 s
        }

        [Test]
        public void FrameRateBarelyChangesTheResult()
        {
            TeaParams tea = TeaPresets.Green();
            ExtractionModel coarse = NewModel(tea, 85f);
            ExtractionModel fine = NewModel(tea, 85f);
            for (int i = 0; i < 100; i++) coarse.Tick(0.1f);   // 10 FPS
            for (int i = 0; i < 1000; i++) fine.Tick(0.01f);   // 100 FPS

            Assert.AreEqual(fine.Aroma, coarse.Aroma, 0.01f);
            Assert.AreEqual(fine.Bitterness, coarse.Bitterness, 0.01f);
        }
    }
}
