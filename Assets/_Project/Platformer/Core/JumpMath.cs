using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Jump physics derived from what a designer can feel: jump height h and
    /// time to apex t. With constant gravity g and launch velocity v0:
    ///
    ///   h = v0·t − ½·g·t²   and   v(t) = v0 − g·t = 0 at the apex
    ///   ⇒ v0 = g·t  ⇒  h = ½·g·t²
    ///   ⇒ g  = 2h / t²
    ///   ⇒ v0 = 2h / t
    ///
    /// Example (defaults): h = 3.2, t = 0.38 s → g ≈ 44.3, v0 ≈ 16.8.
    /// The apex hang and the jump cut change the real curve slightly; the
    /// formula is the reference for the full, held jump without apex hang.
    /// </summary>
    public static class JumpMath
    {
        public static float Gravity(float height, float timeToApex)
        {
            return 2f * height / (timeToApex * timeToApex);
        }

        public static float LaunchVelocity(float height, float timeToApex)
        {
            return 2f * height / timeToApex;
        }

        /// <summary>Peak height for a launch velocity under gravity g: v0² / 2g.</summary>
        public static float PeakHeight(float launchVelocity, float gravity)
        {
            return launchVelocity * launchVelocity / (2f * gravity);
        }

        /// <summary>Launch velocity needed to reach a height under gravity g: √(2gh).</summary>
        public static float VelocityForHeight(float height, float gravity)
        {
            return (float)Math.Sqrt(2f * gravity * Math.Max(0f, height));
        }

        /// <summary>
        /// Horizontal distance of a full jump that lands on the launch height,
        /// with rise gravity g and fall gravity g·fallFactor, at run speed vx.
        /// </summary>
        public static float FlatJumpDistance(float height, float timeToApex, float fallFactor, float runSpeed)
        {
            float g = Gravity(height, timeToApex);
            float fallTime = (float)Math.Sqrt(2f * height / (g * fallFactor));
            return runSpeed * (timeToApex + fallTime);
        }
    }
}
