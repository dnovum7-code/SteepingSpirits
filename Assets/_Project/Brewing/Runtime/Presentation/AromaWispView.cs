using UnityEngine;
using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>Coloured aroma wisps above the vessel; emission ∝ dA/dt (strongest early in the steep).</summary>
    public class AromaWispView : BrewView
    {
        [SerializeField] private MonoBehaviour particles;
        [SerializeField] private Transform vesselTop;

        private float debt;

        public void Configure(MonoBehaviour particles, Transform vesselTop)
        {
            this.particles = particles;
            this.vesselTop = vesselTop;
        }

        private void Update()
        {
            IBrewParticles p = AsParticles(particles);
            BrewSession s = Session;
            if (s == null || Look == null || p == null || vesselTop == null
                || s.Phase != BrewPhase.Steep || s.Extraction == null)
            {
                return;
            }

            debt += Mathf.Max(0f, s.Extraction.AromaRate) * Look.aromaWispsPerRate * Time.deltaTime;
            Color tint = controller.CurrentTea != null ? controller.CurrentTea.liquorColor.Evaluate(1f) : Color.white;
            tint = Color.Lerp(tint, Color.white, 0.5f);
            tint.a = 0.35f;

            while (debt >= 1f)
            {
                debt -= 1f;
                float side = Random.Range(-1f, 1f);
                p.Emit(new ParticleSpawn
                {
                    position = (Vector2)vesselTop.position + new Vector2(side * 0.3f, 0.05f),
                    velocity = new Vector2(side * 0.12f + Mathf.Sin(Time.time * 1.7f) * 0.08f, Random.Range(0.25f, 0.45f)),
                    size = 0.12f,
                    color = tint,
                    lifetime = Random.Range(2f, 3f),
                    endSizeFactor = 3.5f
                });
            }
        }
    }
}
