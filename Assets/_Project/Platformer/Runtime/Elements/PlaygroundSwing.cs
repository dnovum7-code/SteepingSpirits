using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A playground swing hanging from a branch (this transform = pivot).
    /// Land on or walk into the seat to sit down. A/D in rhythm with the swing
    /// builds it up (see <see cref="SwingModel"/>), the wrong rhythm slows it.
    /// Jump flies off in an arc with the seat's speed, down drops off.
    /// </summary>
    public class PlaygroundSwing : MonoBehaviour
    {
        [SerializeField] private Color ropeColor = new Color(0.82f, 0.72f, 0.55f);
        [SerializeField] private Color seatColor = new Color(0.62f, 0.45f, 0.30f);
        [SerializeField] private Color gainColor = new Color(0.75f, 0.92f, 0.6f, 0.5f);

        [Tooltip("Rope length of this swing (0 = SpiritElementsTuning value)")]
        public float ropeLengthOverride;

        [Tooltip("Highest angle of this swing in degrees (0 = SpiritElementsTuning value)")]
        public float maxAngleOverride;

        private readonly SwingParams own = new SwingParams();

        private SwingModel model;
        private RopeVisual ropeLeft;
        private RopeVisual ropeRight;
        private SpriteRenderer seat;
        private JumpNRunPlayer rider;
        private float remountTimer;
        private float lastAngle;

        private static readonly System.Collections.Generic.List<PlaygroundSwing> all = new System.Collections.Generic.List<PlaygroundSwing>();

        /// <summary>All swings in the loaded level (for tools and tests).</summary>
        public static System.Collections.Generic.IReadOnlyList<PlaygroundSwing> All => all;

        public bool Occupied => rider != null;
        public Vector2 Seat => SeatPosition;
        public SwingModel Model => model;

        /// <summary>Live tuning values with this swing's overrides (no allocation).</summary>
        private SwingParams Params
        {
            get
            {
                own.CopyFrom(SpiritElementsTuning.Current.swing);
                if (ropeLengthOverride > 0.5f) own.ropeLength = ropeLengthOverride;
                if (maxAngleOverride >= 20f) own.maxAngleDeg = maxAngleOverride;
                return own;
            }
        }

        private Vector2 Pivot => transform.position;
        private Vector2 SeatPosition => Pivot + ToVector(model.SeatOffset);

        private void Awake()
        {
            model = new SwingModel(Params);
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Start()
        {
            ropeLeft = RopeVisual.Create("SwingRopeL", ropeColor, 0.06f, 3);
            ropeRight = RopeVisual.Create("SwingRopeR", ropeColor, 0.06f, 3);
            seat = SteepingSpirits.Core.PlaceholderSprites.CreateSpriteObject("Seat", SteepingSpirits.Core.PlaceholderSprites.Square,
                seatColor, SeatPosition, new Vector2(1.1f, 0.18f), 4, transform);
        }

        private void OnDestroy()
        {
            if (ropeLeft != null) Destroy(ropeLeft.gameObject);
            if (ropeRight != null) Destroy(ropeRight.gameObject);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            model.Params = Params;
            remountTimer -= dt;

            if (rider == null)
            {
                model.Step(dt, 0f);
                TryMount();
                return;
            }

            if (rider.ConsumeJumpPress())
            {
                Leave(ToVector(model.ReleaseVelocity), true);
                return;
            }

            if (rider.MoveInput.y < -0.6f)
            {
                Leave(ToVector(model.SeatVelocity), false);
                return;
            }

            lastAngle = model.Angle;
            bool auto = JumpNRunOptions.Instance != null && JumpNRunOptions.Instance.Options.autoSwing;
            model.Step(dt, rider.MoveInput.x, auto);
            if (model.TurnedThisStep)
            {
                TurnFeedback();
            }

            Vector2 feet = SeatPosition + Vector2.up * 0.1f;
            Vector2 offset = (Vector2)rider.transform.position - rider.Feet;
            rider.Body.MovePosition(feet + offset);

            // Soft cue when passing the lowest point while building up.
            if (Mathf.Sign(lastAngle) != Mathf.Sign(model.Angle) && model.LastPump == SwingPump.Gain
                && Mathf.Abs(model.TangentialSpeed) > 3f)
            {
                JumpNRunSounds.Play(JumpNRunSound.Wind, 0.06f + 0.01f * Mathf.Abs(model.TangentialSpeed));
                JumpNRunParticles.Burst(SeatPosition, 2, -ToVector(model.SeatVelocity).normalized, 30f, 1f, gainColor,
                    0.14f, 0.7f, 1f, 1, 200f);
            }
        }

        /// <summary>At the high points: a soft creak that rises with the swing, a few leaves at big swings.</summary>
        private void TurnFeedback()
        {
            float share = Mathf.Clamp01(model.Amplitude / Mathf.Max(0.1f, model.MaxAngle));
            if (share < 0.2f)
            {
                return;
            }

            JumpNRunSounds.PlayPitched(JumpNRunSound.Creak, 0.04f + 0.1f * share, 0.8f + 0.6f * share);
            int leaves = Mathf.RoundToInt(share * 4f);
            if (leaves > 0)
            {
                JumpNRunParticles.Burst(SeatPosition + Vector2.up * 0.2f, leaves, Vector2.up, 70f, 1.2f, gainColor, 0.16f, 1.1f, 0.8f, 1, 200f);
            }
        }

        private void TryMount()
        {
            JumpNRunPlayer player = JumpNRunPlayer.Instance;
            if (player == null || player.IsControlled || remountTimer > 0f)
            {
                return;
            }

            Vector2 seatPos = SeatPosition;
            bool close = Vector2.Distance(player.Feet, seatPos) <= Params.mountRadius;
            bool landing = player.Velocity.y <= 0.5f;
            bool askedUp = player.MoveInput.y > 0.5f;
            if (!close || !(landing || askedUp))
            {
                return;
            }

            if (!player.TakeControl(this))
            {
                return;
            }

            rider = player;
            Vector2 v = player.Velocity;
            model.Mount(model.SeatOffset, new Vec2(v.x, v.y));
            rider.ConsumeJumpPress();
            JumpNRunSounds.Play(JumpNRunSound.Land, 0.1f);
        }

        private void Leave(Vector2 velocity, bool jumped)
        {
            JumpNRunPlayer player = rider;
            rider = null;
            remountTimer = Params.remountDelay;
            player.TakeControl(null);
            player.Launch(velocity);
            if (jumped)
            {
                var feedback = player.GetComponent<JumpNRunFeedback>();
                if (feedback != null) feedback.Stretch();
                JumpNRunSounds.Play(JumpNRunSound.Jump, 0.18f);
            }
        }

        private void LateUpdate()
        {
            if (seat == null || model == null)
            {
                return;
            }

            Vector2 s = SeatPosition;
            seat.transform.position = s;
            seat.transform.rotation = Quaternion.Euler(0f, 0f, model.Angle * Mathf.Rad2Deg);
            Vector2 side = new Vector2(Mathf.Cos(model.Angle), Mathf.Sin(model.Angle)) * 0.45f;
            ropeLeft.Show(Pivot - new Vector2(0.45f, 0f), s - side);
            ropeRight.Show(Pivot + new Vector2(0.45f, 0f), s + side);
        }

        private static Vector2 ToVector(Vec2 v) => new Vector2(v.x, v.y);

        private void OnDrawGizmos()
        {
            float L = Params.ropeLength;
            Gizmos.color = new Color(0.8f, 0.7f, 0.5f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * L);
            Gizmos.DrawWireSphere(transform.position + Vector3.down * L, 0.3f);
        }
    }
}
