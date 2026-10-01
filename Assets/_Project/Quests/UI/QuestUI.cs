using UnityEngine;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// Dünner Koordinator der Quest-UI: verdrahtet Log, Tracker und Popup.
    /// Die UI-Schicht abonniert nur QuestEvents – sie hat keine direkte
    /// Referenz auf den QuestManager und berechnet nichts selbst.
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        [Tooltip("Panel mit dem QuestLogView – wird per ToggleQuestLog() ein-/ausgeblendet")]
        [SerializeField] private GameObject questLogPanel;

        private void Start()
        {
            if (questLogPanel != null)
            {
                questLogPanel.SetActive(false);
            }
        }

        /// <summary>An einen Button oder das Input-System binden (z.B. Taste J).</summary>
        public void ToggleQuestLog()
        {
            if (questLogPanel != null)
            {
                questLogPanel.SetActive(!questLogPanel.activeSelf);
            }
        }
    }
}
