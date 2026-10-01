using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// Bringe etwas zu X – targetID ist die NPC-ID des Empfängers
    /// (z.B. "npc_mira") oder ein verlinktes GameObject. Welches Item übergeben
    /// wurde meldet das Gameplay über GameplayEvents.ItemDelivered.
    /// </summary>
    public class BringStep : QuestStepBase
    {
        protected override void Subscribe()
        {
            GameplayEvents.OnItemDelivered += HandleItemDelivered;
        }

        protected override void Unsubscribe()
        {
            GameplayEvents.OnItemDelivered -= HandleItemDelivered;
        }

        private void HandleItemDelivered(string npcID, string itemID, GameObject source)
        {
            if (MatchesTarget(npcID, source))
            {
                ReportProgress();
            }
        }
    }
}
