using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The workshop's uGUI (built in code with the UI kit): one calm station window with a title, the
    /// batch description (colour, scent, feel, where it leads, notes), buttons that work with mouse,
    /// keyboard and gamepad navigation, an area for the rolling and wok minigames, a hint line and
    /// short messages.
    /// </summary>
    public class WorkshopUi : MonoBehaviour
    {
        private const float MessageSeconds = 3.2f;

        private RectTransform window;
        private Text title;
        private Text body;
        private RectTransform buttons;
        private RectTransform game;
        private Text hint;
        private Text message;
        private CanvasGroup messageGroup;
        private float messageUntil;
        private readonly List<Button> made = new List<Button>();
        private int focusFrame = -1;
        private bool focusFirst;

        // Minigame visuals.
        private Image rollBall;
        private Image beatRing;
        private Image heatBar;
        private Image heatBand;
        private Image heatMarker;
        private Text gameText;

        public bool IsOpen => window != null && window.gameObject.activeSelf;

        public void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("Werkstatt UI", 30);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            Image w = UiFactory.Panel("Fenster", root, UiFactory.WindowBg);
            window = w.rectTransform;
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(980f, 700f);

            title = UiFactory.Label("Titel", window, "", 30, UiFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            Place(title.rectTransform, 28f, 20f, 28f, 640f);
            body = UiFactory.Label("Text", window, "", 21, UiFactory.TextMain, TextAnchor.UpperLeft);
            body.verticalOverflow = VerticalWrapMode.Overflow;
            Place(body.rectTransform, 28f, 66f, 28f, 430f);

            game = UiFactory.NewRect("Minispiel", window);
            Place(game, 28f, 280f, 28f, 220f);
            BuildGameVisuals();

            RectTransform list = UiFactory.MakeScroll("Knöpfe", window, out _);
            buttons = list;
            Place((RectTransform)list.parent.parent, 28f, 500f, 28f, 20f);

            hint = UiFactory.Label("Hinweis", root, "", 22, UiFactory.TextMain, TextAnchor.LowerCenter);
            RectTransform hr = hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f);
            hr.pivot = new Vector2(0.5f, 0f);
            hr.sizeDelta = new Vector2(1000f, 40f);
            hr.anchoredPosition = new Vector2(0f, 40f);
            hint.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            Text keys = UiFactory.Label("Tasten", root, ProcessingTexts.KeysLine, 15, UiFactory.TextDim, TextAnchor.LowerLeft);
            keys.rectTransform.anchorMin = keys.rectTransform.anchorMax = keys.rectTransform.pivot = Vector2.zero;
            keys.rectTransform.sizeDelta = new Vector2(900f, 24f);
            keys.rectTransform.anchoredPosition = new Vector2(24f, 8f);

            message = UiFactory.Label("Nachricht", root, "", 24, UiFactory.TextMain, TextAnchor.MiddleCenter);
            RectTransform mr = message.rectTransform;
            mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 1f);
            mr.pivot = new Vector2(0.5f, 1f);
            mr.sizeDelta = new Vector2(1100f, 80f);
            mr.anchoredPosition = new Vector2(0f, -24f);
            message.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.7f);
            messageGroup = message.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;
            messageGroup.blocksRaycasts = false;

            window.gameObject.SetActive(false);
        }

        private static void Place(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private void BuildGameVisuals()
        {
            beatRing = UiFactory.Panel("Takt", game, new Color(1f, 0.95f, 0.8f, 0.25f));
            Center(beatRing.rectTransform, new Vector2(-220f, 0f), new Vector2(180f, 180f));
            rollBall = UiFactory.Panel("Blätter", game, new Color(0.4f, 0.65f, 0.3f));
            Center(rollBall.rectTransform, new Vector2(-220f, 0f), new Vector2(120f, 80f));

            heatBar = UiFactory.Panel("Hitze", game, new Color(0f, 0f, 0f, 0.35f));
            Center(heatBar.rectTransform, new Vector2(-220f, 0f), new Vector2(60f, 200f));
            heatBand = UiFactory.Panel("Fenster", heatBar.rectTransform, new Color(1f, 0.85f, 0.55f, 0.45f));
            heatMarker = UiFactory.Panel("Glut", heatBar.rectTransform, new Color(1f, 0.6f, 0.3f, 0.95f));

            gameText = UiFactory.Label("Spieltext", game, "", 20, UiFactory.TextMain, TextAnchor.MiddleLeft);
            Center(gameText.rectTransform, new Vector2(150f, 0f), new Vector2(560f, 200f));
            game.gameObject.SetActive(false);
        }

        private static void Center(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        // ---------------- Window ----------------

        public void Open(string heading)
        {
            window.gameObject.SetActive(true);
            title.text = heading;
            body.text = "";
            ClearButtons();
            game.gameObject.SetActive(false);
        }

        public void Close()
        {
            if (window == null) return;
            ClearButtons();
            window.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void SetBody(string text) => body.text = text;

        public void ClearButtons()
        {
            foreach (Button b in made)
            {
                if (b != null) Destroy(b.gameObject);
            }

            made.Clear();
        }

        public Button AddButton(string label, Action onClick)
        {
            Button b = UiFactory.MakeButton("Knopf", buttons, label, 20, UiFactory.AccentSoft, UiFactory.TextMain);
            UiFactory.Sizing(b.gameObject, 46f);
            b.onClick.AddListener(() => onClick());
            made.Add(b);
            return b;
        }

        /// <summary>
        /// Gives the first button the gamepad / keyboard focus (or none during minigames) – one frame
        /// later, so the key press that opened the window does not also press the first button.
        /// </summary>
        public void Focus(bool firstButton)
        {
            focusFirst = firstButton;
            focusFrame = Time.frameCount + 1;
            if (!firstButton && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        // ---------------- Minigames ----------------

        public void ShowRoll(float intensity, float beatPhase, float lastEvenness, Color leaves, string text)
        {
            game.gameObject.SetActive(true);
            heatBar.gameObject.SetActive(false);
            rollBall.gameObject.SetActive(true);
            beatRing.gameObject.SetActive(true);
            float squash = 1f - 0.15f * Mathf.Clamp01(1f - beatPhase * 3f);
            float round = Mathf.Lerp(1f, 0.62f, intensity);
            rollBall.rectTransform.sizeDelta = new Vector2(140f * round / squash, 80f * squash + 40f * intensity);
            rollBall.color = leaves;
            float ring = 120f + 70f * beatPhase;
            beatRing.rectTransform.sizeDelta = new Vector2(ring, ring);
            beatRing.color = new Color(1f, 0.95f, 0.8f, 0.3f * (1f - beatPhase) * Mathf.Lerp(0.5f, 1f, lastEvenness));
            gameText.text = text;
        }

        public void ShowWok(float heat, float windowMin, float windowMax, string text)
        {
            game.gameObject.SetActive(true);
            rollBall.gameObject.SetActive(false);
            beatRing.gameObject.SetActive(false);
            heatBar.gameObject.SetActive(true);
            RectTransform band = heatBand.rectTransform;
            band.anchorMin = new Vector2(0f, Mathf.Clamp01(windowMin));
            band.anchorMax = new Vector2(1f, Mathf.Clamp01(windowMax));
            band.offsetMin = band.offsetMax = Vector2.zero;
            RectTransform marker = heatMarker.rectTransform;
            marker.anchorMin = new Vector2(-0.15f, Mathf.Clamp01(heat) - 0.012f);
            marker.anchorMax = new Vector2(1.15f, Mathf.Clamp01(heat) + 0.012f);
            marker.offsetMin = marker.offsetMax = Vector2.zero;
            gameText.text = text;
        }

        public void HideGame() => game.gameObject.SetActive(false);

        // ---------------- Hints & messages ----------------

        public void SetHint(string line)
        {
            if (hint.text != (line ?? "")) hint.text = line ?? "";
        }

        public void Message(string line)
        {
            message.text = line;
            messageUntil = Time.unscaledTime + MessageSeconds;
        }

        private void Update()
        {
            if (focusFrame >= 0 && Time.frameCount >= focusFrame)
            {
                focusFrame = -1;
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(focusFirst && made.Count > 0 && made[0] != null ? made[0].gameObject : null);
            }

            if (messageGroup != null) messageGroup.alpha = Mathf.Clamp01((messageUntil - Time.unscaledTime) / 0.6f);
        }
    }
}
