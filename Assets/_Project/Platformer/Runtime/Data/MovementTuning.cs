using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Run, jump and assist values of the Jump'n'Run player. Editable during play.</summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/JumpNRun/Movement Tuning", fileName = "MovementTuning")]
    public class MovementTuning : ScriptableObject
    {
        public MovementParams movement = new MovementParams();
    }
}
