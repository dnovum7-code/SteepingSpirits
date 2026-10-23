using UnityEngine;
using UnityEngine.Rendering;
using SteepingSpirits.Core;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The little workshop spirit: floats softly above the shelf once it has learned something and
    /// glides to a station and back when it lends a hand. Purely visual; the work is done in the core.
    /// </summary>
    public class SpiritView : MonoBehaviour
    {
        private SpriteRenderer[] parts;
        private Vector2 home;
        private Vector2 from;
        private Vector2 via;
        private Vector2 to;
        private float trip = -1f;
        private float alpha;
        private bool awake;

        public void Init(Vector2 homePosition)
        {
            home = homePosition;
            transform.position = home;
            gameObject.AddComponent<SortingGroup>().sortingOrder = 60;
            parts = new[]
            {
                PlaceholderSprites.CreateSpriteObject("Körper", PlaceholderSprites.Circle, new Color(0.92f, 0.95f, 1f), Vector2.zero, new Vector2(0.45f, 0.5f), 0, transform),
                PlaceholderSprites.CreateSpriteObject("Schweif", PlaceholderSprites.Triangle, new Color(0.88f, 0.92f, 1f), Vector2.zero, new Vector2(0.3f, 0.25f), 0, transform),
                PlaceholderSprites.CreateSpriteObject("Auge", PlaceholderSprites.Circle, new Color(0.3f, 0.32f, 0.45f), Vector2.zero, new Vector2(0.06f, 0.08f), 1, transform),
                PlaceholderSprites.CreateSpriteObject("Auge", PlaceholderSprites.Circle, new Color(0.3f, 0.32f, 0.45f), Vector2.zero, new Vector2(0.06f, 0.08f), 1, transform)
            };
            parts[1].transform.localPosition = new Vector3(0f, -0.28f, 0f);
            parts[1].transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            parts[2].transform.localPosition = new Vector3(-0.08f, 0.04f, 0f);
            parts[3].transform.localPosition = new Vector3(0.08f, 0.04f, 0f);
            Apply(0f);
        }

        public void SetAwake(bool on) => awake = on;

        /// <summary>Glide to a station, then to where the batch goes, then home.</summary>
        public void Visit(Vector2 station, Vector2 destination)
        {
            from = transform.position;
            via = station + Vector2.up * 0.6f;
            to = destination + Vector2.up * 0.6f;
            trip = 0f;
        }

        private void Update()
        {
            alpha = Mathf.MoveTowards(alpha, awake ? 0.85f : 0f, Time.deltaTime * 0.5f);
            Apply(alpha);
            if (alpha <= 0.01f) return;

            float bob = Mathf.Sin(Time.time * 1.7f) * 0.06f;
            if (trip >= 0f)
            {
                trip += Time.deltaTime / 3f;
                Vector2 p = trip < 0.4f ? Vector2.Lerp(from, via, Smooth(trip / 0.4f))
                    : trip < 0.7f ? Vector2.Lerp(via, to, Smooth((trip - 0.4f) / 0.3f))
                    : Vector2.Lerp(to, home, Smooth((trip - 0.7f) / 0.3f));
                transform.position = p + Vector2.up * bob;
                if (trip >= 1f) trip = -1f;
            }
            else
            {
                transform.position = home + new Vector2(Mathf.Sin(Time.time * 0.4f) * 0.3f, bob);
            }
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private void Apply(float a)
        {
            if (parts == null) return;
            foreach (SpriteRenderer sr in parts)
            {
                Color c = sr.color;
                c.a = a;
                sr.color = c;
            }
        }
    }
}
