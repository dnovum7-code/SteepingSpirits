using System;

namespace SteepingSpirits.Plantation
{
    /// <summary>The soil of a bed cell.</summary>
    public enum Soil
    {
        Untouched,
        Tilled
    }

    /// <summary>Quality of a harvest (handed on with the ingredient).</summary>
    public enum HarvestQuality
    {
        Good = 1,
        Fine = 2,
        Excellent = 3
    }

    /// <summary>A plant growing in a bed cell.</summary>
    [Serializable]
    public sealed class Crop
    {
        public string plantId = "";

        /// <summary>Current growth stage (0-based). The last stage is the ripe one.</summary>
        public int stage;

        /// <summary>Growth days collected in the current stage (or towards regrowth).</summary>
        public int progress;

        public bool ripe;

        /// <summary>How often this plant was harvested (perennials).</summary>
        public int harvests;

        /// <summary>Perennials after a harvest: growing back towards the next harvest.</summary>
        public bool regrowing;

        /// <summary>"Normal" water type: the dry day this stage was forgiven already.</summary>
        public bool forgivenUsed;

        /// <summary>Care in the current cycle (since sowing or the last harvest).</summary>
        public int cycleDays;
        public int cycleGoodDays;

        /// <summary>Last day's watering (for the "every second day" rule of frugal plants).</summary>
        public bool wateredYesterday;

        /// <summary>Last 8 days, bit 0 = yesterday, 1 = watered (debug overlay).</summary>
        public int waterHistory;

        public Crop Clone() => (Crop)MemberwiseClone();

        /// <summary>Share of well-cared days in this cycle (1 when nothing happened yet).</summary>
        public float CareRatio => cycleDays == 0 ? 1f : cycleGoodDays / (float)cycleDays;
    }

    /// <summary>One bed cell of the garden.</summary>
    [Serializable]
    public sealed class Plot
    {
        public GridPos pos;
        public Soil soil = Soil.Untouched;

        /// <summary>Watered today (dries over night).</summary>
        public bool watered;

        /// <summary>Days tilled soil has been left empty (turns back to untouched after a while).</summary>
        public int idleDays;

        /// <summary>Compost worked in: the next sowing gets a quality bonus.</summary>
        public bool compost;

        public Crop crop;

        public bool HasCrop => crop != null;

        public Plot Clone()
        {
            var p = (Plot)MemberwiseClone();
            p.crop = crop?.Clone();
            return p;
        }
    }
}
