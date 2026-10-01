using UnityEngine;
using SteepingSpirits.Quests;

namespace SteepingSpirits.Inventory.Integration
{
    /// <summary>
    /// Brücke Quest-Belohnung → Inventar. Hört auf QuestEvents.OnQuestCompleted
    /// und schreibt Item-Belohnungen ins Inventar gut.
    ///
    /// Duplikatsicher: immer nur EINER aktiv (weitere deaktivieren sich selbst),
    /// damit Belohnungen nicht doppelt gutgeschrieben werden. XP und Gold
    /// ignoriert dieser Collector bewusst (eigene Systeme).
    /// </summary>
    public class QuestRewardCollector : MonoBehaviour
    {
        private static QuestRewardCollector active;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
            QuestEvents.OnQuestCompleted += HandleQuestCompleted;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                QuestEvents.OnQuestCompleted -= HandleQuestCompleted;
                active = null;
            }
        }

        private void HandleQuestCompleted(QuestInstance quest)
        {
            if (PlayerInventory.Instance == null || quest.Data.rewards == null)
            {
                return;
            }

            foreach (Reward reward in quest.Data.rewards)
            {
                if (reward.type == RewardType.Item && !string.IsNullOrEmpty(reward.itemID))
                {
                    int amount = Mathf.Max(1, reward.amount);
                    PlayerInventory.Instance.AddById(reward.itemID, amount);
                }
            }
        }
    }
}
