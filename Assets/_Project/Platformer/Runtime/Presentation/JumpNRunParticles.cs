using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Pooled sprite particles (placeholder backend for <see cref="IJumpNRunParticles"/>).
    /// Soft fade in/out, no allocations after warm-up, no bright flashes.
    /// </summary>
    public class JumpNRunParticles : MonoBehaviour, IJumpNRunParticles
    {
        [SerializeField] private int capacity = 120;
        [SerializeField] private float drag = 2.5f;
        [SerializeField] private int sortingOrder = 12;

        private struct Particle
        {
            public bool alive;
            public JumpNRunParticle data;
            public float age;
            public float angle;
        }

        private Particle[] particles;
        private SpriteRenderer[] renderers;
        private int next;

        public static IJumpNRunParticles Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            particles = new Particle[capacity];
            renderers = new SpriteRenderer[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;
                sr.enabled = false;
                renderers[i] = sr;
            }
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        public void Emit(in JumpNRunParticle particle)
        {
            if (particles == null)
            {
                return;
            }

            int i = next;
            next = (next + 1) % capacity;
            particles[i] = new Particle { alive = true, data = particle, age = 0f, angle = Random.Range(0f, 360f) };
            renderers[i].sprite = particle.shape == 1 ? PlaceholderSprites.Diamond : PlaceholderSprites.Circle;
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
            float dt = Time.deltaTime;
            float damping = Mathf.Exp(-drag * dt);
            for (int i = 0; i < capacity; i++)
            {
                ref Particle p = ref particles[i];
                if (!p.alive)
                {
                    continue;
                }

                p.age += dt;
                float life = Mathf.Max(0.01f, p.data.lifetime);
                if (p.age >= life)
                {
                    p.alive = false;
                    renderers[i].enabled = false;
                    continue;
                }

                p.data.velocity = (p.data.velocity + Vector2.down * p.data.gravity * dt) * damping;
                p.data.position += p.data.velocity * dt;
                p.angle += p.data.spin * dt;

                float t = p.age / life;
                float fade = Mathf.Min(Mathf.Clamp01(t / 0.2f), Mathf.Clamp01((1f - t) / 0.4f));
                Transform tr = renderers[i].transform;
                tr.position = new Vector3(p.data.position.x, p.data.position.y, 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, p.angle);
                float size = p.data.size * (p.data.shape == 1 ? 1f : Mathf.Lerp(1f, 1.6f, t));
                tr.localScale = new Vector3(size, p.data.shape == 1 ? size * 0.55f : size, 1f);
                Color c = p.data.color;
                c.a *= fade;
                renderers[i].color = c;
            }
        }

        /// <summary>Helper for bursts: count particles spread around a direction.</summary>
        public static void Burst(Vector2 position, int count, Vector2 direction, float spread, float speed,
            Color color, float size, float lifetime, float gravity, int shape = 0, float spin = 0f)
        {
            IJumpNRunParticles target = Instance;
            if (target == null)
            {
                return;
            }

            float baseAngle = Mathf.Atan2(direction.y, direction.x);
            for (int i = 0; i < count; i++)
            {
                float a = baseAngle + Random.Range(-spread, spread) * Mathf.Deg2Rad;
                float s = speed * Random.Range(0.6f, 1f);
                target.Emit(new JumpNRunParticle
                {
                    position = position + Random.insideUnitCircle * 0.08f,
                    velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s,
                    size = size * Random.Range(0.8f, 1.2f),
                    color = color,
                    lifetime = lifetime * Random.Range(0.8f, 1.2f),
                    gravity = gravity,
                    shape = shape,
                    spin = spin * Random.Range(-1f, 1f)
                });
            }
        }
    }
}
