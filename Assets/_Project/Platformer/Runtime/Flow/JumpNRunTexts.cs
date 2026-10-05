using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>All player-facing texts of the Jump'n'Run in one place (German).</summary>
    public static class JumpNRunTexts
    {
        // Options menu
        public const string OptionsTitle = "Pause · Komfort";
        public const string OptionsHint = "W/S wählen · A/D ändern · Esc schließt";
        public const string GameSpeed = "Spieltempo";
        public const string GameSpeedHelp = "Alles läuft ruhiger. Gut zum Üben schwieriger Stellen.";
        public const string ExtraAirJump = "Extra-Luftsprung";
        public const string ExtraAirJumpHelp = "Ein zusätzlicher Sprung in der Luft.";
        public const string FallProtection = "Absturzschutz";
        public const string FallProtectionHelp = "Nach einem Sturz geht es dort weiter, wo du zuletzt sicher standest.";
        public const string On = "an";
        public const string Off = "aus";
        public const string Resume = "Weiter";
        public const string BackToMeadow = "Zurück zur Wiese";
        public const string MenuHint = "Esc: Pause";

        // Ingredients
        public static string IngredientName(string id)
        {
            switch (id)
            {
                case IngredientIds.TeaLeaf: return "Teeblatt";
                case IngredientIds.Herb: return "Wiesenkraut";
                case IngredientIds.Blossom: return "Blüte";
                case IngredientIds.MorningDew: return "Morgentau";
                case IngredientIds.SpringWater: return "Quellwasser";
                case IngredientIds.GoldenTip: return "Goldspitze";
                case IngredientIds.MoonHerb: return "Mondkraut";
                case IngredientIds.SpiritBlossom: return "Geisterblüte";
                case IngredientIds.StarDew: return "Sternentau";
                case IngredientIds.SpringCrystal: return "Quellkristall";
                default: return id;
            }
        }

        // End-of-level card
        public const string EndTitle = "Sammelausflug beendet";
        public const string EndCollected = "Im Beutel";
        public const string EndNothing = "Diesmal nur frische Luft geschnappt – auch schön.";
        public const string EndRare = "selten";
        public const string NextLevel = "Weiter";
        public const string Again = "Nochmal";
        public static string LanternsLit(int lit, int total) => $"Laternen entzündet: {lit} von {total}";
        public static string Found(int found, int available) => available > 0 ? $"{found} / {available}" : $"{found}";

        public static string Percent(float value) => $"{UnityEngine.Mathf.RoundToInt(value * 100f)} %";

        public static string OnOff(bool value) => value ? On : Off;
    }
}
