using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Plantation
{
    /// <summary>The five garden tools (toolbar order = keys 1–5).</summary>
    public enum GardenTool
    {
        Hoe,
        Seeds,
        Can,
        Hand,
        Shovel
    }

    /// <summary>All player-facing texts of the tea garden (German), in one place.</summary>
    public static class GardenTexts
    {
        public const string GardenName = "Teegarten";
        public const string MeadowPortalLabel = "Teegarten betreten";
        public const string GateLabel = "Zur Wiese";
        public const string WellLabel = "Brunnen: Kanne füllen";
        public const string LanternLabel = "Laterne: Tag beenden";
        public const string ChestLabel = "Samenkiste öffnen";
        public const string CompostLabel = "Komposter";
        public const string EndDayConfirm = "Nochmal drücken, um den Tag zu beenden.";
        public const string CanFull = "Die Kanne ist schon voll.";
        public const string CanRefilled = "Die Kanne ist wieder voll.";
        public const string ChestEmpty = "Die Kiste ist leer.";
        public const string ChestStarter = "In der Kiste liegt ein Samenpäckchen für dich.";
        public const string BagFull = "Deine Tasche ist voll – der Rest bleibt in der Samenkiste.";
        public const string SeedsRestored = "Dein Saatgut aus dem Garten ist wieder da.";
        public const string NoSeeds = "Kein Saatgut dabei – schau in die Samenkiste.";
        public const string WideCanOn = "Gießkanne: 1×3 (Feld vor dir und die zwei dahinter)";
        public const string WideCanOff = "Gießkanne: einzelnes Feld";
        public const string HelpOn = "Gartenhilfe an: Tropfen zeigen, wer heute Wasser möchte.";
        public const string HelpOff = "Gartenhilfe aus.";
        public const string DebugNextDay = "Debug: nächster Tag";
        public const string SaveFailed = "Der Garten konnte nicht gespeichert werden.";

        public static string ToolName(GardenTool t)
        {
            switch (t)
            {
                case GardenTool.Hoe: return "Hacke";
                case GardenTool.Seeds: return "Saatbeutel";
                case GardenTool.Can: return "Gießkanne";
                case GardenTool.Hand: return "Hand";
                default: return "Schaufel";
            }
        }

        public static GardenAction ActionOf(GardenTool t)
        {
            switch (t)
            {
                case GardenTool.Hoe: return GardenAction.Hoe;
                case GardenTool.Seeds: return GardenAction.Sow;
                case GardenTool.Can: return GardenAction.Water;
                case GardenTool.Hand: return GardenAction.Harvest;
                default: return GardenAction.Shovel;
            }
        }

        /// <summary>What the tool would do – shown calmly above the target cell.</summary>
        public static string Effect(ActionEffect e, string plantId)
        {
            switch (e)
            {
                case ActionEffect.Till: return "Erde lockern";
                case ActionEffect.Sow: return PlantName(plantId) + " säen";
                case ActionEffect.Water: return "Gießen";
                case ActionEffect.Harvest: return "Ernten";
                case ActionEffect.Flatten: return "Erde glätten";
                case ActionEffect.DigUp: return "Ausgraben (Samen zurück)";
                case ActionEffect.Compost: return "Kompost einarbeiten";
                default: return "";
            }
        }

        /// <summary>Why the tool does nothing here – a hint, never an error.</summary>
        public static string Reason(ActionReason r)
        {
            switch (r)
            {
                case ActionReason.NotABed: return "Hier ist kein Beet.";
                case ActionReason.AlreadyTilled: return "Die Erde ist schon locker.";
                case ActionReason.PlantGrowing: return "Hier wächst schon etwas.";
                case ActionReason.NeedsTilling: return "Erst mit der Hacke lockern.";
                case ActionReason.AlreadyPlanted: return "Hier ist schon gesät.";
                case ActionReason.NoSeedSelected: return "Kein Saatgut gewählt (Q wechselt).";
                case ActionReason.NoSeedsLeft: return "Davon ist kein Saatgut mehr da.";
                case ActionReason.AlreadyWatered: return "Heute schon gegossen.";
                case ActionReason.CanEmpty: return "Die Kanne ist leer – am Brunnen füllen.";
                case ActionReason.NotRipe: return "Noch nicht reif.";
                case ActionReason.NothingHere: return "Hier gibt es nichts zu tun.";
                case ActionReason.HarvestFirst: return "Erst ernten.";
                case ActionReason.AlreadyComposted: return "Hier liegt schon Kompost.";
                case ActionReason.NoCompost: return "Kein Kompost dabei.";
                default: return "";
            }
        }

        public static string PlantName(string plantId)
        {
            switch (plantId)
            {
                case StarterPlants.Tea: return "Teestrauch";
                case StarterPlants.Mint: return "Minze";
                case StarterPlants.Chamomile: return "Kamille";
                case StarterPlants.Jasmine: return "Jasmin";
                case StarterPlants.Lavender: return "Lavendel";
                case StarterPlants.LemonBalm: return "Zitronenmelisse";
                default: return plantId ?? "";
            }
        }

        public static string SeedName(string plantId) =>
            plantId == StarterPlants.Tea ? "Teestrauch-Steckling" : PlantName(plantId) + "-Samen";

        public static string SeedDescription(string plantId) => "Saatgut für den Teegarten: " + PlantName(plantId) + ".";

        public static string WaterNeedLabel(WaterNeed w)
        {
            switch (w)
            {
                case WaterNeed.Thirsty: return "durstig";
                case WaterNeed.Frugal: return "genügsam";
                default: return "normal";
            }
        }

        public static string QualityLabel(HarvestQuality q) => IngredientQuality.Label((int)q);

        public static string Harvested(string ingredientId, HarvestQuality q, int count) =>
            "+" + count + " " + IngredientQuality.DisplayName(IngredientQuality.With(ingredientId, (int)q));

        public static string SeedFound(string plantId) => "+1 " + SeedName(plantId);

        public static string SeedBack(string plantId) => SeedName(plantId) + " zurück in den Beutel.";

        public static string StageLabel(PlantParams p, Crop c)
        {
            if (c.ripe) return "reif";
            if (c.regrowing) return "wächst nach";
            return c.stage == 0 ? "Saat" : "Stufe " + c.stage + " von " + p.RipeStage;
        }

        public static string DayLine(int day, bool rainy) => "Tag " + day + (rainy ? " · Regen" : "");

        public static string DateLine(string clockDate, bool rainy) => clockDate + (rainy ? " · Regen" : "");

        public static string SeedSlot(string name, int count) => name == null ? "leer" : name + " ×" + count;

        public static string Options(bool wideCan, bool help) =>
            "1×3-Kanne (R): " + (wideCan ? "an" : "aus") + "   ·   Gartenhilfe (H): " + (help ? "an" : "aus");

        public const string ToPantry = " (in den Vorratsschrank)";

        public static string Morning(int day, bool rainy, int ripe) =>
            "Tag " + day + (rainy ? " – es regnet, der Garten ist gegossen." : " beginnt.") +
            (ripe > 0 ? " " + ripe + (ripe == 1 ? " Pflanze ist reif." : " Pflanzen sind reif.") : "");

        public static string Can(int water, int capacity) => "Kanne " + water + "/" + capacity;

        public const string KeysLine = "1–5 Werkzeug · Leertaste/J benutzen · Q Saat wechseln · R 1×3-Kanne · H Gartenhilfe";
        public const string CompostMade = "Aus dem herben Tee wird Kompost.";
        public const string CompostNothing = "Hier kann herber Tee zu Kompost werden.";
        public const string CompostName = "Kompost";
        public const string CompostDescription = "Wertet ein Beet für die nächste Aussaat auf.";
    }
}
