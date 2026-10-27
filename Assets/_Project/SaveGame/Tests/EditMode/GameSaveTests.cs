using System;
using NUnit.Framework;
using SteepingSpirits.Processing;

namespace SteepingSpirits.SaveGame.Tests
{
    public class GameSaveTests
    {
        [Test]
        public void RoundTrip_WithDynamicIds_KeepsOrderAmountsAndClock()
        {
            var s = new GameSaveData { hasClock = true, clock = new ClockEntry { day = 17, season = 2, year = 3, minuteOfDay = 905 } };
            s.inventory.Add(new ItemEntry("ingredient_tea_oolong:dark/honeyed.roasty+q3", 4));
            s.inventory.Add(new ItemEntry("ingredient_tea_blend:green~jasmine/fresh.floral+q2", 2));
            s.inventory.Add(new ItemEntry("ingredient_mint_dried+q2", 5));
            s.inventory.Add(new ItemEntry("ingredient_tea_leaf+q3", 7));
            s.inventory.Add(new ItemEntry("item_teeblatt", 3));
            s.inventory.Add(new ItemEntry("seed_mint", 9));
            s.pending.Add(new ItemEntry("potion_small", 1));
            string json = s.ToJson();

            GameSaveData back = GameSaveData.FromJson(json);
            Assert.AreEqual(GameSaveData.Version, back.loadedVersion);
            Assert.AreEqual(6, back.inventory.Count);
            for (int i = 0; i < s.inventory.Count; i++)
            {
                Assert.AreEqual(s.inventory[i].id, back.inventory[i].id);
                Assert.AreEqual(s.inventory[i].amount, back.inventory[i].amount);
            }

            Assert.AreEqual("potion_small", back.pending[0].id);
            Assert.IsTrue(back.hasClock);
            Assert.AreEqual(17, back.clock.day);
            Assert.AreEqual(2, back.clock.season);
            Assert.AreEqual(3, back.clock.year);
            Assert.AreEqual(905, back.clock.minuteOfDay);
            Assert.AreEqual(json, back.ToJson(), "stable output");
        }

        [Test]
        public void OldInventorySave_IsMigrated()
        {
            // What PlayerInventory.SaveToDisk wrote (JsonUtility, pretty printed).
            const string legacy = "{\n    \"itemIDs\": [\n        \"item_teeblatt\",\n        \"potion_small\",\n        \"ingredient_tea_green/fresh+q2\"\n    ],\n    \"amounts\": [\n        5,\n        2,\n        1\n    ]\n}";
            GameSaveData s = GameSaveData.FromLegacyInventoryJson(legacy);
            Assert.AreEqual(1, s.loadedVersion);
            Assert.AreEqual(3, s.inventory.Count);
            Assert.AreEqual("potion_small", s.inventory[1].id);
            Assert.AreEqual(2, s.inventory[1].amount);
            Assert.IsFalse(s.hasClock);

            // The same text in game_save.json is recognised too, and saving writes version 2.
            GameSaveData viaMain = GameSaveData.FromJson(legacy);
            Assert.AreEqual(1, viaMain.loadedVersion);
            StringAssert.StartsWith("{\"version\":2", viaMain.ToJson());
        }

        [Test]
        public void UnknownIds_AreKept_NotDropped()
        {
            Assert.IsFalse(ItemRecipes.TryDescribe("dragon_scale", out _));
            var s = new GameSaveData();
            s.inventory.Add(new ItemEntry("dragon_scale", 2));
            GameSaveData back = GameSaveData.FromJson(s.ToJson());
            Assert.AreEqual("dragon_scale", back.inventory[0].id);
            Assert.AreEqual(2, back.inventory[0].amount);
        }

        [Test]
        public void BrokenOrNewer_Throw_EmptyIsFresh()
        {
            Assert.AreEqual(0, GameSaveData.FromJson("").inventory.Count);
            Assert.Throws<FormatException>(() => GameSaveData.FromJson("{\"version\":3}"));
            Assert.Throws<FormatException>(() => GameSaveData.FromJson("[1]"));
        }

        [TestCase("ingredient_tea_oolong:dark/honeyed.roasty+q3", ItemKind.Tea, "Oolong (dunkel; honigsüß, röstig; erlesen)", 10)]
        [TestCase("ingredient_tea_white", ItemKind.Tea, "Weißtee", 4)]
        [TestCase("ingredient_tea_blend:green~jasmine/fresh.floral+q2", ItemKind.Blend, "Jasmin-Grüntee (frisch, blumig; fein)", 9)]
        [TestCase("ingredient_mint_dried+q2", ItemKind.DriedHerb, "Getrocknete Minze (fein)", 5)]
        [TestCase("ingredient_tea_leaf+q3", ItemKind.Ingredient, "Teeblatt (erlesen)", 6)]
        [TestCase("ingredient_tea_leaf_first+q3", ItemKind.Ingredient, "Erster Austrieb (erlesen)", 6)]
        [TestCase("item_teeblatt", ItemKind.Ingredient, "Teeblatt", 2)]
        [TestCase("ingredient_golden_tip", ItemKind.Ingredient, "Goldspitze", 12)]
        [TestCase("ingredient_lavender+q2", ItemKind.Ingredient, "Lavendel (fein)", 4)]
        public void Recipes_RebuildItemsFromTheirId(string id, ItemKind kind, string name, int gold)
        {
            Assert.IsTrue(ItemRecipes.TryDescribe(id, out ItemRecipe r), id);
            Assert.AreEqual(kind, r.kind);
            Assert.AreEqual(name, r.displayName);
            Assert.AreEqual(gold, r.goldValue);
            Assert.IsNotEmpty(r.description);
            Assert.AreEqual(99, r.maxStack);
        }

        [Test]
        public void ModuleResolvers_AreAsked_AfterTheGrammar()
        {
            ItemRecipes.ClearExtra();
            Assert.IsFalse(ItemRecipes.TryDescribe("seed_mint", out _));
            ItemRecipes.Register(id => id.StartsWith("seed_") ? new ItemRecipe { itemId = id, displayName = "Samen", kind = ItemKind.Seed, maxStack = 99 } : (ItemRecipe?)null);
            Assert.IsTrue(ItemRecipes.TryDescribe("seed_mint", out ItemRecipe r));
            Assert.AreEqual(ItemKind.Seed, r.kind);
            ItemRecipes.ClearExtra();
        }

        [Test]
        public void EveryWorkshopTea_CanBeRebuilt()
        {
            foreach (TeaVariety v in Enum.GetValues(typeof(TeaVariety)))
            for (int tier = 1; tier <= 3; tier++)
            {
                string id = "ingredient_" + ProcessedTea.MakeId(v, new[] { TeaNote.Floral }, tier);
                Assert.IsTrue(ItemRecipes.TryDescribe(id, out ItemRecipe r), id);
                Assert.AreEqual(tier, r.tier);
            }
        }
    }
}
