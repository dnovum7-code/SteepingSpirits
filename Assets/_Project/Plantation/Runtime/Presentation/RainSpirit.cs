using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// On rainy days a small rain spirit drifts slowly over the beds and lets drops fall – the
    /// visible reason the beds are wet. Purely visual; the core already watered every tilled cell.
    /// </summary>
    public class RainSpirit : MonoBehaviour
    {
        private TeaGarden root;
        private Transform body;
        private SpriteRenderer[] parts;
        private Rect area;
        private float dropAt;
        private float alpha;

        public void Init(TeaGarden garden)
        {
            root = garden;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (GridPos p in garden.Garden.Beds)
            {
                min = Vector2.Min(min, new Vector2(p.x, p.y));
                max = Vector2.Max(max, new Vector2(p.x + 1, p.y + 1));
            }

            area = garden.Garden.Beds.Count > 0 ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : new Rect(0f, 0f, 4f, 4f);
            body = new GameObject("Körper").transform;
            body.SetParent(transform, false);
            parts = new[]
            {
                PlaceholderSprites.CreateSpriteObject("Wolke", PlaceholderSprites.Circle, new Color(0.88f, 0.92f, 1f), Vector2.zero, new Vector2(0.9f, 0.6f), 60, body),
                PlaceholderSprites.CreateSpriteObject("Wolke", PlaceholderSprites.Circle, new Color(0.92f, 0.95f, 1f), new Vector2(0.3f, 0.12f), new Vector2(0.6f, 0.5f), 61, body),
                PlaceholderSprites.CreateSpriteObject("Auge", PlaceholderSprites.Circle, new Color(0.25f, 0.3f, 0.45f), new Vector2(-0.14f, 0.02f), new Vector2(0.08f, 0.1f), 62, body),
                PlaceholderSprites.CreateSpriteObject("Auge", PlaceholderSprites.Circle, new Color(0.25f, 0.3f, 0.45f), new Vector2(0.12f, 0.02f), new Vector2(0.08f, 0.1f), 62, body)
            };
            SetAlpha(0f);
        }

        private void Update()
        {
            if (root == null || root.Garden == null) return;
            bool rainy = root.Garden.RainyToday;
            alpha = Mathf.MoveTowards(alpha, rainy ? 0.85f : 0f, Time.deltaTime * 0.8f);
            SetAlpha(alpha);
            if (alpha <= 0.01f) return;

            float t = Time.time;
            Vector2 center = area.center + new Vector2(0f, 0.8f);
            Vector2 pos = center + new Vector2(Mathf.Sin(t * 0.21f) * area.width * 0.45f, Mathf.Sin(t * 0.33f + 1f) * area.height * 0.4f);
            body.position = pos + Vector2.up * Mathf.Sin(t * 2.1f) * 0.08f;

            if (rainy && t >= dropAt)
            {
                dropAt = t + 0.16f;
                root.Feedback.RainDrop(pos + Vector2.down * 0.25f);
            }
        }

        private void SetAlpha(float a)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                Color c = parts[i].color;
                c.a = a;
                parts[i].color = c;
            }
        }
    }
}
