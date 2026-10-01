using System;

namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Definition eines einzelnen Quest-Ziels. Reine Daten – die Logik
    /// liegt im zugehörigen Step-Template (siehe Scripts/Steps).
    /// </summary>
    [Serializable]
    public class ObjectiveData
    {
        public string objectiveID;
        public QuestStepType stepType;
        public string description;

        /// <summary>Ziel des Steps: Item-, Enemy-, Location- oder NPC-ID.</summary>
        public string targetID;

        public int requiredAmount = 1;

        /// <summary>
        /// Nur für Sammel-Ziele (Collect): Beim Quest-Start bereits im Inventar
        /// vorhandene Items sofort mitzählen (nicht nur neu gesammelte).
        /// </summary>
        public bool countExistingInventory;
    }
}
