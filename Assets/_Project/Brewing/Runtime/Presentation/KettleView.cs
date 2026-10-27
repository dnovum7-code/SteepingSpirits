using UnityEngine;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>Fire under the kettle: glows softly while lit, fades out when off.</summary>
    public class KettleView : BrewView
    {
        [SerializeField] private SpriteRenderer fire;
        [SerializeField] private Color fireColor = new Color(1f, 0.55f, 0.2f, 0.9f);
        [Tooltip("Seconds for the fire to fade in/out")]
        [SerializeField] private float fadeSeconds = 0.6f;
        [SerializeField] private float flickerSpeed = 3f;

        private float intensity;
        private Vector3 baseScale = Vector3.one;

        public void Configure(SpriteRenderer fire)
        {
            this.fire = fire;
        }

        private void Start()
        {
            if (fire != null) baseScale = fire.transform.localScale;
        }

        private void Update()
        {
            if (Session == null || fire == null)
            {
                return;
            }

            float target = Session.Water.HeatOn ? 1f : 0f;
            intensity = Mathf.MoveTowards(intensity, target, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));

            // Soft flicker via Perlin noise – no flashes.
            float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
            Color c = fireColor;
            c.a *= intensity * flicker;
            fire.color = c;
            fire.transform.localScale = new Vector3(baseScale.x, baseScale.y * (0.6f + 0.4f * intensity * flicker), baseScale.z);
        }
    }
}
