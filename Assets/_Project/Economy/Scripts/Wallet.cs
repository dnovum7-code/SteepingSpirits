using System;
using UnityEngine;

namespace SteepingSpirits.Economy
{
    /// <summary>
    /// Minimaler Geldbeutel des Spielers – Singleton, event-getrieben, im
    /// gleichen Stil wie QuestManager / PlayerInventory. Gibt Gold ein Zuhause,
    /// damit Belohnungen, Shops und Cheats damit arbeiten können.
    /// </summary>
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }

        [SerializeField] private int startingGold;

        public int Gold { get; private set; }

        /// <summary>Neuer Gesamtstand nach jeder Änderung.</summary>
        public static event Action<int> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Gold = Mathf.Max(0, startingGold);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        /// <summary>Gibt Gold aus, wenn genug da ist. true = erfolgreich.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Gold < amount)
            {
                return false;
            }

            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public void Set(int amount)
        {
            Gold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
