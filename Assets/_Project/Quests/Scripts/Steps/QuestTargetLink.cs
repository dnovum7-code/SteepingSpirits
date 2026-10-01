using UnityEngine;

namespace SteepingSpirits.Quests.Steps
{
    /// <summary>
    /// „GameObject verlinken statt ID tippen." Hier ziehst du die konkreten
    /// Szenen-Objekte hinein, die ein bestimmtes Objective erfüllen sollen.
    /// Der passende Step zählt dann hoch, sobald eines dieser Objekte gemeldet
    /// wird – unabhängig davon, ob (oder welche) String-ID gesetzt ist.
    ///
    /// Beispiel: objectiveID = "collect_flowers", Targets = [Blume1, Blume2, …].
    /// Auf ein beliebiges GameObject legen (z.B. neben den QuestManager).
    /// </summary>
    public class QuestTargetLink : MonoBehaviour
    {
        [Tooltip("Die objectiveID aus der QuestData, die diese Objekte erfüllen")]
        [SerializeField] private string objectiveID;

        [Tooltip("Konkrete Szenen-Objekte, die für dieses Objective zählen")]
        [SerializeField] private GameObject[] targets;

        private void OnEnable()
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject t in targets)
            {
                QuestTargetRegistry.Register(objectiveID, t);
            }
        }

        private void OnDisable()
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject t in targets)
            {
                QuestTargetRegistry.Unregister(objectiveID, t);
            }
        }
    }
}
