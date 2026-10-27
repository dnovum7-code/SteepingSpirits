using UnityEngine;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Steam above the kettle (density by boil look) and above the vessel while
    /// it holds hot tea. Large, slow, translucent puffs.
    /// </summary>
    public class SteamView : BrewView
    {
        [SerializeField] private MonoBehaviour particles;
        [SerializeField] private Transform kettleSpout;
        [SerializeField] private Transform vesselTop;
        [SerializeField] private Color steamColor = new Color(1f, 1f, 1f, 0.22f);
        [Tooltip("Vessel steam per second at 90 °C")]
        [SerializeField] private float vesselSteamRate = 2f;

        private float kettleDebt;
        private float vesselDebt;

        public void Configure(MonoBehaviour particles, Transform kettleSpout, Transform vesselTop)
        {
            this.particles = particles;
            this.kettleSpout = kettleSpout;
            this.vesselTop = vesselTop;
        }

        private void Update()
        {
            IBrewParticles p = AsParticles(particles);
            if (Session == null || Look == null || p == null)
            {
                return;
            }

            BoilLookSample s = BoilLook.Evaluate(Look, Sim.boilStages, Sim.water.roomTemperature,
                Sim.water.boilingPoint, Session.Water.Temperature);
            Emit(p, kettleSpout, s.steamRate, ref kettleDebt, 1f);

            if (Session.Extraction != null && vesselTop != null)
            {
                float warmth = Mathf.InverseLerp(40f, 90f, Session.Extraction.Temperature);
                Emit(p, vesselTop, vesselSteamRate * warmth, ref vesselDebt, 0.7f);
            }
        }

        private void Emit(IBrewParticles p, Transform at, float rate, ref float debt, float scale)
        {
            if (at == null)
            {
                return;
            }

            debt += rate * Time.deltaTime;
            while (debt >= 1f)
            {
                debt -= 1f;
                p.Emit(new ParticleSpawn
                {
                    position = (Vector2)at.position + new Vector2(Random.Range(-0.15f, 0.15f), 0f),
                    velocity = new Vector2(Random.Range(-0.1f, 0.15f), Random.Range(0.4f, 0.7f)),
                    size = 0.35f * scale,
                    color = steamColor,
                    lifetime = Random.Range(1.8f, 2.6f),
                    endSizeFactor = 2.8f
                });
            }
        }
    }
}
