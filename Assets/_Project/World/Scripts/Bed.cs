using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Core;
using SteepingSpirits.Dialogue;
using SteepingSpirits.Interaction;
using SteepingSpirits.Player;

namespace SteepingSpirits.World
{
    /// <summary>
    /// Bett (Stardew): Ansprechen → schlafen → nächster Morgen. Der Spieler
    /// wacht mit vollen Herzen auf, und das Bett wird zum neuen Startpunkt
    /// (PlayerRespawn2D). Braucht einen Collider2D.
    ///
    /// Später der natürliche Ort fürs automatische Speichern am Tagesende.
    /// </summary>
    public class Bed : MonoBehaviour, IInteractable, IInteractLabel
    {
        [TextArea] [SerializeField] private string[] sleepLines = { "Du kuschelst dich ins Bett und schläfst tief und fest …" };

        [Tooltip("Wo der Spieler nach dem Aufwachen steht (relativ zum Bett)")]
        [SerializeField] private Vector2 wakeOffset = new Vector2(0f, -1f);

        public string InteractLabel => "Schlafen";

        public void Interact()
        {
            DialogueHUD.Instance.Play("Bett", sleepLines, Sleep);
        }

        private void Sleep()
        {
            if (GameClock.Instance != null)
            {
                GameClock.Instance.AdvanceToNextDay();
            }

            Transform player = PlayerLocator.Find();
            if (player == null)
            {
                return;
            }

            Vector2 wake = (Vector2)transform.position + wakeOffset;

            Health health = player.GetComponent<Health>();
            if (health != null && !health.IsDead)
            {
                health.Heal(health.MaxHealth);
            }

            PlayerRespawn2D respawn = player.GetComponent<PlayerRespawn2D>();
            if (respawn != null)
            {
                respawn.SetSpawn(wake);
            }

            PlayerController2D controller = player.GetComponent<PlayerController2D>();
            if (controller != null)
            {
                controller.Teleport(wake);
            }
        }
    }
}
