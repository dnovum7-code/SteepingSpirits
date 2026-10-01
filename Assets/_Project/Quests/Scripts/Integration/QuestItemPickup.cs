using System.Collections;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Quests.Steps;

namespace SteepingSpirits.Quests.Integration
{
    /// <summary>
    /// Aufsammelbares Quest-Item (Münze, Kraut, ...) in 2D: Läuft der Spieler
    /// in den Trigger-Collider2D, wird GameplayEvents.ItemCollected gemeldet. Collect-Steps
    /// zählen dann mit.
    ///
    /// respawnSeconds = 0 → das Objekt wird nach dem Aufsammeln zerstört
    /// (klassisches Einweg-Item). respawnSeconds &gt; 0 → die Münze verschwindet
    /// nur kurz und taucht danach wieder auf (praktisch für Test-Arenen und
    /// zum wiederholten Einsammeln).
    ///
    /// Die Item-ID muss NICHT von Hand getippt werden: leeres Feld → QuestTarget
    /// → GameObject-Name. Für Referenz-Matching dieses GameObject in einen
    /// QuestTargetLink ziehen (dann ist gar keine passende ID nötig).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class QuestItemPickup : MonoBehaviour
    {
        [Tooltip("Leer lassen = QuestTarget-Id bzw. GameObject-Name")]
        [SerializeField] private string itemID;

        [SerializeField] private string playerTag = "Player";

        [Tooltip("0 = einmalig (wird zerstört). >0 = Münze/Item erscheint nach so vielen Sekunden wieder")]
        [SerializeField] private float respawnSeconds;

        private bool collected;

        /// <summary>Konfiguration aus Code (z.B. für den Demo-Aufbau).</summary>
        public void Configure(string itemID, string playerTag = null, float respawnSeconds = -1f)
        {
            this.itemID = itemID;
            if (!string.IsNullOrEmpty(playerTag))
            {
                this.playerTag = playerTag;
            }
            if (respawnSeconds >= 0f)
            {
                this.respawnSeconds = respawnSeconds;
            }
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private string ResolveId()
        {
            if (!string.IsNullOrEmpty(itemID))
            {
                return itemID;
            }

            QuestTarget target = GetComponent<QuestTarget>();
            return target != null ? target.Id : gameObject.name;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !PlayerLocator.IsPlayer(other, playerTag))
            {
                return;
            }

            collected = true;
            GameplayEvents.ItemCollected(ResolveId(), gameObject);

            if (respawnSeconds > 0f)
            {
                StartCoroutine(RespawnAfter(respawnSeconds));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Kurz unsichtbar/unberührbar machen, dann wieder aktivieren – so bleibt
        // das GameObject (und diese Coroutine) am Leben, anders als bei Destroy.
        private IEnumerator RespawnAfter(float seconds)
        {
            SetPickable(false);
            yield return new WaitForSeconds(seconds);
            SetPickable(true);
            collected = false;
        }

        private void SetPickable(bool on)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = on;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = on;
            }
        }
    }
}
