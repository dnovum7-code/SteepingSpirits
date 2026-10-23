using System.Collections.Generic;
using NUnit.Framework;
using SteepingSpirits.Ingredients;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class ProcessedTeaTests
    {
        [Test]
        public void Ids_CarryBaseStyleNotesAndQuality()
        {
            Assert.AreEqual("tea_white", ProcessedTea.MakeId(TeaVariety.White, null, 1));
            Assert.AreEqual("tea_green/fresh+q3", ProcessedTea.MakeId(TeaVariety.Green, new[] { TeaNote.Fresh }, 3));
            Assert.AreEqual("tea_oolong:dark/honeyed.roasty+q2", ProcessedTea.MakeId(TeaVariety.DarkOolong, new[] { TeaNote.Roasty, TeaNote.Honeyed }, 2));
            Assert.AreEqual("tea_oolong:light", ProcessedTea.MakeId(TeaVariety.LightOolong, new List<TeaNote>(), 1));
        }

        [Test]
        public void Ids_RoundTrip_ForEveryTeaNoteAndTier()
        {
            var all = (TeaNote[])System.Enum.GetValues(typeof(TeaNote));
            foreach (TeaVariety v in System.Enum.GetValues(typeof(TeaVariety)))
            for (int tier = 1; tier <= 3; tier++)
            for (int mask = 0; mask < 1 << all.Length; mask += 5)
            {
                var notes = new List<TeaNote>();
                for (int i = 0; i < all.Length; i++)
                {
                    if ((mask & (1 << i)) != 0) notes.Add(all[i]);
                }

                string id = ProcessedTea.MakeId(v, notes, tier);
                Assert.IsTrue(ProcessedTea.TryParseId(id, out TeaVariety back, out List<TeaNote> backNotes, out int backTier), id);
                Assert.AreEqual(v, back, id);
                Assert.AreEqual(tier, backTier, id);
                CollectionAssert.AreEqual(notes, backNotes, id);
                Assert.IsTrue(IngredientCatalog.IsKnown(ProcessedTea.CatalogIdOf(id)), id);
            }
        }

        [Test]
        public void SameTeaSameNotesSameQuality_StackTogether_DifferentNotesStayApart()
        {
            string a = ProcessedTea.From(Oolong(0.2f), P).Id;
            string b = ProcessedTea.From(Oolong(0.21f), P).Id;
            Assert.AreEqual(a, b);
            var p = new Pantry();
            p.Add(a, 2);
            p.Add(b, 3);
            p.Add(ProcessedTea.MakeId(TeaVariety.LightOolong, new[] { TeaNote.Roasty }, 3), 1);
            Assert.AreEqual(5, p.Count(a));
            Assert.AreEqual(2, new List<KeyValuePair<string, int>>(p.Items).Count);
        }

        [Test]
        public void Finish_GivesTheContract()
        {
            ProcessedTea t = ProcessedTea.From(Black(2), P);
            Assert.AreEqual(TeaVariety.Black, t.variety);
            Assert.AreEqual("tea_black", t.BaseId);
            Assert.AreEqual(2, t.tier);
            Assert.AreEqual(5, t.amount);
            Assert.AreEqual("tea_leaf", t.sourceId);
            Assert.AreEqual("W-R-O-D", t.path);
            Assert.Greater(t.oxidation, 0.9f);
            CollectionAssert.Contains(t.notes, TeaNote.Malty);
            StringAssert.StartsWith("tea_black/", t.Id);
        }

        [Test]
        public void Workshop_FinishRemovesTheBatch()
        {
            var clock = new ManualGameTime();
            var w = new Workshop(new ProcessingParams(), clock);
            LeafBatch b = w.Add("tea_leaf_first", 1, 3, true);
            Assert.IsFalse(w.Finish(b.id, out _));
            w.Do(b.id, BatchAction.Wither);
            clock.Advance(240);
            w.Do(b.id, BatchAction.TakeOut);
            w.Do(b.id, BatchAction.Dry);
            clock.Advance(130);
            w.Do(b.id, BatchAction.TakeOut);
            Assert.IsTrue(w.Finish(b.id, out ProcessedTea tea));
            Assert.AreEqual(TeaVariety.White, tea.variety);
            Assert.AreEqual(3, tea.tier, "first flush lifts the ceiling");
            CollectionAssert.Contains(tea.notes, TeaNote.Delicate);
            Assert.AreEqual(0, w.Batches.Count);
        }

        [Test]
        public void ForeignIds_AreNoWorkshopTea()
        {
            Assert.IsFalse(ProcessedTea.TryParseId("mint+q3", out _, out _, out _));
            Assert.IsFalse(ProcessedTea.TryParseId("", out _, out _, out _));
            Assert.AreEqual("tea_green", ProcessedTea.CatalogIdOf("tea_green/fresh+q2"));
        }

        [Test]
        public void CatalogTeas_HaveGermanNames()
        {
            Assert.AreEqual("Oolong", IngredientTexts.Name("tea_oolong"));
            Assert.AreEqual("Erster Austrieb", IngredientTexts.Name(TeaIds.FirstFlushLeaf));
            Assert.AreEqual(IngredientFamily.Tea, IngredientCatalog.Get("tea_black").Family);
        }
    }
}
