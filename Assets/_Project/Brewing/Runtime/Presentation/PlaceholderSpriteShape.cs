using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Assigns a code-generated placeholder sprite at runtime. Needed because a
    /// saved scene cannot reference sprites that only exist in memory. Replace
    /// with real art by assigning a sprite and removing this component.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlaceholderSpriteShape : MonoBehaviour
    {
        public enum Shape { Square, Circle, Diamond, Triangle }

        [SerializeField] private Shape shape;

        public void Configure(Shape shape)
        {
            this.shape = shape;
        }

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite != null)
            {
                return; // real art wins
            }

            switch (shape)
            {
                case Shape.Circle: sr.sprite = PlaceholderSprites.Circle; break;
                case Shape.Diamond: sr.sprite = PlaceholderSprites.Diamond; break;
                case Shape.Triangle: sr.sprite = PlaceholderSprites.Triangle; break;
                default: sr.sprite = PlaceholderSprites.Square; break;
            }
        }
    }
}
