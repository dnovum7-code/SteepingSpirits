using System;
using UnityEngine;

namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Integrationspunkt zwischen Gameplay und Quest-Steps. Das Spiel meldet
    /// hier was passiert ist – die Step-Templates hören zu und aktualisieren
    /// den Quest-Fortschritt. Gameplay-Code muss dafür kein Quest-System kennen.
    ///
    /// Jede Meldung trägt zusätzlich das Quell-GameObject (kann null sein).
    /// Damit können Steps ihr Ziel entweder per String-ID ODER per direkt
    /// verlinktem GameObject erkennen (siehe QuestTargetLink).
    ///
    /// Beispiel: Beim Aufheben eines Krauts ruft das Inventar
    /// GameplayEvents.ItemCollected("item_heilkraut", gameObject) auf.
    /// </summary>
    public static class GameplayEvents
    {
        public static event Action<string, GameObject> OnItemCollected;          // itemID, quelle
        public static event Action<string, GameObject> OnEnemyDefeated;          // enemyID, quelle
        public static event Action<string, GameObject> OnLocationVisited;        // locationID, quelle
        public static event Action<string, GameObject> OnNpcTalkedTo;            // npcID, quelle
        public static event Action<string, string, GameObject> OnItemDelivered;  // npcID, itemID, quelle

        public static void ItemCollected(string itemID, GameObject source = null)
        {
            OnItemCollected?.Invoke(itemID, source);
        }

        public static void EnemyDefeated(string enemyID, GameObject source = null)
        {
            OnEnemyDefeated?.Invoke(enemyID, source);
        }

        public static void LocationVisited(string locationID, GameObject source = null)
        {
            OnLocationVisited?.Invoke(locationID, source);
        }

        public static void NpcTalkedTo(string npcID, GameObject source = null)
        {
            OnNpcTalkedTo?.Invoke(npcID, source);
        }

        public static void ItemDelivered(string npcID, string itemID, GameObject source = null)
        {
            OnItemDelivered?.Invoke(npcID, itemID, source);
        }
    }
}
