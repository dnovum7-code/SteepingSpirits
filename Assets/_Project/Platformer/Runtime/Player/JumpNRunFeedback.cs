using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Soft player feedback: squash/stretch of the sprite, dust on jump/land,
    /// leaves while running, quiet sounds. No screen shake, no flashes.
    /// </summary>
    [RequireComponent(typeof(JumpNRunPlayer))]
    public class JumpNRunFeedback : MonoBehaviour
    {
        [SerializeField] private FeedbackTuning tuning;
        [SerializeField] private Color dustColor = new Color(0.85f, 0.80f, 0.70f, 0.55f);
        [SerializeField] private Color leafColor = new Color(0.55f, 0.72f, 0.42f, 0.8f);

        private JumpNRunPlayer player;
        private SquashStretch squash;
        private Transform visual;
        private Vector3 visualBaseScale = Vector3.one;
        private float leafTimer;

        public FeedbackParams Params => squash.Params;

        private void Awake()
        {
            player = GetComponent<JumpNRunPlayer>();
            if (tuning == null && JumpNRunLevel.Current != null)
            {
                tuning = JumpNRunLevel.Current.feedbackTuning;
            }

            squash = new SquashStretch(tuning != null ? tuning.feedback : new FeedbackParams());
        }

        private void Start()
        {
            var placeholder = GetComponent<PlaceholderVisual>();
            if (placeholder != null && placeholder.Renderer != null)
            {
                visual = placeholder.Renderer.transform;
                visualBaseScale = visual.localScale;
            }
        }

        private void OnEnable()
        {
            if (player == null) player = GetComponent<JumpNRunPlayer>();
            player.MotorEvent += OnMotorEvent;
        }

        private void OnDisable()
        {
            player.MotorEvent -= OnMotorEvent;
        }

        private void OnMotorEvent(MotorEvents e)
        {
            FeedbackParams p = squash.Params;
            Vector2 feet = player.Feet;

            if ((e & MotorEvents.Landed) != 0)
            {
                float speed = player.Motor.LastLandingSpeed;
                squash.Land(speed);
                float strength = Mathf.Clamp01(speed / Mathf.Max(1f, player.Params.MaxFallSpeed));
                int count = Mathf.RoundToInt(p.landDustCount * Mathf.Lerp(0.4f, 1f, strength));
                JumpNRunParticles.Burst(feet, count, Vector2.right, 15f, 2.2f, dustColor, 0.22f, 0.45f, -0.5f);
                JumpNRunParticles.Burst(feet, count, Vector2.left, 15f, 2.2f, dustColor, 0.22f, 0.45f, -0.5f);
                JumpNRunSounds.Play(JumpNRunSound.Land, p.landVolume * Mathf.Lerp(0.5f, 1f, strength));
            }

            if ((e & (MotorEvents.Jumped | MotorEvents.AirJumped | MotorEvents.WallJumped)) != 0)
            {
                squash.Jump();
                JumpNRunParticles.Burst(feet, p.jumpDustCount, Vector2.down, 70f, 1.5f, dustColor, 0.18f, 0.35f, -0.3f);
                JumpNRunSounds.Play(JumpNRunSound.Jump, p.jumpVolume);
            }
        }

        private void Update()
        {
            squash.Update(Time.deltaTime);
            ApplyScale();
            RunLeaves();
        }

        /// <summary>External stretch (bounce leaf, swing release).</summary>
        public void Stretch()
        {
            squash.Jump();
        }

        private void ApplyScale()
        {
            if (visual == null)
            {
                return;
            }

            Vec2 s = squash.Scale;
            float pivotShift = (s.y - 1f) * visualBaseScale.y * 0.5f;
            visual.localScale = new Vector3(visualBaseScale.x * s.x * player.Facing, visualBaseScale.y * s.y, 1f);

            // Keep the feet on the ground while squashing.
            visual.localPosition = new Vector3(0f, pivotShift, 0f);
        }

        private void RunLeaves()
        {
            float speed = Mathf.Abs(player.Velocity.x);
            if (!player.IsGrounded || speed < 2f)
            {
                leafTimer = 0f;
                return;
            }

            leafTimer -= Time.deltaTime * (speed / Mathf.Max(1f, player.Params.runSpeed));
            if (leafTimer > 0f)
            {
                return;
            }

            leafTimer = squash.Params.runLeafInterval;
            Vector2 back = new Vector2(-Mathf.Sign(player.Velocity.x), 0.8f);
            JumpNRunParticles.Burst(player.Feet, 1, back, 25f, 1.6f, leafColor, 0.2f, 0.9f, 1.2f, 1, 220f);
        }
    }
}
