using System;

namespace SteepingSpirits.Processing
{
    /// <summary>Withering on the rack (passive). Moisture 1 = fresh leaves.</summary>
    [Serializable]
    public sealed class WitherParams
    {
        /// <summary>Moisture the leaves approach but never go below while withering.</summary>
        public float floor = 0.45f;

        /// <summary>Time constant (game minutes) of the exponential water loss on the rack. 275 → about 4 game hours to 0.68.</summary>
        public float tauMinutes = 275f;

        /// <summary>Water loss off the rack (shelf, basket) relative to the rack.</summary>
        public float offRackFactor = 0.25f;

        /// <summary>Leaves count as withered (ready to roll, fix or dry) at or below this moisture.</summary>
        public float witheredMax = 0.85f;

        /// <summary>Best moisture window when the leaves are worked on.</summary>
        public float idealMin = 0.60f;
        public float idealMax = 0.72f;

        /// <summary>Care falls to zero this far outside the ideal window.</summary>
        public float careFalloff = 0.15f;

        public WitherParams Clone() => (WitherParams)MemberwiseClone();
    }

    /// <summary>The rolling table (active).</summary>
    [Serializable]
    public sealed class RollParams
    {
        /// <summary>Kneads for a full roll (intensity 1).</summary>
        public int kneadsForFullRoll = 16;

        /// <summary>Longest rolling session (seconds); the player may stop earlier.</summary>
        public float maxSeconds = 18f;

        /// <summary>Ideal time between two kneads (seconds).</summary>
        public float beatSeconds = 0.75f;

        /// <summary>Timing error (seconds) that still counts as fully even.</summary>
        public float perfectTolerance = 0.08f;

        /// <summary>Timing error at which a knead counts as uneven (evenness of that knead = floor).</summary>
        public float sloppyTolerance = 0.35f;

        /// <summary>Lowest evenness a single knead can get – mistakes only make it a bit uneven.</summary>
        public float evennessFloor = 0.35f;

        /// <summary>Comfort "Gleichmäßig rollen" (hold instead of rhythm): evenness given.</summary>
        public float comfortEvenness = 0.85f;

        /// <summary>Rolling breaks the cells: oxidation added per unit of intensity.</summary>
        public float oxidationKick = 0.04f;

        /// <summary>Moisture squeezed out per full roll.</summary>
        public float moistureLoss = 0.03f;

        /// <summary>A roll below this intensity is "light" (oolong).</summary>
        public float lightRollMax = 0.6f;

        /// <summary>Rolls allowed per batch (oolong: before and after the wok).</summary>
        public int maxRolls = 2;

        public RollParams Clone() => (RollParams)MemberwiseClone();
    }

    /// <summary>Oxidation (in the basket it is watched; it also runs slowly anywhere else).</summary>
    [Serializable]
    public sealed class OxidationParams
    {
        /// <summary>Oxidation per game minute of fully rolled leaves.</summary>
        public float ratePerMinute = 0.012f;

        /// <summary>Share of the rate that does not depend on how much the leaves were rolled.</summary>
        public float rollIndependentShare = 0.35f;

        /// <summary>Unrolled leaves oxidize only very slowly (relative to the full rate).</summary>
        public float unrolledFactor = 0.008f;

        /// <summary>Rolled leaves taken out of the basket (not yet fixed or dried) keep oxidizing this much slower.</summary>
        public float setAsideFactor = 0.15f;

        /// <summary>A weak wok fix leaves this much of the rate per unit of "too cold".</summary>
        public float weakFixResidual = 0.4f;

        /// <summary>Drying stops oxidation completely at this dryness.</summary>
        public float dryStopsAt = 0.5f;

        public OxidationParams Clone() => (OxidationParams)MemberwiseClone();
    }

    /// <summary>The wok (active): keep the heat inside a slowly wandering window.</summary>
    [Serializable]
    public sealed class WokParams
    {
        public float seconds = 12f;

        /// <summary>Width of the good heat window (heat 0..1).</summary>
        public float windowWidth = 0.24f;

        /// <summary>How far and how fast the window wanders.</summary>
        public float windowSwing = 0.18f;
        public float windowSpeed = 0.45f;

        /// <summary>Heat change per second while stoking / while resting.</summary>
        public float heatRise = 0.55f;
        public float heatFall = 0.40f;

        /// <summary>Heat inertia: how quickly the heat follows (1/s).</summary>
        public float response = 4f;

        /// <summary>Roast added per unit of time share spent above the window.</summary>
        public float roastPerOverheat = 0.6f;

        public WokParams Clone() => (WokParams)MemberwiseClone();
    }

    /// <summary>The drying basket over embers (passive).</summary>
    [Serializable]
    public sealed class DryParams
    {
        /// <summary>Game minutes from withered leaves to fully dry.</summary>
        public float minutesToDry = 120f;

        /// <summary>Dryness at which the tea is finished (keeps).</summary>
        public float doneAt = 0.95f;

        /// <summary>Roast per game minute once fully dry ("a little longer gives a light roast").</summary>
        public float overDryRoastPerMinute = 0.0015f;

        /// <summary>Most roast over-drying can add.</summary>
        public float overDryRoastCap = 0.45f;

        public DryParams Clone() => (DryParams)MemberwiseClone();
    }

    /// <summary>Oxidation bands of the teas. Gaps between bands go to the nearest band.</summary>
    [Serializable]
    public sealed class VarietyParams
    {
        /// <summary>Below this: white or green (depending on the path).</summary>
        public float paleMax = 0.10f;

