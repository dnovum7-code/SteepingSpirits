using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SteepingSpirits.Ingredients;
using SteepingSpirits.Inventory;

namespace SteepingSpirits.Plantation
{
    /// <summary>Where a harvest went.</summary>
    public enum GardenDelivery
    {
        Nothing,
        Inventory,
        Pantry
    }

    /// <summary>
    /// The garden's adapter to inventory and pantry. Behaves like the Jump'n'Run handover
    /// (inventory when it runs, else pantry.json; a full bag spills into the pantry), but also
    /// carries the harvest quality: tier 1 uses the plain ingredient id, tiers 2/3 the
    /// IngredientQuality ids ("mint+q3"). Seeds ("seed_mint") and compost are inventory items.
    /// </summary>
    public static class GardenItems
    {
        public const string CompostItemId = "garden_compost";

        /// <summary>Tools (smoke runs) switch delivery off so tests never fill the real inventory or pantry.</summary>
        public static bool DryRun { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DryRun = false;

        public static string PantryPath => Path.Combine(Application.persistentDataPath, Pantry.FileName);

        /// <summary>Inventory item id of a harvested ingredient (the meadow tea leaf keeps its quest id).</summary>
        public static string InventoryItemId(string ingredientId, int tier)
        {
            if (tier <= IngredientQuality.Lowest && IngredientCatalog.Normalize(ingredientId) == IngredientCatalog.TeaLeaf) return "item_teeblatt";
            return "ingredient_" + IngredientQuality.With(ingredientId, tier);
        }

        public static GardenDelivery DeliverHarvest(string ingredientId, HarvestQuality quality, int count)
        {
            if (DryRun || count <= 0 || string.IsNullOrEmpty(ingredientId)) return GardenDelivery.Nothing;
            int tier = (int)quality;
            string pantryId = IngredientQuality.With(ingredientId, tier);
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory != null)
            {
                string itemId = InventoryItemId(ingredientId, tier);
                EnsureIngredientItem(inventory, itemId, ingredientId, tier);
                int leftover = inventory.AddById(itemId, count);
                if (leftover > 0) AddToPantry(pantryId, leftover);
                return GardenDelivery.Inventory;
            }

            AddToPantry(pantryId, count);
            return GardenDelivery.Pantry;
        }

        // ---------------- Seeds and compost ----------------

        public static void RegisterSeedItems(PlantBook book)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;
            foreach (PlantParams p in book.All) EnsureSimpleItem(inventory, p.seedItemId, GardenTexts.SeedName(p.id), GardenTexts.SeedDescription(p.id), 1, new Color(0.72f, 0.58f, 0.38f));
            EnsureSimpleItem(inventory, CompostItemId, GardenTexts.CompostName, GardenTexts.CompostDescription, 1, new Color(0.36f, 0.26f, 0.18f));
        }

        public static int Count(string itemId)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            return inventory != null && !string.IsNullOrEmpty(itemId) ? inventory.Count(itemId) : 0;
        }

        /// <summary>Adds items; returns what did not fit.</summary>
        public static int Add(string itemId, int amount)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (DryRun) return 0;
            if (inventory == null || amount <= 0) return Math.Max(0, amount);
            return inventory.AddById(itemId, amount);
        }

        public static bool RemoveOne(string itemId)
        {
            if (DryRun) return true;
            PlayerInventory inventory = PlayerInventory.Instance;
            return inventory != null && inventory.Remove(itemId, 1) > 0;
        }

        /// <summary>Seed item ids the player carries (in plant book order).</summary>
        public static void CarriedSeeds(PlantBook book, List<string> into)
        {
            into.Clear();
            foreach (PlantParams p in book.All)
            {
                if (Count(p.seedItemId) > 0) into.Add(p.seedItemId);
            }
        }

        /// <summary>First compost input (e.g. tart tea) the player carries, else null.</summary>
        public static string CarriedCompostInput(string[] inputIds)
        {
            if (inputIds == null) return null;
            foreach (string id in inputIds)
            {
                if (Count(id) > 0) return id;
            }

            return null;
        }

        private static void EnsureIngredientItem(PlayerInventory inventory, string itemId, string ingredientId, int tier)
        {
            if (inventory.Resolve(itemId) != null) return;
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = itemId;
            item.itemID = itemId;
            item.displayName = IngredientQuality.DisplayName(IngredientQuality.With(ingredientId, tier));
            item.description = IngredientTexts.Description(ingredientId);
            item.category = ItemCategory.Material;
            item.maxStack = 99;
            item.goldValue = 2 + 2 * (tier - 1);
            item.tint = PlantPalette.Ripe(PlantIdOf(ingredientId));
            inventory.RegisterItem(item);
        }

        private static void EnsureSimpleItem(PlayerInventory inventory, string itemId, string displayName, string description, int gold, Color tint)
        {
            if (string.IsNullOrEmpty(itemId) || inventory.Resolve(itemId) != null) return;
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = itemId;
            item.itemID = itemId;
            item.displayName = displayName;
            item.description = description;
            item.category = ItemCategory.Material;
            item.maxStack = 99;
            item.goldValue = gold;
            item.tint = tint;
            inventory.RegisterItem(item);
        }

        private static string PlantIdOf(string ingredientId) =>
            IngredientCatalog.Normalize(ingredientId) == IngredientCatalog.TeaLeaf ? StarterPlants.Tea : ingredientId;

        private static void AddToPantry(string id, int amount)
        {
            try
            {
                Pantry pantry = File.Exists(PantryPath) ? Pantry.FromJson(File.ReadAllText(PantryPath)) : new Pantry();
                pantry.Add(id, amount);
                pantry.Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                File.WriteAllText(PantryPath, pantry.ToJson());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Plantation] Pantry not written: " + e.Message);
            }
        }
    }
}
