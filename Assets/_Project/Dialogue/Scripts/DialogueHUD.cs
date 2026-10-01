using System;
using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Dialogue
{
    /// <summary>
    /// Dialogfenster unten am Bildschirm (uGUI, im Code gebaut – kein Prefab):
    /// Sprechername, Portrait, Text mit Schreibmaschinen-Effekt, „Weiter".
    /// Aus Everdawn (QuestDialogueHUD) übernommen; neu:
    ///  - Text tippt sich Buchstabe für Buchstabe (Taste zeigt sofort alles),
    ///  - während des Dialogs stehen Spieler, Gegner und Uhr still (GamePause),
    ///  - optionales Portrait-Sprite.
    ///
    /// API: DialogueHUD.Instance.Play(sprecher, zeilen, beiEnde, portrait).
    /// Weiter mit E / F / Leertaste / Enter / Gamepad A oder dem Knopf.
    /// </summary>
    public class DialogueHUD : MonoBehaviour
    {
        private static DialogueHUD instance;

        public static bool IsActive => instance != null && instance.active;

        public static DialogueHUD Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("DialogueHUD");
                    instance = go.AddComponent<DialogueHUD>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        [Tooltip("Buchstaben pro Sekunde (0 = Text sofort komplett)")]
        [SerializeField] private float charactersPerSecond = 45f;

        private bool active;
        private string[] lines;
        private int index;
        private int openFrame;
        private float typedChars;
        private Action onComplete;

        // uGUI-Referenzen (im Code gebaut).
        private GameObject panel;
        private Text speakerText;
        private Text bodyText;
        private Text pageText;
        private Image portraitImage;
        private Text portraitMark;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildUi();
        }

        private void OnDestroy()
        {
            GamePause.Set(this, false);
            if (instance == this)
            {
                instance = null;
            }
        }

        public void Play(string speaker, string[] lines, Action onComplete = null, Sprite portrait = null)
        {
            if (lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            this.lines = lines;
            this.onComplete = onComplete;
            index = 0;
            active = true;
            openFrame = Time.frameCount;
            GamePause.Set(this, true);

            if (panel == null)
            {
                BuildUi();
            }

            speakerText.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
            portraitImage.sprite = portrait;
            portraitImage.color = portrait != null ? Color.white : UiFactory.AccentSoft;
            portraitMark.text = portrait != null ? "" : Initial(speaker);

            StartLine();
            panel.SetActive(true);
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            if (!IsLineComplete)
            {
                typedChars += charactersPerSecond * Time.unscaledDeltaTime;
                RefreshBody();
            }

            if (Time.frameCount != openFrame && GameInput.AdvanceDialoguePressed)
            {
                Advance();
            }
        }

        private bool IsLineComplete => charactersPerSecond <= 0f || typedChars >= lines[index].Length;

        private void Advance()
        {
            // Erster Druck während des Tippens: Zeile sofort komplett zeigen.
            if (!IsLineComplete)
            {
                typedChars = lines[index].Length;
                RefreshBody();
                return;
            }

            index++;
            if (index >= lines.Length)
            {
                Close();
            }
            else
            {
                StartLine();
            }
        }

        private void Close()
        {
            active = false;
            if (panel != null)
            {
                panel.SetActive(false);
            }

            GamePause.Set(this, false);

            Action cb = onComplete;
            onComplete = null;
            cb?.Invoke();
        }

        private void StartLine()
        {
            typedChars = charactersPerSecond > 0f ? 0f : lines[index].Length;
            RefreshBody();
            pageText.text = $"[{index + 1}/{lines.Length}]   Weiter: {GameInput.InteractKeyLabel} / Leertaste";
        }

        private void RefreshBody()
        {
            string line = lines[index] ?? "";
            int count = Mathf.Clamp(Mathf.FloorToInt(typedChars), 0, line.Length);
            bodyText.text = count >= line.Length ? line : line.Substring(0, count);
        }

        private static string Initial(string speaker)
        {
            return string.IsNullOrEmpty(speaker) ? "?" : speaker.Substring(0, 1).ToUpperInvariant();
        }

        // ---------------- Aufbau ----------------

        private void BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas("DialogueCanvas", 90);
            canvas.transform.SetParent(transform, false);

            Image box = UiFactory.Panel("DialoguePanel", canvas.transform, UiFactory.WindowBg);
            panel = box.gameObject;
            RectTransform boxRt = box.rectTransform;
            Place(boxRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(1000f, 230f));

            Image accent = UiFactory.Panel("Accent", boxRt, UiFactory.Accent);
            Place(accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 4f));

            // Portrait links (Sprite oder Initiale als Platzhalter).
            portraitImage = UiFactory.Panel("Portrait", boxRt, UiFactory.AccentSoft);
            portraitImage.preserveAspect = true;
            Place(portraitImage.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -26f), new Vector2(160f, 160f));
            portraitMark = UiFactory.Label("PortraitMark", portraitImage.rectTransform, "?", 64,
                new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.FullStretch(portraitMark.rectTransform);

            speakerText = UiFactory.Label("Speaker", boxRt, "", 22, UiFactory.Accent,
                TextAnchor.LowerLeft, FontStyle.Bold);
            RectTransform spRt = speakerText.rectTransform;
            spRt.anchorMin = new Vector2(0f, 1f);
            spRt.anchorMax = new Vector2(1f, 1f);
            spRt.offsetMin = new Vector2(205f, -56f);
            spRt.offsetMax = new Vector2(-24f, -20f);

            bodyText = UiFactory.Label("Body", boxRt, "", 20, UiFactory.TextMain, TextAnchor.UpperLeft);
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform bodyRt = bodyText.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(205f, 58f);
            bodyRt.offsetMax = new Vector2(-24f, -62f);

            pageText = UiFactory.Label("Page", boxRt, "", 13, UiFactory.TextDim, TextAnchor.LowerLeft);
            Place(pageText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(205f, 18f), new Vector2(520f, 24f));

            Button next = UiFactory.MakeButton("Weiter", boxRt, "Weiter ▶", 16,
                UiFactory.AccentSoft, UiFactory.TextMain);
            Place(next.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-20f, 16f), new Vector2(150f, 40f));
            next.onClick.AddListener(() =>
            {
                if (active && Time.frameCount != openFrame)
                {
                    Advance();
                }
            });

            panel.SetActive(false);
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
