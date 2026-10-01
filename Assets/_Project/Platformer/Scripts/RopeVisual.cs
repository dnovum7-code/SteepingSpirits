using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// Zeichnet ein Seil als gestrecktes Sprite zwischen zwei Punkten – ohne
    /// LineRenderer/Material, funktioniert damit in jeder Render-Pipeline.
    /// </summary>
    public class RopeVisual : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float thickness = 0.08f;

        public static RopeVisual Create(string name, Color color, float thickness, int sortingOrder)
        {
            SpriteRenderer sr = PlaceholderSprites.CreateSpriteObject(name, PlaceholderSprites.Square, color,
                Vector2.zero, Vector2.one, sortingOrder);
            RopeVisual rope = sr.gameObject.AddComponent<RopeVisual>();
            rope.sr = sr;
            rope.thickness = thickness;
            sr.enabled = false;
            return rope;
        }

        public void Show(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            float length = d.magnitude;
            if (length < 0.001f)
            {
                Hide();
                return;
            }

            sr.enabled = true;
            transform.position = (from + to) * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            transform.localScale = new Vector3(length, thickness, 1f);
        }

        public void Hide()
        {
            if (sr != null)
            {
                sr.enabled = false;
            }
        }
    }
}
