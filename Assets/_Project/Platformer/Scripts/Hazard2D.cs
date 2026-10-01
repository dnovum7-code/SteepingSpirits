using UnityEngine;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// Tödliche Zone (Stacheln, Abgrund): Berührt der Platformer-Spieler den
    /// Trigger, stirbt er und startet am letzten Checkpoint neu (wie Celeste).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard2D : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Hit(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Hit(other);
        }

        private static void Hit(Collider2D other)
        {
            PlatformerController2D player = other.GetComponentInParent<PlatformerController2D>();
            if (player != null)
            {
                player.Kill();
            }
        }
    }
}
