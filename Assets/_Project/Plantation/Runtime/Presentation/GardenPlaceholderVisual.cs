using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Look of builder-made garden objects outside the garden scene (the entrance on the meadow).
    /// The sprite is made at runtime, never saved into scenes; in the editor a gizmo shows it.
    /// </summary>
    public class GardenPlaceholderVisual : MonoBehaviour
    {
        public Color color = new Color(0.55f, 0.78f, 0.45f);
        public Vector2 size = new Vector2(1.2f, 1.4f);
        public Color leafColor = new Color(0.35f, 0.62f, 0.32f);

        private void Awake()
        {
            // A little garden arch: two posts, a green top and a leaf.
            Sprite(PlaceholderSprites.Square, color * 0.8f, new Vector2(-size.x * 0.4f, 0f), new Vector2(0.18f, size.y), 0);
            Sprite(PlaceholderSprites.Square, color * 0.8f, new Vector2(size.x * 0.4f, 0f), new Vector2(0.18f, size.y), 0);
            Sprite(PlaceholderSprites.Circle, leafColor, new Vector2(0f, size.y * 0.5f), new Vector2(size.x, size.y * 0.35f), 1);
            Sprite(PlaceholderSprites.Triangle, color, new Vector2(0f, -size.y * 0.2f), new Vector2(0.35f, 0.45f), 1);
        }

        private void Sprite(Sprite sprite, Color c, Vector2 offset, Vector2 s, int order)
        {
            SpriteRenderer sr = PlaceholderSprites.CreateSpriteObject("Visual", sprite, c, Vector2.zero, s, order, transform);
            sr.transform.localPosition = offset;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;
            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
