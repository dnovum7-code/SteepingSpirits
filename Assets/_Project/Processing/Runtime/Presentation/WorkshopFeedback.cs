using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// Soft workshop feedback: one-shot sounds, two quiet loops (wok sizzle following the heat, ember
    /// crackle while something dries) and pooled particles (leaf bits, steam, faint sparks).
    /// No shake, no flashes, no per-frame allocations.
    /// </summary>
    public class WorkshopFeedback : MonoBehaviour
    {
        private struct Particle
        {
            public SpriteRenderer sr;
            public Vector2 pos;
            public Vector2 vel;
            public float age;
            public float life;
            public float grow;
            public Color color;
        }

        private WorkshopFeedbackParams tuning;
        private ProcessingAudioSet audioSet;
        private AudioSource oneShots;
        private AudioSource sizzle;
        private AudioSource crackle;
        private Particle[] pool = new Particle[0];
        private int next;
        private float sizzleTarget;
        private float crackleTarget;

        public void Configure(WorkshopFeedbackParams t, ProcessingAudioSet set)
        {
            tuning = t ?? new WorkshopFeedbackParams();
            audioSet = set;
            oneShots = gameObject.AddComponent<AudioSource>();
            oneShots.playOnAwake = false;
            sizzle = Loop(WorkshopSound.Sizzle);
            crackle = Loop(WorkshopSound.Crackle);

            pool = new Particle[Mathf.Clamp(tuning.particleBudget, 8, 256)];
            for (int i = 0; i < pool.Length; i++)
            {
                SpriteRenderer sr = PlaceholderSprites.CreateSpriteObject("Partikel", PlaceholderSprites.Circle, Color.white, Vector2.zero, Vector2.one * 0.1f, 40, transform);
                sr.enabled = false;
                pool[i].sr = sr;
            }
        }

        private AudioSource Loop(WorkshopSound s)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
            src.clip = Clip(s);
            if (src.clip != null) src.Play();
            return src;
        }

        private AudioClip Clip(WorkshopSound s)
        {
            AudioClip c = audioSet != null ? audioSet.ClipFor(s) : null;
            return c != null ? c : ProcessingSynth.Clip(s);
        }

        public void Play(WorkshopSound s, float scale = 1f)
        {
            if (oneShots == null) return;
            AudioClip c = Clip(s);
            if (c == null) return;
            oneShots.pitch = 1f + Random.Range(-tuning.pitchJitter, tuning.pitchJitter);
            oneShots.PlayOneShot(c, tuning.volume * scale);
        }

        /// <summary>Wok heat 0..1 (0 = silent).</summary>
        public void SetSizzle(float heat) => sizzleTarget = Mathf.Clamp01(heat) * tuning.sizzleVolume;

        public void SetCrackle(bool drying) => crackleTarget = drying ? tuning.crackleVolume : 0f;

        public void LeafBits(Vector2 at, Color c, int n = 5)
        {
            for (int i = 0; i < n; i++) Spawn(PlaceholderSprites.Diamond, at + Random.insideUnitCircle * 0.25f, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.4f, 1.2f)), 0.6f, 0.08f, 0f, c);
        }

        public void Steam(Vector2 at)
        {
            Spawn(PlaceholderSprites.Circle, at + new Vector2(Random.Range(-0.3f, 0.3f), 0.2f), new Vector2(Random.Range(-0.1f, 0.1f), 0.6f), 1.4f, 0.18f, 0.25f, new Color(1f, 1f, 1f, 0.22f));
        }

        public void Spark(Vector2 at)
        {
            Spawn(PlaceholderSprites.Circle, at + new Vector2(Random.Range(-0.35f, 0.35f), 0f), new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.3f, 0.7f)), 0.9f, 0.05f, 0f, new Color(1f, 0.65f, 0.3f, 0.55f));
        }

        private void Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float life, float size, float grow, Color color)
        {
            if (pool.Length == 0) return;
            ref Particle p = ref pool[next];
            next = (next + 1) % pool.Length;
            p.pos = pos;
            p.vel = vel;
            p.age = 0f;
            p.life = life;
            p.grow = grow;
            p.color = color;
            p.sr.sprite = sprite;
            p.sr.enabled = true;
            p.sr.color = color;
            p.sr.transform.position = pos;
            p.sr.transform.localScale = new Vector3(size, size, 1f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (sizzle != null) sizzle.volume = Mathf.MoveTowards(sizzle.volume, sizzleTarget, dt * 0.8f);
            if (crackle != null) crackle.volume = Mathf.MoveTowards(crackle.volume, crackleTarget, dt * 0.3f);
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

                p.pos += p.vel * dt;
                p.vel *= 1f - dt * 0.8f;
                Color c = p.color;
                c.a *= 1f - t;
                p.sr.color = c;
                p.sr.transform.position = p.pos;
                if (p.grow > 0f) p.sr.transform.localScale += Vector3.one * (p.grow * dt);
            }
        }
    }
}
