using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>Besiege X – targetID ist eine Enemy-ID (z.B. "enemy_slime") oder ein verlinktes GameObject.</summary>
    public class DefeatStep : QuestStepBase
    {
        protected override void Subscribe()
        {
            GameplayEvents.OnEnemyDefeated += HandleEnemyDefeated;
        }

        protected override void Unsubscribe()
        {
            GameplayEvents.OnEnemyDefeated -= HandleEnemyDefeated;
        }

        private void HandleEnemyDefeated(string enemyID, GameObject source)
        {
            if (MatchesTarget(enemyID, source))
            {
                ReportProgress();
            }
        }
    }
}
