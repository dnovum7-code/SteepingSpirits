using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>Playground swing ("Schaukel") values.</summary>
    [Serializable]
    public class SwingParams
    {
        public float ropeLength = 3f;

        /// <summary>Pendulum gravity. Period ≈ 2π·√(L/g) → 2 s with the defaults.</summary>
        public float gravity = 30f;

        /// <summary>Tangential push (units/s²) when pressing in the swing direction.</summary>
        public float pumpAcceleration = 6f;

        /// <summary>
        /// Tangential brake (units/s²) when pressing against the swing direction.
        /// Larger than the pump, so holding one key all the time slows the swing down.
        /// </summary>
        public float brakeAcceleration = 9f;

        /// <summary>Below this angular speed a key press starts the swing in its direction.</summary>
        public float startThreshold = 0.15f;

        /// <summary>Natural slowing (1/s).</summary>
        public float damping = 0.06f;

        /// <summary>Highest swing angle (degrees from hanging straight down).</summary>
        public float maxAngleDeg = 80f;

        /// <summary>Release velocity = seat velocity × this + upward bonus.</summary>
        public float releaseBoost = 1.15f;
        public float releaseUpBonus = 4f;

        /// <summary>Seconds after leaving before the swing can be used again.</summary>
        public float remountDelay = 0.4f;

        /// <summary>How close the feet must come to the seat to sit down (units).</summary>
        public float mountRadius = 1.0f;

        public SwingParams Clone() => (SwingParams)MemberwiseClone();

        /// <summary>Copies all values (no allocation; used to follow live tuning with per-swing overrides).</summary>
        public void CopyFrom(SwingParams o)
        {
            ropeLength = o.ropeLength;
            gravity = o.gravity;
            pumpAcceleration = o.pumpAcceleration;
            brakeAcceleration = o.brakeAcceleration;
            startThreshold = o.startThreshold;
            damping = o.damping;
            maxAngleDeg = o.maxAngleDeg;
            releaseBoost = o.releaseBoost;
            releaseUpBonus = o.releaseUpBonus;
            remountDelay = o.remountDelay;
            mountRadius = o.mountRadius;
        }
    }

    public enum SwingPump
    {
        None,
        Gain,
        Brake
    }

    /// <summary>
    /// A pendulum the player sits on. Pumping works like on a real swing: press
    /// towards the direction you are swinging (D while swinging forward/right,
    /// A while swinging back/left) to add energy; pressing against the motion –
    /// or simply holding one key all the time – slows you down. Jumping off
    /// releases the seat velocity, and the player flies on in an arc.
    /// Angle 0 = hanging straight down, positive = to the right.
    /// </summary>
    public sealed class SwingModel
    {
        public SwingParams Params;
        public float Angle;
        public float AngularVelocity;
        public SwingPump LastPump { get; private set; }

        public SwingModel(SwingParams parameters)
        {
            Params = parameters ?? new SwingParams();
        }

        public float MaxAngle => Params.maxAngleDeg * (float)Math.PI / 180f;

        /// <summary>True for the one step in which the swing turned around at a high point.</summary>
        public bool TurnedThisStep { get; private set; }

        /// <summary>Seat speed along the arc (units/s, signed).</summary>
        public float TangentialSpeed => AngularVelocity * Params.ropeLength;

        /// <summary>
        /// Advances the swing. inputX is analog (−1…1, a stick pumps gently when
        /// tilted a little). With autoPump (comfort option) holding any direction
        /// pumps in the right rhythm by itself.
        /// </summary>
        public void Step(float dt, float inputX, bool autoPump = false)
        {
            SwingParams p = Params;
            float L = Math.Max(0.1f, p.ropeLength);
            double sin = Math.Sin(Angle);
            double cos = Math.Cos(Angle);

            double alpha = -(p.gravity / L) * sin;
            LastPump = SwingPump.None;

            float strength = Math.Min(1f, Math.Abs(inputX));
            if (strength > 0.2f)
            {
                int dir = inputX > 0f ? 1 : -1;
                if (autoPump && Math.Abs(AngularVelocity) >= p.startThreshold)
                {
                    dir = Math.Sign(AngularVelocity);
                }

                if (Math.Abs(AngularVelocity) < p.startThreshold || dir == Math.Sign(AngularVelocity))
                {
                    // In rhythm: pushing along the motion. Works best near the bottom (cos).
                    alpha += dir * p.pumpAcceleration * strength * Math.Max(0.0, cos) / L;
                    LastPump = SwingPump.Gain;
                }
                else
                {
                    alpha -= Math.Sign(AngularVelocity) * p.brakeAcceleration * strength / L;
                    LastPump = SwingPump.Brake;
                }
            }

            float before = AngularVelocity;
            TurnedThisStep = false;
            AngularVelocity += (float)(alpha * dt);

            // The brake can stop the swing but never push it the other way.
            if (LastPump == SwingPump.Brake && Math.Sign(before) != Math.Sign(AngularVelocity))
            {
                AngularVelocity = 0f;
            }

            AngularVelocity *= (float)Math.Exp(-p.damping * dt);
            if (before != 0f && Math.Sign(before) != Math.Sign(AngularVelocity) && LastPump != SwingPump.Brake)
            {
                TurnedThisStep = true;
            }

            Angle += AngularVelocity * dt;

            float max = MaxAngle;
            if (Math.Abs(Angle) > max)
            {
                Angle = Math.Sign(Angle) * max;
                AngularVelocity = 0f;
                TurnedThisStep = true;
            }
        }

        /// <summary>Seat position relative to the pivot.</summary>
        public Vec2 SeatOffset => new Vec2((float)Math.Sin(Angle), -(float)Math.Cos(Angle)) * Params.ropeLength;

        /// <summary>Seat velocity (units/s).</summary>
        public Vec2 SeatVelocity => new Vec2((float)Math.Cos(Angle), (float)Math.Sin(Angle)) * TangentialSpeed;

        /// <summary>Velocity the player leaves the swing with when jumping off.</summary>
        public Vec2 ReleaseVelocity => SeatVelocity * Params.releaseBoost + new Vec2(0f, Params.releaseUpBonus);

        /// <summary>Highest angle the current energy reaches (radians, ≥ 0).</summary>
        public float Amplitude
        {
            get
            {
                float L = Params.ropeLength;
                float v = TangentialSpeed;
                double energy = 0.5 * v * v + Params.gravity * L * (1.0 - Math.Cos(Angle));
                double c = 1.0 - energy / (Params.gravity * L);
                return (float)Math.Acos(Math.Max(-1.0, Math.Min(1.0, c)));
            }
        }

        /// <summary>Takes over the player's velocity when they sit down (tangential part only).</summary>
        public void Mount(Vec2 seatOffset, Vec2 playerVelocity)
        {
            float L = Math.Max(0.1f, Params.ropeLength);
            Angle = (float)Math.Atan2(seatOffset.x, -seatOffset.y);
            Angle = PMath.Clamp(Angle, -MaxAngle, MaxAngle);
            var tangent = new Vec2((float)Math.Cos(Angle), (float)Math.Sin(Angle));
            AngularVelocity = Vec2.Dot(playerVelocity, tangent) / L * 0.6f;
        }
    }
}
