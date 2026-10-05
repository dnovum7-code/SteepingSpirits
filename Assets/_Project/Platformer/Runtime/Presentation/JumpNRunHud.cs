using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Minimal HUD: the bag contents fade in at the top left after collecting
    /// and fade out again after a few seconds. Nothing else on screen.
    /// </summary>
    public class JumpNRunHud : MonoBehaviour
    {
        [SerializeField] private float visibleSeconds = 3f;

        private float showTimer;
        private JumpNRunSession session;
        private GUIStyle style;
        private readonly List<string> ids = new List<string>();

        private void Start()
        {
            session = JumpNRunSession.Current;
            if (session != null)
            {
                session.Bag.Added += OnAdded;
            }
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
        }

        private void Update()
        {
            showTimer -= Time.unscaledDeltaTime;
        }

        private void OnGUI()
        {
            if (session == null || session.Finished || showTimer <= 0f)
            {
                return;
            }

            float alpha = Mathf.Clamp01(showTimer / 0.6f);
            if (style == null)
            {
                style = GuiDraw.Rich(14, Color.white);
            }

            ids.Clear();
            foreach (string id in session.Bag.Ids) ids.Add(id);

            var r = new Rect(16f, 16f, 220f, 14f + ids.Count * 24f);
            GuiDraw.Panel(r, 0.75f * alpha);
            float y = r.y + 8f;
            foreach (string id in ids)
            {
                Color c = Ingredient.ColorOf(id);
                GuiDraw.Sprite(new Rect(r.x + 10f, y + 3f, 14f, 14f),
                    IngredientIds.IsRare(id) ? PlaceholderSprites.Diamond : PlaceholderSprites.Circle, new Color(c.r, c.g, c.b, alpha));
                style.normal.textColor = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(r.x + 32f, y, 180f, 22f), $"{JumpNRunTexts.IngredientName(id)}  ×{session.Bag.Count(id)}", style);
                y += 24f;
            }
        }
    }
}
