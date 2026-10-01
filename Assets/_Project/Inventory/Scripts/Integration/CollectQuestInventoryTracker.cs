using UnityEngine;
using SteepingSpirits.Quests;

namespace SteepingSpirits.Inventory.Integration
{
    /// <summary>
    /// Zählt beim Start einer Sammel-Quest bereits im Inventar vorhandene Items
    /// mit – aber nur für Ziele, bei denen <see cref="ObjectiveData.countExistingInventory"/>
    /// aktiv ist. So muss der Spieler nicht neu sammeln, was er schon hat.
    ///
    /// Zero-Wiring, duplikatsicher: immer nur EINES aktiv.
    /// </summary>
    public class CollectQuestInventoryTracker : MonoBehaviour
    {
        private static CollectQuestInventoryTracker active;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
            QuestEvents.OnQuestStarted += HandleQuestStarted;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                QuestEvents.OnQuestStarted -= HandleQuestStarted;
                active = null;
            }
        }

        private void HandleQuestStarted(QuestInstance quest)
        {
            if (PlayerInventory.Instance == null || QuestManager.Instance == null || quest.Data.objectives == null)
            {
                return;
            }

            foreach (ObjectiveData objective in quest.Data.objectives)
            {
                if (objective.stepType != QuestStepType.Collect || !objective.countExistingInventory)
                {
                    continue;
                }

                int have = PlayerInventory.Instance.Count(objective.targetID);
                if (have > 0)
                {
                    // Fortschritt wird geklemmt – der Manager schliesst ggf. direkt ab.
                    QuestManager.Instance.QuestContinue(quest.Data.questID, objective.objectiveID, have);
                }
            }
        }
    }
}
