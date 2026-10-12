using NUnit.Framework;

namespace SteepingSpirits.Plantation.Tests
{
    public class ActionRulesTests
    {
        public enum S
        {
            NotBed,
            Untouched,
            Tilled,
            TilledWatered,
            Growing,
            GrowingWatered,
            Ripe
        }

        private static Plot Make(S s)
        {
            switch (s)
            {
                case S.NotBed: return null;
                case S.Untouched: return new Plot();
                case S.Tilled: return new Plot { soil = Soil.Tilled };
                case S.TilledWatered: return new Plot { soil = Soil.Tilled, watered = true };
                case S.Growing: return new Plot { soil = Soil.Tilled, crop = new Crop { plantId = "mint" } };
                case S.GrowingWatered: return new Plot { soil = Soil.Tilled, watered = true, crop = new Crop { plantId = "mint" } };
                default: return new Plot { soil = Soil.Tilled, crop = new Crop { plantId = "mint", ripe = true } };
            }
        }

        private static readonly ToolContext Full = new ToolContext { seedPlantId = "mint", seedCount = 3, canWater = 10, compost = 2 };

        // The complete truth table: every action on every cell state with a full tool context.
        [TestCase(S.NotBed, GardenAction.Hoe, ActionEffect.None, ActionReason.NotABed)]
        [TestCase(S.NotBed, GardenAction.Sow, ActionEffect.None, ActionReason.NotABed)]
        [TestCase(S.NotBed, GardenAction.Water, ActionEffect.None, ActionReason.NotABed)]
        [TestCase(S.NotBed, GardenAction.Harvest, ActionEffect.None, ActionReason.NotABed)]
        [TestCase(S.NotBed, GardenAction.Shovel, ActionEffect.None, ActionReason.NotABed)]
        [TestCase(S.NotBed, GardenAction.Compost, ActionEffect.None, ActionReason.NotABed)]
        [TestCase(S.Untouched, GardenAction.Hoe, ActionEffect.Till, ActionReason.Ok)]
        [TestCase(S.Untouched, GardenAction.Sow, ActionEffect.None, ActionReason.NeedsTilling)]
        [TestCase(S.Untouched, GardenAction.Water, ActionEffect.None, ActionReason.NeedsTilling)]
        [TestCase(S.Untouched, GardenAction.Harvest, ActionEffect.None, ActionReason.NothingHere)]
        [TestCase(S.Untouched, GardenAction.Shovel, ActionEffect.None, ActionReason.NothingHere)]
        [TestCase(S.Untouched, GardenAction.Compost, ActionEffect.None, ActionReason.NeedsTilling)]
        [TestCase(S.Tilled, GardenAction.Hoe, ActionEffect.None, ActionReason.AlreadyTilled)]
        [TestCase(S.Tilled, GardenAction.Sow, ActionEffect.Sow, ActionReason.Ok)]
        [TestCase(S.Tilled, GardenAction.Water, ActionEffect.Water, ActionReason.Ok)]
        [TestCase(S.Tilled, GardenAction.Harvest, ActionEffect.None, ActionReason.NothingHere)]
        [TestCase(S.Tilled, GardenAction.Shovel, ActionEffect.Flatten, ActionReason.Ok)]
        [TestCase(S.Tilled, GardenAction.Compost, ActionEffect.Compost, ActionReason.Ok)]
        [TestCase(S.TilledWatered, GardenAction.Hoe, ActionEffect.None, ActionReason.AlreadyTilled)]
        [TestCase(S.TilledWatered, GardenAction.Sow, ActionEffect.Sow, ActionReason.Ok)]
        [TestCase(S.TilledWatered, GardenAction.Water, ActionEffect.None, ActionReason.AlreadyWatered)]
        [TestCase(S.TilledWatered, GardenAction.Harvest, ActionEffect.None, ActionReason.NothingHere)]
        [TestCase(S.TilledWatered, GardenAction.Shovel, ActionEffect.Flatten, ActionReason.Ok)]
        [TestCase(S.TilledWatered, GardenAction.Compost, ActionEffect.Compost, ActionReason.Ok)]
        [TestCase(S.Growing, GardenAction.Hoe, ActionEffect.None, ActionReason.PlantGrowing)]
        [TestCase(S.Growing, GardenAction.Sow, ActionEffect.None, ActionReason.AlreadyPlanted)]
        [TestCase(S.Growing, GardenAction.Water, ActionEffect.Water, ActionReason.Ok)]
        [TestCase(S.Growing, GardenAction.Harvest, ActionEffect.None, ActionReason.NotRipe)]
        [TestCase(S.Growing, GardenAction.Shovel, ActionEffect.DigUp, ActionReason.Ok)]
        [TestCase(S.Growing, GardenAction.Compost, ActionEffect.None, ActionReason.PlantGrowing)]
        [TestCase(S.GrowingWatered, GardenAction.Hoe, ActionEffect.None, ActionReason.PlantGrowing)]
        [TestCase(S.GrowingWatered, GardenAction.Sow, ActionEffect.None, ActionReason.AlreadyPlanted)]
        [TestCase(S.GrowingWatered, GardenAction.Water, ActionEffect.None, ActionReason.AlreadyWatered)]
        [TestCase(S.GrowingWatered, GardenAction.Harvest, ActionEffect.None, ActionReason.NotRipe)]
        [TestCase(S.GrowingWatered, GardenAction.Shovel, ActionEffect.DigUp, ActionReason.Ok)]
        [TestCase(S.GrowingWatered, GardenAction.Compost, ActionEffect.None, ActionReason.PlantGrowing)]
        [TestCase(S.Ripe, GardenAction.Hoe, ActionEffect.None, ActionReason.PlantGrowing)]
        [TestCase(S.Ripe, GardenAction.Sow, ActionEffect.None, ActionReason.AlreadyPlanted)]
        [TestCase(S.Ripe, GardenAction.Water, ActionEffect.Water, ActionReason.Ok)]
        [TestCase(S.Ripe, GardenAction.Harvest, ActionEffect.Harvest, ActionReason.Ok)]
        [TestCase(S.Ripe, GardenAction.Shovel, ActionEffect.None, ActionReason.HarvestFirst)]
        [TestCase(S.Ripe, GardenAction.Compost, ActionEffect.None, ActionReason.PlantGrowing)]
        public void TruthTable(S state, GardenAction action, ActionEffect effect, ActionReason reason)
        {
            ActionCheck c = ActionRules.Check(Make(state), action, Full);
            Assert.AreEqual(effect, c.effect, c.ToString());
            Assert.AreEqual(reason, c.reason, c.ToString());
            Assert.AreEqual(effect != ActionEffect.None, c.Valid);
        }

