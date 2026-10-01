using System;
using UnityEngine;
using SteepingSpirits.Quests;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// Ziel des Parcours. Meldet beim Erreichen GameplayEvents.LocationVisited
    /// (locationID) – so kann eine Visit-Quest aus der Wiese hier erfüllt werden –
    /// und feuert <see cref="OnGoalReached"/> (z.B. für das HUD).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Goal2D : MonoBehaviour
    {
        [SerializeField] private string locationID = "loc_kletterpfad_ziel";

        private bool reached;

        public static event Action<Goal2D> OnGoalReached;

        public void Configure(string locationID)
        {
            this.locationID = locationID;
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (reached || other.GetComponentInParent<PlatformerController2D>() == null)
            {
                return;
            }

            reached = true;
            GameplayEvents.LocationVisited(locationID, gameObject);
            OnGoalReached?.Invoke(this);
        }
    }
}
