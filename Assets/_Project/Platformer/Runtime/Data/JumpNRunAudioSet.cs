using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Sound slots of the Jump'n'Run module. Every empty slot plays the quiet
    /// procedural placeholder (see JumpNRunSounds); drop a real clip into a
    /// slot to replace it. Volumes stay in FeedbackTuning.
    /// </summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/JumpNRun/Audio Set", fileName = "JumpNRunAudioSet")]
    public class JumpNRunAudioSet : ScriptableObject
    {
        [Header("Player")]
        [Tooltip("Short, soft take-off (placeholder: plucked tone)")]
        public AudioClip jump;

        [Tooltip("Landing puff (placeholder: low-passed noise)")]
        public AudioClip land;

        [Header("Collecting")]
        public AudioClip collect;
        public AudioClip collectRare;

        [Header("Lanterns and spirits")]
        public AudioClip lantern;

        [Tooltip("A spirit catches the player")]
        public AudioClip spiritCatch;

        public AudioClip dewBounce;
        public AudioClip wind;

        [Tooltip("Swing turning at a high point – played with rising pitch")]
        public AudioClip swingCreak;

        [Header("Level")]
        public AudioClip goal;

        /// <summary>The clip for a sound, or null for the procedural placeholder.</summary>
        public AudioClip ClipFor(JumpNRunSound sound)
        {
            switch (sound)
            {
                case JumpNRunSound.Jump: return jump;
                case JumpNRunSound.Land: return land;
                case JumpNRunSound.Collect: return collect;
                case JumpNRunSound.CollectRare: return collectRare;
                case JumpNRunSound.Lantern: return lantern;
                case JumpNRunSound.Catch: return spiritCatch;
                case JumpNRunSound.Bounce: return dewBounce;
                case JumpNRunSound.Wind: return wind;
                case JumpNRunSound.Goal: return goal;
                case JumpNRunSound.Creak: return swingCreak;
                default: return null;
            }
        }
    }
}
