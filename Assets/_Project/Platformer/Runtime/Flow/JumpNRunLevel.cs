using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Root of a builder-made level scene. Holds the shared tuning assets and
    /// the level's id/name; other level components look it up.
    /// </summary>
    public class JumpNRunLevel : MonoBehaviour
    {
        public string levelId = "level";
        public string displayName = "";
        public MovementTuning movementTuning;
        public CameraTuning cameraTuning;
        public FeedbackTuning feedbackTuning;
        public SpiritElementsTuning elementsTuning;

        /// <summary>Ingredients placed in the level ("id:n,…", written by the builder).</summary>
        public string availableIngredients = "";

        public int lanternCount;

        /// <summary>Scene path of the next level (empty = none).</summary>
        public string nextScenePath = "";

        /// <summary>World bounds of the level (for camera and fall detection).</summary>
        public Rect bounds = new Rect(0f, 0f, 40f, 20f);

        public static JumpNRunLevel Current { get; private set; }

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }
    }
}
