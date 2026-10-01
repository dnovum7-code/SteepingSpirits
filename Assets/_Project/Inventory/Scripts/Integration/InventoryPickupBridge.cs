using UnityEngine;
using SteepingSpirits.Quests;

namespace SteepingSpirits.Inventory.Integration
{
    /// <summary>
    /// Brücke Aufsammeln → Inventar. Hört auf denselben Kanal wie die
    /// Collect-Quest-Steps (GameplayEvents.OnItemCollected) und legt das
    /// aufgesammelte Item zusätzlich ins Inventar.
    ///
    /// Duplikatsicher: Es ist immer nur EINE Brücke aktiv – weitere Kopien
    /// (z.B. durch mehrere Manager in einer Szene) deaktivieren sich selbst,
    /// damit nichts doppelt gezählt wird.
    /// </summary>
    public class InventoryPickupBridge : MonoBehaviour
    {
        private static InventoryPickupBridge active;

        /// <summary>True, wenn eine Brücke aktiv ist und ItemCollected-Meldungen
        /// bereits selbst ins Inventar legt (verhindert doppeltes Hinzufügen).</summary>
        public static bool IsActive => active != null;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
            GameplayEvents.OnItemCollected += HandleItemCollected;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                GameplayEvents.OnItemCollected -= HandleItemCollected;
                active = null;
            }
        }

        private void HandleItemCollected(string itemID, GameObject source)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddById(itemID, 1);
            }
        }
    }
}
