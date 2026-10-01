using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>Sprich mit X – targetID ist eine NPC-ID (z.B. "npc_schmied") oder ein verlinktes GameObject.</summary>
    public class TalkStep : QuestStepBase
    {
        protected override void Subscribe()
        {
            GameplayEvents.OnNpcTalkedTo += HandleNpcTalkedTo;
        }

        protected override void Unsubscribe()
        {
            GameplayEvents.OnNpcTalkedTo -= HandleNpcTalkedTo;
        }

        private void HandleNpcTalkedTo(string npcID, GameObject source)
        {
            if (MatchesTarget(npcID, source))
            {
                ReportProgress();
            }
        }
    }
}
