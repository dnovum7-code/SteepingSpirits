using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SteepingSpirits.Core.UI
{
    /// <summary>
    /// Kleiner Baukasten für Laufzeit-uGUI (Canvas / Panel / Text / Button /
    /// Eingabefeld / Scroll-Liste). Erspart das Wiring im Inspector – Dialog,
    /// Questlog und Tag/Nacht-Overlay bauen ihre Oberfläche damit im
    /// Code auf und funktionieren so ohne vorbereitete Prefabs.
    ///
    /// Design: warmes, dunkles Holz-/Pergament-Layout (cozy 2D).
    /// Bei Bedarf lässt sich jedes hier gebaute Objekt später als Prefab
    /// speichern.
    /// </summary>
    public static class UiFactory
    {
        // Farbpalette (zentral, damit alle Fenster gleich aussehen).
        public static readonly Color Backdrop = new Color(0.05f, 0.03f, 0.02f, 0.82f);
        public static readonly Color WindowBg = new Color(0.24f, 0.16f, 0.10f, 0.98f);
        public static readonly Color HeaderBg = new Color(0.33f, 0.22f, 0.13f, 1f);
        public static readonly Color FieldBg = new Color(1f, 0.95f, 0.85f, 0.08f);
        public static readonly Color RowBg = new Color(1f, 0.95f, 0.85f, 0.05f);
        public static readonly Color Accent = new Color(0.98f, 0.78f, 0.35f, 1f);
        public static readonly Color AccentSoft = new Color(0.48f, 0.33f, 0.18f, 1f);
        public static readonly Color TextMain = new Color(0.98f, 0.95f, 0.88f, 1f);
        public static readonly Color TextDim = new Color(0.80f, 0.72f, 0.60f, 1f);

        private static Font cachedFont;

        public static Font DefaultFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            // Unity 2022+: LegacyRuntime.ttf, ältere Versionen: Arial.ttf.
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (cachedFont == null)
            {
                cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            }

            return cachedFont;
        }

        /// <summary>Stellt sicher, dass ein EventSystem mit passendem Input-Modul da ist.</summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            EnsureEventSystem();

            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        /// <summary>Streckt das RectTransform auf die Elterngrösse (mit Rändern l/t/r/b).</summary>
        public static void FullStretch(RectTransform rt, float l = 0f, float t = 0f, float r = 0f, float b = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        public static Button MakeButton(string name, Transform parent, string label, int size, Color bg, Color fg)
        {
            var img = Panel(name, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            Text t = Label("Text", img.transform, label, size, fg, TextAnchor.MiddleCenter);
            FullStretch(t.rectTransform, 6f, 2f, 6f, 2f);
            return btn;
        }

        public static InputField MakeInput(string name, Transform parent, string placeholder, int size)
        {
            var img = Panel(name, parent, FieldBg);
            var input = img.gameObject.AddComponent<InputField>();

            Text textT = Label("Text", img.transform, "", size, TextMain, TextAnchor.MiddleLeft);
            textT.supportRichText = false;
            FullStretch(textT.rectTransform, 12f, 4f, 12f, 4f);

            Text ph = Label("Placeholder", img.transform, placeholder, size, TextDim, TextAnchor.MiddleLeft, FontStyle.Italic);
            ph.supportRichText = false;
            FullStretch(ph.rectTransform, 12f, 4f, 12f, 4f);

            input.textComponent = textT;
            input.placeholder = ph;
            input.targetGraphic = img;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        /// <summary>
        /// Baut eine vertikale Scroll-Liste. Zurückgegeben wird der Content, in
        /// den man Zeilen hängt (VerticalLayoutGroup + ContentSizeFitter aktiv).
        /// </summary>
        public static RectTransform MakeScroll(string name, Transform parent, out ScrollRect scroll)
        {
            var rootImg = Panel(name, parent, new Color(0f, 0f, 0f, 0.12f));
            scroll = rootImg.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 26f;

            var viewport = Panel("Viewport", rootImg.transform, new Color(0f, 0f, 0f, 0f));
            FullStretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            var content = NewRect("Content", viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(10, 12, 10, 10);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            return content;
        }

        public static VerticalLayoutGroup VerticalGroup(RectTransform rt, float spacing, RectOffset padding,
            bool controlHeight = false)
        {
            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = controlHeight;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = spacing;
            vlg.padding = padding;
            return vlg;
        }

        public static HorizontalLayoutGroup HorizontalGroup(RectTransform rt, float spacing, RectOffset padding)
        {
            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = spacing;
            hlg.padding = padding;
            return hlg;
        }

        public static LayoutElement Sizing(GameObject go, float preferredHeight = -1f, float preferredWidth = -1f,
            float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = go.AddComponent<LayoutElement>();
            }

            if (preferredHeight >= 0f)
            {
                le.preferredHeight = preferredHeight;
                le.minHeight = preferredHeight;
            }
            if (preferredWidth >= 0f)
            {
                le.preferredWidth = preferredWidth;
            }
            if (flexibleWidth >= 0f)
            {
                le.flexibleWidth = flexibleWidth;
            }
            if (flexibleHeight >= 0f)
            {
                le.flexibleHeight = flexibleHeight;
            }

            return le;
        }
    }
}
