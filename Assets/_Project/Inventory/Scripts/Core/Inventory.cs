using System;
using System.Collections.Generic;

namespace SteepingSpirits.Inventory
{
    /// <summary>
    /// Reiner Inventar-Container (kein MonoBehaviour) – die Logik zum
    /// Stapeln, Hinzufügen und Entnehmen. Analog zu QuestInstance im
    /// Quest-System: testbar, beliebig oft instanziierbar, im RAM.
    ///
    /// Feuert selbst keine Events – das übernimmt der PlayerInventory-Wrapper.
    /// </summary>
    public class Inventory
    {
        private readonly List<ItemStack> slots = new List<ItemStack>();

        /// <summary>Maximale Anzahl belegter Slots (0 = unbegrenzt).</summary>
        public int Capacity { get; }

        public Inventory(int capacity = 0)
        {
            Capacity = capacity;
        }

        public IReadOnlyList<ItemStack> Slots => slots;

        public bool IsFull => Capacity > 0 && slots.Count >= Capacity;

        /// <summary>
        /// Legt Items ins Inventar. Füllt zuerst bestehende Stapel, dann neue
        /// Slots (bis zur Kapazität). Gibt zurück, wie viel nicht mehr passte.
        /// </summary>
        public int Add(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return amount;
            }

            int remaining = amount;

            // 1) Bestehende Stapel auffüllen.
            if (item.IsStackable)
            {
                foreach (ItemStack stack in slots)
                {
                    if (stack.Item == item && !stack.IsFull)
                    {
                        remaining = stack.Add(remaining);
                        if (remaining == 0)
                        {
                            return 0;
                        }
                    }
                }
            }

            // 2) Neue Slots anlegen.
            while (remaining > 0 && !IsFull)
            {
                int take = item.IsStackable ? Math.Min(remaining, item.maxStack) : 1;
                slots.Add(new ItemStack(item, take));
                remaining -= take;
            }

            return remaining;
        }

        /// <summary>Entnimmt bis zu amount eines Items; gibt die tatsächlich entnommene Menge zurück.</summary>
        public int Remove(string itemID, int amount)
        {
            if (string.IsNullOrEmpty(itemID) || amount <= 0)
            {
                return 0;
            }

            int removed = 0;

            // Von hinten, damit geleerte Stapel gefahrlos entfernt werden können.
            for (int i = slots.Count - 1; i >= 0 && removed < amount; i--)
            {
                if (slots[i].Item.itemID != itemID)
                {
                    continue;
                }

                removed += slots[i].Remove(amount - removed);
                if (slots[i].Amount == 0)
                {
                    slots.RemoveAt(i);
                }
            }

            return removed;
        }

        public int Count(string itemID)
        {
            int total = 0;
            foreach (ItemStack stack in slots)
            {
                if (stack.Item.itemID == itemID)
                {
                    total += stack.Amount;
                }
            }

            return total;
        }

        public bool Has(string itemID, int amount = 1)
        {
            return Count(itemID) >= amount;
        }

        public void Clear()
        {
            slots.Clear();
        }

        // ---------------------------------------------------------------
        // Speichern / Laden
        // ---------------------------------------------------------------

        public InventorySaveData ToSaveData()
        {
            var save = new InventorySaveData();
            foreach (ItemStack stack in slots)
            {
                save.itemIDs.Add(stack.Item.itemID);
                save.amounts.Add(stack.Amount);
            }

            return save;
        }

        /// <summary>Stellt den Inhalt wieder her. resolve liefert zur itemID die Definition.</summary>
        public void ApplySaveData(InventorySaveData save, Func<string, ItemData> resolve)
        {
            slots.Clear();
            if (save == null)
            {
                return;
            }

            for (int i = 0; i < save.itemIDs.Count && i < save.amounts.Count; i++)
            {
                ItemData item = resolve(save.itemIDs[i]);
                if (item != null)
                {
                    Add(item, save.amounts[i]);
                }
            }
        }
    }

    /// <summary>JSON-serialisierbarer Schnappschuss eines Inventars.</summary>
    [Serializable]
    public class InventorySaveData
    {
        public List<string> itemIDs = new List<string>();
        public List<int> amounts = new List<int>();
    }
}
