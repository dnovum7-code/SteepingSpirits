using System;

namespace SteepingSpirits.Brewing.Core
{
    /// <summary>Kettle water tuning. Serialized inside the BrewingTuning asset.</summary>
    [Serializable]
    public class WaterParams
    {
        /// <summary>Start and ambient temperature (°C).</summary>
        public float roomTemperature = 20f;

        /// <summary>Heating speed with the fire on (°C per second).</summary>
        public float heatingRate = 6f;

        /// <summary>Maximum water temperature (°C).</summary>
        public float boilingPoint = 100f;

        /// <summary>Exponential cooling rate towards room temperature without fire (1/s).</summary>
        public float coolingRate = 0.02f;

        /// <summary>Continuous boiling longer than this makes the water stale (s).</summary>
        public float staleAfterSeconds = 8f;

        /// <summary>Aroma ceiling multiplier while the water is stale.</summary>
        public float staleAromaFactor = 0.85f;

        /// <summary>Temperature drop when ladling water back (°C).</summary>
        public float ladleCooling = 5f;
    }
}
