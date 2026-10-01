using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// HUD-Tracker: zeigt die Objectives aller laufenden Quests,
    /// z.B. "Heilkräuter sammeln: 4/10". Rein event-getrieben.
    /// </summary>
    public class QuestTracker : MonoBehaviour
    {
        [SerializeField] private Text trackerText;

        // Aktive Quests, gepflegt über Zustandsänderungen.
        private readonly Dictionary<string, QuestInstance> trackedQuests = new Dictionary<string, QuestInstance>();

        private void OnEnable()
        {
            QuestEvents.OnQuestStateChanged += HandleQuestStateChanged;
            QuestEvents.OnObjectiveUpdated += HandleObjectiveUpdated;
            Rebuild();
        }

        private void OnDisable()
        {
            QuestEvents.OnQuestStateChanged -= HandleQuestStateChanged;
            QuestEvents.OnObjectiveUpdated -= HandleObjectiveUpdated;
        }

        private void HandleQuestStateChanged(QuestInstance quest)
        {
            if (quest.State == QuestState.ACTIVE)
            {
                trackedQuests[quest.Data.questID] = quest;
            }
            else
            {
                trackedQuests.Remove(quest.Data.questID);
            }

            Rebuild();
        }

        private void HandleObjectiveUpdated(QuestInstance quest, ObjectiveData objective, int currentAmount)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            if (trackerText == null)
            {
                return;
            }

            if (trackedQuests.Count == 0)
            {
                trackerText.text = "";
                return;
            }

            var builder = new StringBuilder();

            foreach (QuestInstance quest in trackedQuests.Values)
            {
                builder.AppendLine(quest.Data.displayName);

                if (quest.Data.objectives != null)
                {
                    foreach (ObjectiveData objective in quest.Data.objectives)
                    {
                        builder.Append("  ")
                               .Append(objective.description)
                               .Append(": ")
                               .Append(quest.GetProgress(objective.objectiveID))
                               .Append("/")
                               .AppendLine(objective.requiredAmount.ToString());
                    }
                }
            }

            trackerText.text = builder.ToString();
        }
    }
}
