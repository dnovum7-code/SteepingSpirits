using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Optional lantern challenge: light every small path lantern of a level
    /// (in any order, no time limit). Completing it before the goal earns a
    /// bonus ingredient. Missing some costs nothing.
    /// </summary>
    public sealed class LanternChallenge
    {
        private readonly HashSet<int> lit = new HashSet<int>();

        public int Total { get; }
        public int Lit => lit.Count;
        public bool Active => Total > 0;
        public bool Complete => Active && lit.Count >= Total;

        /// <summary>Id given once when the challenge completes.</summary>
        public string RewardId { get; }

        public LanternChallenge(int total, string rewardId = IngredientIds.SpringCrystal)
        {
            Total = total < 0 ? 0 : total;
            RewardId = string.IsNullOrEmpty(rewardId) ? IngredientIds.SpringCrystal : rewardId;
        }

        /// <summary>Returns true exactly when this light completes the challenge.</summary>
        public bool Light(int index)
        {
            if (index < 0 || index >= Total || !lit.Add(index))
            {
                return false;
            }

            return lit.Count == Total;
        }

        public bool IsLit(int index) => lit.Contains(index);
    }
}
