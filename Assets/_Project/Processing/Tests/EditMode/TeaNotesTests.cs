using System.Collections.Generic;
using NUnit.Framework;
using static SteepingSpirits.Processing.Tests.Kit;

namespace SteepingSpirits.Processing.Tests
{
    public class TeaNotesTests
    {
        private static List<TeaNote> N(LeafBatch b) => TeaNotes.For(b, P);

        [Test]
        public void GreenTea_IsFresh()
        {
            CollectionAssert.Contains(N(Green()), TeaNote.Fresh);
        }

        [Test]
        public void EvenlyRolledLightOolong_IsFloral()
        {
            CollectionAssert.Contains(N(Oolong(0.2f)), TeaNote.Floral);
        }

        [Test]
        public void FullyRolledBlack_IsMalty()
        {
            CollectionAssert.Contains(N(Black()), TeaNote.Malty);
        }

        [Test]
        public void HotWok_OrLongDrying_IsRoasty_NeverBurnt()
        {
            LeafBatch hot = BatchRules.Roll(BatchRules.Fix(Withered(), Wok(0f, 0.2f, 0.8f), P), Roll(1f), P);
            CollectionAssert.Contains(N(DryFully(hot)), TeaNote.Roasty);
            CollectionAssert.Contains(N(DryFully(Withered(), 400)), TeaNote.Roasty);
        }

        [Test]
        public void LightlyRoastedDarkOolong_IsHoneyed()
        {
            LeafBatch b = BatchRules.Roll(Withered(), Roll(0.5f), P);
            b = OxidizeTo(b, 0.5f);
            b = BatchRules.Fix(b, Wok(0f, 0.75f, 0.25f), P);
            CollectionAssert.Contains(N(DryFully(b)), TeaNote.Honeyed);
        }

        [Test]
        public void FirstFlush_IsDelicate_LongWitheredWhite_IsFloral()
        {
            CollectionAssert.Contains(N(DryFully(Withered(3, 240, true))), TeaNote.Delicate);
            CollectionAssert.Contains(N(White(3, 700)), TeaNote.Floral);
        }

        [Test]
        public void Notes_AreLimited_SortedAndDeterministic()
        {
            ProcessingParams one = P.Clone();
            one.notes.maxNotes = 1;
            LeafBatch b = Green();
            Assert.AreEqual(1, TeaNotes.For(b, one).Count);
            List<TeaNote> a = N(b), c = N(b);
            CollectionAssert.AreEqual(a, c);
            for (int i = 1; i < a.Count; i++) Assert.Less((int)a[i - 1], (int)a[i]);
        }

        [Test]
        public void Keys_RoundTrip()
        {
            foreach (TeaNote n in System.Enum.GetValues(typeof(TeaNote)))
            {
                Assert.IsTrue(TeaNotes.TryParse(TeaNotes.Key(n), out TeaNote back));
                Assert.AreEqual(n, back);
            }

            Assert.IsFalse(TeaNotes.TryParse("burnt", out _));
        }
    }
}
