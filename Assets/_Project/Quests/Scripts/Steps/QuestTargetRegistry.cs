using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// Laufzeit-Register: welche Szenen-GameObjects zählen für welches
    /// Objective. Ermöglicht Referenz-basiertes Matching – ein Step erkennt
    /// sein Ziel am verlinkten GameObject, ohne dass String-IDs passen müssen.
    ///
    /// Gefüllt von QuestTargetLink, abgefragt von QuestStepBase.MatchesTarget.
    /// </summary>
    public static class QuestTargetRegistry
    {
        private static readonly Dictionary<string, HashSet<GameObject>> map =
            new Dictionary<string, HashSet<GameObject>>();

        public static void Register(string objectiveID, GameObject target)
        {
            if (string.IsNullOrEmpty(objectiveID) || target == null)
            {
                return;
            }

            if (!map.TryGetValue(objectiveID, out HashSet<GameObject> set))
            {
                set = new HashSet<GameObject>();
                map[objectiveID] = set;
            }

            set.Add(target);
        }

        public static void Unregister(string objectiveID, GameObject target)
        {
            if (!string.IsNullOrEmpty(objectiveID) && map.TryGetValue(objectiveID, out HashSet<GameObject> set))
            {
                set.Remove(target);
            }
        }

        public static bool IsTargetFor(string objectiveID, GameObject candidate)
        {
            return candidate != null
                   && !string.IsNullOrEmpty(objectiveID)
                   && map.TryGetValue(objectiveID, out HashSet<GameObject> set)
                   && set.Contains(candidate);
        }
    }
}
