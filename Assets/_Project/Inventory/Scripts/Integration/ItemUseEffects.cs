using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Core;

namespace SteepingSpirits.Inventory.Integration
{
    /// <summary>
    /// Brücke Inventar → Spieler: Wird ein Item benutzt (InventoryEvents.OnItemUsed),
    /// wendet diese Komponente seine Wirkung an – aktuell Heilen über
    /// ItemData.healAmount. Weitere Wirkungen (Energie, Buffs) hier ergänzen.
    /// (In Everdawn steckte das als Beispiel im InventoryDemoManager.)
    ///
    /// Zero-Wiring, duplikatsicher: immer nur EINE aktiv.
    /// </summary>
    public class ItemUseEffects : MonoBehaviour
    {
        private static ItemUseEffects active;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
            InventoryEvents.OnItemUsed += HandleItemUsed;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                InventoryEvents.OnItemUsed -= HandleItemUsed;
                active = null;
            }
        }

        private void HandleItemUsed(ItemData item)
        {
            if (item == null || item.healAmount <= 0f)
            {
                return;
            }

            Transform player = PlayerLocator.Find();
            Health health = player != null ? player.GetComponent<Health>() : null;
            if (health != null)
            {
                health.Heal(item.healAmount);
            }
        }
    }
}
