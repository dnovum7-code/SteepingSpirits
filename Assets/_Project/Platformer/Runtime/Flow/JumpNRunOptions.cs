using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Pause menu with the comfort (assist) options, built with uGUI. Esc (or
    /// B on the pad) opens/closes it. Up/down chooses a row, left/right changes
    /// the value, confirm toggles. Choices are kept in the Jump'n'Run save and
    /// applied to the player, the session and the game speed.
    /// </summary>
    public class JumpNRunOptions : MonoBehaviour
    {
        public const string PrefsKey = "SteepingSpirits.JumpNRun.Assists";

        private AssistOptions options = new AssistOptions();
        private bool open;
        private float lastMoveX;

        private GameObject panel;
        private readonly List<Selectable> rows = new List<Selectable>();
        private readonly List<Text> values = new List<Text>();
        private Text hint;

        public AssistOptions Options => options;
        public bool IsOpen => open;

        public static JumpNRunOptions Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            // Options live in the Jump'n'Run save; older PlayerPrefs values are taken over once.
            string stored = JumpNRunSaveStore.Current.Assists;
            if (string.IsNullOrEmpty(stored))
            {
                stored = PlayerPrefs.GetString(PrefsKey, "");
            }

            options = AssistOptions.Parse(stored);
        }

        private void Start()
        {
            Apply();
            hint = JumpNRunUi.Corner("PauseHint", JumpNRunTexts.MenuHint, 18, new Color(1f, 1f, 1f, 0.5f),
                new Vector2(1f, 0f), new Vector2(-16f, 12f), new Vector2(300f, 30f), TextAnchor.LowerRight);
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

            string text = options.Serialize();
            if (JumpNRunSaveStore.Current.Assists != text)
            {
                JumpNRunSaveStore.Current.Assists = text;
                JumpNRunSaveStore.Save();
            }

            RefreshValues();
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

            // Left/right changes the focused row (up/down and confirm come from the EventSystem).
            float x = GameInput.Move.x;
            int dx = 0;
            if (x > 0.5f && lastMoveX <= 0.5f) dx = 1;
            if (x < -0.5f && lastMoveX >= -0.5f) dx = -1;
            lastMoveX = x;
            if (dx != 0)
            {
                for (int i = 0; i < rows.Count - 1; i++)
                {
                    if (JumpNRunUi.IsFocused(rows[i])) Change(i, dx);
                }
            }
        }

        private const int RowCount = 4;

        private void Change(int row, int dir)
        {
            switch (row)
            {
                case 0: options.StepSpeed(dir); break;
                case 1: options.extraAirJump = !options.extraAirJump; break;
                case 2: options.fallProtection = !options.fallProtection; break;
                case 3: options.autoSwing = !options.autoSwing; break;
            }

            Apply();
        }

        private void SetOpen(bool value)
        {
            open = value;
            GamePause.Set(this, open);
            if (open)
            {
                Build();
                panel.SetActive(true);
                RefreshValues();
                JumpNRunUi.Focus(rows[0]);
            }
            else if (panel != null)
            {
                panel.SetActive(false);
            }

            if (hint != null) hint.enabled = !open;
        }

        private void Build()
        {
            if (panel != null)
            {
                return;
            }

            RectTransform root = JumpNRunUi.Root;
            panel = UiFactory.NewRect("OptionsMenu", root).gameObject;
            UiFactory.FullStretch((RectTransform)panel.transform);
            JumpNRunUi.Dim(panel.transform, 0.45f);
            RectTransform content = JumpNRunUi.Window(panel.transform, JumpNRunTexts.OptionsTitle,
                new Vector2(760f, 260f + RowCount * 92f), out _);

            AddRow(content, JumpNRunTexts.GameSpeed, JumpNRunTexts.GameSpeedHelp, 0);
            AddRow(content, JumpNRunTexts.ExtraAirJump, JumpNRunTexts.ExtraAirJumpHelp, 1);
            AddRow(content, JumpNRunTexts.FallProtection, JumpNRunTexts.FallProtectionHelp, 2);
            AddRow(content, JumpNRunTexts.AutoSwing, JumpNRunTexts.AutoSwingHelp, 3);
            Button resume = JumpNRunUi.Button(content, JumpNRunTexts.Resume);
            resume.onClick.AddListener(() => SetOpen(false));
            rows.Add(resume);

            JumpNRunUi.Line(content, JumpNRunTexts.OptionsHint, 18, UiFactory.TextDim, TextAnchor.MiddleCenter);
            JumpNRunUi.Chain(rows, true);
            panel.SetActive(false);
        }

        private void AddRow(Transform parent, string name, string help, int index)
        {
            Button b = JumpNRunUi.Button(parent, "", 54f);
            Text label = b.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.text = "  " + name;

            Text value = UiFactory.Label("Value", b.transform, "", 26, UiFactory.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.FullStretch(value.rectTransform, 12f, 2f, 18f, 2f);
            value.raycastTarget = false;
            values.Add(value);

            b.onClick.AddListener(() => Change(index, 1));
            rows.Add(b);
            JumpNRunUi.Line(parent, help, 18, UiFactory.TextDim, TextAnchor.UpperLeft, 26f);
        }

        private void RefreshValues()
        {
            if (values.Count == 0)
            {
                return;
            }

            SetText(values[0], JumpNRunTexts.Percent(options.ClampedSpeed));
            SetText(values[1], JumpNRunTexts.OnOff(options.extraAirJump));
            SetText(values[2], JumpNRunTexts.OnOff(options.fallProtection));
            SetText(values[3], JumpNRunTexts.OnOff(options.autoSwing));
        }

        private static void SetText(Text t, string s)
        {
            if (t.text != s) t.text = s;
        }
    }
}
