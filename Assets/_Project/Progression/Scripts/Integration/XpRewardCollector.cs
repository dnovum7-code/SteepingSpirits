using UnityEngine;
using SteepingSpirits.Quests;

namespace SteepingSpirits.Progression.Integration
{
    /// <summary>
    /// Brücke Quest-Belohnung → Fortschritt. Hört auf QuestEvents.OnQuestCompleted
    /// und schreibt XP-Belohnungen gut (parallel zum Item- und Gold-Collector).
    /// Duplikatsicher: immer nur EINER aktiv.
    /// </summary>
    public class XpRewardCollector : MonoBehaviour
    {
        private static XpRewardCollector active;

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
            if (PlayerProgression.Instance == null || quest.Data.rewards == null)
            {
                return;
            }

            foreach (Reward reward in quest.Data.rewards)
            {
                if (reward.type == RewardType.XP && reward.amount > 0)
                {
                    PlayerProgression.Instance.AddXp(reward.amount);
                }
            }
        }
    }
}
