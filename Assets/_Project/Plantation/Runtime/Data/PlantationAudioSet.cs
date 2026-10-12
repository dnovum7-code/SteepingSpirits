using UnityEngine;

namespace SteepingSpirits.Plantation
{
    public enum GardenSound
    {
        Hoe,
        Sow,
        Water,
        Refill,
        Harvest,
        Dig,
        Tool,
        Chest,
        EndDay,
        Grow
    }

    /// <summary>
    /// Sound slots of the tea garden. An empty slot plays the quiet procedural placeholder
    /// (GardenSynth); drop a real clip in to replace it. A missing set breaks nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/Plantation/Audio Set", fileName = "PlantationAudioSet")]
    public class PlantationAudioSet : ScriptableObject
    {
        [Tooltip("Hoe in soil (placeholder: soft crumbly noise)")]
        public AudioClip hoe;

        [Tooltip("Seeds into the soil (placeholder: tiny pluck)")]
        public AudioClip sow;

        [Tooltip("Watering (placeholder: gentle trickle)")]
        public AudioClip water;

        public AudioClip refill;

        [Tooltip("Harvest pop (placeholder: two soft rising notes)")]
        public AudioClip harvest;

        [Tooltip("Shovel (placeholder: low thump)")]
        public AudioClip dig;

        [Tooltip("Tool switch (placeholder: wooden tick)")]
        public AudioClip toolSwitch;

        public AudioClip chest;

        [Tooltip("The day ends (placeholder: soft chime)")]
        public AudioClip endDay;

        [Tooltip("Plants grew over night (placeholder: airy shimmer)")]
        public AudioClip grow;

        public AudioClip ClipFor(GardenSound sound)
        {
            switch (sound)
            {
                case GardenSound.Hoe: return hoe;
                case GardenSound.Sow: return sow;
                case GardenSound.Water: return water;
                case GardenSound.Refill: return refill;
                case GardenSound.Harvest: return harvest;
                case GardenSound.Dig: return dig;
                case GardenSound.Tool: return toolSwitch;
                case GardenSound.Chest: return chest;
                case GardenSound.EndDay: return endDay;
                case GardenSound.Grow: return grow;
                default: return null;
            }
        }
    }
}
