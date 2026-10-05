using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Pause menu with the comfort (assist) options. Esc opens/closes it. The
    /// choice is remembered per player in PlayerPrefs and applied to the
    /// player, the session and the game speed.
    /// </summary>
    public class JumpNRunOptions : MonoBehaviour
    {
        public const string PrefsKey = "SteepingSpirits.JumpNRun.Assists";

        private AssistOptions options = new AssistOptions();
        private bool open;
        private int selected;
        private float lastMoveY;
        private float lastMoveX;
        private GUIStyle title, label, help;

        public AssistOptions Options => options;
        public bool IsOpen => open;

        public static JumpNRunOptions Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            try
            {
                options = AssistOptions.Parse(PlayerPrefs.GetString(PrefsKey, ""));
            }
            catch (System.Exception)
            {
                options = new AssistOptions();
            }
        }

        private void Start()
        {
            Apply();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GamePause.Set(this, false);
        }

        public void Apply()
        {
            JumpNRunTime.SetBaseSpeed(options.ClampedSpeed);
            if (JumpNRunPlayer.Instance != null)
            {
                JumpNRunPlayer.Instance.Motor.BonusAirJumps = options.extraAirJump ? 1 : 0;
            }

            if (JumpNRunSession.Current != null)
            {
                JumpNRunSession.Current.FallProtection = options.fallProtection;
            }

            PlayerPrefs.SetString(PrefsKey, options.Serialize());
        }

        private void Update()
        {
            if (GameInput.CancelPressed)
            {
                SetOpen(!open);
                return;
            }

            if (!open)
            {
                return;
            }

            Vector2 move = GameInput.Move;
            if (move.y > 0.5f && lastMoveY <= 0.5f) selected = (selected + 3) % 4;
            if (move.y < -0.5f && lastMoveY >= -0.5f) selected = (selected + 1) % 4;
            int dx = 0;
            if (move.x > 0.5f && lastMoveX <= 0.5f) dx = 1;
            if (move.x < -0.5f && lastMoveX >= -0.5f) dx = -1;
            lastMoveY = move.y;
            lastMoveX = move.x;

            if (dx != 0)
            {
                Change(selected, dx);
            }

            if (GameInput.JumpPressed || GameInput.InteractPressed)
            {
                if (selected == 3) SetOpen(false);
                else Change(selected, 1);
            }
        }

        private void Change(int row, int dir)
        {
            switch (row)
            {
                case 0: options.StepSpeed(dir); break;
                case 1: options.extraAirJump = !options.extraAirJump; break;
                case 2: options.fallProtection = !options.fallProtection; break;
            }

            Apply();
        }

        private void SetOpen(bool value)
        {
            open = value;
            GamePause.Set(this, open);
            selected = 0;
        }

        private void OnGUI()
        {
            if (!open)
            {
                GUI.Label(new Rect(Screen.width - 160f, Screen.height - 28f, 150f, 22f), JumpNRunTexts.MenuHint,
                    GuiDraw.Rich(12, new Color(1f, 1f, 1f, 0.5f), FontStyle.Normal, TextAnchor.MiddleRight));
                return;
            }

            if (title == null)
            {
                title = GuiDraw.Rich(20, new Color(1f, 0.92f, 0.78f), FontStyle.Bold, TextAnchor.MiddleCenter);
                label = GuiDraw.Rich(15, Color.white);
                help = GuiDraw.Rich(12, new Color(1f, 1f, 1f, 0.7f));
            }

            GuiDraw.Solid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.05f, 0.06f, 0.1f, 0.45f));
            var r = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 170f, 460f, 340f);
            GuiDraw.Panel(r, 0.94f);
            GUI.Label(new Rect(r.x, r.y + 12f, r.width, 30f), JumpNRunTexts.OptionsTitle, title);

            float y = r.y + 56f;
            Row(0, ref y, r, JumpNRunTexts.GameSpeed, JumpNRunTexts.Percent(options.ClampedSpeed), JumpNRunTexts.GameSpeedHelp);
            Row(1, ref y, r, JumpNRunTexts.ExtraAirJump, JumpNRunTexts.OnOff(options.extraAirJump), JumpNRunTexts.ExtraAirJumpHelp);
            Row(2, ref y, r, JumpNRunTexts.FallProtection, JumpNRunTexts.OnOff(options.fallProtection), JumpNRunTexts.FallProtectionHelp);
            Row(3, ref y, r, JumpNRunTexts.Resume, "", "");

            GUI.Label(new Rect(r.x, r.yMax - 28f, r.width, 20f), JumpNRunTexts.OptionsHint,
                GuiDraw.Rich(11, new Color(1f, 1f, 1f, 0.55f), FontStyle.Normal, TextAnchor.MiddleCenter));
        }

        private void Row(int index, ref float y, Rect panel, string name, string value, string description)
        {
            var row = new Rect(panel.x + 20f, y, panel.width - 40f, 54f);
            if (selected == index)
            {
                GuiDraw.Solid(row, new Color(1f, 0.85f, 0.6f, 0.12f));
            }

            if (GUI.Button(new Rect(row.x, row.y, row.width, 26f), GUIContent.none, GUIStyle.none))
            {
                selected = index;
                if (index == 3) SetOpen(false);
                else Change(index, 1);
            }

            GUI.Label(new Rect(row.x + 8f, row.y + 2f, row.width - 100f, 24f), name, label);
            GUI.Label(new Rect(row.xMax - 100f, row.y + 2f, 92f, 24f), value,
                GuiDraw.Rich(15, new Color(1f, 0.9f, 0.6f), FontStyle.Bold, TextAnchor.UpperRight));
            if (!string.IsNullOrEmpty(description))
            {
                GUI.Label(new Rect(row.x + 8f, row.y + 26f, row.width - 16f, 26f), description, help);
            }

            y += 62f;
        }
    }
}
