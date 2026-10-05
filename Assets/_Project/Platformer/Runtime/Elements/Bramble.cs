using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Thorny brambles: touching them is not harmful, a spirit just carries the player back.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Bramble : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<JumpNRunPlayer>() != null)
            {
                JumpNRunSession.Current?.Catch();
            }
        }
    }
}
