using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// Basisklasse aller Step-Templates. Ein Step wird vom QuestManager beim
    /// Quest-Start als Komponente auf das Quest-GameObject gelegt, lauscht auf
    /// GameplayEvents und meldet Fortschritt zurück an den Manager.
    /// </summary>
    public abstract class QuestStepBase : MonoBehaviour
    {
        protected QuestInstance Quest { get; private set; }
        protected ObjectiveData Objective { get; private set; }

        private bool initialized;

        public void Initialize(QuestInstance quest, ObjectiveData objective)
        {
            Quest = quest;
            Objective = objective;
            initialized = true;
            Subscribe();
        }

        private void OnDestroy()
        {
            if (initialized)
            {
                Unsubscribe();
            }
        }

        /// <summary>An GameplayEvents anhängen.</summary>
        protected abstract void Subscribe();

        /// <summary>Von GameplayEvents lösen.</summary>
        protected abstract void Unsubscribe();

        /// <summary>
        /// Prüft ob eine Meldung das Ziel dieses Steps ist – entweder per
        /// String-ID (id == targetID) ODER per verlinktem GameObject
        /// (source ist für dieses Objective in QuestTargetLink eingetragen).
        /// </summary>
        protected bool MatchesTarget(string id, GameObject source)
        {
            if (!string.IsNullOrEmpty(id) && id == Objective.targetID)
            {
                return true;
            }

            return QuestTargetRegistry.IsTargetFor(Objective.objectiveID, source);
        }

        /// <summary>Meldet Fortschritt an den QuestManager.</summary>
        protected void ReportProgress(int amount = 1)
        {
            if (Quest.IsObjectiveComplete(Objective.objectiveID))
            {
                return;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning($"[QuestStep] Kein QuestManager in der Szene – Fortschritt für '{Objective.objectiveID}' verworfen.");
                return;
            }

            QuestManager.Instance.QuestContinue(Quest.Data.questID, Objective.objectiveID, amount);
        }
    }
}
