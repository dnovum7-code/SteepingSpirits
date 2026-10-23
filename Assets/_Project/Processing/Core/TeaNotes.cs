using System;
using System.Collections.Generic;

namespace SteepingSpirits.Processing
{
    /// <summary>Small character notes a tea picks up in the workshop – what the spirits will taste.</summary>
    public enum TeaNote
    {
        Fresh,
        Floral,
        Honeyed,
        Malty,
        Roasty,
        Delicate
    }

    /// <summary>Which notes a batch shows (strongest first, at most NoteParams.maxNotes).</summary>
    public static class TeaNotes
    {
        public static List<TeaNote> For(LeafBatch b, ProcessingParams p)
        {
            NoteParams n = p.notes;
            TeaVariety t = TeaClassifier.Classify(b, p.variety);
            float ox = b.oxidation;
            float workMoisture = b.workMoisture >= 0f ? b.workMoisture : b.moisture;
            var found = new List<KeyValuePair<TeaNote, float>>();

            if (b.roast >= n.roastyFrom) found.Add(Note(TeaNote.Roasty, 0.5f + b.roast));
            if (b.roast >= n.honeyedRoastFrom && b.roast < n.roastyFrom && ox >= n.honeyedOxMin && ox <= n.honeyedOxMax)
            {
                found.Add(Note(TeaNote.Honeyed, 0.6f + b.roast));
            }

            bool floralOolong = b.rollCount > 0 && b.rollEvenness >= n.floralEvenness && ox >= n.floralOxMin && ox <= n.floralOxMax;
            bool floralWhite = t == TeaVariety.White && workMoisture <= n.longWitherMoisture;
            if (floralOolong || floralWhite) found.Add(Note(TeaNote.Floral, 0.6f + b.rollEvenness * 0.2f));

            if (ox >= n.maltyOxFrom && b.rolled >= n.maltyRollFrom) found.Add(Note(TeaNote.Malty, ox));

            if (ox <= n.freshOxMax && b.roast < n.honeyedRoastFrom && (b.fixedByWok || workMoisture >= n.freshMoistureFrom))
            {
                found.Add(Note(TeaNote.Fresh, 0.7f));
            }

            if (b.firstFlush) found.Add(Note(TeaNote.Delicate, 0.9f));
            else if (t == TeaVariety.White && b.roast < n.honeyedRoastFrom) found.Add(Note(TeaNote.Delicate, 0.5f));

            // Strongest first, ties by enum order; then cut and sort by enum order for stable ids.
            found.Sort((a, c) => a.Value != c.Value ? c.Value.CompareTo(a.Value) : a.Key.CompareTo(c.Key));
            var notes = new List<TeaNote>();
            for (int i = 0; i < found.Count && notes.Count < Math.Max(0, n.maxNotes); i++) notes.Add(found[i].Key);
            notes.Sort();
            return notes;
        }

        /// <summary>Lower-case English key used in ids and save files ("roasty").</summary>
        public static string Key(TeaNote note) => note.ToString().ToLowerInvariant();

        public static bool TryParse(string key, out TeaNote note)
        {
            foreach (TeaNote n in (TeaNote[])Enum.GetValues(typeof(TeaNote)))
            {
                if (Key(n) == key)
                {
                    note = n;
                    return true;
                }
            }

            note = TeaNote.Fresh;
            return false;
        }

        private static KeyValuePair<TeaNote, float> Note(TeaNote n, float strength) => new KeyValuePair<TeaNote, float>(n, strength);
    }
}
