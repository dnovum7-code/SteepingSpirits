using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SteepingSpirits.Ingredients;
using SteepingSpirits.Inventory;

namespace SteepingSpirits.Processing
{
    /// <summary>Where a finished tea went.</summary>
    public enum WorkshopDelivery
    {
        Nothing,
        Inventory,
        Pantry
    }

    /// <summary>Fresh leaves the store can offer (one source, one garden tier).</summary>
    public struct LeafSource
    {
        public string sourceId;
        public int tier;
        public int inInventory;
        public int inPantry;
        public bool FirstFlush => sourceId == TeaIds.FirstFlushLeaf || sourceId == IngredientCatalog.GoldenTip;
        public int Total => inInventory + inPantry;
    }

    /// <summary>
    /// The workshop's adapter to inventory and pantry, with the same rules as the garden's: the
    /// inventory when it runs (a full bag spills into the pantry), otherwise pantry.json. Leaves are
    /// read with the garden's ids (item_teeblatt / ingredient_tea_leaf+q2 …); finished teas are stored
    /// under their ProcessedTea id, so equal teas stack and different notes stay apart.
    /// </summary>
    public static class WorkshopItems
    {
        /// <summary>Leaves the workshop can turn into tea.</summary>
        public static readonly string[] LeafIds = { IngredientCatalog.TeaLeaf, TeaIds.FirstFlushLeaf, IngredientCatalog.GoldenTip };

        public static bool DryRun { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DryRun = false;

        public static string PantryPath => Path.Combine(Application.persistentDataPath, Pantry.FileName);

        /// <summary>Inventory item id of an ingredient in a tier (same scheme as the garden and the Jump'n'Run).</summary>
        public static string InventoryItemId(string ingredientId, int tier)
        {
            if (tier <= IngredientQuality.Lowest && IngredientCatalog.Normalize(ingredientId) == IngredientCatalog.TeaLeaf) return "item_teeblatt";
            return "ingredient_" + IngredientQuality.With(ingredientId, tier);
        }

        public static List<LeafSource> AvailableLeaves() => Available(LeafIds);

        /// <summary>Garden herbs and blossoms that can be dried.</summary>
        public static List<LeafSource> AvailableHerbs() => Available(DriedHerb.Herbs);

        private static List<LeafSource> Available(string[] ids)
        {
            var list = new List<LeafSource>();
            Pantry pantry = LoadPantry();
            PlayerInventory inv = PlayerInventory.Instance;
            foreach (string id in ids)
            {
                for (int tier = 3; tier >= 1; tier--)
                {
                    var s = new LeafSource
                    {
                        sourceId = id,
                        tier = tier,
                        inInventory = inv != null ? inv.Count(InventoryItemId(id, tier)) : 0,
                        inPantry = pantry.Count(IngredientQuality.With(id, tier))
                    };
                    if (s.Total > 0) list.Add(s);
                }
            }

            return list;
        }

        /// <summary>Takes up to "amount" leaves (inventory first, then pantry). Returns how many were taken.</summary>
        public static int TakeLeaves(LeafSource s, int amount)
        {
            if (DryRun) return amount;
            int taken = 0;
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null && s.inInventory > 0) taken += inv.Remove(InventoryItemId(s.sourceId, s.tier), Math.Min(amount, s.inInventory));
            if (taken < amount && s.inPantry > 0)
            {
                try
                {
                    Pantry pantry = LoadPantry();
                    taken += pantry.Take(IngredientQuality.With(s.sourceId, s.tier), amount - taken);
                    pantry.Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    File.WriteAllText(PantryPath, pantry.ToJson());
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Processing] Pantry not read/written: " + e.Message);
                }
            }

            return taken;
        }

        /// <summary>The starter pack: fresh leaves into the inventory (or pantry).</summary>
        public static void GiveLeaves(string sourceId, int tier, int amount)
        {
            if (DryRun || amount <= 0) return;
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null)
            {
                string itemId = InventoryItemId(sourceId, tier);
                Ensure(inv, itemId, IngredientQuality.DisplayName(IngredientQuality.With(sourceId, tier)), IngredientTexts.Description(sourceId), 2, new Color(0.45f, 0.7f, 0.35f));
                int left = inv.AddById(itemId, amount);
                if (left > 0) AddToPantry(IngredientQuality.With(sourceId, tier), left);
                return;
            }

            AddToPantry(IngredientQuality.With(sourceId, tier), amount);
        }

        /// <summary>Hands a finished tea over, with quality and notes in its id.</summary>
        public static WorkshopDelivery DeliverTea(ProcessedTea tea)
        {
            if (DryRun || tea == null || tea.amount <= 0) return WorkshopDelivery.Nothing;
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null)
            {
                string itemId = "ingredient_" + tea.Id;
                Ensure(inv, itemId, ProcessingTexts.TeaName(tea.variety, tea.notes, tea.tier), IngredientTexts.Description(tea.BaseId),
                    4 + 3 * (tea.tier - 1), LeafLook.TeaColor(tea.variety));
                int left = inv.AddById(itemId, tea.amount);
                if (left > 0) AddToPantry(tea.Id, left);
                return WorkshopDelivery.Inventory;
            }

            AddToPantry(tea.Id, tea.amount);
            return WorkshopDelivery.Pantry;
        }

        /// <summary>Hands dried herbs over (own ingredient, garden quality kept).</summary>
        public static WorkshopDelivery DeliverHerb(DriedHerb h)
        {
            if (DryRun || h.amount <= 0) return WorkshopDelivery.Nothing;
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null)
            {
                string itemId = "ingredient_" + h.Id;
                Ensure(inv, itemId, IngredientQuality.DisplayName(h.Id), IngredientTexts.Description(DriedHerb.DriedId(h.herbId)), 3 + 2 * (h.tier - 1), new Color(0.55f, 0.6f, 0.4f));
                int left = inv.AddById(itemId, h.amount);
                if (left > 0) AddToPantry(h.Id, left);
                return WorkshopDelivery.Inventory;
            }

            AddToPantry(h.Id, h.amount);
            return WorkshopDelivery.Pantry;
        }

        private static void Ensure(PlayerInventory inv, string itemId, string name, string description, int gold, Color tint)
        {
            if (inv.Resolve(itemId) != null) return;
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = itemId;
            item.itemID = itemId;
            item.displayName = name;
            item.description = description;
            item.category = ItemCategory.Material;
            item.maxStack = 99;
            item.goldValue = gold;
            item.tint = tint;
            inv.RegisterItem(item);
        }

        private static Pantry LoadPantry()
        {
            try
            {
                return File.Exists(PantryPath) ? Pantry.FromJson(File.ReadAllText(PantryPath)) : new Pantry();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Processing] Pantry not readable: " + e.Message);
                return new Pantry();
            }
        }

        private static void AddToPantry(string id, int amount)
        {
            try
            {
                Pantry pantry = LoadPantry();
                pantry.Add(id, amount);
                pantry.Updated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                File.WriteAllText(PantryPath, pantry.ToJson());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Processing] Pantry not written: " + e.Message);
            }
        }
    }
}
