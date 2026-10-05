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
        public JumpNRunAudioSet audioSet;

        /// <summary>Ingredients placed in the level ("id:n,…", written by the builder).</summary>
        public string availableIngredients = "";

        public int lanternCount;

        /// <summary>Hubs only: how many levels the doors lead to (for the spirits' progress lines).</summary>
        public int hubLevelCount;

        /// <summary>The level text the scene was built from (route planning in tools and tests).</summary>
        [TextArea(2, 6)] public string layoutText = "";

        /// <summary>Small path lanterns for the optional lantern challenge.</summary>
        public int pathLanternCount;

        /// <summary>Where "back" leads from the end card (the clearing; empty = previous scene or meadow).</summary>
        public string backScenePath = "";

        /// <summary>Id of the next level (unlocked when this one is finished).</summary>
        public string nextLevelId = "";

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
