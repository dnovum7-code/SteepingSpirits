using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Shared uGUI helpers for the Jump'n'Run player screens, built on the
    /// project's UiFactory (same warm look as dialogue and quest log). One
    /// screen-space canvas per level; menus get explicit up/down navigation so
    /// keyboard, mouse and gamepad all work through the EventSystem.
    /// </summary>
    public static class JumpNRunUi
    {
        public const int SortOrder = 40;
        public const int OverlaySortOrder = 60;

        private static Canvas canvas;
        private static Canvas overlay;

        /// <summary>The level's canvas (created on first use, lives as long as the scene).</summary>
        public static RectTransform Root
        {
            get
            {
                if (canvas == null)
                {
                    canvas = UiFactory.CreateCanvas("JumpNRunUI", SortOrder);
                }

                return (RectTransform)canvas.transform;
            }
        }

        /// <summary>Topmost canvas (catch fade): above menus and HUD.</summary>
        public static RectTransform Overlay
        {
            get
            {
                if (overlay == null)
                {
                    overlay = UiFactory.CreateCanvas("JumpNRunOverlay", OverlaySortOrder);
                }

                return (RectTransform)overlay.transform;
            }
        }

        /// <summary>Converts a world position to a position on the canvas.</summary>
        public static bool WorldToCanvas(Vector3 world, out Vector2 canvasPos)
        {
            canvasPos = Vector2.zero;
            Camera cam = Camera.main;
            if (cam == null)
            {
                return false;
            }

            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z < 0f)
            {
                return false;
            }

            RectTransform root = Root;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out canvasPos);
        }

        /// <summary>Full-screen dim layer behind a window.</summary>
        public static Image Dim(Transform parent, float alpha)
        {
            Image img = UiFactory.Panel("Dim", parent, new Color(0.05f, 0.06f, 0.1f, alpha));
            UiFactory.FullStretch(img.rectTransform);
            return img;
        }

        /// <summary>A centred window with header text; returns the content area (vertical layout).</summary>
        public static RectTransform Window(Transform parent, string title, Vector2 size, out Text titleText)
        {
            Image frame = UiFactory.Panel("Window", parent, UiFactory.AccentSoft);
            RectTransform rt = frame.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            Image inner = UiFactory.Panel("Inner", rt, UiFactory.WindowBg);
            UiFactory.FullStretch(inner.rectTransform, 3f, 3f, 3f, 3f);

            titleText = UiFactory.Label("Title", inner.rectTransform, title, 34, UiFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform tr = titleText.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(0f, 64f);
            tr.anchoredPosition = new Vector2(0f, -10f);

            RectTransform content = UiFactory.NewRect("Content", inner.rectTransform);
            UiFactory.FullStretch(content, 28f, 80f, 28f, 22f);
            UiFactory.VerticalGroup(content, 10f, new RectOffset(0, 0, 0, 0), true);
            return content;
        }

        public static Text Line(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft,
            float height = 30f)
        {
            Text t = UiFactory.Label("Line", parent, text, size, color, anchor);
            UiFactory.Sizing(t.gameObject, height);
            return t;
        }

        public static Button Button(Transform parent, string label, float height = 52f)
        {
            Button b = UiFactory.MakeButton("Button", parent, label, 26, UiFactory.HeaderBg, UiFactory.TextMain);
            var colors = b.colors;
            colors.selectedColor = new Color(1f, 0.9f, 0.7f, 1f);
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
            b.colors = colors;
            UiFactory.Sizing(b.gameObject, height);
            return b;
        }

        /// <summary>Explicit navigation through a list (vertical or horizontal), wrapping around.</summary>
        public static void Chain(IList<Selectable> items, bool vertical)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Selectable prev = items[(i + items.Count - 1) % items.Count];
                Selectable next = items[(i + 1) % items.Count];
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (vertical)
                {
                    nav.selectOnUp = prev;
                    nav.selectOnDown = next;
                }
                else
                {
                    nav.selectOnLeft = prev;
                    nav.selectOnRight = next;
                }

                items[i].navigation = nav;
            }
        }

        public static void Focus(Selectable s)
        {
            if (s == null)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(s.gameObject);
            }
        }

        public static bool IsFocused(Selectable s)
        {
            return s != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == s.gameObject;
        }

        /// <summary>A label that sits at a fixed screen corner.</summary>
        public static Text Corner(string name, string text, int size, Color color, Vector2 anchor, Vector2 offset, Vector2 boxSize,
            TextAnchor align)
        {
            Text t = UiFactory.Label(name, Root, text, size, color, align);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = boxSize;
            t.raycastTarget = false;
            return t;
        }
    }
}
