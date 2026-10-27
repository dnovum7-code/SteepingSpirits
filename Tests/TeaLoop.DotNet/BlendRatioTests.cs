using System.Linq;
using System.Text;
using NUnit.Framework;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Plantation;
using SteepingSpirits.Processing;

namespace SteepingSpirits.TeaLoop.Tests
{
    /// <summary>
    /// Evaluation of the blend ratio (E12, open decision): pots and cups per tea-shrub harvest when brewing uses
    /// one portion per pot, for 1 tea + 1 herb = 2 portions (now) against = 1 portion. Prints the table.
    /// </summary>
    public class BlendRatioTests
    {
        [Test]
        public void BlendRatio_PotsAndCupsPerHarvest()
        {
            PlantParams tea = StarterPlants.Create(StarterPlants.Tea);
            PlantParams mint = StarterPlants.Create(StarterPlants.Mint);
            float leaves = (tea.yieldMin + tea.yieldMax) * 0.5f;      // one harvest = leaves = tea portions (workshop 1:1)
            float perWeek = leaves * 7f / tea.regrowDays;
            var sb = new StringBuilder();
            sb.Append($"\nTea shrub: first harvest after {tea.stageDays.Sum()} days, then {tea.yieldMin}–{tea.yieldMax} leaves every {tea.regrowDays} days");
            sb.Append($" (Ø {leaves:0.0} per harvest, {perWeek:0.0} per week). Mint: {mint.stageDays.Sum()} days, {mint.yieldMin}–{mint.yieldMax} per harvest.\n");
            sb.Append("tea      infusions | pots pure | blend 1+1=2: pots, herbs | blend 1+1=1: pots, herbs | cups pure / =2 / =1 per harvest\n");
            foreach (TeaParams t in new[] { TeaPresets.White(), TeaPresets.Green(), TeaPresets.Oolong(), TeaPresets.Black() })
            {
                float pure = leaves;
                float two = leaves * TeaBlend.PortionsPerBlend;
                float one = leaves;
                sb.Append($"{t.id,-8} {t.maxInfusions,9} | {pure,9:0.0} | {two,6:0.0} pots, {leaves:0.0} herbs | {one,6:0.0} pots, {leaves:0.0} herbs |");
                sb.Append($" {pure * t.maxInfusions:0} / {two * t.maxInfusions:0} / {one * t.maxInfusions:0}\n");
            }

            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(2, TeaBlend.PortionsPerBlend, "the open decision is evaluated at the current ratio");
        }
    }
}
