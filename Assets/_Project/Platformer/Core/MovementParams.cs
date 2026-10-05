using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// All movement values of the Jump'n'Run player. Serialized inside the
    /// MovementTuning asset; tests use the defaults.
    /// </summary>
    [Serializable]
    public class MovementParams
    {
        // Running
        public float runSpeed = 8.5f;
        public float groundAcceleration = 90f;
        public float groundDeceleration = 110f;

        /// <summary>Air acceleration = ground acceleration × this (air control).</summary>
        public float airControl = 0.65f;

        /// <summary>Air deceleration without input = ground deceleration × this.</summary>
        public float airBrake = 0.35f;

        // Jump (see JumpMath for the formula)
        public float jumpHeight = 3.2f;
        public float timeToApex = 0.38f;

        /// <summary>Releasing jump while rising multiplies vy by this once.</summary>
        public float jumpCutFactor = 0.5f;

        /// <summary>|vy| below this with jump held counts as apex.</summary>
        public float apexThreshold = 2.2f;

        /// <summary>Gravity × this at the apex (hang time).</summary>
        public float apexGravityFactor = 0.5f;

        /// <summary>Gravity × this while falling.</summary>
        public float fallGravityFactor = 1.8f;

        /// <summary>Max fall speed = launch velocity × this.</summary>
        public float maxFallFactor = 2.5f;

        // Assists
        public float coyoteTime = 0.10f;
        public float jumpBufferTime = 0.12f;

        /// <summary>Max sideways nudge (units) when the head clips a corner.</summary>
        public float cornerCorrection = 0.15f;

        /// <summary>Max upward nudge (units) when the feet clip a ledge while moving sideways.</summary>
        public float ledgeCorrection = 0.15f;

        // Optional mechanics (all switchable)
        public bool wallSlideEnabled = true;
        public float wallSlideSpeed = 3.5f;
        public bool wallJumpEnabled = true;
        public float wallJumpX = 8.5f;
        public float wallJumpLockSeconds = 0.14f;

        public bool dashEnabled = false;
        public float dashSpeed = 19f;
        public float dashDuration = 0.15f;

        /// <summary>Speed above run speed (swing, bounce, dash) fades by this many units/s² in the air.</summary>
        public float momentumFade = 8f;

        /// <summary>After a dash the velocity is multiplied by this.</summary>
        public float dashEndFactor = 0.5f;

        /// <summary>Extra jumps in the air (assist option "extra air jump").</summary>
        public int airJumps = 0;

        /// <summary>Air jump velocity = jump velocity × this.</summary>
        public float airJumpFactor = 0.85f;

        /// <summary>Wall jump vertical velocity = jump velocity × this.</summary>
        public float wallJumpUpFactor = 0.9f;

        /// <summary>Holding down falls faster: max fall × this.</summary>
        public float fastFallFactor = 1.25f;

        public float Gravity => JumpMath.Gravity(jumpHeight, timeToApex);
        public float JumpVelocity => JumpMath.LaunchVelocity(jumpHeight, timeToApex);
        public float MaxFallSpeed => JumpVelocity * maxFallFactor;

        public MovementParams Clone() => (MovementParams)MemberwiseClone();
    }
}
