using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>
    /// Boil stages after Lu Yu. The player reads them from bubbles, sound and
    /// steam instead of a number.
    /// </summary>
    public enum BoilStage
    {
        Still,
        ShrimpEyes,
        CrabEyes,
        FishEyes,
        StringOfPearls,
        RagingWaves
    }

    /// <summary>Lower temperature bounds (°C) of each boil stage. Everything below ShrimpEyes is Still.</summary>
    [Serializable]
    public class BoilStageThresholds
    {
        public float shrimpEyes = 70f;
        public float crabEyes = 75f;
        public float fishEyes = 80f;
        public float stringOfPearls = 85f;
        public float ragingWaves = 95f;

        public BoilStage Classify(float temperature)
        {
            if (temperature >= ragingWaves) return BoilStage.RagingWaves;
            if (temperature >= stringOfPearls) return BoilStage.StringOfPearls;
            if (temperature >= fishEyes) return BoilStage.FishEyes;
            if (temperature >= crabEyes) return BoilStage.CrabEyes;
            if (temperature >= shrimpEyes) return BoilStage.ShrimpEyes;
            return BoilStage.Still;
        }
    }
}
