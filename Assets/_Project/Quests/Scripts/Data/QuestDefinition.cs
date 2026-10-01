using System;
using UnityEngine;

namespace SteepingSpirits.Quests.Data
{
    /// <summary>
    /// Reines C#-Datenobjekt, das das Quest-JSON 1:1 spiegelt (vor der
    /// Umwandlung in QuestData). Enums bewusst als string, damit die
    /// Validierung aussagekräftige Fehler liefern kann.
    ///
    /// JSON ist hier nur ein bequemes Autoren-Format für handgeschriebene
    /// Quests (die KI-Generierung aus Everdawn ist bewusst nicht dabei).
    /// </summary>
    [Serializable]
    public class QuestDefinition
    {
        public string questID;
        public string displayName;
        public string description;
        public string category;
        public bool isRepeatable;
        public Vector2 startLocation;
        public ObjectiveDefinition[] objectives;
        public RewardDefinition[] rewards;
    }

    [Serializable]
    public class ObjectiveDefinition
    {
        public string objectiveID;
        public string stepType;
        public string description;
        public string targetID;
        public int requiredAmount = 1;
        public bool countExistingInventory;
    }

    [Serializable]
    public class RewardDefinition
    {
        public string type;
        public int amount;
        public string itemID;
    }
}
