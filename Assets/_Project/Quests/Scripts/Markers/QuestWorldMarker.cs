using UnityEngine;
using SteepingSpirits.Interaction;

namespace SteepingSpirits.Quests.Markers
{
    /// <summary>
    /// Zeigt den Quest-Startpunkt in der Welt an (z.B. Anschlagbrett, Icon
    /// auf der Karte). Kann manuell in der Szene platziert (questID setzen) oder
    /// vom QuestManager an QuestData.startLocation gespawnt werden (Bind).
    ///
    /// Sichtbarkeit: availableIcon bei AVAILABLE, activeIcon bei ACTIVE,
    /// sonst nichts.
    /// </summary>
    public class QuestWorldMarker : MonoBehaviour, IInteractable, IInteractLabel
    {
        [Tooltip("Nur für manuell platzierte Marker – gespawnte Marker werden per Bind() verknüpft")]
        [SerializeField] private string questID;

        [Tooltip("Icon solange die Quest angenommen werden kann")]
        [SerializeField] private GameObject availableIcon;

        [Tooltip("Optionales Icon solange die Quest läuft")]
        [SerializeField] private GameObject activeIcon;

        private QuestInstance quest;

        /// <summary>Verb für den Interaktions-Prompt.</summary>
        public string InteractLabel =>
            quest != null && quest.State == QuestState.AVAILABLE ? "Quest annehmen" : "Quest läuft";

        private void OnEnable()
        {
            QuestEvents.OnQuestRegistered += HandleQuestChanged;
            QuestEvents.OnQuestStateChanged += HandleQuestChanged;
            TryResolveQuest();
            Refresh();
        }

        private void OnDisable()
        {
            QuestEvents.OnQuestRegistered -= HandleQuestChanged;
            QuestEvents.OnQuestStateChanged -= HandleQuestChanged;
        }

        /// <summary>Verknüpft einen gespawnten Marker mit seiner Quest.</summary>
        public void Bind(QuestInstance instance)
        {
            quest = instance;
            questID = instance.Data.questID;
            Refresh();
        }

        /// <summary>
        /// Vom Interaktionssystem aufgerufen (Spieler drückt E am Marker):
        /// nimmt die Quest an, wenn sie verfügbar ist.
        /// </summary>
        public void Interact()
        {
            if (quest != null && quest.State == QuestState.AVAILABLE && QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStart(questID);
            }
        }

        private void HandleQuestChanged(QuestInstance instance)
        {
            if (quest == null && instance.Data.questID == questID)
            {
                quest = instance;
            }

            if (instance == quest)
            {
                Refresh();
            }
        }

        private void TryResolveQuest()
        {
            if (quest == null && !string.IsNullOrEmpty(questID) && QuestManager.Instance != null)
            {
                quest = QuestManager.Instance.GetQuest(questID);
            }
        }

        private void Refresh()
        {
            QuestState state = quest != null ? quest.State : QuestState.INACTIVE;

            if (availableIcon != null)
            {
                availableIcon.SetActive(state == QuestState.AVAILABLE);
            }

            if (activeIcon != null)
            {
                activeIcon.SetActive(state == QuestState.ACTIVE);
            }
        }
    }
}
