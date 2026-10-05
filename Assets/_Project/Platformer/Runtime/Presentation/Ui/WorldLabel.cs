using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A uGUI text box that follows a world position (speech bubbles, door
    /// signs). Fades with a CanvasGroup; text is only set when it changes.
    /// </summary>
    public sealed class WorldLabel
    {
        private readonly RectTransform box;
        private readonly Text text;
        private readonly CanvasGroup group;
        private readonly Image background;

        public WorldLabel(string name, int fontSize, Color textColor, Color backgroundColor, float width)
        {
            background = UiFactory.Panel(name, JumpNRunUi.Root, backgroundColor);
            background.raycastTarget = false;
            box = background.rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0f);
            box.sizeDelta = new Vector2(width, fontSize * 2.2f);
            group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0f;

            text = UiFactory.Label("Text", box, "", fontSize, textColor, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            UiFactory.FullStretch(text.rectTransform, 12f, 6f, 12f, 6f);
        }

        public void SetText(string value, int lines = 1)
        {
            if (text.text == value)
            {
                return;
            }

            text.text = value;
            box.sizeDelta = new Vector2(box.sizeDelta.x, text.fontSize * (1.5f * lines + 0.9f));
        }

        public void SetAlpha(float alpha)
        {
            if (!Mathf.Approximately(group.alpha, alpha))
            {
                group.alpha = alpha;
            }
        }

        /// <summary>Places the box above a world point; hides it when off screen.</summary>
        public void Follow(Vector3 world)
        {
            if (group.alpha <= 0.001f)
            {
                return;
            }

            if (JumpNRunUi.WorldToCanvas(world, out Vector2 pos))
            {
                box.anchoredPosition = pos;
            }
        }

        public void Destroy()
        {
            if (background != null)
            {
                Object.Destroy(background.gameObject);
            }
        }
    }
}
