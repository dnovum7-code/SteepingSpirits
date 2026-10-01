using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Laufzeitzustand einer Quest. Reines C#-Objekt (kein MonoBehaviour,
    /// kein ScriptableObject) – existiert nur im RAM und wird beim Speichern
    /// als JSON auf die Disk geschrieben.
    ///
    /// Zustandsänderungen laufen ausschliesslich über den QuestManager.
    /// </summary>
    public class QuestInstance
    {
        /// <summary>Referenz auf den unveränderlichen Bauplan.</summary>
        public QuestData Data { get; }

        /// <summary>Aktueller Zustand. Wird nur vom QuestManager gesetzt.</summary>
        public QuestState State { get; set; }

        // Fortschritt pro Objective-ID
        private readonly Dictionary<string, int> objectiveProgress = new Dictionary<string, int>();

        public QuestInstance(QuestData data)
        {
            Data = data;
            State = QuestState.INACTIVE;
            ResetProgress();
        }

        /// <summary>Setzt den Fortschritt aller Objectives auf 0 zurück.</summary>
        public void ResetProgress()
        {
            objectiveProgress.Clear();
            if (Data.objectives == null)
            {
                return;
            }

            foreach (ObjectiveData objective in Data.objectives)
            {
                objectiveProgress[objective.objectiveID] = 0;
            }
        }

        public int GetProgress(string objectiveID)
        {
            return objectiveProgress.TryGetValue(objectiveID, out int value) ? value : 0;
        }

        public bool TryGetObjective(string objectiveID, out ObjectiveData objective)
        {
            if (Data.objectives != null)
            {
                foreach (ObjectiveData candidate in Data.objectives)
                {
                    if (candidate.objectiveID == objectiveID)
                    {
                        objective = candidate;
                        return true;
                    }
                }
            }

            objective = null;
            return false;
        }

        /// <summary>
        /// Erhöht den Fortschritt eines Objectives, geklemmt auf [0, requiredAmount].
        /// Gibt die tatsächliche Änderung zurück (0 wenn nichts passiert ist).
        /// </summary>
        public int AddProgress(string objectiveID, int amount)
        {
            if (!TryGetObjective(objectiveID, out ObjectiveData objective))
            {
                return 0;
            }

            int current = GetProgress(objectiveID);
            int next = Mathf.Clamp(current + amount, 0, objective.requiredAmount);
            objectiveProgress[objectiveID] = next;
            return next - current;
        }

        public bool IsObjectiveComplete(string objectiveID)
        {
            return TryGetObjective(objectiveID, out ObjectiveData objective)
                   && GetProgress(objectiveID) >= objective.requiredAmount;
        }

        public bool AreAllObjectivesComplete()
        {
            if (Data.objectives == null)
            {
                return true;
            }

            foreach (ObjectiveData objective in Data.objectives)
            {
                if (GetProgress(objective.objectiveID) < objective.requiredAmount)
                {
                    return false;
                }
            }

            return true;
        }

        // ---------------------------------------------------------------
        // Speichern / Laden
        // ---------------------------------------------------------------

        public QuestInstanceSaveData ToSaveData()
        {
            var save = new QuestInstanceSaveData
            {
                questID = Data.questID,
                state = State
            };

            foreach (KeyValuePair<string, int> entry in objectiveProgress)
            {
                save.objectiveIDs.Add(entry.Key);
                save.amounts.Add(entry.Value);
            }

            return save;
        }

        public void ApplySaveData(QuestInstanceSaveData save)
        {
            State = save.state;
            ResetProgress();

            for (int i = 0; i < save.objectiveIDs.Count && i < save.amounts.Count; i++)
            {
                // Unbekannte Objectives (geänderte QuestData) still ignorieren.
                if (objectiveProgress.ContainsKey(save.objectiveIDs[i]))
                {
                    objectiveProgress[save.objectiveIDs[i]] = save.amounts[i];
                }
            }
        }
    }

    /// <summary>JSON-serialisierbarer Schnappschuss einer QuestInstance.</summary>
    [Serializable]
    public class QuestInstanceSaveData
    {
        public string questID;
        public QuestState state;
        public List<string> objectiveIDs = new List<string>();
        public List<int> amounts = new List<int>();
    }
}
