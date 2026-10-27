using UnityEngine;

namespace SteepingSpirits.Brewing.Data
{
    /// <summary>
    /// Swappable brewing sounds. Every slot is optional: a missing stage loop
    /// falls back to filtered placeholder noise, other missing clips are silent.
    /// </summary>
    [CreateAssetMenu(fileName = "BrewAudioSet", menuName = "SteepingSpirits/Brewing/Brew Audio Set")]
    public class BrewAudioSet : ScriptableObject
    {
        [Tooltip("Loop per BoilStage: Still, Shrimp, Crab, Fish, Pearls, Raging")]
        public AudioClip[] stageLoops = new AudioClip[6];

        [Tooltip("Pouring loop – pitch rises with the fill level")]
        public AudioClip pourLoop;

        [Tooltip("Played when the leaves are lifted")]
        public AudioClip liftClip;

        [Tooltip("Played for a perfect cup")]
        public AudioClip bellClip;

        public AudioClip StageLoop(int index)
        {
            return stageLoops != null && index >= 0 && index < stageLoops.Length ? stageLoops[index] : null;
        }
    }
}
