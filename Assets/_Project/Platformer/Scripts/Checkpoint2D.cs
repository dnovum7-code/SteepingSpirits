using UnityEngine;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// Checkpoint: Läuft der Spieler durch den Trigger, wird hier neu gestartet.
    /// Färbt optional ein Fähnchen ein, sobald er aktiv ist.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint2D : MonoBehaviour
    {
        [Tooltip("Wo der Spieler erscheint (leer = dieses Objekt)")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private SpriteRenderer flag;
        [SerializeField] private Color activeColor = new Color(0.4f, 1f, 0.5f);

        public void Configure(SpriteRenderer flag)
        {
            this.flag = flag;
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlatformerController2D player = other.GetComponentInParent<PlatformerController2D>();
            if (player == null)
            {
                return;
            }

            Vector2 at = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
            if (player.SetCheckpoint(at) && flag != null)
            {
                flag.color = activeColor;
            }
        }
    }
}
