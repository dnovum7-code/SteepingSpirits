using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// Szenen-Helfer für Visit-Objectives (2D): Meldet GameplayEvents.LocationVisited
    /// sobald der Spieler den Trigger-Collider2D betritt. Die Location-ID kommt
    /// aus dem Feld, sonst aus einem QuestTarget, sonst dem GameObject-Namen.
    /// Für Referenz-Matching einfach dieses GameObject in einen QuestTargetLink ziehen.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class QuestLocationTrigger : MonoBehaviour
    {
        [Tooltip("Leer lassen = QuestTarget-Id bzw. GameObject-Name")]
        [SerializeField] private string locationID;
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Nur einmal melden. Aus = bei jedem Betreten (z.B. für wiederholbare Quests)")]
        [SerializeField] private bool fireOnce;

        private bool fired;

        /// <summary>Konfiguration aus Code (z.B. für die Test-Szene).</summary>
        public void Configure(string locationID, string playerTag = null)
        {
            this.locationID = locationID;
            if (!string.IsNullOrEmpty(playerTag))
            {
                this.playerTag = playerTag;
            }
        }

        private void Reset()
        {
            // Trigger-Collider sicherstellen, damit OnTriggerEnter2D feuert.
            GetComponent<Collider2D>().isTrigger = true;
        }

        private string ResolveId()
        {
            if (!string.IsNullOrEmpty(locationID))
            {
                return locationID;
            }

            QuestTarget target = GetComponent<QuestTarget>();
            return target != null ? target.Id : gameObject.name;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired && fireOnce)
            {
                return;
            }

            if (!PlayerLocator.IsPlayer(other, playerTag))
            {
                return;
            }

            fired = true;
            GameplayEvents.LocationVisited(ResolveId(), gameObject);
        }
    }
}
