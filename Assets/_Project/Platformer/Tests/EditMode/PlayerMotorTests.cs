using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class PlayerMotorTests
    {
        private static MotorInput Right => new MotorInput { moveX = 1f };

        [Test]
        public void HeldJump_ReachesDesignHeight()
        {
            var p = new MovementParams { apexGravityFactor = 1f };
            float peak = new MotorSim(p).JumpPeak(true);
            Assert.AreEqual(p.jumpHeight, peak, 0.08f, "without apex hang the formula is exact up to step error");
        }

        [Test]
        public void ApexHang_AddsALittleHeight()
        {
            float plain = new MotorSim(new MovementParams { apexGravityFactor = 1f }).JumpPeak(true);
            float hang = new MotorSim().JumpPeak(true);
            Assert.Greater(hang, plain);
            Assert.Less(hang, plain * 1.15f, "hang must stay subtle");
        }

        [Test]
        public void ShortPress_CutsJump()
        {
            float full = new MotorSim().JumpPeak(true);
            float tap = new MotorSim().JumpPeak(true, 0.06f);
            Assert.Less(tap, full * 0.6f);
            Assert.Greater(tap, 0.3f, "a tap still hops");
        }

        [Test]
        public void CoyoteTime_AllowsJumpShortlyAfterLeavingGround()
        {
            Assert.IsTrue(JumpAfterLedge(0.08f));
            Assert.IsFalse(JumpAfterLedge(0.12f));
        }

        private static bool JumpAfterLedge(float delay)
        {
            var sim = new MotorSim();
            sim.Run(0.05f, default);
            sim.HasGround = false; // walked off the edge
            sim.Run(delay, default);
            MotorEvents e = sim.Step(new MotorInput { jumpPressed = true, jumpHeld = true });
            return (e & MotorEvents.Jumped) != 0;
        }

        [Test]
        public void JumpBuffer_JumpsOnLandingWhenPressedJustBefore()
        {
            Assert.IsTrue(BufferedJump(0.10f));
            Assert.IsFalse(BufferedJump(0.14f));
        }

        private static bool BufferedJump(float pressBeforeLanding)
        {
            var sim = new MotorSim();
            sim.Position = new Vec2(0f, 0f);
            sim.HasGround = false;
            sim.Motor.Velocity = new Vec2(0f, -6f);
            sim.Run(0.3f, default); // falling, out of coyote
            sim.Step(new MotorInput { jumpPressed = true, jumpHeld = true });
            sim.Run(pressBeforeLanding - MotorSim.Dt, new MotorInput { jumpHeld = true });
            sim.HasGround = true;
            sim.GroundY = sim.Position.y; // ground appears under the feet now
            sim.AllEvents = MotorEvents.None;
            sim.Run(0.03f, new MotorInput { jumpHeld = true });
            return (sim.AllEvents & MotorEvents.Jumped) != 0;
        }

        [Test]
        public void Falling_UsesStrongerGravityAndIsCapped()
        {
            var p = new MovementParams();
            var sim = new MotorSim(p) { HasGround = false };
            sim.Step(default);
            Assert.AreEqual(-p.Gravity * p.fallGravityFactor * MotorSim.Dt, sim.Motor.Velocity.y, 1e-3f,
                "dropping from rest already uses fall gravity");
            sim.Run(5f, default);
            Assert.AreEqual(-p.MaxFallSpeed, sim.Motor.Velocity.y, 1e-3f);
            Assert.AreEqual(p.JumpVelocity * 2.5f, p.MaxFallSpeed, 1e-3f);
        }

        [Test]
        public void AirControl_Is65PercentOfGround()
        {
            var p = new MovementParams();
            var ground = new MotorSim(p);
            ground.Step(Right);
            var air = new MotorSim(p) { HasGround = false };
            air.Step(Right);
            Assert.AreEqual(0.65f, air.Motor.Velocity.x / ground.Motor.Velocity.x, 1e-3f);
        }

        [Test]
        public void RunSpeed_IsReachedAndNotExceeded()
        {
            var p = new MovementParams();
            var sim = new MotorSim(p);
            sim.Run(1f, Right);
            Assert.AreEqual(p.runSpeed, sim.Motor.Velocity.x, 1e-4f);
        }

        [Test]
        public void WallSlide_LimitsFallAndWallJumpPushesAway()
        {
            var p = new MovementParams();
            var sim = new MotorSim(p) { HasGround = false, WallRightX = 0f };
            sim.Run(1f, Right);
            Assert.AreEqual(-p.wallSlideSpeed, sim.Motor.Velocity.y, 1e-3f);

            MotorEvents e = sim.Step(new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true });
            Assert.IsTrue((e & MotorEvents.WallJumped) != 0);
            Assert.Less(sim.Motor.Velocity.x, 0f);
            Assert.Greater(sim.Motor.Velocity.y, 0f);
            Assert.AreEqual(-1, sim.Motor.Facing);
        }

        [Test]
        public void WallMechanics_CanBeSwitchedOff()
        {
            var p = new MovementParams { wallSlideEnabled = false, wallJumpEnabled = false };
            var sim = new MotorSim(p) { HasGround = false, WallRightX = 0f };
            sim.Run(1f, Right);
            Assert.AreEqual(-p.MaxFallSpeed, sim.Motor.Velocity.y, 1e-3f);
            MotorEvents e = sim.Step(new MotorInput { moveX = 1f, jumpPressed = true, jumpHeld = true });
            Assert.AreEqual(MotorEvents.None, e & MotorEvents.WallJumped);
        }

        [Test]
        public void AirJump_OnlyWithAssistAndRefillsOnLanding()
        {
            var none = new MotorSim { HasGround = false };
            none.Run(0.3f, default);
            Assert.AreEqual(MotorEvents.None, none.Step(new MotorInput { jumpPressed = true }) & MotorEvents.AirJumped);

            var sim = new MotorSim(new MovementParams { airJumps = 1 }) { HasGround = false };
            sim.Run(0.3f, default);
            Assert.IsTrue((sim.Step(new MotorInput { jumpPressed = true, jumpHeld = true }) & MotorEvents.AirJumped) != 0);
            sim.Run(0.2f, new MotorInput { jumpHeld = true });
            Assert.AreEqual(MotorEvents.None, sim.Step(new MotorInput { jumpPressed = true }) & MotorEvents.AirJumped);
            Assert.AreEqual(0, sim.Motor.AirJumpsLeft);

            sim.HasGround = true;
            sim.GroundY = -100f;
            sim.Position = new Vec2(0f, -100f);
            sim.Motor.Velocity = new Vec2(0f, -1f);
            sim.Step(default);
            Assert.AreEqual(1, sim.Motor.AirJumpsLeft);
        }

        [Test]
        public void Landing_ReportsFallSpeed()
        {
            var sim = new MotorSim();
            sim.Position = new Vec2(0f, 4f);
            sim.Run(0.02f, default);
            sim.AllEvents = MotorEvents.None;
            sim.Run(2f, default);
            Assert.IsTrue((sim.AllEvents & MotorEvents.Landed) != 0);
            Assert.Greater(sim.Motor.LastLandingSpeed, 5f);
        }

        [Test]
        public void Dash_IsOffByDefaultAndSnapsTo8Directions()
        {
            var off = new MotorSim();
            Assert.AreEqual(MotorEvents.None, off.Step(new MotorInput { dashPressed = true }) & MotorEvents.DashStarted);

            var p = new MovementParams { dashEnabled = true };
            var sim = new MotorSim(p) { HasGround = false };
            sim.Step(new MotorInput { dashPressed = true, moveX = 0.9f, moveY = 0.5f });
            Assert.AreEqual(sim.Motor.Velocity.x, sim.Motor.Velocity.y, 1e-3f, "snapped to 45°");
            sim.Run(0.3f, default);
            Assert.IsTrue((sim.AllEvents & MotorEvents.DashEnded) != 0);
            Assert.IsFalse(sim.Motor.DashReady, "no refill in the air");
        }

        [Test]
        public void Launch_KeepsSpeedAboveRunSpeedInTheAir()
        {
            var p = new MovementParams();
            var sim = new MotorSim(p) { HasGround = false };
            sim.Motor.Launch(new Vec2(p.runSpeed * 2f, 5f), true);
            sim.Run(0.1f, Right);
            Assert.Greater(sim.Motor.Velocity.x, p.runSpeed * 1.8f, "swing/bounce momentum fades gently");
        }
    }

    public class CornerCorrectionTests
    {
        [Test]
        public void FindsSmallestFreeOffset_PreferredSideFirst()
        {
            // Blocked unless |offset| ≥ 0.06 on the right side.
            bool found = CornerCorrection.TryHead(dx => dx < 0.06f, 0.15f, 1, out float dx);
            Assert.IsTrue(found);
            Assert.AreEqual(0.06f, dx, 1e-4f);

            found = CornerCorrection.TryHead(dx2 => dx2 > -0.05f, 0.15f, 1, out dx);
            Assert.IsTrue(found);
            Assert.Less(dx, 0f);
        }

        [Test]
        public void GivesUpBeyondMax()
        {
            Assert.IsFalse(CornerCorrection.TryHead(dx => System.Math.Abs(dx) < 0.2f, 0.15f, 1, out float _));
        }

        [Test]
        public void LedgeNudge_OnlyUpwards()
        {
            Assert.IsTrue(CornerCorrection.TryLedge(dy => dy < 0.1f, 0.15f, out float dy));
            Assert.Greater(dy, 0.099f);
            Assert.LessOrEqual(dy, 0.15f);
        }
    }
}
