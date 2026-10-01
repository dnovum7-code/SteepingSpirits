using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// Erzeugt zu einem Objective das passende Step-Template auf dem
    /// Quest-GameObject. Neue Step-Typen: Enum-Wert in QuestStepType
    /// ergänzen und hier einen Case hinzufügen.
    /// </summary>
    public static class QuestStepFactory
    {
        public static QuestStepBase AddStep(GameObject host, QuestInstance quest, ObjectiveData objective)
        {
            QuestStepBase step;

            switch (objective.stepType)
            {
                case QuestStepType.Collect:
                    step = host.AddComponent<CollectStep>();
                    break;
                case QuestStepType.Defeat:
                    step = host.AddComponent<DefeatStep>();
                    break;
                case QuestStepType.Visit:
                    step = host.AddComponent<VisitStep>();
                    break;
                case QuestStepType.Bring:
                    step = host.AddComponent<BringStep>();
                    break;
                case QuestStepType.Talk:
                    step = host.AddComponent<TalkStep>();
                    break;
                default:
                    Debug.LogError($"[QuestStepFactory] Unbekannter Step-Typ '{objective.stepType}' für Objective '{objective.objectiveID}'.");
                    return null;
            }

            step.Initialize(quest, objective);
            return step;
        }
    }
}
