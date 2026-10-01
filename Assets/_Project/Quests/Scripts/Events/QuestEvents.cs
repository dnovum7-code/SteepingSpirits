using System;

namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Kommunikationsschicht des Quest Systems. Der QuestManager feuert
    /// Signale – interessiert sich aber nicht dafür, wer zuhört (UI, Sound,
    /// Inventory, NPCs, Achievements, XP, Analytics, ...).
    ///
    /// Die Raise-Methoden werden ausschliesslich vom QuestManager aufgerufen.
    /// </summary>
    public static class QuestEvents
    {
        /// <summary>Neue Quest wurde registriert (Asset oder Runtime-Generierung).</summary>
        public static event Action<QuestInstance> OnQuestRegistered;

        /// <summary>Nach QuestStart().</summary>
        public static event Action<QuestInstance> OnQuestStarted;

        /// <summary>Nach QuestContinue(). Liefert Objective und neuen Fortschritt.</summary>
        public static event Action<QuestInstance, ObjectiveData, int> OnObjectiveUpdated;

        /// <summary>
        /// Nach QuestFinish(). Reward-Verteilung: Listener lesen
        /// instance.Data.rewards (Inventory, XP-System, ...).
        /// </summary>
        public static event Action<QuestInstance> OnQuestCompleted;

        /// <summary>Quest-Bedingung nicht erfüllt (Timer, NPC gestorben, ...).</summary>
        public static event Action<QuestInstance> OnQuestFailed;

        /// <summary>Spieler hat die Quest manuell abgebrochen.</summary>
        public static event Action<QuestInstance> OnQuestAbandoned;

        /// <summary>Feuert bei jeder Zustandsänderung (praktisch für Marker und Quest-Log).</summary>
        public static event Action<QuestInstance> OnQuestStateChanged;

        public static void RaiseQuestRegistered(QuestInstance quest)
        {
            OnQuestRegistered?.Invoke(quest);
        }

        public static void RaiseQuestStarted(QuestInstance quest)
        {
            OnQuestStarted?.Invoke(quest);
        }

        public static void RaiseObjectiveUpdated(QuestInstance quest, ObjectiveData objective, int currentAmount)
        {
            OnObjectiveUpdated?.Invoke(quest, objective, currentAmount);
        }

        public static void RaiseQuestCompleted(QuestInstance quest)
        {
            OnQuestCompleted?.Invoke(quest);
        }

        public static void RaiseQuestFailed(QuestInstance quest)
        {
            OnQuestFailed?.Invoke(quest);
        }

        public static void RaiseQuestAbandoned(QuestInstance quest)
        {
            OnQuestAbandoned?.Invoke(quest);
        }

        public static void RaiseQuestStateChanged(QuestInstance quest)
        {
            OnQuestStateChanged?.Invoke(quest);
        }
    }
}
