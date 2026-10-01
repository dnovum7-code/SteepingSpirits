using UnityEngine;

namespace SteepingSpirits.Inventory
{
    /// <summary>
    /// Unveränderliche Item-Definition (der „Bauplan" eines Gegenstands) –
    /// analog zu QuestData im Quest-System. Beschreibt was ein Item ist, nie
    /// wie viele der Spieler davon hat (das lebt in ItemStack / Inventory).
    ///
    /// Speicherort: Assets/_Project/Inventory/Data/[Kategorie]/item_[name].asset
    /// </summary>
    [CreateAssetMenu(fileName = "item_neu", menuName = "SteepingSpirits/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identität")]
        [Tooltip("Eindeutige ID, z.B. potion_small – muss zur targetID in Quests passen")]
        public string itemID;

        public string displayName;

        [TextArea(2, 4)]
        public string description;

        public Sprite icon;

        [Header("Einordnung")]
        public ItemCategory category = ItemCategory.Misc;

        [Tooltip("Wie viele in einen Stapel passen (1 = nicht stapelbar)")]
        [Min(1)]
        public int maxStack = 99;

        [Tooltip("Verkaufswert in Gold")]
        [Min(0)]
        public int goldValue;

        [Tooltip("Kann das Item benutzt/verbraucht werden (Trank …)?")]
        public bool usable;

        [Header("Wirkung beim Benutzen")]
        [Tooltip("Heilt den Spieler um so viel (1 = halbes Herz). Wird von ItemUseEffects angewendet")]
        [Min(0f)]
        public float healAmount;

        [Header("Welt (2D)")]
        [Tooltip("Farbe für Platzhalter-Grafiken, solange es kein Icon gibt")]
        public Color tint = Color.white;

        public bool IsStackable => maxStack > 1;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Asset-Name als Fallback für die itemID.
            if (string.IsNullOrEmpty(itemID))
            {
                itemID = name;
            }
        }
#endif
    }
}
