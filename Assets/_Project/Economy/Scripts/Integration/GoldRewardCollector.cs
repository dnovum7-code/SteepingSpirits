using UnityEngine;
using SteepingSpirits.Quests;

namespace SteepingSpirits.Economy.Integration
{
    /// <summary>
    /// Brücke Quest-Belohnung → Geldbeutel. Hört auf QuestEvents.OnQuestCompleted
    /// und schreibt Gold-Belohnungen gut (parallel zum Item- und XP-Collector).
    /// Duplikatsicher: immer nur EINER aktiv.
    /// </summary>
    public class GoldRewardCollector : MonoBehaviour
    {
        private static GoldRewardCollector active;

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
            if (Wallet.Instance == null || quest.Data.rewards == null)
            {
                return;
            }

            foreach (Reward reward in quest.Data.rewards)
            {
                if (reward.type == RewardType.Gold && reward.amount > 0)
                {
                    Wallet.Instance.Add(reward.amount);
                }
            }
        }
    }
}
