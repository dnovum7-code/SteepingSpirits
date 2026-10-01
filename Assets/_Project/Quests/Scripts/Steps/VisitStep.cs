using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// Besuche X – targetID ist eine Location-ID (z.B. "loc_alter_turm") oder
    /// ein verlinktes GameObject. In der Szene meldet ein QuestLocationTrigger.
    /// </summary>
    public class VisitStep : QuestStepBase
    {
        protected override void Subscribe()
        {
            GameplayEvents.OnLocationVisited += HandleLocationVisited;
        }

        protected override void Unsubscribe()
        {
            GameplayEvents.OnLocationVisited -= HandleLocationVisited;
        }

        private void HandleLocationVisited(string locationID, GameObject source)
        {
            if (MatchesTarget(locationID, source))
            {
                ReportProgress();
            }
        }
    }
}
