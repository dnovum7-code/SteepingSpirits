using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Values of the spirit-world elements (wind, lantern spirit, leaves, swing). Editable during play.</summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/JumpNRun/Spirit Elements Tuning", fileName = "SpiritElementsTuning")]
    public class SpiritElementsTuning : ScriptableObject
    {
        public SpiritElementParams elements = new SpiritElementParams();

        /// <summary>The level's asset, or defaults if none is assigned.</summary>
        public static SpiritElementParams Current
        {
            get
            {
                JumpNRunLevel level = JumpNRunLevel.Current;
                if (level != null && level.elementsTuning != null)
                {
                    return level.elementsTuning.elements;
                }

                return fallback ?? (fallback = new SpiritElementParams());
            }
        }

        private static SpiritElementParams fallback;
    }
}
