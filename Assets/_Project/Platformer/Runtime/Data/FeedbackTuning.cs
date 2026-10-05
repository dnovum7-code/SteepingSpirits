using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Soft feedback: squash/stretch, particles, sounds, catch fade. Editable during play.</summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/JumpNRun/Feedback Tuning", fileName = "FeedbackTuning")]
    public class FeedbackTuning : ScriptableObject
    {
        public FeedbackParams feedback = new FeedbackParams();
    }
}
