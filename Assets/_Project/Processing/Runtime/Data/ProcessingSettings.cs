using System;
using UnityEngine;

namespace SteepingSpirits.Processing
{
    /// <summary>Comfort options of the workshop.</summary>
    [Serializable]
    public class WorkshopComfortParams
    {
        [Tooltip("Leaves per new batch taken from the store")]
        public int batchSize = 4;

        [Tooltip("\"Gleichmäßig rollen\" (hold instead of rhythm) on for a new workshop")]
        public bool evenRollingDefault;

        [Tooltip("Show percent values next to colour, scent and feel")]
        public bool showPercentDefault;

        [Tooltip("Seconds between two updates of the batches (passive steps) – not every frame")]
        public float syncSeconds = 0.25f;

        [Tooltip("Debug fast forward per key press (game minutes): F2 small, F3 large")]
        public float forwardSmallMinutes = 30f;
        public float forwardLargeMinutes = 240f;
    }

    /// <summary>Soft feedback (no screen shake, no bright flashes).</summary>
    [Serializable]
    public class WorkshopFeedbackParams
    {
        [Range(0f, 1f)] public float volume = 0.45f;
        [Range(0f, 0.3f)] public float pitchJitter = 0.07f;
        public int particleBudget = 90;

        [Tooltip("Loudness of the wok sizzle at full heat")]
        [Range(0f, 1f)] public float sizzleVolume = 0.35f;

        [Tooltip("Loudness of the ember crackle while something dries")]
        [Range(0f, 1f)] public float crackleVolume = 0.18f;
    }

    /// <summary>All workshop tuning in one asset (durations, oxidation bands, heat window, rhythm, quality, notes).</summary>
    [CreateAssetMenu(menuName = "SteepingSpirits/Processing/Processing Settings", fileName = "ProcessingSettings")]
    public class ProcessingSettings : ScriptableObject
    {
        public ProcessingParams processing = new ProcessingParams();
        public WorkshopComfortParams comfort = new WorkshopComfortParams();
        public WorkshopFeedbackParams feedback = new WorkshopFeedbackParams();

        public static ProcessingSettings CreateDefault()
        {
            var s = CreateInstance<ProcessingSettings>();
            s.name = "ProcessingSettings (default)";
            return s;
        }
    }
}
