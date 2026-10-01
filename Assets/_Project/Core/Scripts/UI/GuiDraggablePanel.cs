using UnityEngine;

namespace SteepingSpirits.Core.UI
{
    /// <summary>
    /// Macht ein OnGUI-Panel per Maus verschiebbar. Jedes Panel hält eine eigene
    /// Instanz und ruft in OnGUI <c>rect = panel.Apply(baseRect)</c> auf – danach
    /// bei <c>rect</c> zeichnen. Ziehen am oberen Streifen (Höhe = handleHeight);
    /// ein Bereich rechts (handleRightInset) bleibt frei, z.B. für ein ✕.
    /// </summary>
    public class GuiDraggablePanel
    {
        private Vector2 offset;
        private bool dragging;
        private Vector2 grab;
        private static GUIStyle gripStyle;

        public Rect Apply(Rect baseRect, float handleHeight = 26f, float handleRightInset = 0f)
        {
            var r = new Rect(baseRect.x + offset.x, baseRect.y + offset.y, baseRect.width, baseRect.height);
            var handle = new Rect(r.x, r.y, Mathf.Max(0f, r.width - handleRightInset), handleHeight);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                dragging = true;
                grab = e.mousePosition - offset;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && dragging)
            {
                offset = e.mousePosition - grab;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && dragging)
            {
                dragging = false;
                e.Use();
            }

            if (e.type == EventType.Repaint)
            {
                DrawGrip(r); // Zieh-Hinweis
            }

            return r;
        }

        /// <summary>
        /// Zeichnet den „≡"-Zieh-Hinweis. Wer nach Apply() einen deckenden
        /// Hintergrund malt, ruft das danach nochmal auf, damit er sichtbar bleibt.
        /// </summary>
        public static void DrawGrip(Rect panelRect)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureStyle();
            GUI.Label(new Rect(panelRect.x + 4f, panelRect.y + 2f, 20f, 18f), "≡", gripStyle);
        }

        private static void EnsureStyle()
        {
            if (gripStyle != null)
            {
                return;
            }

            gripStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            gripStyle.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        }
    }
}
