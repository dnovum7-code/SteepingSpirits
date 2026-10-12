using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// Soft garden feedback: pooled sprite particles (soil crumbs, seeds, water drops, sparkles,
    /// harvest pieces hopping to the player) and sounds from the audio set or the procedural
    /// placeholders. No screen shake, no bright flashes; nothing allocates per frame.
    /// </summary>
    public class GardenFeedback : MonoBehaviour
    {
        private struct Particle
        {
            public SpriteRenderer sr;
            public Vector2 pos;
            public Vector2 vel;
            public float gravity;
            public float age;
            public float life;
            public float size;
            public Color color;
            public bool hop;
            public Vector2 hopFrom;
        }

        private GardenFeedbackParams tuning;
        private PlantationAudioSet audioSet;
        private Transform player;
        private AudioSource source;
        private Particle[] pool = new Particle[0];
        private int next;

        public void Configure(GardenFeedbackParams tuning, PlantationAudioSet audioSet, Transform player)
        {
            this.tuning = tuning ?? new GardenFeedbackParams();
            this.audioSet = audioSet;
            this.player = player;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            int budget = Mathf.Clamp(this.tuning.particleBudget, 8, 256);
            pool = new Particle[budget];
            for (int i = 0; i < budget; i++)
            {
                SpriteRenderer sr = PlaceholderSprites.CreateSpriteObject("Particle", PlaceholderSprites.Circle, Color.white, Vector2.zero, Vector2.one * 0.1f, 40, transform);
                sr.enabled = false;
                pool[i].sr = sr;
            }
        }

        public void Play(GardenSound sound, float volumeScale = 1f)
        {
            if (source == null) return;
            AudioClip clip = audioSet != null ? audioSet.ClipFor(sound) : null;
            if (clip == null) clip = GardenSynth.Clip(sound);
            if (clip == null) return;
            source.pitch = 1f + Random.Range(-tuning.pitchJitter, tuning.pitchJitter);
            source.PlayOneShot(clip, tuning.volume * volumeScale);
        }

        public void Crumbs(Vector2 at)
        {
            for (int i = 0; i < 7; i++)
            {
                Spawn(PlaceholderSprites.Square, at + Random.insideUnitCircle * 0.2f, new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(1.2f, 2.4f)),
                    7f, Random.Range(0.35f, 0.55f), Random.Range(0.06f, 0.11f), new Color(0.45f, 0.32f, 0.2f, 0.9f));
            }
        }

        public void Seeds(Vector2 at)
        {
            for (int i = 0; i < 4; i++)
            {
                Spawn(PlaceholderSprites.Circle, at + new Vector2(Random.Range(-0.2f, 0.2f), 0.45f), new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.2f, 0.6f)),
                    5f, 0.35f, 0.07f, new Color(0.85f, 0.75f, 0.5f, 0.95f));
            }
        }

        public void Drops(Vector2 at)
        {
            for (int i = 0; i < 8; i++)
            {
                Spawn(PlaceholderSprites.Circle, at + new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0.5f, 0.8f)), new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.6f, 0.2f)),
                    6f, Random.Range(0.3f, 0.45f), Random.Range(0.06f, 0.09f), new Color(0.55f, 0.75f, 0.95f, 0.8f));
            }

            for (int i = 0; i < 3; i++)
            {
                // A small splash ring on the soil.
                Spawn(PlaceholderSprites.Circle, at + Random.insideUnitCircle * 0.25f, Vector2.zero, 0f, 0.4f, 0.16f, new Color(0.6f, 0.78f, 0.95f, 0.35f));
            }
        }

        public void Sparkle(Vector2 at, Color color)
        {
            for (int i = 0; i < 5; i++)
            {
                color.a = 0.7f;
                Spawn(PlaceholderSprites.Diamond, at + Random.insideUnitCircle * 0.35f + Vector2.up * 0.3f, Vector2.up * Random.Range(0.2f, 0.5f),
                    0f, Random.Range(0.6f, 0.9f), 0.1f, color);
            }
        }

        /// <summary>Harvested pieces hop in a small arc from the cell to the player.</summary>
        public void HarvestHop(Vector2 from, Color color, int pieces)
        {
            for (int i = 0; i < Mathf.Clamp(pieces, 1, 5); i++)
            {
                int k = Spawn(PlaceholderSprites.Circle, from, Vector2.zero, 0f, tuning.hopSeconds + i * 0.06f, 0.2f, color);
                if (k < 0) continue;
                pool[k].hop = true;
                pool[k].hopFrom = from + Random.insideUnitCircle * 0.15f;
            }
        }

        private int Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float gravity, float life, float size, Color color)
        {
            if (pool.Length == 0) return -1;
            int k = next;
            next = (next + 1) % pool.Length;
            ref Particle p = ref pool[k];
            p.pos = pos;
            p.vel = vel;
            p.gravity = gravity;
            p.age = 0f;
            p.life = Mathf.Max(0.05f, life);
            p.size = size;
            p.color = color;
            p.hop = false;
            p.sr.sprite = sprite;
            p.sr.enabled = true;
            p.sr.color = color;
            p.sr.transform.position = pos;
            p.sr.transform.localScale = new Vector3(size, size, 1f);
            return k;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < pool.Length; i++)
            {
                ref Particle p = ref pool[i];
                if (p.sr == null || !p.sr.enabled) continue;
                p.age += dt;
                float t = p.age / p.life;
                if (t >= 1f)
                {
                    p.sr.enabled = false;
                    continue;
                }

                if (p.hop)
                {
                    Vector2 target = player != null ? (Vector2)player.position + Vector2.up * 0.2f : p.hopFrom + Vector2.up;
                    float e = t * t * (3f - 2f * t);
                    p.pos = Vector2.Lerp(p.hopFrom, target, e) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * 0.8f);
                    float s = p.size * (1f - 0.5f * t);
                    p.sr.transform.localScale = new Vector3(s, s, 1f);
                }
                else
                {
                    p.vel.y -= p.gravity * dt;
                    p.pos += p.vel * dt;
                }

                Color c = p.color;
                c.a *= 1f - t * t;
                p.sr.color = c;
                p.sr.transform.position = p.pos;
            }
        }
    }
}
