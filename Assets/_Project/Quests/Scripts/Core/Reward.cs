using System;

namespace SteepingSpirits.Quests
{
    public enum RewardType
    {
        XP,
        Gold,
        Item
    }

    /// <summary>
    /// Belohnung bei Quest-Abschluss. Die Verteilung übernehmen Listener
    /// von QuestEvents.OnQuestCompleted (Inventory, XP-System, ...).
    /// </summary>
    [Serializable]
    public class Reward
    {
        public RewardType type;

        /// <summary>Menge für XP/Gold. Bei Items optional (Stackgrösse).</summary>
        public int amount;

        /// <summary>Nur für type == Item relevant.</summary>
        public string itemID;
    }
}
