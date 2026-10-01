using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// Popup bei Quest-Abschluss: "Quest abgeschlossen: Kräutersammler"
    /// plus Reward-Zusammenfassung (+150 XP, +50 Gold, ...). Blendet sich
    /// nach displaySeconds automatisch aus.
    /// </summary>
    public class QuestPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text rewardText;
        [SerializeField] private float displaySeconds = 4f;

        private Coroutine hideRoutine;

        private void OnEnable()
        {
            QuestEvents.OnQuestCompleted += HandleQuestCompleted;
        }

        private void OnDisable()
        {
            QuestEvents.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void Start()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void HandleQuestCompleted(QuestInstance quest)
        {
            if (panel == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = "Quest abgeschlossen: " + quest.Data.displayName;
            }

            if (rewardText != null)
            {
                rewardText.text = BuildRewardSummary(quest.Data.rewards);
            }

            panel.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displaySeconds);
            panel.SetActive(false);
            hideRoutine = null;
        }

        private static string BuildRewardSummary(Reward[] rewards)
        {
            if (rewards == null || rewards.Length == 0)
            {
                return "";
            }

            var builder = new StringBuilder();

            foreach (Reward reward in rewards)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                switch (reward.type)
                {
                    case RewardType.XP:
                        builder.Append("+").Append(reward.amount).Append(" XP");
                        break;
                    case RewardType.Gold:
                        builder.Append("+").Append(reward.amount).Append(" Gold");
                        break;
                    case RewardType.Item:
                        builder.Append(reward.itemID);
                        if (reward.amount > 1)
                        {
                            builder.Append(" x").Append(reward.amount);
                        }
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
