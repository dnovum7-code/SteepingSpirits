using System;
using System.Collections.Generic;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Processing;

namespace SteepingSpirits.Brewing.Integration
{
    /// <summary>A tea of the player's own (workshop tea or blend) as the brewing table offers it.</summary>
    public sealed class OwnTea
    {
        /// <summary>Ingredient id (tea_oolong:dark/roasty+q3, tea_blend:green~jasmine/…).</summary>
        public string ingredientId = "";
        public string displayName = "";
        public int count;
        public ProcessedTeaInfo info;
        public List<TeaNote> notes = new List<TeaNote>();
        public bool isBlend;
    }

    /// <summary>
    /// Engine-free translation of a workshop tea id into what brewing needs (base tea, oxidation, tier,
    /// notes). No Unity types, so it is tested with dotnet together with both cores (Tests/TeaLoop.DotNet).
    /// </summary>
    public static class OwnTeaInfo
    {
        /// <summary>Item a tart cup leaves behind; the garden compost bin takes it (GardenSettings.compostInputIds).</summary>
        public const string TartItemId = "tea_tart";

        public static BrewBase BaseOf(TeaVariety v)
        {
            switch (v)
            {
                case TeaVariety.White: return BrewBase.White;
                case TeaVariety.Green: return BrewBase.Green;
                case TeaVariety.Black: return BrewBase.Black;
                default: return BrewBase.Oolong;
            }
        }

        /// <summary>TeaParams.id of the TeaDefinition preset for a base tea.</summary>
        public static string PresetId(BrewBase b)
        {
            switch (b)
            {
                case BrewBase.White: return "white";
                case BrewBase.Green: return "green";
                case BrewBase.Black: return "black";
                default: return "oolong";
            }
        }

        public static BrewNotes NotesOf(IEnumerable<TeaNote> notes)
        {
            BrewNotes n = BrewNotes.None;
            if (notes == null) return n;
            foreach (TeaNote note in notes)
            {
                switch (note)
                {
                    case TeaNote.Fresh: n |= BrewNotes.Fresh; break;
                    case TeaNote.Floral: n |= BrewNotes.Floral; break;
                    case TeaNote.Honeyed: n |= BrewNotes.Honeyed; break;
                    case TeaNote.Malty: n |= BrewNotes.Malty; break;
                    case TeaNote.Roasty: n |= BrewNotes.Roasty; break;
                    case TeaNote.Delicate: n |= BrewNotes.Delicate; break;
                }
            }

            return n;
        }

        /// <summary>
        /// Reads a workshop tea or blend id. Oxidation comes from the workshop ledger (exact average) when
        /// known, else the sweet spot of the variety.
        /// </summary>
        public static bool TryRead(string ingredientId, Func<string, float?> ledgerOxidation, VarietyParams varieties, out OwnTea tea)
        {
            tea = null;
            varieties = varieties ?? new VarietyParams();
            if (ProcessedTea.TryParseId(ingredientId, out TeaVariety v, out List<TeaNote> notes, out int tier))
            {
                float? ox = ledgerOxidation?.Invoke(ingredientId);
                tea = new OwnTea
                {
                    ingredientId = ingredientId,
                    displayName = ProcessingTexts.TeaName(v, notes, tier),
                    notes = notes,
                    info = new ProcessedTeaInfo { baseTea = BaseOf(v), oxidation = ox ?? TeaClassifier.SweetSpot(v, varieties), tier = tier, notes = NotesOf(notes) }
                };
                return true;
            }

            if (TeaBlend.TryParseId(ingredientId, out TeaBlend blend))
            {
                tea = new OwnTea
                {
                    ingredientId = ingredientId,
                    displayName = ProcessingTexts.BlendName(blend),
                    notes = blend.notes,
                    isBlend = true,
                    info = new ProcessedTeaInfo { baseTea = BaseOf(blend.tea), oxidation = TeaClassifier.SweetSpot(blend.tea, varieties), tier = blend.tier, notes = NotesOf(blend.notes) }
                };
                return true;
            }

            return false;
        }

        /// <summary>The tea that is actually brewed: base preset adjusted, with a stable id per workshop tea.</summary>
        public static TeaParams Brewable(TeaParams preset, OwnTea tea, ProcessedBrewParams k)
        {
            TeaParams t = ProcessedBrewing.ForProcessedTea(preset, tea.info, k);
            t.id = "own:" + tea.ingredientId;
            return t;
        }
    }
}
