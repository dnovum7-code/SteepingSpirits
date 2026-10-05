using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Grabbing for the climbing path with the Jump'n'Run player: hold points
    /// (hang still), swing points (pendulum, pumping in rhythm like the
    /// playground swing) and physics vines (swing, climb up/down). Hold the
    /// grab button; jump or dash lets go with momentum, releasing the button drops.
    /// Mirrors the grab part of the old PlatformerController2D on top of
    /// <see cref="JumpNRunPlayer.TakeControl"/>.
    /// </summary>
    [RequireComponent(typeof(JumpNRunPlayer))]
    public class ClimbGrab : MonoBehaviour
    {
        [Header("Grabbing")]
        [SerializeField] private float regrabDelay = 0.25f;

        [Header("Hold points")]
        [SerializeField] private float holdOffset = 0.75f;
        [SerializeField] private float holdJumpFactor = 0.95f;

        [Header("Swing points")]
        [SerializeField] private float swingPump = 14f;
        [SerializeField] private float swingBrake = 20f;
        [SerializeField] private float swingMaxAngle = 100f;

        [Header("Vines")]
        [SerializeField] private float vinePumpForce = 14f;
        [SerializeField] private float vineWeight = 0.6f;
        [SerializeField] private float vineClimbInterval = 0.12f;
        [SerializeField] private float vineReleaseBoost = 1.15f;
        [SerializeField] private float vineReleaseUpBonus = 6f;
        [SerializeField] private float vineCatchRadius = 0.9f;

        private enum State
        {
            Free,
            Hold,
            Swing,
            Vine
        }

        private JumpNRunPlayer player;
        private State state;
        private GrabPoint point;
        private GrabPoint lastPoint;
        private VineSegment segment;
        private Vine lastVine;
        private SwingModel swing;
        private float regrabTimer;
        private float climbTimer;
        private RopeVisual rope;

        public bool IsGrabbing => state != State.Free;

        private void Awake()
        {
            player = GetComponent<JumpNRunPlayer>();
        }

        private void Start()
        {
            rope = RopeVisual.Create("GrabRope", new Color(0.85f, 0.75f, 0.55f), 0.08f, 5);
        }

        private void OnDestroy()
        {
            if (rope != null) Destroy(rope.gameObject);
        }

        private Vector2 Feet => player.Feet;
        private Vector2 Center => player.Body.position;

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            regrabTimer -= dt;
            climbTimer -= dt;

            switch (state)
            {
                case State.Free:
                    if (player.GrabHeld && regrabTimer <= 0f && !player.IsControlled) TryGrab();
                    break;
                case State.Hold:
                    HoldStep(dt);
                    break;
                case State.Swing:
                    SwingStep(dt);
                    break;
                case State.Vine:
                    VineStep(dt);
                    break;
            }
        }

        private void TryGrab()
        {
            Vector2 c = Center;
            GrabPoint bestPoint = null;
            VineSegment bestSegment = null;
            float best = float.MaxValue;

            var points = GrabPoint.All;
            for (int i = 0; i < points.Count; i++)
            {
                GrabPoint p = points[i];
                if (p == lastPoint && regrabTimer > -0.2f) continue;
                Vector2 hand = p.GrabMode == GrabPoint.Mode.Hold ? c + Vector2.up * holdOffset : c;
                float d = Vector2.Distance(hand, p.Position);
                if (d <= p.CatchRadius && d < best)
                {
                    best = d;
                    bestPoint = p;
                }
            }

            var segments = VineSegment.All;
            for (int i = 0; i < segments.Count; i++)
            {
                VineSegment s = segments[i];
                if (!s.IsGrabbable || (s.Vine == lastVine && regrabTimer > -0.2f)) continue;
                float d = Vector2.Distance(c, s.Body.position);
                if (d <= vineCatchRadius && d < best)
                {
                    best = d;
                    bestPoint = null;
                    bestSegment = s;
                }
            }

            if (bestSegment == null && bestPoint == null) return;
            if (!player.TakeControl(this)) return;

            if (bestSegment != null)
            {
                segment = bestSegment;
                state = State.Vine;
                segment.Body.AddForce(player.Velocity * player.Body.mass * 0.5f, ForceMode2D.Impulse);
            }
            else if (bestPoint.GrabMode == GrabPoint.Mode.Hold)
            {
                point = bestPoint;
                state = State.Hold;
            }
            else
            {
                point = bestPoint;
                state = State.Swing;
                Vector2 offset = c - point.Position;
                var p = new SwingParams
                {
                    ropeLength = Mathf.Clamp(offset.magnitude, point.MinRopeLength, point.RopeLength),
                    gravity = player.Params.Gravity,
                    pumpAcceleration = swingPump,
                    brakeAcceleration = swingBrake,
                    maxAngleDeg = swingMaxAngle,
                    damping = 0.08f
                };
                swing = new SwingModel(p);
                Vector2 v = player.Velocity;
                swing.Mount(new Vec2(offset.x, offset.y), new Vec2(v.x, v.y));
            }

            player.Motor.Reset();
            player.ConsumeJumpPress();
        }

        /// <summary>Shared exits: jump (with velocity), dash, or letting go of the button.</summary>
        private bool HandleRelease(Vector2 currentVelocity, Vector2 jumpVelocity)
        {
            if (player.ConsumeJumpPress())
            {
                Release(jumpVelocity, false);
                return true;
            }

            if (player.ConsumeDashPress())
            {
                Release(currentVelocity, true);
                return true;
            }

            if (!player.GrabHeld)
            {
                Release(currentVelocity, false);
                return true;
            }

            return false;
        }

        private void Release(Vector2 velocity, bool dash)
        {
            if (point != null) lastPoint = point;
            if (segment != null) lastVine = segment.Vine;
            point = null;
            segment = null;
            state = State.Free;
            regrabTimer = regrabDelay;
            if (rope != null) rope.Hide();
            player.TakeControl(null);
            player.Launch(velocity);
            if (dash) player.QueueDash();
        }

        private void PlaceCenter(Vector2 center)
        {
            player.Body.MovePosition(center);
            player.Body.linearVelocity = Vector2.zero;
        }

        private void HoldStep(float dt)
        {
            if (point == null)
            {
                Release(Vector2.zero, false);
                return;
            }

            Vector2 move = player.MoveInput;
            var jump = new Vector2(move.x * player.Params.runSpeed, move.y < -0.5f ? 0f : player.Params.JumpVelocity * holdJumpFactor);
            if (HandleRelease(Vector2.zero, jump)) return;

            Vector2 target = point.Position + Vector2.down * holdOffset;
            PlaceCenter(Vector2.MoveTowards(Center, target, 30f * dt));
            if (rope != null) rope.Show(point.Position, Center + Vector2.up * 0.3f);
        }

        private void SwingStep(float dt)
        {
            if (point == null)
            {
                Release(Vector2.zero, false);
                return;
            }

            Vec2 sv = swing.SeatVelocity;
            Vec2 rv = swing.ReleaseVelocity;
            if (HandleRelease(new Vector2(sv.x, sv.y), new Vector2(rv.x, rv.y))) return;

            swing.Step(dt, player.MoveInput.x);
            Vec2 o = swing.SeatOffset;
            Vector2 pos = point.Position + new Vector2(o.x, o.y);
            PlaceCenter(pos);
            if (rope != null) rope.Show(point.Position, pos);
        }

        private void VineStep(float dt)
        {
            if (segment == null || segment.Body == null)
            {
                Release(Vector2.zero, false);
                return;
            }

            Rigidbody2D seg = segment.Body;
            Vector2 move = player.MoveInput;
            Vector2 segVel = seg.linearVelocity;
            Vector2 jump = segVel * vineReleaseBoost + new Vector2(move.x * 3f, vineReleaseUpBonus);
            if (HandleRelease(segVel, jump)) return;

            if (Mathf.Abs(move.y) > 0.5f && climbTimer <= 0f)
            {
                Vine vine = segment.Vine;
                int next = segment.Index + (move.y > 0f ? -1 : 1);
                if (next >= vine.FirstGrabbableIndex && next < vine.Segments.Count)
                {
                    segment = vine.Segments[next];
                    seg = segment.Body;
                }

                climbTimer = vineClimbInterval;
            }

            seg.AddForce(Vector2.down * player.Body.mass * player.Params.Gravity * vineWeight);
            seg.AddForce(Vector2.right * move.x * vinePumpForce);
            PlaceCenter(seg.position);
        }
    }
}
