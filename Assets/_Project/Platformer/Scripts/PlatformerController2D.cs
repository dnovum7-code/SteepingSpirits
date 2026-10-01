using System;
using System.Collections;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Player;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// Seitenansicht-Steuerung für die Jump'n'Run-Szene (Celeste-Gefühl):
    ///  - Laufen mit schneller Beschleunigung, Schwung aus der Luft bleibt erhalten,
    ///  - Sprung mit variabler Höhe, Coyote-Time, Sprung-Puffer, weicher Scheitelpunkt,
    ///  - Wandrutschen + Wandsprung,
    ///  - 8-Wege-Dash (1 Ladung, am Boden/beim Greifen wieder voll), Nachbilder,
    ///    „Super-Sprung": während des Dashes am Boden springen,
    ///  - Greifen (Taste halten): an <see cref="GrabPoint"/> festhalten (Hold) oder
    ///    schwingen (Swing, Schwung aufbauen durch Links/Rechts im Takt),
    ///    an <see cref="Vine"/>-Lianen hängen, schaukeln und klettern,
    ///  - Tod durch <see cref="Hazard2D"/> → sofort am Checkpoint neu.
    ///
    /// Die Physik läuft über einen Rigidbody2D, dessen Geschwindigkeit hier
    /// komplett selbst berechnet wird (eigene Schwerkraft, gravityScale = 0).
    /// Alle Werte sind im Inspector zum Experimentieren freigegeben.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlatformerController2D : MonoBehaviour
    {
        public enum State
        {
            Normal,
            Dashing,
            Holding,
            Swinging,
            Vine,
            Dead
        }

        [Header("Laufen")]
        [SerializeField] private float runSpeed = 9f;
        [SerializeField] private float groundAcceleration = 110f;
        [SerializeField] private float groundDeceleration = 140f;
        [SerializeField] private float airAcceleration = 70f;
        [SerializeField] private float airDeceleration = 35f;

        [Header("Springen & Fallen")]
        [SerializeField] private float jumpVelocity = 15f;
        [SerializeField] private float gravity = 48f;
        [SerializeField] private float maxFallSpeed = 18f;
        [Tooltip("Fallgeschwindigkeit mit gedrückt „runter\"")]
        [SerializeField] private float fastFallSpeed = 25f;
        [Tooltip("Sprung früh losgelassen → Aufwärtstempo × Wert (variable Sprunghöhe)")]
        [SerializeField] private float jumpCutMultiplier = 0.45f;
        [Tooltip("Am Scheitelpunkt (|vy| kleiner) mit gehaltenem Sprung weniger Schwerkraft")]
        [SerializeField] private float apexThreshold = 2.5f;
        [SerializeField] private float apexGravityMultiplier = 0.5f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Wand")]
        [SerializeField] private float wallSlideSpeed = 4f;
        [SerializeField] private Vector2 wallJumpVelocity = new Vector2(10f, 14f);
        [Tooltip("So lange nach dem Wandsprung weniger Luftsteuerung (sonst klebt man wieder an der Wand)")]
        [SerializeField] private float wallJumpControlLock = 0.16f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 21f;
        [SerializeField] private float dashDuration = 0.15f;
        [Tooltip("Tempo nach dem Dash = Dash-Tempo × Wert")]
        [SerializeField] private float dashEndMultiplier = 0.5f;
        [SerializeField] private int maxDashes = 1;
        [Tooltip("Super-Sprung: Sprung während eines Dashes am Boden (x-Tempo × Wert)")]
        [SerializeField] private float superJumpSpeedMultiplier = 0.8f;
        [SerializeField] private Color dashReadyColor = new Color(0.95f, 0.4f, 0.35f);
        [SerializeField] private Color dashUsedColor = new Color(0.35f, 0.6f, 1f);

        [Header("Greifen")]
        [Tooltip("Greift nur solange die Greif-Taste gehalten wird (Celeste-Stil)")]
        [SerializeField] private bool holdToGrab = true;
        [Tooltip("Nach dem Loslassen kurz nicht neu greifen (sonst hängt man sofort wieder)")]
        [SerializeField] private float regrabDelay = 0.25f;
        [SerializeField] private bool refillDashOnGrab = true;

        [Header("Halten (Hold-Punkte)")]
        [Tooltip("So weit hängt der Spieler unter dem Punkt")]
        [SerializeField] private float holdOffset = 0.55f;
        [SerializeField] private float holdJumpVelocity = 15f;

        [Header("Schwingen (Swing-Punkte)")]
        [Tooltip("Wie stark Links/Rechts den Schwung verstärkt")]
        [SerializeField] private float pumpAcceleration = 22f;
        [Tooltip("Luftwiderstand beim Schwingen (0 = ewig)")]
        [SerializeField] private float swingDamping = 0.12f;
        [SerializeField] private float maxSwingSpeed = 24f;
        [Tooltip("Maximaler Ausschlag in Grad (90 = waagrecht). Darüber bleibt man kurz stehen und schwingt zurück")]
        [SerializeField] private float maxSwingAngle = 110f;
        [Tooltip("Abflug-Tempo = Schwung × Wert")]
        [SerializeField] private float swingReleaseBoost = 1.1f;
        [Tooltip("Zusätzlicher Aufwärts-Schub beim Abspringen")]
        [SerializeField] private float swingReleaseUpBonus = 5f;

        [Header("Lianen")]
        [SerializeField] private float vinePumpForce = 14f;
        [Tooltip("Faktor fürs Spieler-Gewicht an der Liane")]
        [SerializeField] private float vineWeight = 0.6f;
        [SerializeField] private float vineClimbInterval = 0.12f;
        [SerializeField] private float vineReleaseBoost = 1.15f;
        [SerializeField] private float vineReleaseUpBonus = 6f;
        [SerializeField] private float vineCatchRadius = 0.9f;

        [Header("Sonstiges")]
        [Tooltip("Unter dieser Höhe stirbt man (aus der Welt gefallen)")]
        [SerializeField] private float killY = -30f;
        [SerializeField] private float respawnDelay = 0.4f;
        [SerializeField] private SpriteRenderer bodyRenderer;

        private Rigidbody2D body;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[6];
        private ContactFilter2D solidFilter;

        // Eingabe (in Update gelesen, in FixedUpdate verbraucht)
        private Vector2 move;
        private float jumpBufferTimer;
        private bool jumpHeld;
        private bool dashQueued;
        private bool grabHeld;

        // Zustand
        private float coyoteTimer;
        private float wallJumpLockTimer;
        private float regrabTimer;
        private bool jumping;
        private float dashTimer;
        private Vector2 dashDir;
        private float afterImageTimer;
        private int wallDir;

        private GrabPoint grabPoint;
        private GrabPoint lastReleasedPoint;
        private float swingAngle;
        private float swingAngularVelocity;
        private float swingLength;

        private VineSegment vineSegment;
        private Vine lastReleasedVine;
        private float vineClimbTimer;

        private RopeVisual rope;
        private Vector2 checkpoint;
        private Vector3 visualBaseScale = Vector3.one;

        public State CurrentState { get; private set; } = State.Normal;
        public bool IsGrounded { get; private set; }
        public int DashesLeft { get; private set; }
        public int MaxDashes => maxDashes;
        public int Deaths { get; private set; }

        /// <summary>Blickrichtung: +1 rechts, −1 links.</summary>
        public int Facing { get; private set; } = 1;

        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        /// <summary>Dev-Cheat: Dash lädt sich nie ab.</summary>
        public bool InfiniteDashes { get; set; }

        public event Action Died;
        public event Action Respawned;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Reibungsfrei, sonst bleibt man an Wänden hängen.
            var slippery = new PhysicsMaterial2D("Spieler_reibungsfrei") { friction = 0f, bounciness = 0f };
            foreach (Collider2D c in GetComponents<Collider2D>())
            {
                c.sharedMaterial = slippery;
            }

            solidFilter = new ContactFilter2D { useTriggers = false };
            solidFilter.useLayerMask = false;

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (bodyRenderer != null)
            {
                Vector3 sc = bodyRenderer.transform.localScale;
                visualBaseScale = new Vector3(Mathf.Abs(sc.x), sc.y, sc.z);
            }

            rope = RopeVisual.Create("Seil", new Color(0.85f, 0.75f, 0.55f), 0.08f, 5);
            DashesLeft = maxDashes;
            checkpoint = transform.position;
        }

        private void OnDestroy()
        {
            if (rope != null)
            {
                Destroy(rope.gameObject);
            }
        }

        // ---------------------------------------------------------------
        // Eingabe
        // ---------------------------------------------------------------

        private void Update()
        {
            if (GamePause.IsBlocked || CurrentState == State.Dead)
            {
                move = Vector2.zero;
                jumpHeld = false;
                grabHeld = false;
                return;
            }

            move = GameInput.Move;
            jumpHeld = GameInput.JumpHeld;
            grabHeld = GameInput.GrabHeld;

            if (GameInput.JumpPressed)
            {
                jumpBufferTimer = jumpBufferTime;
            }

            if (GameInput.DashPressed)
            {
                dashQueued = true;
            }

            if (Mathf.Abs(move.x) > 0.2f && CurrentState == State.Normal && wallJumpLockTimer <= 0f)
            {
                Facing = move.x > 0f ? 1 : -1;
            }

            UpdateVisuals();
        }

        // ---------------------------------------------------------------
        // Physik-Schritt
        // ---------------------------------------------------------------

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            if (CurrentState == State.Dead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (GamePause.IsBlocked)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            UpdateContacts();
            TickTimers(dt);

            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                if (CurrentState == State.Normal)
                {
                    DashesLeft = maxDashes;
                }
            }

            if (InfiniteDashes)
            {
                DashesLeft = maxDashes;
            }

            switch (CurrentState)
            {
                case State.Normal:
                    if (!TryStartDash() && !TryGrab())
                    {
                        NormalMove(dt);
                    }
                    break;
                case State.Dashing:
                    DashUpdate(dt);
                    break;
                case State.Holding:
                    HoldUpdate(dt);
                    break;
                case State.Swinging:
                    SwingUpdate(dt);
                    break;
                case State.Vine:
                    VineUpdate(dt);
                    break;
            }

            dashQueued = false;

            if (body.position.y < killY)
            {
                Kill();
            }
        }

        private void TickTimers(float dt)
        {
            coyoteTimer -= dt;
            jumpBufferTimer -= dt;
            wallJumpLockTimer -= dt;
            regrabTimer -= dt;
            vineClimbTimer -= dt;
        }

        private void UpdateContacts()
        {
            IsGrounded = CastHits(Vector2.down, n => n.y > 0.6f);
            wallDir = 0;
            if (!IsGrounded)
            {
                if (CastHits(Vector2.right, n => n.x < -0.6f)) wallDir = 1;
                else if (CastHits(Vector2.left, n => n.x > 0.6f)) wallDir = -1;
            }
        }

        private bool CastHits(Vector2 direction, Func<Vector2, bool> normalOk)
        {
            int count = body.Cast(direction, solidFilter, hits, 0.06f);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider != null && !hits[i].collider.transform.IsChildOf(transform) && normalOk(hits[i].normal))
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------------------------------------------------------
        // Normal: Laufen, Springen, Wand
        // ---------------------------------------------------------------

        private void NormalMove(float dt)
        {
            Vector2 vel = body.linearVelocity;

            // Horizontal
            float target = move.x * runSpeed;
            bool hasInput = Mathf.Abs(target) > 0.01f;
            float accel = IsGrounded
                ? (hasInput ? groundAcceleration : groundDeceleration)
                : (hasInput ? airAcceleration : airDeceleration);

            if (wallJumpLockTimer > 0f)
            {
                accel *= 0.25f;
            }

            bool keepMomentum = !IsGrounded && Mathf.Abs(vel.x) > runSpeed
                                && (!hasInput || Mathf.Sign(vel.x) == Mathf.Sign(target));
            if (keepMomentum)
            {
                // Schwung aus Dash/Seil in der Luft nur sanft abbauen.
                vel.x = Mathf.MoveTowards(vel.x, Mathf.Sign(vel.x) * runSpeed, airDeceleration * 0.5f * dt);
            }
            else
            {
                vel.x = Mathf.MoveTowards(vel.x, target, accel * dt);
            }

            // Sprung (gepuffert) – vom Boden/Coyote oder von der Wand.
            if (jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    vel.y = jumpVelocity;
                    ConsumeJump();
                }
                else if (wallDir != 0)
                {
                    vel = new Vector2(-wallDir * wallJumpVelocity.x, wallJumpVelocity.y);
                    Facing = -wallDir;
                    wallJumpLockTimer = wallJumpControlLock;
                    ConsumeJump();
                }
            }

            // Variable Sprunghöhe
            if (jumping && vel.y > 0f && !jumpHeld)
            {
                vel.y *= jumpCutMultiplier;
                jumping = false;
            }
            if (vel.y <= 0f)
            {
                jumping = false;
            }

            // Schwerkraft (weicher am Scheitelpunkt)
            float g = gravity;
            if (Mathf.Abs(vel.y) < apexThreshold && jumpHeld)
            {
                g *= apexGravityMultiplier;
            }
            vel.y -= g * dt;

            // Maximale Fallgeschwindigkeit / Wandrutschen
            float maxFall = move.y < -0.5f ? fastFallSpeed : maxFallSpeed;
            if (wallDir != 0 && vel.y < 0f && Mathf.Abs(move.x) > 0.2f && (int)Mathf.Sign(move.x) == wallDir)
            {
                maxFall = wallSlideSpeed;
            }
            vel.y = Mathf.Max(vel.y, -maxFall);

            body.linearVelocity = vel;
        }

        private void ConsumeJump()
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumping = true;
        }

        // ---------------------------------------------------------------
        // Dash
        // ---------------------------------------------------------------

        private bool TryStartDash()
        {
            if (!dashQueued || DashesLeft <= 0)
            {
                return false;
            }

            Vector2 dir = move;
            if (dir.sqrMagnitude < 0.04f)
            {
                dir = new Vector2(Facing, 0f);
            }

            // Auf 8 Richtungen einrasten (präzise wie Celeste).
            float angle = Mathf.Round(Mathf.Atan2(dir.y, dir.x) / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
            dashDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (Mathf.Abs(dashDir.x) > 0.1f)
            {
                Facing = dashDir.x > 0f ? 1 : -1;
            }

            DashesLeft--;
            dashQueued = false;
            jumping = false;
            dashTimer = dashDuration;
            afterImageTimer = 0f;
            CurrentState = State.Dashing;
            CameraFollow2D.Shake(0.12f, 0.08f);
            body.linearVelocity = dashDir * dashSpeed;
            return true;
        }

        private void DashUpdate(float dt)
        {
            body.linearVelocity = dashDir * dashSpeed;

            afterImageTimer -= dt;
            if (afterImageTimer <= 0f)
            {
                afterImageTimer = 0.03f;
                AfterImage.Spawn(bodyRenderer, new Color(dashUsedColor.r, dashUsedColor.g, dashUsedColor.b, 0.55f), 0.22f);
            }

            // Super-Sprung: während des Dashes am Boden springen → weiter Satz.
            if (jumpBufferTimer > 0f && IsGrounded)
            {
                CurrentState = State.Normal;
                body.linearVelocity = new Vector2(dashDir.x * dashSpeed * superJumpSpeedMultiplier, jumpVelocity);
                ConsumeJump();
                return;
            }

            // Während des Dashes zugreifen ist erlaubt.
            if (TryGrab())
            {
                return;
            }

            dashTimer -= dt;
            if (dashTimer <= 0f)
            {
                CurrentState = State.Normal;
                Vector2 end = dashDir * dashSpeed * dashEndMultiplier;
                if (dashDir.y > 0.1f)
                {
                    end.y = Mathf.Min(end.y, jumpVelocity * 0.7f);
                }
                body.linearVelocity = end;
            }
        }

        // ---------------------------------------------------------------
        // Greifen
        // ---------------------------------------------------------------

        private bool TryGrab()
        {
            if (!grabHeld || regrabTimer > 0f)
            {
                return false;
            }

            Vector2 pos = body.position;
            GrabPoint bestPoint = null;
            VineSegment bestSegment = null;
            float best = float.MaxValue;

            foreach (GrabPoint p in GrabPoint.All)
            {
                if (p == lastReleasedPoint && regrabTimer > -0.2f)
                {
                    continue;
                }

                // Hold-Punkte greift man mit den Händen (über dem Kopf).
                Vector2 hand = p.GrabMode == GrabPoint.Mode.Hold ? pos + Vector2.up * holdOffset : pos;
                float d = Vector2.Distance(hand, p.Position);
                if (d <= p.CatchRadius && d < best)
                {
                    best = d;
                    bestPoint = p;
                }
            }

            foreach (VineSegment s in VineSegment.All)
            {
                if (!s.IsGrabbable || (s.Vine == lastReleasedVine && regrabTimer > -0.2f))
                {
                    continue;
                }

                float d = Vector2.Distance(pos, s.Body.position);
                if (d <= vineCatchRadius && d < best)
                {
                    best = d;
                    bestPoint = null;
                    bestSegment = s;
                }
            }

            if (bestSegment != null)
            {
                AttachVine(bestSegment);
                return true;
            }

            if (bestPoint != null)
            {
                if (bestPoint.GrabMode == GrabPoint.Mode.Hold) AttachHold(bestPoint);
                else AttachSwing(bestPoint);
                return true;
            }

            return false;
        }

        private void OnGrabbed()
        {
            jumping = false;
            if (refillDashOnGrab)
            {
                DashesLeft = maxDashes;
            }
        }

        /// <summary>Loslassen mit Abflug-Geschwindigkeit.</summary>
        private void Release(Vector2 velocity)
        {
            if (grabPoint != null) lastReleasedPoint = grabPoint;
            if (vineSegment != null) lastReleasedVine = vineSegment.Vine;

            grabPoint = null;
            vineSegment = null;
            rope.Hide();
            regrabTimer = regrabDelay;
            CurrentState = State.Normal;
            body.linearVelocity = velocity;
        }

        /// <summary>Gemeinsame Ausstiege: Taste losgelassen, Sprung, Dash.</summary>
        private bool HandleReleaseInputs(Vector2 currentVelocity, Vector2 jumpVelocityOnRelease)
        {
            if (jumpBufferTimer > 0f)
            {
                Release(jumpVelocityOnRelease);
                ConsumeJump();
                return true;
            }

            if (dashQueued && DashesLeft > 0)
            {
                Release(currentVelocity);
                TryStartDash();
                return true;
            }

            if (holdToGrab && !grabHeld)
            {
                Release(currentVelocity);
                return true;
            }

            return false;
        }

        // ---- Hold ----

        private void AttachHold(GrabPoint point)
        {
            grabPoint = point;
            CurrentState = State.Holding;
            body.linearVelocity = Vector2.zero;
            OnGrabbed();
        }

        private void HoldUpdate(float dt)
        {
            if (grabPoint == null)
            {
                Release(Vector2.zero);
                return;
            }

            Vector2 jump = new Vector2(move.x * runSpeed, move.y < -0.5f ? 0f : holdJumpVelocity);
            if (HandleReleaseInputs(Vector2.zero, jump))
            {
                return;
            }

            Vector2 target = grabPoint.Position + Vector2.down * holdOffset;
            body.MovePosition(Vector2.MoveTowards(body.position, target, 30f * dt));
            body.linearVelocity = Vector2.zero;
            rope.Show(grabPoint.Position, body.position + Vector2.up * 0.3f);
        }

        // ---- Swing ----

        private void AttachSwing(GrabPoint point)
        {
            grabPoint = point;
            Vector2 offset = body.position - point.Position;
            swingLength = Mathf.Clamp(offset.magnitude, point.MinRopeLength, point.RopeLength);

            // Winkel 0 = senkrecht unter dem Punkt, positiv = rechts.
            swingAngle = offset.sqrMagnitude > 0.0001f ? Mathf.Atan2(offset.x, -offset.y) : 0f;

            // Bisherigen Schwung übernehmen (Tangentialanteil der Geschwindigkeit).
            Vector2 tangent = new Vector2(Mathf.Cos(swingAngle), Mathf.Sin(swingAngle));
            swingAngularVelocity = Vector2.Dot(body.linearVelocity, tangent) / swingLength;

            CurrentState = State.Swinging;
            OnGrabbed();
        }

        private void SwingUpdate(float dt)
        {
            if (grabPoint == null)
            {
                Release(Vector2.zero);
                return;
            }

            float cos = Mathf.Cos(swingAngle);
            float sin = Mathf.Sin(swingAngle);
            Vector2 tangent = new Vector2(cos, sin);
            Vector2 swingVelocity = tangent * swingAngularVelocity * swingLength;

            Vector2 jump = swingVelocity * swingReleaseBoost + Vector2.up * swingReleaseUpBonus;
            if (HandleReleaseInputs(swingVelocity, jump))
            {
                return;
            }

            // Pendel: Schwerkraft zieht zurück zur Mitte, Links/Rechts „pumpt" Energie
            // hinein, wenn man in Bewegungsrichtung drückt (hin und her = mehr Speed).
            float alpha = -(gravity / swingLength) * sin;
            alpha += pumpAcceleration * move.x * cos / swingLength;

            swingAngularVelocity += alpha * dt;
            swingAngularVelocity *= Mathf.Max(0f, 1f - swingDamping * dt);
            float maxAngular = maxSwingSpeed / swingLength;
            swingAngularVelocity = Mathf.Clamp(swingAngularVelocity, -maxAngular, maxAngular);
            swingAngle += swingAngularVelocity * dt;

            // Kein Überschlag: am höchsten Punkt anhalten, dann zurückschwingen.
            float maxAngle = maxSwingAngle * Mathf.Deg2Rad;
            if (Mathf.Abs(swingAngle) > maxAngle)
            {
                swingAngle = Mathf.Sign(swingAngle) * maxAngle;
                swingAngularVelocity = 0f;
            }

            Vector2 pos = grabPoint.Position + new Vector2(Mathf.Sin(swingAngle), -Mathf.Cos(swingAngle)) * swingLength;
            body.linearVelocity = Vector2.zero;
            body.MovePosition(pos);

            if (Mathf.Abs(swingAngularVelocity) > 0.05f)
            {
                Facing = swingAngularVelocity * cos > 0f ? 1 : -1;
            }

            rope.Show(grabPoint.Position, pos);
        }

        // ---- Liane ----

        private void AttachVine(VineSegment segment)
        {
            vineSegment = segment;
            CurrentState = State.Vine;
            vineClimbTimer = 0f;

            // Eigenen Schwung an die Liane weitergeben.
            segment.Body.AddForce(body.linearVelocity * body.mass * 0.5f, ForceMode2D.Impulse);
            OnGrabbed();
        }

        private void VineUpdate(float dt)
        {
            if (vineSegment == null || vineSegment.Body == null)
            {
                Release(Vector2.zero);
                return;
            }

            Rigidbody2D seg = vineSegment.Body;
            Vector2 segVel = seg.linearVelocity;
            Vector2 jump = segVel * vineReleaseBoost + new Vector2(move.x * 3f, vineReleaseUpBonus);
            if (HandleReleaseInputs(segVel, jump))
            {
                return;
            }

            // Klettern (hoch = Richtung Ast = kleinerer Index).
            if (Mathf.Abs(move.y) > 0.5f && vineClimbTimer <= 0f)
            {
                Vine vine = vineSegment.Vine;
                int next = vineSegment.Index + (move.y > 0f ? -1 : 1);
                if (next >= vine.FirstGrabbableIndex && next < vine.Segments.Count)
                {
                    vineSegment = vine.Segments[next];
                    seg = vineSegment.Body;
                }
                vineClimbTimer = vineClimbInterval;
            }

            // Gewicht + Schaukeln über Kräfte auf das gegriffene Segment.
            seg.AddForce(Vector2.down * body.mass * gravity * vineWeight);
            seg.AddForce(Vector2.right * move.x * vinePumpForce);

            body.linearVelocity = Vector2.zero;
            body.MovePosition(seg.position);
            if (Mathf.Abs(move.x) > 0.2f)
            {
                Facing = move.x > 0f ? 1 : -1;
            }
        }

        // ---------------------------------------------------------------
        // Tod / Checkpoints
        // ---------------------------------------------------------------

        /// <summary>Neuen Checkpoint setzen. true = war neu.</summary>
        public bool SetCheckpoint(Vector2 position)
        {
            if ((position - checkpoint).sqrMagnitude < 0.01f)
            {
                return false;
            }

            checkpoint = position;
            return true;
        }

        public void Kill()
        {
            if (CurrentState == State.Dead)
            {
                return;
            }

            if (CurrentState != State.Normal && CurrentState != State.Dashing)
            {
                Release(Vector2.zero);
            }

            CurrentState = State.Dead;
            Deaths++;
            body.linearVelocity = Vector2.zero;
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            CameraFollow2D.Shake(0.3f, 0.2f);
            Died?.Invoke();
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            body.position = checkpoint;
            transform.position = checkpoint;
            body.linearVelocity = Vector2.zero;
            DashesLeft = maxDashes;
            jumping = false;
            jumpBufferTimer = 0f;
            regrabTimer = 0f;
            CurrentState = State.Normal;
            if (bodyRenderer != null) bodyRenderer.enabled = true;
            Respawned?.Invoke();
        }

        /// <summary>Sofort zum letzten Checkpoint (Dev-Cheat / Menü).</summary>
        public void RespawnNow()
        {
            if (CurrentState != State.Dead)
            {
                Kill();
            }
        }

        // ---------------------------------------------------------------
        // Darstellung
        // ---------------------------------------------------------------

        private void UpdateVisuals()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            // Wie Celestes Haarfarbe: rot = Dash bereit, blau = verbraucht.
            bodyRenderer.color = DashesLeft > 0 ? dashReadyColor : dashUsedColor;

            if (bodyRenderer.transform == transform)
            {
                bodyRenderer.flipX = Facing < 0; // Grafik am Wurzelobjekt: nur spiegeln
                return;
            }

            // Grafik als Kind: spiegeln (inkl. Kinder wie Augen) + leicht strecken/stauchen.
            Vector2 v = body.linearVelocity;
            float stretch = CurrentState == State.Dashing ? 0.15f : Mathf.Clamp(Mathf.Abs(v.y) / 60f, 0f, 0.2f);
            bodyRenderer.transform.localScale = new Vector3(
                visualBaseScale.x * (1f - stretch * 0.5f) * Facing,
                visualBaseScale.y * (1f + stretch),
                visualBaseScale.z);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, vineCatchRadius);
        }
    }
}
