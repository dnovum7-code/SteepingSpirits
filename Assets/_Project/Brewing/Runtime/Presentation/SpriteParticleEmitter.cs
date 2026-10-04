using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Minimal pooled sprite particles – pipeline-independent placeholder backend
    /// for <see cref="IBrewParticles"/>. Particles fade in and out softly; no
    /// allocations after warm-up.
    /// </summary>
    public class SpriteParticleEmitter : MonoBehaviour, IBrewParticles
    {
        [SerializeField] private int capacity = 160;
        [SerializeField] private Vector2 acceleration = Vector2.zero;
        [Tooltip("Velocity damping per second (0 = none)")]
        [SerializeField] private float drag;
        [SerializeField] private int sortingOrder = 5;
        [Tooltip("Optional sprite – default is a soft placeholder circle")]
        [SerializeField] private Sprite sprite;
        [Tooltip("Share of the lifetime used to fade in and out")]
        [Range(0f, 0.5f)] [SerializeField] private float fadePortion = 0.25f;

        private struct Particle
        {
            public bool alive;
            public Vector2 position;
            public Vector2 velocity;
            public float size;
            public float endSizeFactor;
            public Color color;
            public float age;
            public float lifetime;
        }

        private Particle[] particles;
        private SpriteRenderer[] renderers;
        private int next;

        /// <summary>Set behaviour from code (sandbox builder).</summary>
        public void Configure(int capacity, Vector2 acceleration, float drag, int sortingOrder)
        {
            this.capacity = Mathf.Max(1, capacity);
            this.acceleration = acceleration;
            this.drag = Mathf.Max(0f, drag);
            this.sortingOrder = sortingOrder;
        }

        private void Awake()
        {
            EnsurePool();
        }

        private void EnsurePool()
        {
            if (particles != null)
            {
                return;
            }

            particles = new Particle[capacity];
            renderers = new SpriteRenderer[capacity];
            Sprite s = sprite != null ? sprite : PlaceholderSprites.Circle;
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = s;
                sr.sortingOrder = sortingOrder;
                sr.enabled = false;
                renderers[i] = sr;
            }
        }

        public void Emit(in ParticleSpawn spawn)
        {
            EnsurePool();
            int i = next;
            next = (next + 1) % capacity; // oldest gets recycled when full

            particles[i] = new Particle
            {
                alive = true,
                position = spawn.position,
                velocity = spawn.velocity,
                size = spawn.size,
                endSizeFactor = spawn.endSizeFactor <= 0f ? 1f : spawn.endSizeFactor,
                color = spawn.color,
                age = 0f,
                lifetime = Mathf.Max(0.01f, spawn.lifetime)
            };
            renderers[i].enabled = true;
        }

        public void Clear()
        {
            if (particles == null)
            {
                return;
            }

            for (int i = 0; i < capacity; i++)
            {
                particles[i].alive = false;
                renderers[i].enabled = false;
            }
        }

        private void Update()
        {
            if (particles == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float damping = drag > 0f ? Mathf.Exp(-drag * dt) : 1f;

            for (int i = 0; i < capacity; i++)
            {
                ref Particle p = ref particles[i];
                if (!p.alive)
                {
                    continue;
                }

                p.age += dt;
                if (p.age >= p.lifetime)
                {
                    p.alive = false;
                    renderers[i].enabled = false;
                    continue;
                }

                p.velocity = (p.velocity + acceleration * dt) * damping;
                p.position += p.velocity * dt;

                float t = p.age / p.lifetime;
                float fade = fadePortion > 0f
                    ? Mathf.Min(Mathf.Clamp01(t / fadePortion), Mathf.Clamp01((1f - t) / fadePortion))
                    : 1f;
                float size = p.size * Mathf.Lerp(1f, p.endSizeFactor, t);

                Transform tr = renderers[i].transform;
                tr.position = new Vector3(p.position.x, p.position.y, 0f);
                tr.localScale = new Vector3(size, size, 1f);
                Color c = p.color;
                c.a *= fade;
                renderers[i].color = c;
            }
        }
    }
}
