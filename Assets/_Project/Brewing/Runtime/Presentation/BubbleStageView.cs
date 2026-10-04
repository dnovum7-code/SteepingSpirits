using UnityEngine;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// Bubbles inside the (glass) kettle. Size and rate follow the blended boil
    /// look; shrimp eyes stay near the bottom, strings of pearls cling to the
    /// walls, raging waves fill the whole kettle.
    /// </summary>
    public class BubbleStageView : BrewView
    {
        [SerializeField] private MonoBehaviour particles;
        [Tooltip("Water area inside the kettle (world space, centre + size)")]
        [SerializeField] private Rect waterArea = new Rect(-5f, -1.6f, 2f, 1.3f);
        [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.75f);

        private float emitDebt;

        public void Configure(MonoBehaviour particles, Rect waterArea)
        {
            this.particles = particles;
            this.waterArea = waterArea;
        }

        private void Update()
        {
            IBrewParticles p = AsParticles(particles);
            if (Session == null || Look == null || p == null)
            {
                return;
            }

            float temp = Session.Water.Temperature;
            BoilLookSample s = BoilLook.Evaluate(Look, Sim.boilStages, Sim.water.roomTemperature, Sim.water.boilingPoint, temp);

            emitDebt += s.bubbleRate * Time.deltaTime;
            while (emitDebt >= 1f)
            {
                emitDebt -= 1f;
                EmitBubble(p, s, temp);
            }
        }

        private void EmitBubble(IBrewParticles p, BoilLookSample s, float temp)
        {
            // Hotter water: bubbles rise higher and faster.
            float heat = Mathf.InverseLerp(Sim.boilStages.shrimpEyes, Sim.water.boilingPoint, temp);
            float x;
            if (temp >= Sim.boilStages.stringOfPearls && temp < Sim.boilStages.ragingWaves && Random.value < 0.7f)
            {
                // String of pearls: chains along the walls.
                x = Random.value < 0.5f ? waterArea.xMin + 0.12f : waterArea.xMax - 0.12f;
            }
            else
            {
                x = Random.Range(waterArea.xMin + 0.1f, waterArea.xMax - 0.1f);
            }

            float riseHeight = Mathf.Lerp(0.15f, waterArea.height, heat);
            float speed = Mathf.Lerp(0.25f, 1.6f, heat);

            p.Emit(new ParticleSpawn
            {
                position = new Vector2(x, waterArea.yMin + 0.05f),
                velocity = new Vector2(Random.Range(-0.05f, 0.05f) * (1f + heat), speed),
                size = s.bubbleSize * Random.Range(0.7f, 1.3f),
                color = bubbleColor,
                lifetime = riseHeight / speed,
                endSizeFactor = 1.15f
            });
        }
    }
}
