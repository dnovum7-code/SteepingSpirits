using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>Quiet prompt line at the bottom: which tea, what the player can do now.</summary>
    public class BrewPromptView : BrewView
    {
        private GUIStyle style;
        private GUIStyle titleStyle;

        private void OnGUI()
        {
            BrewSession s = Session;
            if (s == null)
            {
                return;
            }

            if (style == null)
            {
                style = GuiDraw.Rich(15, new Color(0.98f, 0.94f, 0.85f), FontStyle.Normal, TextAnchor.MiddleCenter);
                titleStyle = GuiDraw.Rich(20, new Color(1f, 0.88f, 0.6f), FontStyle.Bold, TextAnchor.MiddleCenter);
            }

            string title;
            string prompt;
            switch (s.Phase)
            {
                case BrewPhase.SelectTea:
                    title = "";
                    prompt = TeaChoices();
                    break;
                case BrewPhase.HeatWater:
                    title = TeaName();
                    prompt = BrewTexts.HeatPrompt + "\n" + (s.Water.HeatOn ? BrewTexts.FireOn : BrewTexts.FireOff)
                             + (s.VesselPrewarmed ? "   ·   " + BrewTexts.Prewarmed : "");
                    break;
                case BrewPhase.PreWarm:
                    title = TeaName();
                    prompt = BrewTexts.PrewarmPrompt;
                    break;
                case BrewPhase.Pour:
                    title = TeaName();
                    prompt = BrewTexts.PourPrompt;
                    break;
                case BrewPhase.Steep:
                    title = TeaName();
                    prompt = BrewTexts.SteepPrompt;
                    break;
                default:
                    title = "";
                    prompt = BrewTexts.ResultPrompt;
                    break;
            }

            var box = new Rect(Screen.width * 0.5f - 420f, Screen.height - 96f, 840f, 72f);
            GuiDraw.Panel(box, 0.75f);
            if (!string.IsNullOrEmpty(title))
            {
                GUI.Label(new Rect(box.x, box.y - 34f, box.width, 30f), title, titleStyle);
            }
            GUI.Label(box, prompt, style);
        }

        private string TeaName()
        {
            TeaDefinition tea = controller.CurrentTea;
            return tea != null ? tea.displayName : "";
        }

        private string TeaChoices()
        {
            var sb = new System.Text.StringBuilder(BrewTexts.ChooseTea).Append('\n');
            for (int i = 0; i < controller.Teas.Count; i++)
            {
                if (i > 0) sb.Append("     ");
                sb.Append('[').Append(i + 1).Append("] ").Append(controller.Teas[i].displayName);
            }

            return sb.ToString();
        }
    }
}
