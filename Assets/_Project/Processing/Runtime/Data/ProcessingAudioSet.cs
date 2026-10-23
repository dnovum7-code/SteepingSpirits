using UnityEngine;

namespace SteepingSpirits.Processing
{
    public enum WorkshopSound
    {
        Rustle,
        Knead,
        Sizzle,
        Crackle,
        Pack,
        Place,
        Chime
    }

    /// <summary>
    /// Sound slots of the tea workshop. An empty slot plays a quiet procedural placeholder
    /// (ProcessingSynth); a missing set breaks nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/Processing/Audio Set", fileName = "ProcessingAudioSet")]
    public class ProcessingAudioSet : ScriptableObject
    {
        [Tooltip("Leaves rustling on the wither rack (placeholder: soft dry noise)")]
        public AudioClip rustle;

        [Tooltip("One dull knead on the rolling table")]
        public AudioClip knead;

        [Tooltip("Wok sizzle loop (volume follows the heat)")]
        public AudioClip sizzle;

        [Tooltip("Quiet ember crackle loop under the drying basket")]
        public AudioClip crackle;

        [Tooltip("Tea packed")]
        public AudioClip pack;

        [Tooltip("A batch laid down / taken off")]
        public AudioClip place;

        [Tooltip("Soft chime (oxidation stopped, tea finished)")]
        public AudioClip chime;

        public AudioClip ClipFor(WorkshopSound s)
        {
            switch (s)
            {
                case WorkshopSound.Rustle: return rustle;
                case WorkshopSound.Knead: return knead;
                case WorkshopSound.Sizzle: return sizzle;
                case WorkshopSound.Crackle: return crackle;
                case WorkshopSound.Pack: return pack;
                case WorkshopSound.Place: return place;
                default: return chime;
            }
        }
    }
}
