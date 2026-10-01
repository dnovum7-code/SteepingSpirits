using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Dialogue;
using SteepingSpirits.Quests.Integration;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// Zeichnet schwebende Quest-Marker über allen QuestNpc in der Szene:
    ///  „!" = hier gibt es eine Quest,  „?" = Ziel einer laufenden Quest.
    /// Zero-Wiring (OnGUI) – einfach die Komponente in die Szene legen.
    /// Duplikatsicher: immer nur EINES aktiv.
    /// </summary>
    public class QuestMarkerHUD : MonoBehaviour
    {
        private static QuestMarkerHUD active;

        private const float RescanInterval = 1.5f;

        [Tooltip("Höhe des Markers über dem NPC-Ursprung (Einheiten)")]
        [SerializeField] private float markerHeight = 1.35f;

        private readonly List<QuestNpc> npcs = new List<QuestNpc>();
        private float nextScan;
        private GUIStyle style;

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

        private void Update()
        {
            // NPC-Liste selten neu einlesen (NPCs ändern sich kaum).
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + RescanInterval;
                npcs.Clear();
                npcs.AddRange(Object.FindObjectsByType<QuestNpc>());
            }
        }

        private void OnGUI()
        {
            // Bei offenem Journal / laufendem Dialog keine Welt-Marker.
            if (QuestJournalCanvas.IsOpen || DialogueHUD.IsActive)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            EnsureStyle();
            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 5f;

            foreach (QuestNpc npc in npcs)
            {
                if (npc == null || !npc.TryGetMarker(out string glyph, out Color color))
                {
                    continue;
                }

                Vector3 sp = cam.WorldToScreenPoint(npc.transform.position + Vector3.up * markerHeight);
                if (sp.z <= 0f)
                {
                    continue; // hinter der Kamera
                }

                float x = sp.x - 16f;
                float y = (Screen.height - sp.y) - 20f + bob;
                var rect = new Rect(x, y, 32f, 36f);

                style.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
                GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), glyph, style);
                style.normal.textColor = color;
                GUI.Label(rect, glyph, style);
            }
        }

        private void EnsureStyle()
        {
            if (style != null)
            {
                return;
            }

            style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
