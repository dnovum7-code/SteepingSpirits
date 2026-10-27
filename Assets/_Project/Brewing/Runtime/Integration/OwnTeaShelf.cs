using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SteepingSpirits.Ingredients;
using SteepingSpirits.Inventory;
using SteepingSpirits.Processing;

namespace SteepingSpirits.Brewing.Integration
{
    /// <summary>
    /// The player's own teas for the brewing table: workshop teas and blends (catalogue family Tea) from the
    /// inventory and the pantry, with the exact oxidation from the workshop ledger. One portion is taken
    /// when the leaves go into the pot (inventory first, then pantry).
    /// </summary>
    public static class OwnTeaShelf
    {
        private const string Prefix = "ingredient_";

        public static string PantryPath => Path.Combine(Application.persistentDataPath, Pantry.FileName);
        public static string WorkshopPath => Path.Combine(Application.persistentDataPath, WorkshopSave.FileName);

        public static List<OwnTea> Load(VarietyParams varieties)
        {
            var counts = new Dictionary<string, int>();
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null)
            {
                foreach (ItemStack stack in inv.Slots)
                {
                    if (stack?.Item == null || !stack.Item.itemID.StartsWith(Prefix, StringComparison.Ordinal)) continue;
                    Add(counts, stack.Item.itemID.Substring(Prefix.Length), stack.Amount);
                }
            }

            foreach (KeyValuePair<string, int> kv in LoadPantry().Items) Add(counts, kv.Key, kv.Value);

            Dictionary<string, TeaLedgerEntry> ledger = LoadLedger();
            var list = new List<OwnTea>();
            foreach (KeyValuePair<string, int> kv in counts)
            {
                if (kv.Value <= 0) continue;
                if (!OwnTeaInfo.TryRead(kv.Key, id => ledger.TryGetValue(id, out TeaLedgerEntry e) && e.made > 0 ? e.AverageOxidation : (float?)null, varieties, out OwnTea tea)) continue;
                tea.count = kv.Value;
                list.Add(tea);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.ingredientId, b.ingredientId));
            return list;
        }

        public static bool TakeOne(string ingredientId)
        {
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null && inv.Remove(Prefix + ingredientId, 1) > 0) return true;
            try
            {
                Pantry pantry = LoadPantry();
                if (pantry.Take(ingredientId, 1) <= 0) return false;
                File.WriteAllText(PantryPath, pantry.ToJson());
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Brewing] Pantry not written: " + e.Message);
                return false;
            }
        }

        private static void Add(Dictionary<string, int> counts, string id, int n)
        {
            counts.TryGetValue(id, out int have);
            counts[id] = have + n;
        }

        private static Pantry LoadPantry()
        {
            try
            {
                return File.Exists(PantryPath) ? Pantry.FromJson(File.ReadAllText(PantryPath)) : new Pantry();
            }
            catch (Exception)
            {
                return new Pantry();
            }
        }

        private static Dictionary<string, TeaLedgerEntry> LoadLedger()
        {
            try
            {
                if (File.Exists(WorkshopPath)) return WorkshopSave.FromJson(File.ReadAllText(WorkshopPath)).ledger;
            }
            catch (Exception)
            {
                // No ledger: sweet spots are used.
            }

            return new Dictionary<string, TeaLedgerEntry>();
        }
    }

    /// <summary>
    /// Hand-over from the world (workshop brewing table) to the brewing scene and back. Set before the
    /// scene loads; the brewing scene reads it once. Without it the scene is the sandbox.
    /// </summary>
    public static class BrewHandoff
    {
        /// <summary>Ingredient id of the tea chosen at the brewing table (or null).</summary>
        public static string RequestedTeaId { get; set; }

        /// <summary>Scene to go back to; set = brewing was opened from the world.</summary>
        public static string ReturnScenePath { get; set; }

        public static bool FromWorld => !string.IsNullOrEmpty(ReturnScenePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            RequestedTeaId = null;
            ReturnScenePath = null;
        }
    }
}
