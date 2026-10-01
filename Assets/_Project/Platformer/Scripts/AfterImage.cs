using UnityEngine;

namespace SteepingSpirits.Platformer
{
    /// <summary>Kurzes Nachbild beim Dash (wie Celeste) – blendet aus und löscht sich selbst.</summary>
    public class AfterImage : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Color start;
        private float life;
        private float age;

        public static void Spawn(SpriteRenderer source, Color color, float lifetime)
        {
            if (source == null || source.sprite == null)
            {
                return;
            }

            var go = new GameObject("Nachbild");
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            go.transform.localScale = source.transform.lossyScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.flipX = source.flipX;
            sr.color = color;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder - 1;

            var img = go.AddComponent<AfterImage>();
            img.sr = sr;
            img.start = color;
            img.life = Mathf.Max(0.01f, lifetime);
        }

        private void Update()
        {
            age += Time.deltaTime;
            Color c = start;
            c.a = start.a * (1f - age / life);
            sr.color = c;
            if (age >= life)
            {
                Destroy(gameObject);
            }
        }
    }
}
