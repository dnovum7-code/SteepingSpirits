using UnityEngine;

namespace SteepingSpirits.Core
{
    /// <summary>
    /// Findet den Spieler (Tag „Player") – gecacht, damit nicht jedes Skript
    /// jeden Frame die Szene durchsucht. Plus Trigger-Helfer: zählt auch, wenn
    /// der Collider an einem Kind-Objekt des Spielers hängt.
    /// </summary>
    public static class PlayerLocator
    {
        public const string DefaultTag = "Player";

        private static Transform cached;
        private static float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cached = null;
            nextScan = 0f;
        }

        /// <summary>Spieler-Transform oder null (sucht höchstens 1× pro Sekunde neu).</summary>
        public static Transform Find()
        {
            if (cached == null && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 1f;
                GameObject p = GameObject.FindGameObjectWithTag(DefaultTag);
                cached = p != null ? p.transform : null;
            }

            return cached;
        }

        /// <summary>Gehört dieser Collider zum Spieler (direkt oder über dessen Rigidbody)?</summary>
        public static bool IsPlayer(Collider2D col, string playerTag = DefaultTag)
        {
            if (col == null)
            {
                return false;
            }

            return col.CompareTag(playerTag)
                   || (col.attachedRigidbody != null && col.attachedRigidbody.CompareTag(playerTag));
        }
    }
}
