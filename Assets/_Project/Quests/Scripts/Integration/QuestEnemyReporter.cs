using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Quests.Steps;

namespace SteepingSpirits.Quests.Integration
{
    /// <summary>
    /// Brücke Combat → Quest-System: Meldet den Tod dieses Gegners als
    /// GameplayEvents.EnemyDefeated, damit Defeat-Steps zählen können.
    /// Auf das Gegner-GameObject neben Health legen.
    ///
    /// Enemy-ID: leeres Feld → QuestTarget → GameObject-Name. Für „besiege
    /// genau diesen Gegner" das GameObject in einen QuestTargetLink ziehen.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class QuestEnemyReporter : MonoBehaviour
    {
        [Tooltip("Leer lassen = QuestTarget-Id bzw. GameObject-Name")]
        [SerializeField] private string enemyID;

        private Health health;

        /// <summary>Konfiguration aus Code (z.B. für die Test-Arena).</summary>
        public void Configure(string enemyID)
        {
            this.enemyID = enemyID;
        }

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            health.Died -= HandleDied;
        }

        private string ResolveId()
        {
            if (!string.IsNullOrEmpty(enemyID))
            {
                return enemyID;
            }

            QuestTarget target = GetComponent<QuestTarget>();
            return target != null ? target.Id : gameObject.name;
        }

        private void HandleDied()
        {
            GameplayEvents.EnemyDefeated(ResolveId(), gameObject);
        }
    }
}
