using System;
using System.Globalization;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Player-facing assist options (like Celeste's assist mode, but framed
    /// as comfort settings). Stored as a short text, e.g. "speed=0.8;air=1;fall=1".
    /// </summary>
    [Serializable]
    public class AssistOptions
    {
        public const float MinSpeed = 0.6f;
        public const float MaxSpeed = 1f;
        public const float SpeedStep = 0.1f;

        /// <summary>Game speed (time scale) 0.6 … 1.</summary>
        public float gameSpeed = 1f;

        /// <summary>One extra jump in the air.</summary>
        public bool extraAirJump;

        /// <summary>Respawn at the last safe ground instead of the last lantern.</summary>
        public bool fallProtection;

        public bool AnyActive => gameSpeed < MaxSpeed - 0.001f || extraAirJump || fallProtection;

        public AssistOptions Clone() => (AssistOptions)MemberwiseClone();

        public float ClampedSpeed => PMath.Clamp((float)Math.Round(gameSpeed / SpeedStep) * SpeedStep, MinSpeed, MaxSpeed);

        /// <summary>Effective movement values: the tuning asset plus the assists (asset stays untouched).</summary>
        public MovementParams Apply(MovementParams tuned)
        {
            MovementParams p = tuned.Clone();
            if (extraAirJump)
            {
                p.airJumps = Math.Max(p.airJumps, 0) + 1;
            }

            return p;
        }

        public void StepSpeed(int direction)
        {
            gameSpeed = PMath.Clamp(ClampedSpeed + direction * SpeedStep, MinSpeed, MaxSpeed);
        }

        public string Serialize()
        {
            return string.Format(CultureInfo.InvariantCulture, "speed={0:0.0};air={1};fall={2}",
                ClampedSpeed, extraAirJump ? 1 : 0, fallProtection ? 1 : 0);
        }

        public static AssistOptions Parse(string text)
        {
            var o = new AssistOptions();
            if (string.IsNullOrEmpty(text))
            {
                return o;
            }

            foreach (string part in text.Split(';'))
            {
                string[] kv = part.Split('=');
                if (kv.Length != 2)
                {
                    continue;
                }

                string key = kv[0].Trim();
                string value = kv[1].Trim();
                switch (key)
                {
                    case "speed":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
                        {
                            o.gameSpeed = s;
                            o.gameSpeed = o.ClampedSpeed;
                        }

                        break;
                    case "air":
                        o.extraAirJump = value == "1";
                        break;
                    case "fall":
                        o.fallProtection = value == "1";
                        break;
                }
            }

            return o;
        }
    }
}
