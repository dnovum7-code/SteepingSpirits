using System;
using System.Collections.Generic;
using System.Text;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The blending table: one portion of a workshop tea and one of a dried herb or blossom become two
    /// portions of a blend (e.g. jasmine green tea) with its own notes.
    /// </summary>
    public class BlendStation
    {
        private const int MaxChoices = 8;
        protected readonly TeaWorkshop root;

        public BlendStation(TeaWorkshop root)
        {
            this.root = root;
        }

        public virtual void Fill(StringBuilder text, List<(string, Action)> actions)
        {
            text.Append(ProcessingTexts.BlendIntro).Append("\n\n");
            List<KeyValuePair<string, int>> teas = WorkshopItems.Holding(id => ProcessedTea.TryParseId(id, out _, out _, out _));
            List<KeyValuePair<string, int>> herbs = WorkshopItems.Holding(id => IngredientCatalog.Get(IngredientQuality.Plain(id))?.Family != IngredientFamily.Tea
                                                                                  && (IngredientQuality.Plain(id) ?? "").EndsWith("_dried", StringComparison.Ordinal));
            if (teas.Count == 0 || herbs.Count == 0)
            {
                text.Append(ProcessingTexts.BlendNothing);
                return;
            }

            int choices = 0;
            foreach (KeyValuePair<string, int> tea in teas)
            {
                foreach (KeyValuePair<string, int> herb in herbs)
                {
                    if (choices >= MaxChoices) return;
                    if (!TeaBlend.TryMake(tea.Key, herb.Key, root.Model.settings.notes.maxNotes, out TeaBlend blend)) continue;
                    string teaId = tea.Key, herbId = herb.Key;
                    actions.Add((ProcessingTexts.BlendVerb + ": " + ProcessingTexts.BlendName(blend), () => Blend(teaId, herbId, blend)));
                    choices++;
                }
            }
        }

        private void Blend(string teaId, string herbId, TeaBlend blend)
        {
            if (!WorkshopItems.TakeOne(teaId)) return;
            if (!WorkshopItems.TakeOne(herbId))
            {
                WorkshopItems.DeliverTeaById(teaId);
                return;
            }

            WorkshopDelivery where = WorkshopItems.DeliverBlend(blend, TeaBlend.PortionsPerBlend);
            root.Feedback.Play(WorkshopSound.Pack);
            root.Feedback.LeafBits(root.StationCenter(WorkshopTile.BlendTable), LeafLook.TeaColor(blend.tea), 5);
            root.Ui.Message(ProcessingTexts.BlendDone(blend, TeaBlend.PortionsPerBlend) + (where == WorkshopDelivery.Pantry ? ProcessingTexts.ToPantry : ""));
        }
    }
}
