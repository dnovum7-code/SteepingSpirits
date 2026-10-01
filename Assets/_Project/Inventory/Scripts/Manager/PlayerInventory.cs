using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SteepingSpirits.Inventory
{
    /// <summary>
    /// Zentraler Zugriffspunkt auf das Spieler-Inventar – analog zum
    /// QuestManager. Umschliesst einen reinen Inventory-Container, kennt den
    /// Item-Katalog (ID → ItemData), feuert InventoryEvents und speichert/lädt.
    ///
    /// Alle anderen Systeme (UI, Aufsammeln, Quest-Belohnungen) gehen über ihn.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Inventar")]
        [Tooltip("Maximale Slots (0 = unbegrenzt)")]
        [SerializeField] private int capacity = 24;

        [Header("Item-Katalog")]
        [Tooltip("Bekannte Items – nötig um IDs (z.B. aus Quests/Aufsammeln) aufzulösen")]
        [SerializeField] private List<ItemData> itemCatalog = new List<ItemData>();

        [Tooltip("Zusätzlich alle ItemData aus einem Resources-Ordner laden")]
        [SerializeField] private bool loadCatalogFromResources;
        [SerializeField] private string resourcesFolder = "Items";

        [Header("Speichern")]
        [SerializeField] private string saveFileName = "inventory_save.json";
        [SerializeField] private bool loadSaveOnStart;
        [SerializeField] private bool saveOnQuit = true;

        private readonly Dictionary<string, ItemData> catalog = new Dictionary<string, ItemData>();
        private Inventory inventory;

        public IReadOnlyList<ItemStack> Slots => inventory.Slots;
        public int Capacity => capacity;

        /// <summary>Alle bekannten Items (für Tools wie das Dev-Cheat-Fenster).</summary>
        public IEnumerable<ItemData> CatalogItems => catalog.Values;

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PlayerInventory] Es existiert bereits eine Instanz – Duplikat wird zerstört.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            inventory = new Inventory(capacity);
            BuildCatalog();
        }

        private void Start()
        {
            if (loadSaveOnStart)
            {
                LoadFromDisk();
            }
        }

        private void OnApplicationQuit()
        {
            if (saveOnQuit)
            {
                SaveToDisk();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ---------------------------------------------------------------
        // Katalog (ID → ItemData)
        // ---------------------------------------------------------------

        private void BuildCatalog()
        {
            catalog.Clear();

            foreach (ItemData item in itemCatalog)
            {
                RegisterItem(item);
            }

            if (loadCatalogFromResources)
            {
                foreach (ItemData item in Resources.LoadAll<ItemData>(resourcesFolder))
                {
                    RegisterItem(item);
                }
            }
        }

        /// <summary>Macht ein Item bekannt (auch zur Laufzeit nutzbar).</summary>
        public void RegisterItem(ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.itemID))
            {
                return;
            }

            catalog[item.itemID] = item;
        }

        public ItemData Resolve(string itemID)
        {
            return !string.IsNullOrEmpty(itemID) && catalog.TryGetValue(itemID, out ItemData item) ? item : null;
        }

        // ---------------------------------------------------------------
        // Hinzufügen / Entnehmen / Benutzen
        // ---------------------------------------------------------------

        /// <summary>Legt Items ins Inventar. Gibt zurück, wie viel nicht mehr passte (0 = alles).</summary>
        public int Add(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return amount;
            }

            int leftover = inventory.Add(item, amount);
            int added = amount - leftover;

            if (added > 0)
            {
                InventoryEvents.RaiseItemAdded(item, added);
            }

            if (leftover > 0)
            {
                Debug.LogWarning($"[PlayerInventory] Inventar voll – {leftover}x '{item.itemID}' passte nicht mehr.");
            }

            return leftover;
        }

        /// <summary>Wie Add, aber über die itemID (löst über den Katalog auf).</summary>
        public int AddById(string itemID, int amount = 1)
        {
            ItemData item = Resolve(itemID);
            if (item == null)
            {
                Debug.LogWarning($"[PlayerInventory] Unbekannte itemID '{itemID}' – nicht im Katalog. Item ergänzen oder RegisterItem() aufrufen.");
                return amount;
            }

            return Add(item, amount);
        }

        public int Remove(string itemID, int amount = 1)
        {
            int removed = inventory.Remove(itemID, amount);
            if (removed > 0)
            {
                InventoryEvents.RaiseItemRemoved(Resolve(itemID), removed);
            }

            return removed;
        }

        public bool Has(string itemID, int amount = 1) => inventory.Has(itemID, amount);
        public int Count(string itemID) => inventory.Count(itemID);

        /// <summary>
        /// Benutzt ein Item (z.B. Trank): entfernt 1 Stück und feuert
        /// OnItemUsed. Die eigentliche Wirkung (Heilen …) übernimmt ein
        /// Listener – das Inventar kennt sie nicht.
        /// </summary>
        public bool Use(string itemID)
        {
            ItemData item = Resolve(itemID);
            if (item == null || !item.usable || !Has(itemID))
            {
                return false;
            }

            inventory.Remove(itemID, 1);
            InventoryEvents.RaiseItemUsed(item);
            InventoryEvents.RaiseItemRemoved(item, 1);
            return true;
        }

        // ---------------------------------------------------------------
        // Speichern / Laden
        // ---------------------------------------------------------------

        public void SaveToDisk()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(inventory.ToSaveData(), true));
            Debug.Log($"[PlayerInventory] Inventar gespeichert: {SavePath}");
        }

        public bool LoadFromDisk()
        {
            if (!File.Exists(SavePath))
            {
                return false;
            }

            var save = JsonUtility.FromJson<InventorySaveData>(File.ReadAllText(SavePath));
            inventory.ApplySaveData(save, Resolve);
            InventoryEvents.RaiseInventoryChanged();
            return true;
        }
    }
}
