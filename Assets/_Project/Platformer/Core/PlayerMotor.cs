using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>Player intent for one physics step.</summary>
    public struct MotorInput
    {
        public float moveX;
        public float moveY;

        /// <summary>Jump was pressed since the last step (edge).</summary>
        public bool jumpPressed;
        public bool jumpHeld;
        public bool dashPressed;
    }

    /// <summary>What the engine side found around the player before this step.</summary>
    public struct MotorContacts
    {
        public bool grounded;

        /// <summary>+1 wall on the right, −1 on the left, 0 none (only while airborne).</summary>
        public int wallDir;
    }

    [Flags]
    public enum MotorEvents
    {
        None = 0,
        Jumped = 1,
        WallJumped = 2,
        AirJumped = 4,
        Landed = 8,
        LeftGround = 16,
        JumpCut = 32,
        DashStarted = 64,
        DashEnded = 128
    }

    /// <summary>
    /// Engine-free run/jump logic with all assists from the brief:
    /// coyote time, jump buffer, jump cut, apex hang, stronger fall gravity,
    /// max fall speed, reduced air control, optional wall slide/jump, air jumps
    /// and dash. The engine side feeds contacts, applies Velocity to its body
    /// and handles corner correction with <see cref="CornerCorrection"/>.
    /// </summary>
    public sealed class PlayerMotor
    {
        public MovementParams Params;

        public Vec2 Velocity;
        public bool Grounded { get; private set; }
        public float CoyoteTimer { get; private set; }
        public float BufferTimer { get; private set; }
        public float WallLockTimer { get; private set; }
        public float DashTimer { get; private set; }
        public bool IsDashing => DashTimer > 0f;
        public bool Rising { get; private set; }
        public int AirJumpsLeft { get; private set; }
        public bool DashReady { get; private set; } = true;
        public int Facing { get; private set; } = 1;

        /// <summary>Fall speed (positive) at the moment of the last landing.</summary>
        public float LastLandingSpeed { get; private set; }

        /// <summary>Which gravity band was used last step (for the debug overlay).</summary>
        public string GravityBand { get; private set; } = "ground";

        private float lowestAirVy;
        private Vec2 dashDir;

        public PlayerMotor(MovementParams parameters)
        {
            Params = parameters ?? new MovementParams();
            AirJumpsLeft = Params.airJumps;
        }

        /// <summary>Clears timers and velocity (respawn, catch).</summary>
        public void Reset()
        {
            Velocity = Vec2.Zero;
            CoyoteTimer = 0f;
            BufferTimer = 0f;
            WallLockTimer = 0f;
            DashTimer = 0f;
            Rising = false;
            DashReady = true;
            AirJumpsLeft = Params.airJumps;
            lowestAirVy = 0f;
        }

        /// <summary>External launch (bounce leaf, swing release, updraft). Cancels jump cut.</summary>
        public void Launch(Vec2 velocity, bool refillAirJumps)
        {
            Velocity = velocity;
            Rising = false;
            DashTimer = 0f;
            CoyoteTimer = 0f;
            if (refillAirJumps)
            {
                AirJumpsLeft = Params.airJumps;
                DashReady = true;
            }
        }

        public MotorEvents Step(float dt, MotorInput input, MotorContacts contacts)
        {
            MovementParams p = Params;
            MotorEvents events = MotorEvents.None;
            bool wasGrounded = Grounded;
            Grounded = contacts.grounded;

            if (Grounded && !wasGrounded)
            {
                events |= MotorEvents.Landed;
                LastLandingSpeed = -Math.Min(lowestAirVy, Velocity.y < 0f ? Velocity.y : 0f);
                lowestAirVy = 0f;
            }

            if (Grounded)
            {
                CoyoteTimer = p.coyoteTime;
                AirJumpsLeft = p.airJumps;
                if (!IsDashing)
                {
                    DashReady = true;
                }
            }
            else
            {
                CoyoteTimer -= dt;
            }

            BufferTimer = input.jumpPressed ? p.jumpBufferTime : BufferTimer - dt;
            WallLockTimer -= dt;

            if (input.dashPressed && p.dashEnabled && DashReady && !IsDashing)
            {
                StartDash(input);
                events |= MotorEvents.DashStarted;
            }

            if (IsDashing)
            {
                events |= DashStep(dt);
                TrackAir();
                return events;
            }

            float vx = HorizontalStep(dt, input);
            float vy = Velocity.y;

            // Jump: ground (incl. coyote) → wall → air jump.
            if (BufferTimer > 0f)
            {
                if (CoyoteTimer > 0f)
                {
                    vy = p.JumpVelocity;
                    events |= MotorEvents.Jumped;
                    ConsumeJump();
                }
                else if (p.wallJumpEnabled && contacts.wallDir != 0 && !Grounded)
                {
                    vx = -contacts.wallDir * p.wallJumpX;
                    vy = p.JumpVelocity * p.wallJumpUpFactor;
                    Facing = -contacts.wallDir;
                    WallLockTimer = p.wallJumpLockSeconds;
                    events |= MotorEvents.WallJumped;
                    ConsumeJump();
                }
                else if (AirJumpsLeft > 0 && !Grounded)
                {
                    AirJumpsLeft--;
                    vy = p.JumpVelocity * p.airJumpFactor;
                    events |= MotorEvents.AirJumped;
                    ConsumeJump();
                }
            }

            // Variable height: releasing jump while rising cuts the rise once.
            if (Rising && vy > 0f && !input.jumpHeld)
            {
                vy *= p.jumpCutFactor;
                Rising = false;
                events |= MotorEvents.JumpCut;
            }

            if (vy <= 0f)
            {
                Rising = false;
            }

            bool justJumped = (events & (MotorEvents.Jumped | MotorEvents.WallJumped | MotorEvents.AirJumped)) != 0;
            if (Grounded && vy <= 0f && !justJumped)
            {
                vy = 0f;
                GravityBand = "ground";
            }
            else
            {
                vy -= p.Gravity * GravityFactor(vy, input.jumpHeld) * dt;
            }

            float maxFall = p.MaxFallSpeed * (input.moveY < -0.5f ? p.fastFallFactor : 1f);
            bool pushingIntoWall = contacts.wallDir != 0 && Math.Abs(input.moveX) > 0.2f && Math.Sign(input.moveX) == contacts.wallDir;
            if (p.wallSlideEnabled && pushingIntoWall && vy < 0f && !Grounded)
            {
                maxFall = p.wallSlideSpeed;
                GravityBand = "wall";
            }

            vy = Math.Max(vy, -maxFall);

            if (wasGrounded && !Grounded && !justJumped)
            {
                events |= MotorEvents.LeftGround;
            }

            if (Math.Abs(input.moveX) > 0.2f && WallLockTimer <= 0f)
            {
                Facing = input.moveX > 0f ? 1 : -1;
            }

            Velocity = new Vec2(vx, vy);
            TrackAir();
            return events;
        }

        /// <summary>
        /// Gravity multiplier: apex hang while jump is held near the top,
        /// stronger gravity while falling, normal while rising.
        /// </summary>
        public float GravityFactor(float vy, bool jumpHeld)
        {
            if (jumpHeld && Math.Abs(vy) < Params.apexThreshold)
            {
                GravityBand = "apex";
                return Params.apexGravityFactor;
            }

            if (vy <= 0f)
            {
                GravityBand = "fall";
                return Params.fallGravityFactor;
            }

            GravityBand = "rise";
            return 1f;
        }

        private float HorizontalStep(float dt, MotorInput input)
        {
            MovementParams p = Params;
            float vx = Velocity.x;
            float target = input.moveX * p.runSpeed;
            bool hasInput = Math.Abs(input.moveX) > 0.01f;

            float accel;
            if (Grounded)
            {
                accel = hasInput ? p.groundAcceleration : p.groundDeceleration;
                if (hasInput && vx * target < 0f)
                {
                    accel = Math.Max(p.groundAcceleration, p.groundDeceleration);
                }
            }
            else
            {
                accel = hasInput ? p.groundAcceleration * p.airControl : p.groundDeceleration * p.airBrake;
            }

            if (WallLockTimer > 0f)
            {
                accel *= 0.25f;
            }

            // Momentum above run speed (dash, swing, bounce) fades gently in the air.
            bool keepMomentum = !Grounded && Math.Abs(vx) > p.runSpeed && (!hasInput || vx * target > 0f);
            if (keepMomentum)
            {
                return PMath.MoveTowards(vx, PMath.Sign(vx) * p.runSpeed, p.momentumFade * dt);
            }

            return PMath.MoveTowards(vx, target, accel * dt);
        }

        private void ConsumeJump()
        {
            BufferTimer = 0f;
            CoyoteTimer = 0f;
            Rising = true;
        }

        private void StartDash(MotorInput input)
        {
            Vec2 dir = new Vec2(input.moveX, input.moveY);
            if (dir.SqrMagnitude < 0.04f)
            {
                dir = new Vec2(Facing, 0f);
            }

            // Snap to 8 directions.
            double step = Math.PI / 4.0;
            double angle = Math.Round(Math.Atan2(dir.y, dir.x) / step) * step;
            dashDir = new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle));
            if (Math.Abs(dashDir.x) > 0.1f)
            {
                Facing = dashDir.x > 0f ? 1 : -1;
            }

            DashReady = false;
            Rising = false;
            DashTimer = Params.dashDuration;
            Velocity = dashDir * Params.dashSpeed;
        }

        private MotorEvents DashStep(float dt)
        {
            Velocity = dashDir * Params.dashSpeed;
            GravityBand = "dash";
            DashTimer -= dt;
            if (DashTimer > 0f)
            {
                return MotorEvents.None;
            }

            DashTimer = 0f;
            Vec2 end = dashDir * (Params.dashSpeed * Params.dashEndFactor);
            if (end.y > 0f)
            {
                end.y = Math.Min(end.y, Params.JumpVelocity * 0.7f);
            }

            Velocity = end;
            return MotorEvents.DashEnded;
        }

        private void TrackAir()
        {
            if (!Grounded && Velocity.y < lowestAirVy)
            {
                lowestAirVy = Velocity.y;
            }
        }
    }

    /// <summary>
    /// Corner correction: when a move is blocked only at the very edge, find the
    /// smallest sideways (or upward) nudge that frees it. The engine side
    /// passes a query "is the move still blocked with this offset?".
    /// </summary>
    public static class CornerCorrection
    {
        /// <summary>
        /// Tries offsets ±max·i/steps (i = 1..steps), smallest first; the
        /// preferred sign is tried first at each size. Returns false if no
        /// offset within max frees the move.
        /// </summary>
        public static bool TryFind(Func<float, bool> blockedWithOffset, float max, int steps, int preferredSign,
            out float offset)
        {
            offset = 0f;
            if (max <= 0f || steps <= 0)
            {
                return false;
            }

            int first = preferredSign < 0 ? -1 : 1;
            for (int i = 1; i <= steps; i++)
            {
                float size = max * i / steps;
                if (!blockedWithOffset(first * size))
                {
                    offset = first * size;
                    return true;
                }

                if (!blockedWithOffset(-first * size))
                {
                    offset = -first * size;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Head bumps only nudge sideways (no upward variant).</summary>
        public static bool TryHead(Func<float, bool> blockedWithXOffset, float max, int preferredSign, out float dx)
        {
            return TryFind(blockedWithXOffset, max, 5, preferredSign, out dx);
        }

        /// <summary>Feet catching a ledge while moving sideways: only upward nudges.</summary>
        public static bool TryLedge(Func<float, bool> blockedWithYOffset, float max, out float dy)
        {
            dy = 0f;
            for (int i = 1; i <= 5; i++)
            {
                float size = max * i / 5;
                if (!blockedWithYOffset(size))
                {
                    dy = size;
                    return true;
                }
            }

            return false;
        }
    }
}
