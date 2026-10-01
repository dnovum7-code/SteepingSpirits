using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SteepingSpirits.Quests.Data
{
    /// <summary>
    /// Validiert Quest-JSON bevor daraus eine Quest wird – im Editor (Asset)
    /// wie zur Laufzeit (RAM). Tippfehler in handgeschriebenen Dateien fallen
    /// so sofort mit einer klaren Fehlerliste auf, statt still kaputt zu laufen.
    /// </summary>
    public static class QuestJsonValidator
    {
        // Namenskonvention: Q_[KATEGORIE]_[Beschreibender Name]
        private static readonly Regex QuestIdPattern =
            new Regex(@"^Q_(MAIN|SIDE|DAILY|TUTORIAL|EVENT|WORLD|FACTION)_[A-Za-z0-9]+$");

        /// <summary>
        /// Parst und validiert das JSON. Liefert true wenn die Definition
        /// benutzbar ist, sonst false mit einer Liste aller Fehler.
        /// </summary>
        public static bool TryParse(string json, out QuestDefinition definition, out List<string> errors)
        {
            definition = null;
            errors = new List<string>();

            if (string.IsNullOrEmpty(json))
            {
                errors.Add("JSON ist leer.");
                return false;
            }

            try
            {
                definition = JsonUtility.FromJson<QuestDefinition>(json);
            }
            catch (Exception e)
            {
                errors.Add("JSON konnte nicht geparst werden: " + e.Message);
                return false;
            }

            if (!Validate(definition, out errors))
            {
                definition = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prüft eine bereits geparste Definition (z.B. im Code gebaut).
        /// Gleiche Regeln wie beim JSON-Pfad.
        /// </summary>
        public static bool Validate(QuestDefinition definition, out List<string> errors)
        {
            errors = new List<string>();

            if (definition == null)
            {
                errors.Add("Keine Quest-Definition.");
                return false;
            }

            ValidateIdentity(definition, errors);
            ValidateObjectives(definition, errors);
            ValidateRewards(definition, errors);

            return errors.Count == 0;
        }

        private static void ValidateIdentity(QuestDefinition def, List<string> errors)
        {
            if (string.IsNullOrEmpty(def.questID))
            {
                errors.Add("questID fehlt.");
            }
            else if (!QuestIdPattern.IsMatch(def.questID))
            {
                errors.Add($"questID '{def.questID}' folgt nicht dem Schema Q_[MAIN|SIDE|DAILY|TUTORIAL|EVENT|WORLD|FACTION]_[Name].");
            }

            if (string.IsNullOrEmpty(def.displayName))
            {
                errors.Add("displayName fehlt.");
            }

            QuestCategory category;
            if (string.IsNullOrEmpty(def.category))
            {
                errors.Add("category fehlt.");
            }
            else if (!TryParseEnum(def.category, out category))
            {
                errors.Add($"category '{def.category}' ist unbekannt (erlaubt: MAIN, SIDE, DAILY, TUTORIAL, EVENT, WORLD, FACTION).");
            }
            else if (!string.IsNullOrEmpty(def.questID)
                     && QuestIdPattern.IsMatch(def.questID)
                     && !def.questID.StartsWith("Q_" + category + "_"))
            {
                errors.Add($"questID '{def.questID}' passt nicht zur category '{def.category}'.");
            }
        }

        private static void ValidateObjectives(QuestDefinition def, List<string> errors)
        {
            if (def.objectives == null || def.objectives.Length == 0)
            {
                errors.Add("Mindestens ein Objective wird benötigt.");
                return;
            }

            var seenIDs = new HashSet<string>();

            for (int i = 0; i < def.objectives.Length; i++)
            {
                ObjectiveDefinition objective = def.objectives[i];
                string prefix = $"objectives[{i}]: ";

                if (objective == null)
                {
                    errors.Add(prefix + "Eintrag ist leer.");
                    continue;
                }

                if (string.IsNullOrEmpty(objective.objectiveID))
                {
                    errors.Add(prefix + "objectiveID fehlt.");
                }
                else if (!seenIDs.Add(objective.objectiveID))
                {
                    errors.Add(prefix + $"objectiveID '{objective.objectiveID}' ist doppelt.");
                }

                QuestStepType stepType;
                if (string.IsNullOrEmpty(objective.stepType))
                {
                    errors.Add(prefix + "stepType fehlt.");
                }
                else if (!TryParseEnum(objective.stepType, out stepType))
                {
                    errors.Add(prefix + $"stepType '{objective.stepType}' ist unbekannt (erlaubt: Collect, Defeat, Visit, Bring, Talk).");
                }

                if (string.IsNullOrEmpty(objective.targetID))
                {
                    errors.Add(prefix + "targetID fehlt.");
                }

                if (objective.requiredAmount < 1)
                {
                    errors.Add(prefix + $"requiredAmount muss mindestens 1 sein (ist {objective.requiredAmount}).");
                }
            }
        }

        private static void ValidateRewards(QuestDefinition def, List<string> errors)
        {
            if (def.rewards == null)
            {
                return; // Rewards sind optional.
            }

            for (int i = 0; i < def.rewards.Length; i++)
            {
                RewardDefinition reward = def.rewards[i];
                string prefix = $"rewards[{i}]: ";

                if (reward == null)
                {
                    errors.Add(prefix + "Eintrag ist leer.");
                    continue;
                }

                RewardType type;
                if (string.IsNullOrEmpty(reward.type))
                {
                    errors.Add(prefix + "type fehlt.");
                    continue;
                }

                if (!TryParseEnum(reward.type, out type))
                {
                    errors.Add(prefix + $"type '{reward.type}' ist unbekannt (erlaubt: XP, Gold, Item).");
                    continue;
                }

                if (type == RewardType.Item && string.IsNullOrEmpty(reward.itemID))
                {
                    errors.Add(prefix + "itemID fehlt für Item-Reward.");
                }

                if (type != RewardType.Item && reward.amount <= 0)
                {
                    errors.Add(prefix + $"amount muss grösser 0 sein (ist {reward.amount}).");
                }
            }
        }

        private static bool TryParseEnum<T>(string value, out T result) where T : struct
        {
            return Enum.TryParse(value, true, out result);
        }
    }
}
