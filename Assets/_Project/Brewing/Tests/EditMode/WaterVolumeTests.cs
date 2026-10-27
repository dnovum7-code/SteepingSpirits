using NUnit.Framework;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    public class WaterVolumeTests
    {
        [Test]
        public void FullKettleHeatsAtTheBaseRate()
        {
            var p = new WaterParams();
            var water = new WaterModel(p, new BoilStageThresholds());
            Assert.AreEqual(p.capacity, water.Volume, 1e-5f);
            Assert.AreEqual(p.heatingRate, water.EffectiveHeatingRate, 1e-5f);
        }

        [Test]
        public void LessWaterHeatsFasterWithinTheThermalFloor()
        {
            var p = new WaterParams();
            var water = new WaterModel(p, new BoilStageThresholds());
            water.TakePour();
            water.TakePour();

            Assert.Greater(water.EffectiveHeatingRate, p.heatingRate);
            Assert.LessOrEqual(water.EffectiveHeatingRate, p.heatingRate / p.minThermalShare + 1e-4f);
        }

        [Test]
        public void TakePourRefusesWhenTooLittleIsLeftAndRefillRestores()
        {
            var p = new WaterParams();
            var water = new WaterModel(p, new BoilStageThresholds());
            int pours = 0;
            while (water.TakePour()) pours++;

            Assert.AreEqual(4, pours, "1.2 l kettle, 0.3 l per pour");
            float left = water.Volume;
            Assert.IsFalse(water.TakePour());
            Assert.AreEqual(left, water.Volume, 1e-6f);

            water.Refill();
            Assert.AreEqual(p.capacity, water.Volume, 1e-6f);
        }
    }
}
