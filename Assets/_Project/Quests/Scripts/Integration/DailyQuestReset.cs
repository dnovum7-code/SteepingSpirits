using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.World;

namespace SteepingSpirits.Quests.Integration
{
    /// <summary>
    /// Brücke Spielzeit → Quests: Jeden Morgen (GameClock.OnNewDay) werden alle
    /// wiederholbaren DAILY-Quests, die erledigt/gescheitert/abgebrochen sind,
    /// wieder auf AVAILABLE gesetzt – der NPC zeigt dann wieder sein „!".
    /// (Ersetzt in diesem Projekt die KI-Nachschub-Generierung aus Everdawn.)
    ///
    /// Zero-Wiring, duplikatsicher: immer nur EINE aktiv.
    /// </summary>
    public class DailyQuestReset : MonoBehaviour
    {
        private static DailyQuestReset active;

        [Tooltip("Auch laufende Daily-Quests zurücksetzen (Fortschritt geht verloren)")]
        [SerializeField] private bool resetActiveQuests;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
            GameClock.OnNewDay += HandleNewDay;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                GameClock.OnNewDay -= HandleNewDay;
                active = null;
            }
        }

        private void HandleNewDay(GameClock clock)
        {
            QuestManager qm = QuestManager.Instance;
            if (qm == null)
            {
                return;
            }

            // Erst sammeln, dann ändern (ResetQuest feuert Events).
            var toReset = new List<string>();
            foreach (QuestInstance quest in qm.GetAllQuests())
            {
                if (quest.Data.category != QuestCategory.DAILY || !quest.Data.isRepeatable)
                {
                    continue;
                }

                bool finished = quest.State == QuestState.COMPLETED
                                || quest.State == QuestState.FAILED
                                || quest.State == QuestState.ABANDONED;
                if (finished || (resetActiveQuests && quest.State == QuestState.ACTIVE))
                {
                    toReset.Add(quest.Data.questID);
                }
            }

            foreach (string id in toReset)
            {
                qm.ResetQuest(id);
            }

            if (toReset.Count > 0)
            {
                Debug.Log($"[DailyQuestReset] {clock.DateLabel}: {toReset.Count} Daily-Quest(s) wieder verfügbar.");
            }
        }
    }
}