        public float lightOolongMin = 0.10f;
        public float lightOolongMax = 0.30f;
        public float darkOolongMin = 0.30f;
        public float darkOolongMax = 0.70f;
        public float blackMin = 0.80f;

        /// <summary>Leaves rolled at least this much count as "rolled" for the white/green decision.</summary>
        public float rolledMin = 0.15f;

        /// <summary>Sweet spots: stopping here gives the best care for that tea.</summary>
        public float paleSweet = 0.02f;
        public float lightOolongSweet = 0.20f;
        public float darkOolongSweet = 0.50f;
        public float blackSweet = 0.92f;

        public VarietyParams Clone() => (VarietyParams)MemberwiseClone();
    }

    /// <summary>How care and origin become one of three quality tiers.</summary>
    [Serializable]
    public sealed class TeaQualityParams
    {
        /// <summary>Weight of the leaf's garden quality (rest = care in the workshop).</summary>
        public float originWeight = 0.3f;

        public float fineScore = 0.5f;
        public float excellentScore = 0.8f;

        /// <summary>
        /// Distance from the sweet spot at which oxidation care reaches zero (squared fall-off, so close
        /// is nearly perfect). Oolongs use half their band width; pale and black teas these values.
        /// </summary>
        public float paleTolerance = 0.08f;

        public float blackTolerance = 0.12f;

        /// <summary>Highest tier for ordinary leaves below "erlesen" garden quality. First flush lifts it to 3.</summary>
        public int ordinaryCeiling = 2;

        public TeaQualityParams Clone() => (TeaQualityParams)MemberwiseClone();
    }

    /// <summary>When the character notes appear.</summary>
    [Serializable]
    public sealed class NoteParams
    {
        public float roastyFrom = 0.25f;
        public float honeyedRoastFrom = 0.08f;
        public float floralOxMin = 0.08f;
        public float floralOxMax = 0.40f;
        public float floralEvenness = 0.6f;
        public float maltyOxFrom = 0.70f;
        public float maltyRollFrom = 0.6f;
        public float freshOxMax = 0.06f;
        public float freshMoistureFrom = 0.68f;
        public float honeyedOxMin = 0.40f;
        public float honeyedOxMax = 1.00f;

        /// <summary>White tea withered this dry (long wither) turns floral.</summary>
        public float longWitherMoisture = 0.60f;

        public int maxNotes = 3;

        public NoteParams Clone() => (NoteParams)MemberwiseClone();
    }

    /// <summary>Pu-erh preparation (data only for now – no maturation yet).</summary>
    [Serializable]
    public sealed class PuErhParams
    {
        /// <summary>Maocha must be fixed gently: at least this much oxidation left after the wok ("too cool" on purpose).</summary>
        public float minFixResidual = 0.05f;

        /// <summary>Maocha is rolled firmly.</summary>
        public float minRolled = 0.6f;

        /// <summary>Maocha stays a pale, green-ish tea.</summary>
        public float maxOxidation = 0.30f;

        /// <summary>Portions pressed into one brick.</summary>
        public int portionsPerBrick = 4;

        public PuErhParams Clone() => (PuErhParams)MemberwiseClone();
    }

    /// <summary>When the workshop spirit takes over passive steps the player has mastered.</summary>
    [Serializable]
    public sealed class SpiritParams
    {
        /// <summary>Finished teas withered inside the ideal window before the spirit takes leaves off the rack.</summary>
        public int witherMasteredAfter = 3;

        /// <summary>Finished batches before the spirit takes dry batches out of the drying basket.</summary>
        public int dryMasteredAfter = 3;

        /// <summary>White teas of at least "fein" before the spirit may carry a batch from the rack to the dryer.</summary>
        public int whiteMasteredAfter = 2;

        /// <summary>Wither care that counts as "mastered" for one tea.</summary>
        public float goodWitherCare = 0.9f;

        public SpiritParams Clone() => (SpiritParams)MemberwiseClone();
    }

    /// <summary>All workshop tuning (lives in the ProcessingSettings asset in Unity).</summary>
    [Serializable]
    public sealed class ProcessingParams
    {
        public WitherParams wither = new WitherParams();
        public RollParams roll = new RollParams();
        public OxidationParams oxidation = new OxidationParams();
        public WokParams wok = new WokParams();
        public DryParams dry = new DryParams();
        public VarietyParams variety = new VarietyParams();
        public TeaQualityParams quality = new TeaQualityParams();
        public NoteParams notes = new NoteParams();
        public PuErhParams puerh = new PuErhParams();
        public SpiritParams spirit = new SpiritParams();

        /// <summary>Game minutes per real second of the game clock (10 per 7 s) – for time estimates only.</summary>
        public float gameMinutesPerRealSecond = 10f / 7f;

        /// <summary>Largest time step used when integrating passive changes (game minutes).</summary>
        public float integrationStep = 2f;

        public ProcessingParams Clone() => new ProcessingParams
        {
            wither = wither.Clone(),
            roll = roll.Clone(),
            oxidation = oxidation.Clone(),
            wok = wok.Clone(),
            dry = dry.Clone(),
            variety = variety.Clone(),
            quality = quality.Clone(),
            notes = notes.Clone(),
            puerh = puerh.Clone(),
            spirit = spirit.Clone(),
            gameMinutesPerRealSecond = gameMinutesPerRealSecond,
            integrationStep = integrationStep
        };
    }
}
