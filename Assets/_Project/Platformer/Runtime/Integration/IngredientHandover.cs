using System;
using System.IO;
using UnityEngine;
using SteepingSpirits.Ingredients;
using SteepingSpirits.Inventory;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Where the collected ingredients went.</summary>
    public enum HandoverTarget
    {
        Nothing,
        Inventory,
        Pantry
    }

    /// <summary>
    /// Adapter between the Jump'n'Run bag and the rest of the game. If the
    /// player inventory is running (started from the meadow), the ingredients
    /// go there as Material items; otherwise they are put into the pantry file
    /// (persistentDataPath/pantry.json) that the brewing system can read later.
    /// Never both, so nothing is counted twice.
    /// </summary>
    public static class IngredientHandover
    {
        /// <summary>Inventory item id for an ingredient (the meadow's tea leaf keeps its id so its quest counts).</summary>
        public static string InventoryItemId(string ingredientId)
        {
            return IngredientCatalog.Normalize(ingredientId) == IngredientCatalog.TeaLeaf ? "item_teeblatt" : "ingredient_" + ingredientId;
        }

        public static string PantryPath => Path.Combine(Application.persistentDataPath, Pantry.FileName);

        public static HandoverTarget Deliver(IngredientBag bag)
        {
            if (bag == null || bag.Total == 0)
            {
                return HandoverTarget.Nothing;
            }

            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory != null)
            {
                foreach (string id in bag.Ids)
                {
                    EnsureItem(inventory, id);
                    int leftover = inventory.AddById(InventoryItemId(id), bag.Count(id));
                    if (leftover > 0)
                    {
                        // A full bag must not swallow ingredients: the rest goes to the pantry.
                        AddToPantry(id, leftover);
                    }
                }

                return HandoverTarget.Inventory;
            }

            foreach (string id in bag.Ids)
            {
                AddToPantry(id, bag.Count(id));
            }

            return HandoverTarget.Pantry;
        }

        private static void EnsureItem(PlayerInventory inventory, string ingredientId)
        {
            string itemId = InventoryItemId(ingredientId);
            if (inventory.Resolve(itemId) != null)
            {
                return;
            }

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = itemId;
            item.itemID = itemId;
            item.displayName = IngredientTexts.Name(ingredientId);
            item.description = IngredientTexts.Description(ingredientId);
            item.category = ItemCategory.Material;
            item.maxStack = 99;
            item.goldValue = IngredientCatalog.IsRare(ingredientId) ? 12 : 2;
            item.tint = Ingredient.ColorOf(ingredientId);
            inventory.RegisterItem(item);
        }

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
                Debug.LogWarning("[JumpNRun] Pantry not written: " + e.Message);
            }
        }
    }
}
