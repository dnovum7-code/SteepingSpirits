using System.Text;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Dialogue;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// Kompakter Quest-Tracker LINKS: zeigt die aktiven Quests mit dem
    /// NÄCHSTEN offenen Schritt ("→ mit npc_aiden reden: 0/1"). Verschiebbar
    /// (oben ziehen). Zero-Wiring, duplikatsicher.
    /// </summary>
    public class QuestTrackerHUD : MonoBehaviour
    {
        private static QuestTrackerHUD active;

        private readonly GuiDraggablePanel panel = new GuiDraggablePanel();
        private GUIStyle headStyle;
        private GUIStyle bodyStyle;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }
            active = this;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                active = null;
            }
        }

        private void OnGUI()
        {
            if (QuestJournalCanvas.IsOpen || DialogueHUD.IsActive)
            {
                return;
            }

            QuestManager qm = QuestManager.Instance;
            if (qm == null)
            {
                return;
            }

            var actives = qm.GetActiveQuests();
            if (actives.Count == 0)
            {
                return;
            }

            EnsureStyles();

            var sb = new StringBuilder();
            foreach (QuestInstance quest in actives)
            {
                sb.Append("<b>").Append(quest.Data.displayName).Append("</b>\n");
                ObjectiveData next = NextObjective(quest);
                if (next != null)
                {
                    sb.Append("   → ").Append(next.description).Append(": ")
                      .Append(quest.GetProgress(next.objectiveID)).Append('/').Append(next.requiredAmount).Append('\n');
                }
                else
                {
                    sb.Append("   <color=#7fe07f>→ bereit zum Abschluss</color>\n");
                }
            }

            string text = sb.ToString().TrimEnd('\n');
            const float width = 320f;
            float height = bodyStyle.CalcHeight(new GUIContent(text), width - 24f) + 34f;

            var box = panel.Apply(new Rect(12f, 118f, width, height));
            GuiDraw.Panel(box);
            GuiDraggablePanel.DrawGrip(box);
            GameInput.ClaimGuiArea(box);
            GUI.Label(new Rect(box.x + 24f, box.y + 6f, width - 30f, 20f), "Aktive Quest", headStyle);
            GUI.Label(new Rect(box.x + 12f, box.y + 28f, width - 24f, height - 34f), text, bodyStyle);
        }

        private static ObjectiveData NextObjective(QuestInstance quest)
        {
            if (quest.Data.objectives == null)
            {
                return null;
            }

            foreach (ObjectiveData objective in quest.Data.objectives)
            {
                if (!quest.IsObjectiveComplete(objective.objectiveID))
                {
                    return objective;
                }
            }

            return null;
        }

        private void EnsureStyles()
        {
            if (headStyle != null)
            {
                return;
            }

            headStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            headStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);

            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = true };
            bodyStyle.normal.textColor = new Color(0.92f, 0.94f, 0.98f);
        }
    }
}
