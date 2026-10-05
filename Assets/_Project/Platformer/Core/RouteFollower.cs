using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>What the route bot can see each frame (filled by the sim or by the Unity scene).</summary>
    public struct RouteObservation
    {
        public Vec2 feet;
        public Vec2 velocity;
        public bool grounded;
        public bool groundAheadLeft;
        public bool groundAheadRight;
        public int catches;

        public bool riding;
        public float swingAngle;
        public float swingAngularVelocity;
        public float swingAmplitude;
        public float swingMaxAngle;
        public Vec2 swingPivot;
    }

    /// <summary>
    /// Plays a planned route with ordinary inputs (left/right, jump press/hold),
    /// the way a careful player would: walk to the take-off point, jump, steer
    /// in the air, ride wind columns, bounce on dew, pump swings in rhythm and
    /// let go towards the target. Used by the smoke test and by core tests.
    /// </summary>
    public sealed class RouteFollower
    {
        private readonly List<RouteStep> steps;
        private readonly LevelReachability reach;
        private readonly MovementParams move;
        private int index;
        private float stepTime;
        private float jumpCooldown;
        private bool released;
        private bool jumpLatch;
        private float blockedTime;
        private bool holdJump;
        private int holdFrames;

        /// <summary>Seconds a single step may take before the bot gives up.</summary>
        public float StepTimeout = 8f;

        public bool Done => index >= steps.Count;
        public bool Failed { get; private set; }
        public string FailReason { get; private set; } = "";
        public int Index => index;
        public RouteStep? Current => index < steps.Count ? steps[index] : (RouteStep?)null;
        public IReadOnlyList<RouteStep> Steps => steps;

        /// <summary>Seat position provider for swing steps (pivot → seat).</summary>
        public Func<Vec2, Vec2> SeatOf;

        public RouteFollower(List<RouteStep> steps, LevelReachability reach, MovementParams move)
        {
            this.steps = steps ?? new List<RouteStep>();
            this.reach = reach;
            this.move = move ?? new MovementParams();
        }

        public MotorInput Tick(float dt, RouteObservation o)
        {
            jumpCooldown -= dt;
            var input = new MotorInput();
            if (Done || Failed)
            {
                return input;
            }

            // Arrived at the current (or a later) target?
            for (int look = index; look < Math.Min(steps.Count, index + 4); look++)
            {
                if (Arrived(o, steps[look]))
                {
                    index = look + 1;
                    stepTime = 0f;
                    released = false;
                    if (Done) return input;
                }
            }

            stepTime += dt;
            RouteStep s = steps[index];
            if (stepTime > StepTimeout)
            {
                Failed = true;
                FailReason = $"stuck at step {index} ({s}) near ({o.feet.x:0.0}, {o.feet.y:0.0})";
                return input;
            }

            Vec2 target = s.Target;
            float dx = target.x - o.feet.x;
            int dir = dx >= 0f ? 1 : -1;

            switch (s.kind)
            {
                case LevelReachability.MoveKind.Walk:
                    input.moveX = Math.Abs(dx) < 0.1f ? 0f : dir * (Math.Abs(dx) < 0.6f ? 0.5f : 1f);
                    break;
                case LevelReachability.MoveKind.Wind:
                    WindStep(o, s, ref input);
                    break;
                case LevelReachability.MoveKind.Swing:
                    SwingStep(o, s, ref input);
                    break;
                default:
                    JumpStep(o, s, ref input);
                    break;
            }

            // A jump we pressed is held until the rise is over (the ground flag can lag a frame
            // behind the take-off, and releasing early would cut the jump).
            if (input.jumpPressed)
            {
                holdJump = true;
                holdFrames = 0;
            }

            if (holdJump)
            {
                holdFrames++;
                if (holdFrames > 3 && o.velocity.y <= 0f)
                {
                    holdJump = false;
                }
                else
                {
                    input.jumpHeld = true;
                }
            }

            return input;
        }

        private static bool Arrived(RouteObservation o, RouteStep s)
        {
            Vec2 t = s.Target;
            return o.grounded && !o.riding && Math.Abs(o.feet.x - t.x) < 0.45f && Math.Abs(o.feet.y - t.y) < 0.4f;
        }

        private void JumpStep(RouteObservation o, RouteStep s, ref MotorInput input)
        {
            Vec2 target = s.Target;
            float dx = target.x - o.feet.x;
            int dir = dx >= 0f ? 1 : -1;

            // Keep holding a jump we started (the ground flag can lag one frame behind the take-off).
            if (jumpLatch && o.velocity.y <= 0f && !o.grounded) jumpLatch = false;
            input.jumpHeld = jumpLatch;

            if (o.grounded && !(jumpLatch && o.velocity.y > 0.5f))
            {
                jumpLatch = jumpLatch && jumpCooldown > 0.2f;
                input.jumpHeld = jumpLatch;
                int dy = s.to.y - (int)Math.Round(o.feet.y);
                input.moveX = Math.Abs(dx) > 0.1f ? dir : 0f;
                bool edge = dir > 0 ? !o.groundAheadRight : !o.groundAheadLeft;
                float reachDy = reach != null ? reach.Reach(dy) : 4f;
                bool inRange = Math.Abs(dx) <= Math.Max(1.6f, reachDy * (dy > 0 ? 0.6f : 0.5f));
                bool onDew = s.kind == LevelReachability.MoveKind.Dew;

                // Pressing against a wall without moving: jump up along it.
                blockedTime = Math.Abs(input.moveX) > 0f && Math.Abs(o.velocity.x) < 0.5f ? blockedTime + 1f / 60f : 0f;
                bool blocked = blockedTime > 0.15f;
                if (!onDew && (edge || inRange || blocked) && jumpCooldown <= 0f && (edge || blocked || (dy >= 0 && inRange)))
                {
                    input.jumpPressed = true;
                    input.jumpHeld = true;
                    jumpCooldown = 0.3f;
                    jumpLatch = true;
                }

                if (onDew)
                {
                    input.jumpHeld = true;
                }

                return;
            }

            // In the air: hold jump while rising (full height), steer onto the target.
            input.jumpHeld = jumpLatch || s.kind == LevelReachability.MoveKind.Dew;
            input.moveX = Steer(dx, o.velocity.x, target.y > o.feet.y + 0.2f && o.velocity.y < 0f);
        }

        private void WindStep(RouteObservation o, RouteStep s, ref MotorInput input)
        {
            Vec2 target = s.Target;
            bool highEnough = o.feet.y >= target.y + 0.3f;
            if (!highEnough)
            {
                float toColumn = s.anchor.x - o.feet.x;
                input.moveX = Math.Abs(toColumn) < 0.2f ? 0f : Math.Sign(toColumn) * Math.Min(1f, Math.Abs(toColumn));
                return;
            }

            input.moveX = Steer(target.x - o.feet.x, o.velocity.x, false);
        }

        private void SwingStep(RouteObservation o, RouteStep s, ref MotorInput input)
        {
            Vec2 target = s.Target;
            int dir = target.x >= s.anchor.x ? 1 : -1;

            if (o.riding)
            {
                released = false;
                float distBeyond = Math.Max(0f, Math.Abs(target.x - s.anchor.x) - s.rope * 0.5f);
                float need = Math.Min(o.swingMaxAngle - 0.09f, 0.45f + 0.09f * distBeyond);
                bool ready = o.swingAmplitude >= need;
                bool goingOut = o.swingAngularVelocity * dir > 0f;
                bool upSide = o.swingAngle * dir >= Math.Min(0.4f, need * 0.6f);
                if (ready && goingOut && upSide)
                {
                    input.jumpPressed = true;
                    input.jumpHeld = true;
                    released = true;
                    return;
                }

                // Pump in rhythm: press the way the swing is moving.
                float w = o.swingAngularVelocity;
                input.moveX = Math.Abs(w) < 0.15f ? dir : Math.Sign(w);
                return;
            }

            if (released || !o.grounded && stepTime > 0.2f && o.velocity.y < -0.1f && SeatDistance(o, s) > 1.5f)
            {
                // Flying off the swing (or falling back): steer to the target.
                input.moveX = Steer(target.x - o.feet.x, o.velocity.x, false);
                return;
            }

            // Get onto the seat: approach and hop on.
            Vec2 seat = SeatOf != null ? SeatOf(s.anchor) : new Vec2(s.anchor.x, s.anchor.y - s.rope);
            float sx = seat.x - o.feet.x;
            if (o.grounded)
            {
                input.moveX = Math.Abs(sx) > 0.15f ? Math.Sign(sx) : 0f;
                bool edge = sx > 0 ? !o.groundAheadRight : !o.groundAheadLeft;
                if ((Math.Abs(sx) < 2.6f || edge) && jumpCooldown <= 0f)
                {
                    input.jumpPressed = true;
                    input.jumpHeld = true;
                    jumpCooldown = 0.4f;
                }

                return;
            }

            input.jumpHeld = o.velocity.y > 0f && seat.y > o.feet.y;
            input.moveX = Steer(sx, o.velocity.x, false);
        }

        private float SeatDistance(RouteObservation o, RouteStep s)
        {
            Vec2 seat = SeatOf != null ? SeatOf(s.anchor) : new Vec2(s.anchor.x, s.anchor.y - s.rope);
            return Vec2.Distance(seat, o.feet);
        }

        /// <summary>Proportional steering in the air; avoids overshooting small targets.</summary>
        private float Steer(float dx, float vx, bool keepDistance)
        {
            // Keep some speed until close: landing a little past the edge beats falling short.
            float desired = Math.Sign(dx) * Math.Min(move.runSpeed, 2f + 4f * Math.Abs(dx));
            if (Math.Abs(dx) < 0.15f) desired = 0f;
            if (keepDistance && Math.Abs(dx) < 1.2f)
            {
                // Falling below a ledge we want to land on: do not drift under it.
                desired = 0f;
            }

            // The motor treats moveX as a target speed (fraction of run speed).
            return PMath.Clamp(desired / move.runSpeed, -1f, 1f);
        }
    }
}
