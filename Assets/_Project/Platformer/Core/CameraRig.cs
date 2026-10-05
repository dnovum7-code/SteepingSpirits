using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Engine-free follow camera: horizontal dead zone + look-ahead in the
    /// running direction, vertical "platform snapping" (the camera only moves
    /// up/down after landing on a new height, or when the player leaves a band
    /// while airborne), exponential smoothing that is frame-rate independent
    /// and a clamp to the level bounds.
    /// </summary>
    public sealed class CameraRig
    {
        public CameraParams Params;
        public Vec2 Position;
        public float LookAheadX { get; private set; }
        public float AnchorY { get; private set; }

        /// <summary>Level bounds (min/max corner); width 0 = no clamp.</summary>
        public Vec2 BoundsMin;
        public Vec2 BoundsMax;

        /// <summary>Half the visible size (set from aspect × orthographic size).</summary>
        public Vec2 HalfView = new Vec2(12f, 7f);

        public CameraRig(CameraParams parameters)
        {
            Params = parameters ?? new CameraParams();
        }

        public void Snap(Vec2 target)
        {
            AnchorY = target.y;
            LookAheadX = 0f;
            Position = Clamp(new Vec2(target.x, target.y + Params.verticalOffset));
        }

        public Vec2 Update(float dt, Vec2 target, float velocityX, bool grounded)
        {
            CameraParams p = Params;

            // Look-ahead keeps its direction when the player stops.
            if (Math.Abs(velocityX) > p.lookAheadMinSpeed)
            {
                LookAheadX = PMath.Damp(LookAheadX, Math.Sign(velocityX) * p.lookAhead, p.lookAheadRate, dt);
            }

            float focusX = target.x + LookAheadX;
            float desiredX = Position.x;
            if (focusX > desiredX + p.deadZoneX) desiredX = focusX - p.deadZoneX;
            else if (focusX < desiredX - p.deadZoneX) desiredX = focusX + p.deadZoneX;

            if (grounded)
            {
                // Small height changes (a sinking leaf, a one-tile step) keep the camera calm.
                if (Math.Abs(target.y - AnchorY) > p.deadZoneY)
                {
                    AnchorY = target.y;
                }
            }
            else if (target.y > AnchorY + p.airBandUp)
            {
                AnchorY = target.y - p.airBandUp;
            }
            else if (target.y < AnchorY - p.airBandDown)
            {
                AnchorY = target.y + p.airBandDown;
            }

            float desiredY = AnchorY + p.verticalOffset;

            var next = new Vec2(
                PMath.Damp(Position.x, desiredX, p.followRateX, dt),
                PMath.Damp(Position.y, desiredY, p.followRateY, dt));
            Position = Clamp(next);
            return Position;
        }

        public Vec2 Clamp(Vec2 pos)
        {
            if (BoundsMax.x - BoundsMin.x <= 0f)
            {
                return pos;
            }

            return new Vec2(ClampAxis(pos.x, BoundsMin.x, BoundsMax.x, HalfView.x),
                ClampAxis(pos.y, BoundsMin.y, BoundsMax.y, HalfView.y));
        }

        private static float ClampAxis(float v, float min, float max, float half)
        {
            if (max - min <= half * 2f)
            {
                return (min + max) * 0.5f;
            }

            return PMath.Clamp(v, min + half, max - half);
        }
    }
}
