using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class AssistOptionsTests
    {
        [Test]
        public void Defaults_AreOff()
        {
            var o = new AssistOptions();
            Assert.IsFalse(o.AnyActive);
            Assert.AreEqual(0, o.Apply(new MovementParams()).airJumps);
        }

        [Test]
        public void ExtraAirJump_AddsOneWithoutTouchingTheAsset()
        {
            var tuned = new MovementParams();
            var o = new AssistOptions { extraAirJump = true };
            Assert.AreEqual(1, o.Apply(tuned).airJumps);
            Assert.AreEqual(0, tuned.airJumps);
        }

        [Test]
        public void MotorBonusAirJumps_WorkLikeTheAssist()
        {
            var sim = new MotorSim { HasGround = false };
            sim.Motor.BonusAirJumps = 1;
            sim.Run(0.3f, default);
            MotorEvents e = sim.Step(new MotorInput { jumpPressed = true, jumpHeld = true });
            Assert.IsTrue((e & MotorEvents.AirJumped) != 0);
        }

        [Test]
        public void Speed_StepsAndClamps()
        {
            var o = new AssistOptions();
            o.StepSpeed(+1);
            Assert.AreEqual(1f, o.gameSpeed, 1e-5f);
            for (int i = 0; i < 10; i++) o.StepSpeed(-1);
            Assert.AreEqual(AssistOptions.MinSpeed, o.gameSpeed, 1e-5f);
        }

        [Test]
        public void SerializeParse_RoundTrip()
        {
            var o = new AssistOptions { gameSpeed = 0.8f, extraAirJump = true, fallProtection = true };
            string s = o.Serialize();
            Assert.AreEqual("speed=0.8;air=1;fall=1", s);
            AssistOptions back = AssistOptions.Parse(s);
            Assert.AreEqual(0.8f, back.gameSpeed, 1e-5f);
            Assert.IsTrue(back.extraAirJump);
            Assert.IsTrue(back.fallProtection);
        }

        [Test]
        public void Parse_IgnoresGarbageAndClamps()
        {
            AssistOptions o = AssistOptions.Parse("speed=0.1;foo;air=x");
            Assert.AreEqual(AssistOptions.MinSpeed, o.gameSpeed, 1e-5f);
            Assert.IsFalse(o.extraAirJump);
        }
    }
}
