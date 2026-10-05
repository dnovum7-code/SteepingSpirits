using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class CameraRigTests
    {
        private const float Dt = 1f / 60f;

        private static CameraRig Rig()
        {
            var rig = new CameraRig(new CameraParams());
            rig.Snap(new Vec2(10f, 5f));
            return rig;
        }

        [Test]
        public void SmallMoves_InsideDeadZone_DoNotMoveCamera()
        {
            CameraRig rig = Rig();
            Vec2 start = rig.Position;
            for (int i = 0; i < 60; i++) rig.Update(Dt, new Vec2(10.4f, 5f), 0f, true);
            Assert.AreEqual(start.x, rig.Position.x, 1e-4f);
            Assert.AreEqual(start.y, rig.Position.y, 1e-4f);
        }

        [Test]
        public void Running_LooksAhead()
        {
            CameraRig rig = Rig();
            float x = 10f;
            for (int i = 0; i < 240; i++)
            {
                x += 8f * Dt;
                rig.Update(Dt, new Vec2(x, 5f), 8f, true);
            }

            Assert.Greater(rig.Position.x, x, "camera centre is ahead of the player");
            Assert.AreEqual(2.2f, rig.LookAheadX, 0.05f);
        }

        [Test]
        public void JumpingInPlace_DoesNotMoveCameraVertically()
        {
            CameraRig rig = Rig();
            float y0 = rig.Position.y;
            for (int i = 0; i < 40; i++)
            {
                float h = 3f * (float)System.Math.Sin(i / 40f * System.Math.PI);
                rig.Update(Dt, new Vec2(10f, 5f + h), 0f, false);
            }

            rig.Update(Dt, new Vec2(10f, 5f), 0f, true);
            Assert.AreEqual(y0, rig.Position.y, 1e-3f);
        }

        [Test]
        public void LandingHigher_MovesCameraUpSmoothly()
        {
            CameraRig rig = Rig();
            float y0 = rig.Position.y;
            rig.Update(Dt, new Vec2(10f, 8f), 0f, true);
            float afterOne = rig.Position.y;
            Assert.Greater(afterOne, y0);
            Assert.Less(afterOne - y0, 0.3f, "no jump cut");
            for (int i = 0; i < 180; i++) rig.Update(Dt, new Vec2(10f, 8f), 0f, true);
            Assert.AreEqual(8f + rig.Params.verticalOffset, rig.Position.y, 0.01f);
        }

        [Test]
        public void FallingFar_FollowsBeforeLanding()
        {
            CameraRig rig = Rig();
            for (int i = 0; i < 60; i++) rig.Update(Dt, new Vec2(10f, 5f - i * 0.2f), 0f, false);
            Assert.Less(rig.AnchorY, 5f - 10f);
        }

        [Test]
        public void Smoothing_IsFrameRateIndependent()
        {
            CameraRig a = Rig(), b = Rig();
            var target = new Vec2(10f, 9f);
            for (int i = 0; i < 30; i++) a.Update(1f / 30f, target, 0f, true);
            for (int i = 0; i < 120; i++) b.Update(1f / 120f, target, 0f, true);
            Assert.AreEqual(a.Position.y, b.Position.y, 1e-3f);
        }

        [Test]
        public void Bounds_ClampAndCenterSmallLevels()
        {
            var rig = new CameraRig(new CameraParams())
            {
                BoundsMin = new Vec2(0f, 0f), BoundsMax = new Vec2(100f, 10f), HalfView = new Vec2(12f, 7f)
            };
            rig.Snap(new Vec2(1f, 1f));
            Assert.AreEqual(12f, rig.Position.x, 1e-4f);
            Assert.AreEqual(5f, rig.Position.y, 1e-4f, "level lower than the view → centred");
        }
    }
}

namespace SteepingSpirits.Platforming.Tests
{
    public class CameraRigDeadZoneTests
    {
        [Test]
        public void SinkingLeaf_KeepsCameraStill()
        {
            var rig = new CameraRig(new CameraParams());
            rig.Snap(new Vec2(0f, 5f));
            float y0 = rig.Position.y;
            for (int i = 0; i < 60; i++) rig.Update(1f / 60f, new Vec2(0f, 5f - i * 0.005f), 0f, true);
            Assert.AreEqual(y0, rig.Position.y, 1e-4f);
        }
    }
}