        [Test]
        public void Sow_NeedsChosenSeedAndSeedsLeft()
        {
            Plot p = Make(S.Tilled);
            Assert.AreEqual(ActionReason.NoSeedSelected, ActionRules.Check(p, GardenAction.Sow, new ToolContext { seedCount = 5 }).reason);
            Assert.AreEqual(ActionReason.NoSeedsLeft, ActionRules.Check(p, GardenAction.Sow, new ToolContext { seedPlantId = "mint" }).reason);
        }

        [Test]
        public void Water_NeedsWaterInCan()
        {
            Assert.AreEqual(ActionReason.CanEmpty, ActionRules.Check(Make(S.Growing), GardenAction.Water, new ToolContext()).reason);
            Assert.AreEqual(ActionReason.CanEmpty, ActionRules.Check(Make(S.Tilled), GardenAction.Water, new ToolContext()).reason);
        }

        [Test]
        public void StateReasonsWin_OverEmptyToolContext()
        {
            // An empty can on dry untouched soil says "needs tilling", not "can empty" – the more useful hint.
            Assert.AreEqual(ActionReason.NeedsTilling, ActionRules.Check(Make(S.Untouched), GardenAction.Water, new ToolContext()).reason);
            Assert.AreEqual(ActionReason.AlreadyWatered, ActionRules.Check(Make(S.TilledWatered), GardenAction.Water, new ToolContext()).reason);
            Assert.AreEqual(ActionReason.NeedsTilling, ActionRules.Check(Make(S.Untouched), GardenAction.Sow, new ToolContext()).reason);
        }

        [Test]
        public void Compost_NeedsCompost_AndOnlyOncePerSowing()
        {
            Assert.AreEqual(ActionReason.NoCompost, ActionRules.Check(Make(S.Tilled), GardenAction.Compost, new ToolContext()).reason);
            Plot p = Make(S.Tilled);
            p.compost = true;
            Assert.AreEqual(ActionReason.AlreadyComposted, ActionRules.Check(p, GardenAction.Compost, Full).reason);
        }

        [Test]
        public void Check_DoesNotChangeThePlot()
        {
            Plot p = Make(S.Ripe);
            foreach (GardenAction a in System.Enum.GetValues(typeof(GardenAction)))
            {
                ActionRules.Check(p, a, Full);
            }

            Assert.IsTrue(p.crop.ripe);
            Assert.AreEqual(Soil.Tilled, p.soil);
            Assert.IsFalse(p.watered);
        }
    }
}
