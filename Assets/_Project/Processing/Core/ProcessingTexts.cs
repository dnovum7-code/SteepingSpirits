using System.Collections.Generic;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// All player-facing texts of the tea workshop (German), in one place. The batch state is told
    /// through colour, scent and feel – numbers only appear when the player switches them on.
    /// </summary>
    public static class ProcessingTexts
    {
        public const string WorkshopName = "Teewerkstatt";
        public const string GardenDoorLabel = "Teewerkstatt betreten";
        public const string ExitLabel = "Zurück in den Garten";
        public const string ShelfName = "Regal";
        public const string StoreName = "Vorrat";
        public const string RackName = "Welkgestell";
        public const string RollName = "Rolltisch";
        public const string BasketName = "Oxidationskorb";
        public const string WokName = "Wok";
        public const string DryerName = "Trockenkorb über der Glut";
        public const string BlendTableName = "Mischtisch";

        public const string Close = "Schließen";
        public const string NewBatch = "Neue Charge anlegen";
        public const string StarterPack = "Startpaket nehmen";
        public const string StarterTaken = "Ein Körbchen frischer Blätter steht bereit.";
        public const string NoLeaves = "Keine frischen Teeblätter im Vorrat – im Teegarten ernten.";
        public const string Empty = "Hier liegt gerade nichts.";
        public const string ShelfEmpty = "Das Regal ist leer. Am Vorrat eine neue Charge anlegen.";
        public const string LayOn = "Auflegen";
        public const string TakeOff = "Abnehmen";
        public const string StopOxidation = "Jetzt stoppen";
        public const string Pack = "Einpacken";
        public const string RollStart = "Rollen";
        public const string RollStop = "Fertig rollen";
        public const string RollHint = "Im Takt kneten: Leertaste / J / A – oder den Stick kreisen.";
        public const string RollComfortHint = "Gleichmäßig rollen: Taste halten.";
        public const string WokHint = "Halten: Glut anfachen · Loslassen: abkühlen. Die Hitze im hellen Band halten.";
        public const string WokStart = "In den Wok";
        public const string PercentOn = "Zahlen an";
        public const string PercentOff = "Zahlen aus";
        public const string ComfortOn = "Gleichmäßig rollen: an";
        public const string ComfortOff = "Gleichmäßig rollen: aus";
        public const string DebugForward = "Debug: Zeit vorgespult";
        public const string ToPantry = " (in den Vorratsschrank)";
        public const string SaveFailed = "Die Werkstatt konnte nicht gespeichert werden.";
        public const string NoGarden = "Der Weg in den Garten ist noch nicht gebaut (Szene fehlt).";
        public const string KeysLine = "Leertaste/E benutzen · Esc/B schließen · F1 Debug";

        public static string VarietyName(TeaVariety t)
        {
            switch (t)
            {
                case TeaVariety.White: return "Weißtee";
                case TeaVariety.Green: return "Grüntee";
                case TeaVariety.LightOolong: return "heller Oolong";
                case TeaVariety.DarkOolong: return "dunkler Oolong";
                default: return "Schwarztee";
            }
        }

        public static string NoteName(TeaNote n)
        {
            switch (n)
            {
                case TeaNote.Fresh: return "frisch";
                case TeaNote.Floral: return "blumig";
                case TeaNote.Honeyed: return "honigsüß";
                case TeaNote.Malty: return "malzig";
                case TeaNote.Roasty: return "röstig";
                default: return "zart";
            }
        }

        public static string NotesLine(IList<TeaNote> notes)
        {
            if (notes == null || notes.Count == 0) return "noch keine besondere Note";
            var sb = new StringBuilder();
            for (int i = 0; i < notes.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(NoteName(notes[i]));
            }

            return sb.ToString();
        }

        public static string QualityName(int tier) => IngredientQuality.Label(tier);

        /// <summary>"Oolong (dunkel; honigsüß, röstig; erlesen)" – the inventory name of a workshop tea.</summary>
        public static string TeaName(TeaVariety t, IList<TeaNote> notes, int tier)
        {
            string name = IngredientTexts.Name(TeaClassifier.IngredientId(t));
            var parts = new List<string>();
            if (t == TeaVariety.LightOolong) parts.Add("hell");
            if (t == TeaVariety.DarkOolong) parts.Add("dunkel");
            if (notes != null && notes.Count > 0) parts.Add(NotesLine(notes));
            if (tier > 1) parts.Add(QualityName(tier));
            return parts.Count == 0 ? name : name + " (" + string.Join("; ", parts) + ")";
        }

        public static string TeaNameOfId(string id)
        {
            return ProcessedTea.TryParseId(id, out TeaVariety v, out List<TeaNote> notes, out int tier) ? TeaName(v, notes, tier) : IngredientQuality.DisplayName(id);
        }

        public static string SourceName(string sourceId, int tier)
        {
            string name = IngredientTexts.Name(sourceId);
            return tier > 1 ? name + " (" + QualityName(tier) + ")" : name;
        }

        public static string BatchTitle(LeafBatch b) => "Charge " + b.id + " · " + b.amount + "× " + SourceName(b.sourceId, b.originTier);

        // ---------------- Reading the leaves ----------------

        /// <summary>Colour of the leaves, from green over copper to dark brown.</summary>
        public static string Colour(LeafBatch b)
        {
            float ox = b.oxidation;
            if (ox < 0.05f) return b.moisture > 0.85f && b.dryness <= 0f ? "sattgrün" : "jadegrün";
            if (ox < 0.12f) return "grün mit hellen Rändern";
            if (ox < 0.25f) return "grün, die Ränder färben sich rötlich";
            if (ox < 0.40f) return "olivgrün mit Kupfer";
            if (ox < 0.60f) return "kupferfarben";
            if (ox < 0.80f) return "rotbraun";
            return "dunkelbraun";
        }

        /// <summary>Scent: grassy → floral → fruity → malty, with roast on top.</summary>
        public static string Scent(LeafBatch b)
        {
            float ox = b.oxidation;
            string s;
            if (ox < 0.08f) s = b.moisture > 0.85f ? "grasig" : "frisch wie Heu";
            else if (ox < 0.30f) s = "blumig";
            else if (ox < 0.55f) s = "fruchtig, nach Steinobst";
            else if (ox < 0.80f) s = "warm und süßlich";
            else s = "malzig";
            if (b.roast >= 0.25f) s += ", geröstet";
            else if (b.roast >= 0.08f) s += ", ein Hauch Honig";
            return s;
        }

        /// <summary>How the leaves feel.</summary>
        public static string Feel(LeafBatch b, ProcessingParams p)
        {
            if (b.dryness >= p.dry.doneAt) return b.roast >= 0.25f ? "knisternd trocken, leicht geröstet" : "knisternd trocken";
            if (b.dryness > 0.5f) return "fast trocken, ledrig";
            if (b.dryness > 0f) return "werden trockener";
            string shape = b.rolled >= 0.6f ? "fest gerollt" : b.rolled > 0f ? "leicht gedreht" : "flach";
            string touch;
            if (b.moisture > p.wither.witheredMax) touch = "prall und frisch";
            else if (b.moisture > p.wither.idealMax) touch = "werden weich";
            else if (b.moisture >= p.wither.idealMin) touch = "weich und geschmeidig";
            else touch = "schlaff und welk";
            return shape + ", " + touch;
        }

        public static string Place(BatchPlace place)
        {
            switch (place)
            {
                case BatchPlace.WitherRack: return "liegt auf dem Welkgestell";
                case BatchPlace.OxidationBasket: return "oxidiert im Korb";
                case BatchPlace.DryBasket: return "trocknet über der Glut";
                default: return "liegt im Regal";
            }
        }

        /// <summary>One sentence: what the batch is now.</summary>
        public static string State(LeafBatch b, ProcessingParams p, bool numbers)
        {
            string s = Colour(b) + ", duftet " + Scent(b) + ", " + Feel(b, p) + ".";
            if (!numbers) return s;
            return s + $"  [Feuchte {b.moisture * 100f:0} % · Rollgrad {b.rolled * 100f:0} % · Oxidation {b.oxidation * 100f:0} % · Trocken {System.Math.Min(1f, b.dryness) * 100f:0} %]";
        }

        /// <summary>Where the path leads, if the player dries it now (or stops oxidation now).</summary>
        public static string Heading(LeafBatch b, ProcessingParams p)
        {
            if (b.isHerb) return (b.IsDry(p.dry) ? "Fertig: " : "Wird zu: ") + IngredientTexts.Name(DriedHerb.DriedId(b.sourceId));
            TeaVariety t = TeaClassifier.Heading(b, p);
            return (b.IsDry(p.dry) ? "Fertig: " : "Würde jetzt zu: ") + VarietyName(t);
        }

        public static string NotesPreview(LeafBatch b, ProcessingParams p) => b.isHerb ? "" : "Noten: " + NotesLine(TeaNotes.For(b, p));

        public const string HerbsHeader = "Kräuter und Blüten zum Trocknen:";
        public const string NewHerbBatch = "Trocknen";

        public static string HerbDone(DriedHerb h) => "+" + h.amount + " " + IngredientQuality.DisplayName(h.Id);

        public static string Reason(BatchReason r)
        {
            switch (r)
            {
                case BatchReason.NotOnShelf: return "Die Charge liegt gerade woanders.";
                case BatchReason.AlreadyOnShelf: return "Liegt schon im Regal.";
                case BatchReason.NotWithered: return "Die Blätter sind noch zu frisch – erst welken lassen.";
                case BatchReason.AlreadyRolled: return "Gerollte Blätter welken nicht mehr.";
                case BatchReason.RolledEnough: return "Die Blätter sind genug gerollt.";
                case BatchReason.NotRolled: return "Nur gerollte Blätter oxidieren im Korb.";
                case BatchReason.AlreadyFixed: return "Schon im Wok fixiert.";
                case BatchReason.TooDry: return "Dafür sind die Blätter schon zu trocken.";
                case BatchReason.AlreadyDry: return "Schon trocken.";
                case BatchReason.NotDry: return "Noch nicht trocken.";
                case BatchReason.HerbOnlyDries: return "Kräuter und Blüten werden nur getrocknet.";
                default: return "";
            }
        }

        public static string Finished(ProcessedTea t) => "+" + t.amount + " " + TeaName(t.variety, t.notes, t.tier);

        public static string StarterLine(string sourceId, int tier, int count) => count + "× " + SourceName(sourceId, tier);

        public static string Amount(int n) => "Menge: " + n;

        public static string RollWords(float intensity, float rhythm, bool numbers)
        {
            string roll = intensity < 0.3f ? "kaum gerollt" : intensity < 0.6f ? "leicht gedreht – gut für Oolong" : intensity < 0.9f ? "gut gerollt" : "fest gerollt";
            string beat = rhythm >= 0.9f ? "gleichmäßiger Takt" : rhythm >= 0.6f ? "etwas unruhig" : "ungleichmäßig";
            string s = "Die Blätter sind " + roll + ".\n" + beat + ".";
            return numbers ? s + $"\n[Rollgrad {intensity * 100f:0} % · Takt {rhythm * 100f:0} %]" : s;
        }

        public static string WokWords(float heat, float min, float max, bool numbers)
        {
            string where = heat < min ? "zu kalt – anfachen" : heat > max ? "zu heiß – loslassen" : "genau richtig";
            string s = WokHeat(heat, numbers) + "\n" + where + ".";
            return numbers ? s + $"\n[Hitze {heat * 100f:0} % · Fenster {min * 100f:0}–{max * 100f:0} %]" : s;
        }

        public const string RollDone = "Fertig gerollt.";
        public const string WokDone = "Die Blätter sind fixiert – die Oxidation steht.";
        public const string Stopped = "Oxidation gestoppt.";
        public const string BlendSoon = "Hier entstehen Mischungen aus Tee und getrockneten Kräutern.";
        public const string BlendIntro = "Ein Teil Tee und ein Teil getrocknete Kräuter oder Blüten ergeben zwei Teile Mischung.";
        public const string BlendNothing = "Dafür braucht es fertigen Tee und getrocknete Kräuter (Trockenkorb).";
        public const string BlendVerb = "Mischen";

        /// <summary>"Jasmin-Grüntee (blumig, frisch; fein)".</summary>
        public static string BlendName(TeaBlend b)
        {
            string prefix;
            switch (b.herbId)
            {
                case "mint": prefix = "Minz"; break;
                case "chamomile": prefix = "Kamillen"; break;
                case "jasmine": prefix = "Jasmin"; break;
                case "lavender": prefix = "Lavendel"; break;
                default: prefix = "Melissen"; break;
            }

            string tea = b.tea == TeaVariety.LightOolong || b.tea == TeaVariety.DarkOolong ? "Oolong" : VarietyName(b.tea);
            var parts = new List<string>();
            if (b.notes.Count > 0) parts.Add(NotesLine(b.notes));
            if (b.tier > 1) parts.Add(QualityName(b.tier));
            return prefix + "-" + tea + (parts.Count > 0 ? " (" + string.Join("; ", parts) + ")" : "");
        }

        public static string BlendDone(TeaBlend b, int n) => "+" + n + " " + BlendName(b);

        public static string BatchStarted(int id) => "Charge " + id + " liegt im Regal.";
        public static string Act(string verb, int id) => verb + ": Charge " + id;
        public const string PutInBasket = "In den Korb";
        public const string PutOnEmbers = "Über die Glut";
        public const string Unpack = "Als Tee einpacken";

        public static string WokHeat(float heat, bool numbers) =>
            heat < 0.3f ? "Die Pfanne ist lauwarm." : heat < 0.6f ? "Es zischt leise." : heat < 0.8f ? "Es zischt kräftig." : "Es duftet schon röstig.";
    }
}
