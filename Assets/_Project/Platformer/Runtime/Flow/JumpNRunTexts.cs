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
        public const string AutoSwing = "Auto-Schwung";
        public const string AutoSwingHelp = "Schaukeln: eine Richtung halten genügt, der Takt kommt von allein.";
        public const string On = "an";
        public const string Off = "aus";
        public const string Resume = "Weiter";
        public const string BackToMeadow = "Zurück zur Wiese";
        public const string Back = "Zurück";
        public const string BackToClearing = "Zur Lichtung";
        public const string MeadowPortalLabel = "Zur Lichtung der Geister";
        public const string MenuHint = "Esc: Pause";

        // Ingredients
        /// <summary>Names come from the shared catalogue texts.</summary>
        public static string IngredientName(string id) => SteepingSpirits.Ingredients.IngredientTexts.Name(id);

        // End-of-level card
        public const string EndTitle = "Sammelausflug beendet";
        public const string EndCollected = "Im Beutel";
        public const string EndNothing = "Diesmal nur frische Luft geschnappt – auch schön.";
        public const string EndRare = "selten";
        public const string HandedToInventory = "Alles liegt jetzt in deinem Inventar.";
        public const string HandedToPantry = "Alles steht jetzt im Vorratsschrank des Teehauses.";
        public const string NextLevel = "Weiter";
        public const string Again = "Nochmal";
        public static string PathLanterns(int lit, int total) => $"Laternenpfad: {lit} von {total}";
        public const string ChallengeDone = "Alle Pfadlaternen leuchten – ein Quellkristall als Dank!";
        public static string LanternsLit(int lit, int total) => $"Laternen entzündet: {lit} von {total}";
        public static string Found(int found, int available) => available > 0 ? $"{found} / {available}" : $"{found}";

        // Spirit NPCs: one line each, chosen in the level file with "@npc<index> <key>".
        public static string NpcLine(string key)
        {
            switch (key)
            {
                case "meadow_hello": return "Na, auch auf Sammeltour? Die Blätter hier riechen nach Morgen.";
                case "meadow_spring": return "Das Quellwasser da vorn ist kühl wie eine alte Geschichte.";
                case "grove_wind": return "Der Wind hier hat Launen. Lass dich einfach tragen.";
                case "grove_swing": return "Auf der Schaukel nicht drängeln – mitschwingen!";
                case "grove_light": return "Ohne Licht sind die Stufen nur eine Erinnerung.";
                case "hub_welcome": return "Willkommen auf der Lichtung. Jede Tür führt zu einem Sammelplatz.";
                default: return "…";
            }
        }

        public static string DoorLabel(string levelName) => string.IsNullOrEmpty(levelName) ? "Tür" : levelName;
        public const string DoorMeadow = "Zur Wiese";
        public const string DoorClimb = "Kletterpfad";
        public const string DoorHint = "W: hinein";
        public const string DoorLocked = "noch verschlossen – erst den Weg davor gehen";

        public static string Percent(float value) => $"{UnityEngine.Mathf.RoundToInt(value * 100f)} %";

        public static string OnOff(bool value) => value ? On : Off;
    }
}
