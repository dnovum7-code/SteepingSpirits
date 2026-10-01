using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>Sammle X – targetID ist eine Item-ID (z.B. "item_heilkraut") oder ein verlinktes GameObject.</summary>
    public class CollectStep : QuestStepBase
    {
        protected override void Subscribe()
        {
            GameplayEvents.OnItemCollected += HandleItemCollected;
        }

        protected override void Unsubscribe()
        {
            GameplayEvents.OnItemCollected -= HandleItemCollected;
        }

        private void HandleItemCollected(string itemID, GameObject source)
        {
            if (MatchesTarget(itemID, source))
            {
                ReportProgress();
            }
        }
    }
}
