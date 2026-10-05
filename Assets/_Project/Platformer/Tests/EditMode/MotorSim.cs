using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    /// <summary>Tiny test world: flat ground at y = groundY (or none), optional wall at x = wallX.</summary>
    public sealed class MotorSim
    {
        public const float Dt = 1f / 120f;

        public readonly PlayerMotor Motor;
        public Vec2 Position;
        public bool HasGround = true;
        public float GroundY;
        public float? WallRightX;
        public MotorEvents AllEvents;

        public MotorSim(MovementParams p = null)
        {
            Motor = new PlayerMotor(p ?? new MovementParams());
        }

        public bool OnGround => HasGround && Position.y <= GroundY + 1e-4f && Motor.Velocity.y <= 0f;

        public MotorEvents Step(MotorInput input)
        {
            var contacts = new MotorContacts
            {
                grounded = OnGround,
                wallDir = WallRightX.HasValue && !OnGround && Position.x >= WallRightX.Value - 0.01f ? 1 : 0
            };

            MotorEvents e = Motor.Step(Dt, input, contacts);
            AllEvents |= e;
            Position += Motor.Velocity * Dt;

            if (HasGround && Position.y < GroundY)
            {
                Position.y = GroundY;
                Motor.Velocity = new Vec2(Motor.Velocity.x, 0f);
            }

            if (WallRightX.HasValue && Position.x > WallRightX.Value)
            {
                Position.x = WallRightX.Value;
                Motor.Velocity = new Vec2(0f, Motor.Velocity.y);
            }

            return e;
        }

        public void Run(float seconds, MotorInput input)
        {
            int n = (int)(seconds / Dt + 0.5f);
            for (int i = 0; i < n; i++)
            {
                Step(input);
                input.jumpPressed = false;
                input.dashPressed = false;
            }
        }

        /// <summary>Full held jump from the ground; returns the peak height.</summary>
        public float JumpPeak(bool held, float releaseAfter = 999f)
        {
            Run(0.05f, default);
            float peak = Position.y;
            var input = new MotorInput { jumpPressed = true, jumpHeld = true };
            float t = 0f;
            for (int i = 0; i < 400; i++)
            {
                input.jumpHeld = held && t < releaseAfter;
                Step(input);
                input.jumpPressed = false;
                t += Dt;
                if (Position.y > peak) peak = Position.y;
                if (i > 5 && OnGround) break;
            }

            return peak - GroundY;
        }
    }
}
