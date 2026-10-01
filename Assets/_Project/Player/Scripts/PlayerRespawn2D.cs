using System.Collections;
using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Economy;

namespace SteepingSpirits.Player
{
    /// <summary>
    /// Was passiert, wenn der Spieler keine Herzen mehr hat: kurz liegen
    /// bleiben, dann am Startpunkt mit vollen Herzen aufwachen. Optional geht
    /// dabei etwas Gold verloren (wie Stardew beim Umkippen).
    ///
    /// Health am Spieler muss destroyOnDeath = false haben.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerRespawn2D : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float respawnDelay = 1.5f;

        [Tooltip("Anteil des Golds, der beim Umkippen verloren geht (0 = nichts)")]
        [Range(0f, 1f)] [SerializeField] private float goldLossFraction = 0.1f;

        private Health health;
        private Vector2 spawnPosition;

        private void Awake()
        {
            health = GetComponent<Health>();
            spawnPosition = transform.position;
        }

        private void OnEnable()
        {
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            health.Died -= HandleDied;
        }

        /// <summary>Neuen Startpunkt setzen (z.B. Bett, Checkpoint).</summary>
        public void SetSpawn(Vector2 position)
        {
            respawnPoint = null;
            spawnPosition = position;
        }

        private void HandleDied()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            Vector2 target = respawnPoint != null ? (Vector2)respawnPoint.position : spawnPosition;
            var controller = GetComponent<PlayerController2D>();
            if (controller != null)
            {
                controller.Teleport(target);
            }
            else
            {
                transform.position = target;
            }

            if (goldLossFraction > 0f && Wallet.Instance != null)
            {
                int loss = Mathf.FloorToInt(Wallet.Instance.Gold * goldLossFraction);
                if (loss > 0)
                {
                    Wallet.Instance.TrySpend(loss);
                    Debug.Log($"[Respawn] Umgekippt – {loss} Gold verloren.");
                }
            }

            health.Revive();
        }
    }
}
