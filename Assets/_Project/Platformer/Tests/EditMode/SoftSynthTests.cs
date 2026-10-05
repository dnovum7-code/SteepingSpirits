using System;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class SoftSynthTests
    {
        private const int Rate = 22050;

        private static float Peak(float[] b)
        {
            float p = 0f;
            foreach (float s in b) p = Math.Max(p, Math.Abs(s));
            return p;
        }

        [Test]
        public void Pluck_StaysBelowVolumeAndStartsAndEndsSilent()
        {
            var b = new float[SoftSynth.Samples(Rate, 0.25f)];
            SoftSynth.Pluck(b, Rate, 440f, 0.2f, 0.08f);
            Assert.LessOrEqual(Peak(b), 0.2001f);
            Assert.Less(Math.Abs(b[0]), 1e-3f);
            Assert.Less(Math.Abs(b[b.Length - 1]), 1e-3f);
        }

        [Test]
        public void Puff_IsNormalizedAndDeterministic()
        {
            var a = new float[SoftSynth.Samples(Rate, 0.2f)];
            var c = new float[a.Length];
            SoftSynth.Puff(a, Rate, 0.15f, 900f, 0.06f, 7);
            SoftSynth.Puff(c, Rate, 0.15f, 900f, 0.06f, 7);
            Assert.AreEqual(0.15f, Peak(a), 1e-4f);
            CollectionAssert.AreEqual(a, c);
            Assert.Less(Math.Abs(a[a.Length - 1]), 1e-3f);
        }

        [Test]
        public void Puff_LowPassRemovesHighFrequencies()
        {
            var b = new float[SoftSynth.Samples(Rate, 0.2f)];
            SoftSynth.Puff(b, Rate, 0.2f, 600f, 1f, 3);
            // Mean absolute sample-to-sample step is small for low-passed noise.
            float diff = 0f;
            for (int i = 1; i < b.Length; i++) diff += Math.Abs(b[i] - b[i - 1]);
            diff /= b.Length;
            Assert.Less(diff, 0.02f);
        }

        [Test]
        public void Chime_NeverClips()
        {
            var b = new float[SoftSynth.Samples(Rate, 0.6f)];
            SoftSynth.Chime(b, Rate, new[] { 523f, 659f, 784f }, 0.22f, 0.07f, 0.25f);
            Assert.LessOrEqual(Peak(b), 0.2201f);
        }
    }
}
