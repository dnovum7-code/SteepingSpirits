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

        public static string Percent(float value) => $"{UnityEngine.Mathf.RoundToInt(value * 100f)} %";

        public static string OnOff(bool value) => value ? On : Off;
    }
}
