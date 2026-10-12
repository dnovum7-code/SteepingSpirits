using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Garden HUD (uGUI built in code): toolbar with the five tools (keys 1–5), the chosen seed,
    /// the can level, day and weather, a calm hint for the target cell, short messages and the
    /// soft fade at the end of the day. Texts only change when their values change.
    /// </summary>
    public class GardenHud : MonoBehaviour
    {
        private const int Slots = 5;
        private const float SlotSize = 104f;
        private const float MessageSeconds = 3.2f;

        private readonly Image[] slotBg = new Image[Slots];
        private readonly Text[] slotName = new Text[Slots];
        private readonly Text[] slotSub = new Text[Slots];
        private Image canFill;
        private Text dayText;
        private Text optionsText;
        private Text hintText;
        private Text messageText;
        private Text keysText;
        private Image fade;
        private CanvasGroup messageGroup;

        private int shownTool = -1;
        private string shownHint;
        private string shownDay;
        private string shownSeed;
        private string shownCan;
        private string shownOptions;
        private float messageUntil;
        private float fadeTarget;
        private float fadeSpeed = 2f;

        public bool FadeDone => fade == null || Mathf.Abs(fade.color.a - fadeTarget) < 0.01f;

        public void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("Teegarten HUD", 20);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            RectTransform bar = UiFactory.NewRect("Werkzeugleiste", root);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(Slots * (SlotSize + 10f), SlotSize);
            bar.anchoredPosition = new Vector2(0f, 24f);
            for (int i = 0; i < Slots; i++)
            {
                Image bg = UiFactory.Panel("Slot " + (i + 1), bar, UiFactory.WindowBg);
                RectTransform rt = bg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(SlotSize, SlotSize);
                rt.anchoredPosition = new Vector2(5f + i * (SlotSize + 10f), 0f);
                slotBg[i] = bg;

                Text key = UiFactory.Label("Taste", rt, (i + 1).ToString(), 18, UiFactory.TextDim, TextAnchor.UpperLeft);
                UiFactory.FullStretch(key.rectTransform, 8f, 4f, 8f, 4f);
                slotName[i] = UiFactory.Label("Name", rt, GardenTexts.ToolName((GardenTool)i), 19, UiFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
                UiFactory.FullStretch(slotName[i].rectTransform, 4f, 18f, 4f, 30f);
                slotSub[i] = UiFactory.Label("Info", rt, "", 15, UiFactory.TextDim, TextAnchor.LowerCenter);
                UiFactory.FullStretch(slotSub[i].rectTransform, 4f, 50f, 4f, 8f);
            }

            // Can level as a thin bar inside the can slot.
            Image canBack = UiFactory.Panel("Kanne leer", slotBg[(int)GardenTool.Can].rectTransform, new Color(0f, 0f, 0f, 0.35f));
            canBack.rectTransform.anchorMin = new Vector2(0.12f, 0.06f);
            canBack.rectTransform.anchorMax = new Vector2(0.88f, 0.1f);
            canBack.rectTransform.offsetMin = canBack.rectTransform.offsetMax = Vector2.zero;
            canFill = UiFactory.Panel("Kanne voll", canBack.rectTransform, new Color(0.55f, 0.78f, 1f, 0.9f));
            canFill.rectTransform.anchorMin = Vector2.zero;
            canFill.rectTransform.anchorMax = Vector2.one;
            canFill.rectTransform.offsetMin = canFill.rectTransform.offsetMax = Vector2.zero;
            canFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            hintText = UiFactory.Label("Hinweis", root, "", 22, UiFactory.TextMain, TextAnchor.LowerCenter);
            RectTransform hr = hintText.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f);
            hr.pivot = new Vector2(0.5f, 0f);
            hr.sizeDelta = new Vector2(900f, 40f);
            hr.anchoredPosition = new Vector2(0f, SlotSize + 34f);
            hintText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            dayText = UiFactory.Label("Tag", root, "", 24, UiFactory.TextMain, TextAnchor.UpperLeft, FontStyle.Bold);
            Corner(dayText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(600f, 34f));
            dayText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            optionsText = UiFactory.Label("Optionen", root, "", 17, UiFactory.TextDim, TextAnchor.UpperLeft);
            Corner(optionsText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -56f), new Vector2(700f, 28f));
            optionsText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            keysText = UiFactory.Label("Tasten", root, GardenTexts.KeysLine, 15, UiFactory.TextDim, TextAnchor.LowerLeft);
            Corner(keysText.rectTransform, Vector2.zero, new Vector2(24f, 8f), new Vector2(1100f, 24f));

            messageText = UiFactory.Label("Nachricht", root, "", 24, UiFactory.TextMain, TextAnchor.MiddleCenter);
            RectTransform mr = messageText.rectTransform;
            mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 1f);
            mr.pivot = new Vector2(0.5f, 1f);
            mr.sizeDelta = new Vector2(1000f, 80f);
            mr.anchoredPosition = new Vector2(0f, -24f);
            messageText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.7f);
            messageGroup = messageText.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;

            fade = UiFactory.Panel("Abenddämmerung", root, new Color(0.05f, 0.04f, 0.08f, 0f));
            UiFactory.FullStretch(fade.rectTransform);
            fade.raycastTarget = false;
        }

        private static void Corner(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        public void SetTool(GardenTool tool)
        {
            int t = (int)tool;
            if (t == shownTool) return;
            shownTool = t;
            for (int i = 0; i < Slots; i++)
            {
                slotBg[i].color = i == t ? UiFactory.AccentSoft : UiFactory.WindowBg;
                slotName[i].color = i == t ? UiFactory.Accent : UiFactory.TextMain;
            }
        }

        public void SetSeed(string line)
        {
            if (line == shownSeed) return;
            shownSeed = line;
            slotSub[(int)GardenTool.Seeds].text = line;
        }

        public void SetCan(int water, int capacity)
        {
            string line = GardenTexts.Can(water, capacity);
            if (line == shownCan) return;
            shownCan = line;
            slotSub[(int)GardenTool.Can].text = line;
            float f = capacity > 0 ? Mathf.Clamp01(water / (float)capacity) : 0f;
            canFill.rectTransform.anchorMax = new Vector2(f, 1f);
        }

        public void SetDay(string line)
        {
            if (line == shownDay) return;
            shownDay = line;
            dayText.text = line;
        }

        public void SetOptions(string line)
        {
            if (line == shownOptions) return;
            shownOptions = line;
            optionsText.text = line;
        }

        public void SetHint(string line)
        {
            if (line == shownHint) return;
            shownHint = line;
            hintText.text = line ?? "";
        }

        public void Message(string line)
        {
            messageText.text = line;
            messageUntil = Time.unscaledTime + MessageSeconds;
        }

        /// <summary>Fades the screen softly to dusk (true) or back (false).</summary>
        public void Fade(bool toDark, float seconds)
        {
            fadeTarget = toDark ? 0.92f : 0f;
            fadeSpeed = 1f / Mathf.Max(0.05f, seconds);
        }

        private void Update()
        {
            if (messageGroup != null)
            {
                float left = messageUntil - Time.unscaledTime;
                messageGroup.alpha = Mathf.Clamp01(left / 0.6f);
            }

            if (fade != null)
            {
                Color c = fade.color;
                c.a = Mathf.MoveTowards(c.a, fadeTarget, fadeSpeed * Time.unscaledDeltaTime);
                fade.color = c;
            }
        }
    }
}
