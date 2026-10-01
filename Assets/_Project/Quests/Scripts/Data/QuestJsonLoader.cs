using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Quests.Data
{
    /// <summary>
    /// Macht aus validiertem Quest-JSON eine QuestData-Instanz im RAM
    /// (ScriptableObject.CreateInstance – geht auch im Build, anders als
    /// AssetDatabase.CreateAsset). Es entsteht kein Asset auf der Disk.
    /// Der Editor-Importer nutzt denselben Weg und speichert danach als Asset.
    ///
    /// Verantwortung: JSON lesen &amp; validieren, Quest-Objekt bauen.
    /// Nicht seine Aufgabe: Quests starten, Fortschritt, Rewards.
    /// </summary>
    public static class QuestJsonLoader
    {
        /// <summary>
        /// Validiert das JSON und baut daraus eine QuestData-Instanz.
        /// Gibt null zurück wenn die Validierung fehlschlägt (Fehler in errors).
        /// </summary>
        public static QuestData CreateFromJson(string json, out List<string> errors)
        {
            if (!QuestJsonValidator.TryParse(json, out QuestDefinition definition, out errors))
            {
                return null;
            }

            return CreateFromDefinition(definition);
        }

        /// <summary>Baut aus einer (bereits validierten) QuestDefinition eine QuestData-Instanz.</summary>
        public static QuestData CreateFromDefinition(QuestDefinition definition)
        {
            var data = ScriptableObject.CreateInstance<QuestData>();
            data.name = definition.questID;

            data.questID = definition.questID;
            data.displayName = definition.displayName;
            data.description = definition.description;
            data.category = ParseEnum(definition.category, QuestCategory.SIDE);
            data.isRepeatable = definition.isRepeatable;
            data.startLocation = definition.startLocation;

            if (definition.objectives != null)
            {
                data.objectives = new ObjectiveData[definition.objectives.Length];
                for (int i = 0; i < definition.objectives.Length; i++)
                {
                    ObjectiveDefinition source = definition.objectives[i];
                    data.objectives[i] = new ObjectiveData
                    {
                        objectiveID = source.objectiveID,
                        stepType = ParseEnum(source.stepType, QuestStepType.Collect),
                        description = source.description,
                        targetID = source.targetID,
                        requiredAmount = source.requiredAmount,
                        countExistingInventory = source.countExistingInventory
                    };
                }
            }
            else
            {
                data.objectives = new ObjectiveData[0];
            }

            if (definition.rewards != null)
            {
                data.rewards = new Reward[definition.rewards.Length];
                for (int i = 0; i < definition.rewards.Length; i++)
                {
                    RewardDefinition source = definition.rewards[i];
                    data.rewards[i] = new Reward
                    {
                        type = ParseEnum(source.type, RewardType.XP),
                        amount = source.amount,
                        itemID = source.itemID
                    };
                }
            }
            else
            {
                data.rewards = new Reward[0];
            }

            return data;
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            if (Enum.TryParse(value, true, out T result))
            {
                return result;
            }

            // Sollte nach der Validierung nicht passieren – defensiv abfangen.
            Debug.LogWarning($"[QuestJsonLoader] '{value}' ist kein gültiger {typeof(T).Name} – Fallback {fallback}.");
            return fallback;
        }
    }
}
