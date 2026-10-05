using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Code-generated placeholder look for builder-made objects. The sprite is
    /// created at runtime (never saved into scenes); in the editor the object
    /// is shown as a gizmo. Replace by a real SpriteRenderer when art exists.
    /// </summary>
    public class PlaceholderVisual : MonoBehaviour
    {
        public enum Shape
        {
            Square,
            Circle,
            Diamond,
            Triangle
        }

        public Shape shape = Shape.Square;
        public Color color = Color.white;
        public Vector2 size = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public int sortingOrder;

        public SpriteRenderer Renderer { get; private set; }

        private void Awake()
        {
            Renderer = PlaceholderSprites.CreateSpriteObject("Visual", SpriteFor(shape), color,
                Vector2.zero, size, sortingOrder, transform);
            Renderer.transform.localPosition = offset;
        }

        public static Sprite SpriteFor(Shape s)
        {
            switch (s)
            {
                case Shape.Circle: return PlaceholderSprites.Circle;
                case Shape.Diamond: return PlaceholderSprites.Diamond;
                case Shape.Triangle: return PlaceholderSprites.Triangle;
                default: return PlaceholderSprites.Square;
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Gizmos.color = color;
            Vector3 c = transform.position + (Vector3)offset;
            if (shape == Shape.Square)
            {
                Gizmos.DrawCube(c, size);
            }
            else
            {
                Gizmos.DrawSphere(c, Mathf.Min(size.x, size.y) * 0.5f);
            }
        }
    }
}
