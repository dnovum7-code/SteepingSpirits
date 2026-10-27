using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>
    /// All player-facing brewing texts in one place (German for the prototype).
    /// Swap this class for a localization table later.
    /// </summary>
    public static class BrewTexts
    {
        public const string ChooseTea = "Welchen Tee möchtest du brühen?";
        public const string HeatPrompt = "[F] Feuer an/aus   [S] zurückschöpfen   [W] Gefäß vorwärmen   [Leertaste] aufgießen   [Q] frisches Wasser";
        public const string KettleEmpty = "Der Kessel ist fast leer – [Q] frisches Wasser holen.";
        public const string SparkVisible = "✦ Ein Erinnerungsfunke! [E] fangen";
        public const string SparkFollowing = "✦ Du folgst der Erinnerung … (sie vertieft sich, solange der Tee zieht)";
        public const string MemoryCaught = "Eine Erinnerung schwingt im Tee mit.";
        public const string NextInfusionPrompt = "[Leertaste] dieselben Blätter noch einmal aufgießen";
        public const string LeavesSpent = "Die Blätter haben alles gegeben.";
        public const string PrewarmPrompt = "Das Gefäß wird vorgewärmt …";
        public const string PourPrompt = "Du gießt auf …";
        public const string SteepPrompt = "[Leertaste] Blätter herausheben";
        public const string ResultPrompt = "[1] [2] [3] nächster Tee   ·   [R] neu beginnen";
        public const string FireOn = "Feuer brennt";
        public const string FireOff = "Feuer aus";
        public const string Prewarmed = "Gefäß vorgewärmt";
        public const string StaleWater = "Das Wasser hat lange gekocht und schmeckt flach.";
        public const string Tart = "Ein herber Nachklang.";
        public const string ThermometerHint = "[T] Thermometer";

        public static string Infusion(int index) => $"{index + 1}. Aufguss";

        public static string Character(InfusionCharacter character)
        {
            switch (character)
            {
                case InfusionCharacter.Bright: return "hell und frisch";
                case InfusionCharacter.Robust: return "kräftig";
                case InfusionCharacter.Mellow: return "weich und rund";
                default: return "zart";
            }
        }

        public static string Tier(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Perfect: return "Vollendet";
                case QualityTier.Harmonious: return "Harmonisch";
                case QualityTier.Decent: return "Ordentlich";
                default: return "Fad";
            }
        }

        public static string TierMessage(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Perfect: return "Der Duft füllt still den Raum. Alles stimmt.";
                case QualityTier.Harmonious: return "Warm und rund – ein schöner Tee.";
                case QualityTier.Decent: return "Ein freundlicher Tee, der gut tut.";
                default: return "Ein leiser, zarter Tee. Danke, dass du ihn gebrüht hast.";
            }
        }

        public static string Hint(BrewHint hint)
        {
            switch (hint)
            {
                case BrewHint.TooShort: return "Tipp: Lass die Blätter etwas länger ziehen.";
                case BrewHint.TooLong: return "Tipp: Heb die Blätter etwas früher heraus.";
                case BrewHint.TooCold: return "Tipp: Dieser Tee mag heißeres Wasser – achte auf größere Blasen.";
                case BrewHint.TooHot: return "Tipp: Dieser Tee mag es sanfter – gieß bei kleineren Blasen auf.";
                case BrewHint.StaleWater: return "Tipp: Frisches Wasser, das nicht zu lange kocht, duftet mehr.";
                default: return "";
            }
        }

        public static string Stage(BoilStage stage)
        {
            switch (stage)
            {
                case BoilStage.ShrimpEyes: return "Garnelenaugen";
                case BoilStage.CrabEyes: return "Krabbenaugen";
                case BoilStage.FishEyes: return "Fischaugen";
                case BoilStage.StringOfPearls: return "Perlenschnur";
                case BoilStage.RagingWaves: return "Tosende Wellen";
                default: return "Still";
            }
        }
    }
}
