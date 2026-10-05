using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>End of the level: a resting place. Touching it shows the end-of-level card.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Goal : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<JumpNRunPlayer>() != null)
            {
                JumpNRunSession.Current?.FinishLevel();
            }
        }
    }
}
