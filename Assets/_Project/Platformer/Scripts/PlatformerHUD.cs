using UnityEngine;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// HUD für die Jump'n'Run-Szene (OnGUI, Zero-Wiring): Zeit, Tode,
    /// Dash-Anzeige, Steuerungs-Hinweis und Ziel-Banner.
    /// </summary>
    public class PlatformerHUD : MonoBehaviour
    {
        [SerializeField] private bool showControls = true;

        private PlatformerController2D player;
        private float startTime;
        private float finishTime = -1f;
        private int finishDeaths;

        private void OnEnable()
        {
            Goal2D.OnGoalReached += HandleGoal;
        }

        private void OnDisable()
        {
            Goal2D.OnGoalReached -= HandleGoal;
        }

        private void Start()
        {
            startTime = Time.time;
        }

        private void HandleGoal(Goal2D goal)
        {
            if (finishTime < 0f)
            {
                finishTime = Time.time - startTime;
                finishDeaths = player != null ? player.Deaths : 0;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlatformerController2D>();
            }

            if (Input2DToggle())
            {
                showControls = !showControls;
            }
        }

        private static bool Input2DToggle() => Core.GameInput.HelpTogglePressed;

        private void OnGUI()
        {
            if (player == null)
            {
                return;
            }

            float t = finishTime >= 0f ? finishTime : Time.time - startTime;
            var box = new Rect(12f, 12f, 230f, 64f);
            GuiDraw.Panel(box);
            GUIStyle big = GuiDraw.Rich(20, Color.white, FontStyle.Bold);
            GUIStyle small = GuiDraw.Rich(13, new Color(0.95f, 0.88f, 0.75f));
            GUI.Label(new Rect(box.x + 12f, box.y + 6f, 200f, 28f), FormatTime(t), big);
            GUI.Label(new Rect(box.x + 12f, box.y + 36f, 120f, 22f), $"Tode: {player.Deaths}", small);

            // Dash-Pips
            for (int i = 0; i < player.MaxDashes; i++)
            {
                bool ready = i < player.DashesLeft;
                var pip = new Rect(box.x + 150f + i * 22f, box.y + 38f, 16f, 16f);
                GuiDraw.Solid(pip, ready ? new Color(0.95f, 0.4f, 0.35f) : new Color(0.3f, 0.3f, 0.4f));
                GuiDraw.Border(pip, new Color(0f, 0f, 0f, 0.6f), 1f);
            }

            if (showControls)
            {
                var hint = new Rect(12f, Screen.height - 74f, 560f, 62f);
                GuiDraw.Panel(hint, 0.75f);
                GUI.Label(new Rect(hint.x + 12f, hint.y + 6f, hint.width - 24f, hint.height - 10f),
                    "<b>Laufen</b> WASD/Pfeile · <b>Springen</b> Leertaste/C · <b>Dash</b> Shift/X (+Richtung)\n" +
                    "<b>Greifen</b> K/Strg halten · Schwingen: im Takt links/rechts · Liane: hoch/runter klettern · <b>N</b> Hilfe aus",
                    GuiDraw.Rich(12));
            }

            if (finishTime >= 0f)
            {
                var banner = new Rect((Screen.width - 520f) * 0.5f, Screen.height * 0.25f, 520f, 90f);
                GuiDraw.Solid(banner, new Color(0.08f, 0.1f, 0.08f, 0.9f));
                GuiDraw.Border(banner, new Color(0.5f, 0.95f, 0.5f), 2f);
                GUI.Label(new Rect(banner.x, banner.y + 12f, banner.width, 34f), "✔ Geschafft!",
                    GuiDraw.Rich(26, new Color(0.6f, 1f, 0.6f), FontStyle.Bold, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(banner.x, banner.y + 50f, banner.width, 26f),
                    $"Zeit {FormatTime(finishTime)}   ·   Tode {finishDeaths}   ·   [E] am Portal = zurück",
                    GuiDraw.Rich(15, new Color(1f, 0.92f, 0.6f), FontStyle.Normal, TextAnchor.MiddleCenter));
            }
        }

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            float s = seconds - m * 60f;
            return $"{m:00}:{s:00.00}";
        }
    }
}
