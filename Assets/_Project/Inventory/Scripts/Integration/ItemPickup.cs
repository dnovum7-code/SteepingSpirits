using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Quests;

namespace SteepingSpirits.Inventory.Integration
{
    /// <summary>
    /// Designer-freundliches Sammelobjekt (2D): referenziert direkt eine ItemData
    /// (statt nur einer ID) und meldet beim Betreten des Trigger-Collider2D
    /// GameplayEvents.ItemCollected. Über die InventoryPickupBridge landet es
    /// im Inventar, über die Collect-Steps zählt es für Quests – beides mit
    /// einem einzigen Event, ohne Doppelzählung.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int amount = 1;
        [SerializeField] private string playerTag = "Player";

        private bool collected;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        /// <summary>Konfiguration aus Code (z.B. Test-Szene).</summary>
        public void Configure(ItemData item, int amount = 1)
        {
            this.item = item;
            this.amount = Mathf.Max(1, amount);
        }

        private void Start()
        {
            // Ohne eigene Grafik: Icon bzw. Item-Farbe übernehmen.
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && item != null)
            {
                if (sr.sprite == null && item.icon != null)
                {
                    sr.sprite = item.icon;
                }
                else if (sr.color == Color.white)
                {
                    sr.color = item.tint;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || item == null || !PlayerLocator.IsPlayer(other, playerTag))
            {
                return;
            }

            collected = true;

            // Bei Bedarf zur Laufzeit bekannt machen, damit AddById() auflöst.
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.RegisterItem(item);
            }

            for (int i = 0; i < Mathf.Max(1, amount); i++)
            {
                GameplayEvents.ItemCollected(item.itemID, gameObject);
            }

            Destroy(gameObject);
        }
    }
}
