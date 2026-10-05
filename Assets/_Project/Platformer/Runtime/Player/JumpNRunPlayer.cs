using System;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.Hooks;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Player of the Jump'n'Run levels. All movement math lives in
    /// <see cref="PlayerMotor"/> (Core); this class only reads input, asks the
    /// physics engine for contacts, applies corner correction and hands the
    /// motor's velocity to the Rigidbody2D.
    ///
    /// Spirit elements interact through <see cref="AddAcceleration"/>,
    /// <see cref="Launch"/> and <see cref="TakeControl"/> (swing, catch).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class JumpNRunPlayer : MonoBehaviour
    {
        [SerializeField] private MovementTuning tuning;
        [SerializeField] private float contactSkin = 0.05f;
        [SerializeField] private float dropThroughSeconds = 0.3f;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private PlayerMotor motor;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private ContactFilter2D solidFilter;

        private Vector2 move;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool dashPressed;
        private Vector2 externalAcceleration;
        private Collider2D ignoredPlatform;
        private float ignoreTimer;
        private object controller;

        // Corner correction queries, created once (no garbage per physics step).
        private Func<float, bool> headQuery;
        private Func<float, bool> ledgeQuery;
        private Func<Vector2, bool> ceilingNormal;
        private Func<Vector2, bool> wallNormal;
        private float queryDistance;
        private Vector2 queryDirection;

        public PlayerMotor Motor => motor;
        public MovementParams Params => motor.Params;
        public Rigidbody2D Body => body;
        public bool IsGrounded => motor.Grounded;
        public int Facing => motor.Facing;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Vector2 MoveInput => move;
        public bool JumpHeld => jumpHeld;
        public bool GrabHeld { get; private set; }

        /// <summary>Ground the player stands on (null in the air).</summary>
        public Collider2D Ground { get; private set; }

        /// <summary>True while a swing or a catch moves the player.</summary>
        public bool IsControlled => controller != null;

        /// <summary>Bottom centre of the collider in world space.</summary>
        public Vector2 Feet => (Vector2)box.bounds.center + Vector2.down * box.bounds.extents.y;

        public event Action<MotorEvents> MotorEvent;

        public static JumpNRunPlayer Instance { get; private set; }

        public void Configure(MovementTuning movementTuning)
        {
            tuning = movementTuning;
            if (motor != null && tuning != null)
            {
                motor.Params = tuning.movement;
            }
        }

        private void Awake()
        {
            Instance = this;
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            box.sharedMaterial = new PhysicsMaterial2D("JumpNRunPlayer_frictionless") { friction = 0f, bounciness = 0f };

            solidFilter = new ContactFilter2D { useTriggers = false };
            ceilingNormal = n => n.y < -0.6f;
            wallNormal = n => Mathf.Abs(n.x) > 0.6f;
            headQuery = dx => BoxBlocked(new Vector2(dx, 0f), Vector2.up, queryDistance, ceilingNormal);
            ledgeQuery = dy => BoxBlocked(new Vector2(0f, dy), queryDirection, queryDistance, wallNormal);
            if (tuning == null && JumpNRunLevel.Current != null)
            {
                tuning = JumpNRunLevel.Current.movementTuning;
            }

            // A missing asset falls back to defaults so the scene always runs.
            motor = new PlayerMotor(tuning != null ? tuning.movement : new MovementParams());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (GamePause.IsBlocked)
            {
                move = Vector2.zero;
                jumpHeld = false;
                return;
            }

            PlayerInputFrame frame = ReadInput();
            move = frame.move;
            jumpHeld = frame.jumpHeld;
            jumpPressed |= frame.jumpPressed;
            dashPressed |= frame.dashPressed;
            GrabHeld = frame.grabHeld;
        }

        /// <summary>Keyboard/pad, or a bot/recording when one is plugged in.</summary>
        public static PlayerInputFrame ReadInput()
        {
            IPlayerInputSource source = JumpNRunHooks.InputOverride;
            if (source != null)
            {
                return source.Read();
            }

            if (JumpNRunKeys.IsCustom)
            {
                return JumpNRunKeys.Read();
            }

            return new PlayerInputFrame
            {
                move = GameInput.Move,
                jumpPressed = GameInput.JumpPressed,
                jumpHeld = GameInput.JumpHeld,
                dashPressed = GameInput.DashPressed,
                grabHeld = GameInput.GrabHeld
            };
        }

        /// <summary>The swing or the catch takes over; null hands control back.</summary>
        public bool TakeControl(object owner)
        {
            if (controller != null && owner != null && controller != owner)
            {
                return false;
            }

            controller = owner;
            if (owner == null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
            }
            else
            {
                body.linearVelocity = Vector2.zero;
            }

            return true;
        }

        /// <summary>Consumes a buffered jump press (used by elements that react to jump).</summary>
        public bool ConsumeJumpPress()
        {
            bool pressed = jumpPressed;
            jumpPressed = false;
            return pressed;
        }

        /// <summary>Consumes a buffered dash press (grab release with dash).</summary>
        public bool ConsumeDashPress()
        {
            bool pressed = dashPressed;
            dashPressed = false;
            return pressed;
        }

        /// <summary>Queues a dash for the next physics step (after letting go of a grab).</summary>
        public void QueueDash()
        {
            dashPressed = true;
        }

        /// <summary>Adds an acceleration for the next physics step (updraft …).</summary>
        public void AddAcceleration(Vector2 acceleration)
        {
            externalAcceleration += acceleration;
        }

        /// <summary>Sets the velocity directly (bounce, swing release).</summary>
        public void Launch(Vector2 velocity, bool refill = true)
        {
            motor.Launch(new Vec2(velocity.x, velocity.y), refill);
            body.linearVelocity = velocity;
        }

        /// <summary>Places the player (respawn) and clears motion.</summary>
        public void Teleport(Vector2 feetPosition)
        {
            Vector2 offset = (Vector2)transform.position - Feet;
            body.position = feetPosition + offset;
            transform.position = body.position;
            body.linearVelocity = Vector2.zero;
            motor.Reset();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            ignoreTimer -= dt;
            if (ignoredPlatform != null && ignoreTimer <= 0f)
            {
                Physics2D.IgnoreCollision(box, ignoredPlatform, false);
                ignoredPlatform = null;
            }

            if (IsControlled || GamePause.IsBlocked)
            {
                jumpPressed = jumpPressed && IsControlled;
                dashPressed = dashPressed && IsControlled;
                externalAcceleration = Vector2.zero;
                if (GamePause.IsBlocked)
                {
                    body.linearVelocity = Vector2.zero;
                }

                return;
            }

            MotorContacts contacts = ReadContacts();

            // Down + jump on a thin platform drops through it.
            if (jumpPressed && move.y < -0.5f && Ground != null && Ground.TryGetComponent(out OneWayPlatform _))
            {
                ignoredPlatform = Ground;
                ignoreTimer = dropThroughSeconds;
                Physics2D.IgnoreCollision(box, Ground, true);
                jumpPressed = false;
                contacts.grounded = false;
                Ground = null;
            }

            Vector2 groundVelocity = GroundVelocity();
            Vector2 v = body.linearVelocity - groundVelocity;
            motor.Velocity = new Vec2(v.x, v.y);

            var input = new MotorInput
            {
                moveX = move.x,
                moveY = move.y,
                jumpPressed = jumpPressed,
                jumpHeld = jumpHeld,
                dashPressed = dashPressed
            };
            jumpPressed = false;
            dashPressed = false;

            MotorEvents events = motor.Step(dt, input, contacts);

            Vector2 velocity = new Vector2(motor.Velocity.x, motor.Velocity.y) + externalAcceleration * dt;
            externalAcceleration = Vector2.zero;
            motor.Velocity = new Vec2(velocity.x, velocity.y);

            ApplyCornerCorrection(ref velocity, dt);

            // Carry along with moving ground (sinking leaf platforms).
            if (motor.Grounded && (events & (MotorEvents.Jumped | MotorEvents.AirJumped)) == 0)
            {
                velocity += groundVelocity;
            }

            body.linearVelocity = velocity;

            if (events != MotorEvents.None)
            {
                MotorEvent?.Invoke(events);
            }
        }

        private MotorContacts ReadContacts()
        {
            var c = new MotorContacts();
            Ground = FindGround();
            c.grounded = Ground != null;
            if (!c.grounded)
            {
                if (CastBlocked(Vector2.right, contactSkin, n => n.x < -0.6f)) c.wallDir = 1;
                else if (CastBlocked(Vector2.left, contactSkin, n => n.x > 0.6f)) c.wallDir = -1;
            }

            return c;
        }

        private Collider2D FindGround()
        {
            if (body.linearVelocity.y > 0.5f && motor.Rising)
            {
                return null;
            }

            int count = body.Cast(Vector2.down, solidFilter, hits, contactSkin);
            float feet = Feet.y;
            for (int i = 0; i < count; i++)
            {
                Collider2D col = hits[i].collider;
                if (col == null || col == ignoredPlatform || hits[i].normal.y < 0.6f)
                {
                    continue;
                }

                if (col.TryGetComponent(out OneWayPlatform oneWay) && feet < oneWay.Top - 0.08f)
                {
                    continue;
                }

                return col;
            }

            return null;
        }

        private Vector2 GroundVelocity()
        {
            if (Ground == null || Ground.attachedRigidbody == null || Ground.attachedRigidbody == body)
            {
                return Vector2.zero;
            }

            return Ground.attachedRigidbody.linearVelocity;
        }

        private bool CastBlocked(Vector2 dir, float distance, Func<Vector2, bool> normalOk)
        {
            int count = body.Cast(dir, solidFilter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = hits[i].collider;
                if (col != null && col != ignoredPlatform && !col.TryGetComponent(out OneWayPlatform _) && normalOk(hits[i].normal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Box cast of the player's collider from an offset position (for corner correction).</summary>
        private bool BoxBlocked(Vector2 offset, Vector2 dir, float distance, Func<Vector2, bool> normalOk)
        {
            Bounds b = box.bounds;
            int count = Physics2D.BoxCast((Vector2)b.center + offset, b.size * 0.98f, 0f, dir, solidFilter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = hits[i].collider;
                if (col == null || col == box || col == ignoredPlatform || col.TryGetComponent(out OneWayPlatform _))
                {
                    continue;
                }

                if (hits[i].distance <= 0f && offset == Vector2.zero)
                {
                    continue;
                }

                if (normalOk(hits[i].normal))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyCornerCorrection(ref Vector2 velocity, float dt)
        {
            MovementParams p = motor.Params;

            // Head clips a corner while rising → slide around it.
            if (velocity.y > 0f && p.cornerCorrection > 0f)
            {
                queryDistance = velocity.y * dt + contactSkin;
                if (BoxBlocked(Vector2.zero, Vector2.up, queryDistance, ceilingNormal))
                {
                    int prefer = Mathf.Abs(velocity.x) > 0.1f ? (int)Mathf.Sign(velocity.x) : motor.Facing;
                    if (CornerCorrection.TryHead(headQuery, p.cornerCorrection, prefer, out float dx))
                    {
                        body.position += new Vector2(dx, 0f);
                    }
                }
            }

            // Feet catch a ledge while moving sideways → step up onto it.
            if (Mathf.Abs(velocity.x) > 0.5f && p.ledgeCorrection > 0f && velocity.y <= 1f)
            {
                queryDirection = new Vector2(Mathf.Sign(velocity.x), 0f);
                queryDistance = Mathf.Abs(velocity.x) * dt + contactSkin;
                if (BoxBlocked(Vector2.zero, queryDirection, queryDistance, wallNormal))
                {
                    if (CornerCorrection.TryLedge(ledgeQuery, p.ledgeCorrection, out float dy))
                    {
                        body.position += new Vector2(0f, dy);
                        if (velocity.y < 0f)
                        {
                            velocity.y = 0f;
                        }
                    }
                }
            }
        }
    }
}
