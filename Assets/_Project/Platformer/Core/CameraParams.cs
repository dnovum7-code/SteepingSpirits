using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>Follow camera values. Serialized inside the CameraTuning asset.</summary>
    [Serializable]
    public class CameraParams
    {
        /// <summary>Half width / height of the dead zone around the focus (units).</summary>
        public float deadZoneX = 0.6f;
        public float deadZoneY = 1.2f;

        /// <summary>Look-ahead distance at full run speed (units).</summary>
        public float lookAhead = 2.2f;

        /// <summary>How fast the look-ahead offset follows the facing (1/s).</summary>
        public float lookAheadRate = 2.5f;

        /// <summary>Speed below which look-ahead keeps its last direction.</summary>
        public float lookAheadMinSpeed = 1.5f;

        /// <summary>Horizontal follow rate (1/s), exponential.</summary>
        public float followRateX = 6f;

        /// <summary>Vertical follow rate (1/s) once the vertical anchor moved.</summary>
        public float followRateY = 4f;

        /// <summary>
        /// While airborne the camera only follows vertically when the player
        /// leaves this band above / below the last landing height (units).
        /// After landing the vertical anchor moves to the new ground height.
        /// </summary>
        public float airBandUp = 3.5f;
        public float airBandDown = 1.5f;

        /// <summary>Camera sits this far above the player focus (units).</summary>
        public float verticalOffset = 1.2f;

        public float orthographicSize = 7f;

        public CameraParams Clone() => (CameraParams)MemberwiseClone();
    }

    /// <summary>Soft feedback values. Serialized inside the FeedbackTuning asset.</summary>
    [Serializable]
    public class FeedbackParams
    {
        /// <summary>Squash on landing (scale y −, x +) per unit of fall speed, capped.</summary>
        public float landSquashPerSpeed = 0.012f;
        public float landSquashMax = 0.22f;

        /// <summary>Stretch when jumping (scale y +).</summary>
        public float jumpStretch = 0.16f;

        /// <summary>How fast the squash/stretch returns to 1 (1/s).</summary>
        public float scaleRecoverRate = 14f;

        /// <summary>Dust puffs on landing / jumping.</summary>
        public int landDustCount = 6;
        public int jumpDustCount = 3;

        /// <summary>Leaves while running: one every N seconds at full speed.</summary>
        public float runLeafInterval = 0.22f;

        /// <summary>Sound volumes (0..1), all quiet by default.</summary>
        public float jumpVolume = 0.18f;
        public float landVolume = 0.16f;
        public float collectVolume = 0.22f;
        public float catchVolume = 0.2f;
        public float lanternVolume = 0.2f;

        /// <summary>Fade to soft dark and back when a spirit catches the player (s).</summary>
        public float catchFadeOut = 0.35f;
        public float catchHold = 0.15f;
        public float catchFadeIn = 0.45f;

        /// <summary>Optional slow-motion when collecting a rare ingredient (unscaled seconds, 1 = off).</summary>
        public float rareSlowMoScale = 0.6f;
        public float rareSlowMoSeconds = 0.35f;

        public FeedbackParams Clone() => (FeedbackParams)MemberwiseClone();
    }
}
