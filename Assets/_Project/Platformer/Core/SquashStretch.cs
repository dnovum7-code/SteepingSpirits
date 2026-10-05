using System;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Soft squash and stretch of the player sprite. Amount &gt; 0 stretches
    /// (taller, thinner), &lt; 0 squashes (flatter, wider). Area stays roughly
    /// constant: scale = (1 − a/2, 1 + a).
    /// </summary>
    public sealed class SquashStretch
    {
        public FeedbackParams Params;
        public float Amount { get; private set; }

        public SquashStretch(FeedbackParams parameters)
        {
            Params = parameters ?? new FeedbackParams();
        }

        public void Land(float fallSpeed)
        {
            Amount = -Math.Min(Params.landSquashMax, Math.Max(0f, fallSpeed) * Params.landSquashPerSpeed);
        }

        public void Jump()
        {
            Amount = Params.jumpStretch;
        }

        public void Update(float dt)
        {
            Amount = PMath.Damp(Amount, 0f, Params.scaleRecoverRate, dt);
        }

        public Vec2 Scale => new Vec2(1f - Amount * 0.5f, 1f + Amount);
    }
}
