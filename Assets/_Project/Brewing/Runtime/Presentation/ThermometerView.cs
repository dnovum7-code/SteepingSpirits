using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Optional thermometer (T). Off by default: the water should be read from
    /// bubbles, sound and steam. Usage is recorded in the brew result.
    /// </summary>
    public class ThermometerView : BrewView
    {
        [SerializeField] private Rect screenRect = new Rect(20f, 20f, 190f, 64f);

        private GUIStyle big;
        private GUIStyle small;

        private void OnGUI()
        {
            BrewSession s = Session;
            if (s == null || controller == null)
            {
                return;
            }

            if (big == null)
            {
                big = GuiDraw.Rich(22, Color.white, FontStyle.Bold);
                small = GuiDraw.Rich(12, new Color(0.9f, 0.85f, 0.75f));
            }

            if (!controller.ThermometerVisible)
            {
                GuiDraw.ShadowLabel(new Rect(screenRect.x, screenRect.y, 200f, 20f), BrewTexts.ThermometerHint, small);
                return;
            }

            GuiDraw.Panel(screenRect, 0.8f);
            GUI.Label(new Rect(screenRect.x + 12f, screenRect.y + 6f, screenRect.width, 30f),
                $"Kessel {s.Water.Temperature:0} °C", big);

            string vessel = s.Extraction != null && s.Phase == BrewPhase.Steep
                ? $"Gefäß {s.Extraction.Temperature:0} °C"
                : "";
            GUI.Label(new Rect(screenRect.x + 12f, screenRect.y + 38f, screenRect.width, 20f), vessel, small);
        }
    }
}
