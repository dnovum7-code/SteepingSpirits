using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Checkpoint lantern. Walking past lights it with a warm glow; a later
    /// catch brings the player back here.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Lantern : MonoBehaviour
    {
        public int index;
        [SerializeField] private Color unlitColor = new Color(0.45f, 0.42f, 0.38f);
        [SerializeField] private Color litColor = new Color(1f, 0.78f, 0.42f);
        [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.45f, 0.18f);

        private PlaceholderVisual visual;
        private SpriteRenderer glow;
        private float litTime = -1f;

        public bool IsLit => litTime >= 0f;

        /// <summary>Where the player's feet go on respawn (ground below the lantern).</summary>
        public Vector2 RespawnFeet => new Vector2(transform.position.x, transform.position.y - 0.5f);

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            visual = GetComponent<PlaceholderVisual>();
        }

        private void Start()
        {
            if (visual != null && visual.Renderer != null)
            {
                visual.Renderer.color = unlitColor;
                glow = SteepingSpirits.Core.PlaceholderSprites.CreateSpriteObject("Glow",
                    SteepingSpirits.Core.PlaceholderSprites.Circle, new Color(glowColor.r, glowColor.g, glowColor.b, 0f),
                    transform.position, Vector2.one * 3f, -1, transform);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsLit || other.GetComponentInParent<JumpNRunPlayer>() == null)
            {
                return;
            }

            litTime = 0f;
            JumpNRunSession.Current?.LightLantern(index, RespawnFeet);
            FeedbackTuning ft = JumpNRunLevel.Current != null ? JumpNRunLevel.Current.feedbackTuning : null;
            JumpNRunSounds.Play(JumpNRunSound.Lantern, ft != null ? ft.feedback.lanternVolume : 0.2f);
            JumpNRunParticles.Burst(transform.position, 8, Vector2.up, 50f, 1f, litColor, 0.15f, 1f, -0.5f);
        }

        private void Update()
        {
            if (!IsLit || visual == null || visual.Renderer == null)
            {
                return;
            }

            litTime += Time.deltaTime;
            float t = Mathf.Clamp01(litTime / 0.8f);
            visual.Renderer.color = Color.Lerp(unlitColor, litColor, t);
            if (glow != null)
            {
                // Slow, gentle breathing – no flashing.
                float breathe = 0.85f + 0.15f * Mathf.Sin(litTime * 1.6f);
                glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * t * breathe);
            }
        }
    }
}
