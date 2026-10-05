using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Minimal HUD (uGUI): the bag contents fade in at the top left after
    /// collecting and fade out again after a few seconds. Rows are created
    /// once per ingredient and only updated when the bag changes.
    /// </summary>
    public class JumpNRunHud : MonoBehaviour
    {
        [SerializeField] private float visibleSeconds = 3f;

        private float showTimer;
        private JumpNRunSession session;
        private CanvasGroup group;
        private RectTransform list;
        private readonly Dictionary<string, Text> counts = new Dictionary<string, Text>();

        private void Start()
        {
            session = JumpNRunSession.Current;
            if (session == null)
            {
                return;
            }

            session.Bag.Added += OnAdded;
            Image panel = UiFactory.Panel("Bag", JumpNRunUi.Root, new Color(0.2f, 0.13f, 0.08f, 0.8f));
            RectTransform rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -24f);
            panel.raycastTarget = false;
            list = rt;
            UiFactory.VerticalGroup(rt, 4f, new RectOffset(14, 14, 10, 10), true);
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.Bag.Added -= OnAdded;
            }
        }

        private void OnAdded(string id, int amount)
        {
            showTimer = visibleSeconds;
            if (!counts.TryGetValue(id, out Text count))
            {
                RectTransform row = UiFactory.NewRect("Row", list);
                UiFactory.HorizontalGroup(row, 10f, new RectOffset(0, 0, 0, 0));
                UiFactory.Sizing(row.gameObject, 30f);
                Image icon = UiFactory.Panel("Icon", row, Ingredient.ColorOf(id));
                icon.sprite = IngredientIds.IsRare(id) ? PlaceholderSprites.Diamond : PlaceholderSprites.Circle;
                icon.raycastTarget = false;
                UiFactory.Sizing(icon.gameObject, 22f, 22f);
                Text name = UiFactory.Label("Name", row, JumpNRunTexts.IngredientName(id), 22, UiFactory.TextMain, TextAnchor.MiddleLeft);
                name.raycastTarget = false;
                UiFactory.Sizing(name.gameObject, 30f, 200f);
                count = UiFactory.Label("Count", row, "", 22, UiFactory.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
                count.raycastTarget = false;
                UiFactory.Sizing(count.gameObject, 30f, 60f);
                counts[id] = count;
            }

            count.text = "×" + session.Bag.Count(id);
        }

        private void Update()
        {
            if (group == null)
            {
                return;
            }

            showTimer -= Time.unscaledDeltaTime;
            float target = session != null && !session.Finished && showTimer > 0f ? Mathf.Clamp01(showTimer / 0.6f) : 0f;
            if (!Mathf.Approximately(group.alpha, target))
            {
                group.alpha = target;
            }
        }
    }
}
