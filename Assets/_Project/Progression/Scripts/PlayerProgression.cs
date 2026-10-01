using System;
using UnityEngine;

namespace SteepingSpirits.Progression
{
    /// <summary>
    /// Einfaches XP-/Level-System – Singleton, event-getrieben, im Stil der
    /// anderen Systeme. Schliesst den letzten offenen Reward-Typ: XP-Belohnungen
    /// aus Quests landen jetzt hier (siehe XpRewardCollector).
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        public static PlayerProgression Instance { get; private set; }

        [SerializeField] private int level = 1;
        [SerializeField] private int xp;
        [Tooltip("XP für den Sprung von Level 1 auf 2")]
        [SerializeField] private int baseXpToLevel = 100;
        [Tooltip("Wachstum pro Level (1.5 = jedes Level 50% mehr)")]
        [SerializeField] private float growth = 1.5f;

        public int Level => level;
        public int Xp => xp;
        public int XpToNext => XpForLevel(level);

        public static event Action<int, int> OnXpChanged; // aktuelles XP, XP bis nächstes Level
        public static event Action<int> OnLevelUp;         // neues Level

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            xp += amount;
            while (xp >= XpForLevel(level))
            {
                xp -= XpForLevel(level);
                level++;
                OnLevelUp?.Invoke(level);
            }

            OnXpChanged?.Invoke(xp, XpForLevel(level));
        }

        private int XpForLevel(int lvl)
        {
            return Mathf.RoundToInt(baseXpToLevel * Mathf.Pow(growth, lvl - 1));
        }
    }
}
