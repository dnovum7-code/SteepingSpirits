using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Result card: tier, a kind sentence, notes (tart, stale water) and one tip
    /// for non-perfect cups. Fades in slowly on unscaled time – no flash, no cut.
    /// </summary>
    public class BrewResultView : BrewView
    {
        [SerializeField] private Color perfectColor = new Color(1f, 0.9f, 0.6f);
        [SerializeField] private Color normalColor = new Color(0.98f, 0.94f, 0.86f);

        private float shownSince = -1f;
        private BrewResult shown;
        private GUIStyle tierStyle;
        private GUIStyle textStyle;
        private GUIStyle noteStyle;

        private void OnGUI()
        {
            BrewSession s = Session;
            if (s == null || Look == null)
            {
                return;
            }

            if (s.Phase != BrewPhase.Result || s.LastResult == null)
            {
                shown = null;
                return;
            }

            if (shown != s.LastResult)
            {
                shown = s.LastResult;
                shownSince = Time.unscaledTime;
            }

            EnsureStyles();
            float alpha = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - shownSince) / Mathf.Max(0.01f, Look.resultFadeSeconds));
            BrewResult r = shown;

            var box = new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.16f, 600f, 170f);
            GuiDraw.Solid(box, new Color(0.18f, 0.12f, 0.08f, 0.82f * alpha));
            GuiDraw.Border(box, new Color(0.7f, 0.55f, 0.35f, 0.7f * alpha), 2f);

            Color tierColor = r.Tier == QualityTier.Perfect ? perfectColor : normalColor;
            tierColor.a = alpha;
            tierStyle.normal.textColor = tierColor;
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 40f), BrewTexts.Tier(r.Tier), tierStyle);

            textStyle.normal.textColor = new Color(normalColor.r, normalColor.g, normalColor.b, alpha);
            GUI.Label(new Rect(box.x + 20f, box.y + 60f, box.width - 40f, 28f), BrewTexts.TierMessage(r.Tier), textStyle);

            string notes = "";
            if (r.IsTart) notes += BrewTexts.Tart + "  ";
            if (r.StaleWater) notes += BrewTexts.StaleWater;
            if (r.Tier == QualityTier.Flat || r.Tier == QualityTier.Decent)
            {
                string hint = BrewTexts.Hint(r.Hint);
                if (hint.Length > 0) notes += (notes.Length > 0 ? "\n" : "") + hint;
            }

            noteStyle.normal.textColor = new Color(0.85f, 0.78f, 0.65f, alpha);
            GUI.Label(new Rect(box.x + 20f, box.y + 96f, box.width - 40f, 64f), notes, noteStyle);
        }

        private void EnsureStyles()
        {
            if (tierStyle != null)
            {
                return;
            }

            tierStyle = GuiDraw.Rich(30, normalColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            textStyle = GuiDraw.Rich(16, normalColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            noteStyle = GuiDraw.Rich(13, normalColor, FontStyle.Italic, TextAnchor.UpperCenter);
        }
    }
}
