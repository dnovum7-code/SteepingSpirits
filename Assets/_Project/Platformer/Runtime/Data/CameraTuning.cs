using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Follow camera of the Jump'n'Run levels. Editable during play.</summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/JumpNRun/Camera Tuning", fileName = "CameraTuning")]
    public class CameraTuning : ScriptableObject
    {
        public CameraParams camera = new CameraParams();
    }
}
