using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Marks a thin platform that can be jumped through from below and dropped
    /// through with down + jump. The collision itself comes from the
    /// PlatformEffector2D; the player asks this component when checking ground.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        public Collider2D Collider { get; private set; }

        /// <summary>World y of the walkable top surface.</summary>
        public float Top => Collider.bounds.max.y;

        private void Awake()
        {
            Collider = GetComponent<Collider2D>();
        }
    }
}
