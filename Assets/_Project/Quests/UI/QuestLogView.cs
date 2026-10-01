using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// Quest-Log: Übersicht aller bekannten Quests mit Zustand und
    /// Fortschritt. Baut seinen Datenbestand ausschliesslich aus QuestEvents
    /// auf – keine direkte Referenz auf den QuestManager.
    /// </summary>
    public class QuestLogView : MonoBehaviour
    {
        [SerializeField] private Text logText;

        // Lokaler Cache, gefüllt über OnQuestRegistered.
        private readonly Dictionary<string, QuestInstance> knownQuests = new Dictionary<string, QuestInstance>();

        private void OnEnable()
        {
            QuestEvents.OnQuestRegistered += HandleQuestRegistered;
            QuestEvents.OnQuestStateChanged += HandleQuestChanged;
            QuestEvents.OnObjectiveUpdated += HandleObjectiveUpdated;
            Rebuild();
        }

        private void OnDisable()
        {
            QuestEvents.OnQuestRegistered -= HandleQuestRegistered;
            QuestEvents.OnQuestStateChanged -= HandleQuestChanged;
            QuestEvents.OnObjectiveUpdated -= HandleObjectiveUpdated;
        }

        private void HandleQuestRegistered(QuestInstance quest)
        {
            knownQuests[quest.Data.questID] = quest;
            Rebuild();
        }

        private void HandleQuestChanged(QuestInstance quest)
        {
            knownQuests[quest.Data.questID] = quest;
            Rebuild();
        }

        private void HandleObjectiveUpdated(QuestInstance quest, ObjectiveData objective, int currentAmount)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            if (logText == null)
            {
                return;
            }

            if (knownQuests.Count == 0)
            {
                logText.text = "Keine Quests bekannt.";
                return;
            }

            var builder = new StringBuilder();

            foreach (QuestInstance quest in knownQuests.Values)
            {
                builder.Append("• ")
                       .Append(quest.Data.displayName)
                       .Append(" [")
                       .Append(StateLabel(quest.State))
                       .AppendLine("]");

                if (quest.State == QuestState.ACTIVE && quest.Data.objectives != null)
                {
                    foreach (ObjectiveData objective in quest.Data.objectives)
                    {
                        builder.Append("    ")
                               .Append(objective.description)
                               .Append(": ")
                               .Append(quest.GetProgress(objective.objectiveID))
                               .Append("/")
                               .AppendLine(objective.requiredAmount.ToString());
                    }
                }
            }

            logText.text = builder.ToString();
        }

        private static string StateLabel(QuestState state)
        {
            switch (state)
            {
                case QuestState.AVAILABLE: return "verfügbar";
                case QuestState.ACTIVE: return "aktiv";
                case QuestState.COMPLETED: return "abgeschlossen";
                case QuestState.FAILED: return "fehlgeschlagen";
                case QuestState.ABANDONED: return "abgebrochen";
                default: return "inaktiv";
            }
        }
    }
}
