using System;

namespace SteepingSpirits.Inventory
{
    /// <summary>
    /// Kommunikationsschicht des Inventars – exakt das Muster von QuestEvents.
    /// Der PlayerInventory feuert Signale, UI und andere Systeme hören zu,
    /// ohne dass eines das andere direkt kennt.
    ///
    /// Die Raise-Methoden ruft nur der PlayerInventory auf.
    /// </summary>
    public static class InventoryEvents
    {
        public static event Action<ItemData, int> OnItemAdded;    // Item, Menge
        public static event Action<ItemData, int> OnItemRemoved;  // Item, Menge
        public static event Action<ItemData> OnItemUsed;          // benutztes Item
        public static event Action OnInventoryChanged;            // irgendetwas hat sich geändert

        public static void RaiseItemAdded(ItemData item, int amount)
        {
            OnItemAdded?.Invoke(item, amount);
            OnInventoryChanged?.Invoke();
        }

        public static void RaiseItemRemoved(ItemData item, int amount)
        {
            OnItemRemoved?.Invoke(item, amount);
            OnInventoryChanged?.Invoke();
        }

        public static void RaiseItemUsed(ItemData item)
        {
            OnItemUsed?.Invoke(item);
        }

        public static void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
