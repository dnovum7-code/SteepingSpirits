using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Inventory;
using SteepingSpirits.Plantation;
using SteepingSpirits.Processing;
using SteepingSpirits.World;

namespace SteepingSpirits.SaveGame
{
    /// <summary>
    /// The shared save service: keeps the player inventory (including items made at runtime – teas,
    /// blends, dried herbs, leaf variants, seeds) and the game clock across restarts in
    /// persistentDataPath/game_save.json. Starts before any scene Start() (it creates PlayerInventory
    /// and GameClock if needed and loads at once), so module code that looks at the inventory on Start
    /// already sees the saved items. Saves on scene change, on a new day, every few seconds when
    /// something changed and on quit. Unknown items are kept and come back as soon as a scene
    /// registers them.
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        private const float SaveEverySeconds = 20f;

        public static SaveService Instance { get; private set; }

        /// <summary>Tools and tests switch the service off (no file access).</summary>
        public static bool Disabled { get; set; }

        private readonly List<ItemEntry> pending = new List<ItemEntry>();
        private bool loaded;
        private float saveAt;
        private float retryAt;

        public static string SavePath => Path.Combine(Application.persistentDataPath, GameSaveData.FileName);
        public static string LegacyPath => Path.Combine(Application.persistentDataPath, GameSaveData.LegacyInventoryFileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            Disabled = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Disabled || Instance != null) return;
            var go = new GameObject("SaveService");
            DontDestroyOnLoad(go);
            go.AddComponent<SaveService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RegisterModuleRecipes();
            EnsureSystems();
            Load();
            SceneManager.activeSceneChanged += OnSceneChanged;
            GameClock.OnNewDay += OnNewDay;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            GameClock.OnNewDay -= OnNewDay;
            Instance = null;
        }

        private void OnApplicationQuit() => Save();

        private void OnSceneChanged(Scene from, Scene to)
        {
            Save();
            retryAt = 0f;
        }

        private void OnNewDay(GameClock clock) => Save();

        private void Update()
        {
            if (!loaded) return;
            if (pending.Count > 0 && Time.unscaledTime >= retryAt)
            {
                retryAt = Time.unscaledTime + 1f;
                PutBack(pending);
            }

            if (Time.unscaledTime >= saveAt)
            {
                saveAt = Time.unscaledTime + SaveEverySeconds;
                Save();
            }
        }

        // ---------------- Load / save ----------------

        private static void EnsureSystems()
        {
            if (FindAnyObjectByType<PlayerInventory>() == null) new GameObject("PlayerInventory").AddComponent<PlayerInventory>();
            if (FindAnyObjectByType<GameClock>() == null) new GameObject("GameClock").AddComponent<GameClock>();
        }

        public void Load()
        {
            loaded = true;
            saveAt = Time.unscaledTime + SaveEverySeconds;
            GameSaveData data = Read();
            if (data == null) return;

            if (data.hasClock && GameClock.Instance != null)
            {
                GameClock.Instance.ApplySaveData(new GameClock.ClockSaveData
                {
                    day = data.clock.day,
                    season = (Season)Mathf.Clamp(data.clock.season, 0, 3),
                    year = data.clock.year,
                    minuteOfDay = data.clock.minuteOfDay
                });
            }

            var all = new List<ItemEntry>(data.inventory);
            all.AddRange(data.pending);
            pending.Clear();
            PutBack(all);
            if (data.loadedVersion < GameSaveData.Version) Save(); // migrate right away
        }

        private static GameSaveData Read()
        {
            try
            {
                if (File.Exists(SavePath)) return GameSaveData.FromJson(File.ReadAllText(SavePath));
                if (File.Exists(LegacyPath)) return GameSaveData.FromLegacyInventoryJson(File.ReadAllText(LegacyPath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveGame] Save could not be read (" + e.Message + ") – moved aside, starting fresh.");
                MoveAside();
            }

            return null;
        }

        private static void MoveAside()
        {
            try
            {
                string aside = Path.Combine(Application.persistentDataPath, "game_save.broken.json");
                if (File.Exists(aside)) File.Delete(aside);
                if (File.Exists(SavePath)) File.Move(SavePath, aside);
            }
            catch (Exception)
            {
                // Keep going; the next save overwrites it.
            }
        }

        /// <summary>Puts entries into the inventory, rebuilding runtime items; whatever does not fit or is unknown waits.</summary>
        private void PutBack(List<ItemEntry> entries)
        {
            PlayerInventory inv = PlayerInventory.Instance;
            var waiting = new List<ItemEntry>();
            foreach (ItemEntry e in entries)
            {
                if (inv == null || !EnsureItem(inv, e.id))
                {
                    waiting.Add(e);
                    continue;
                }

                int left = inv.AddById(e.id, e.amount);
                if (left > 0) waiting.Add(new ItemEntry(e.id, left));
            }

            if (ReferenceEquals(entries, pending)) pending.Clear();
            pending.AddRange(waiting);
        }

        /// <summary>The item is known to the inventory – or can be rebuilt from its id.</summary>
        public static bool EnsureItem(PlayerInventory inv, string itemId)
        {
            if (inv.Resolve(itemId) != null) return true;
            if (!ItemRecipes.TryDescribe(itemId, out ItemRecipe r)) return false;
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.name = itemId;
            item.itemID = itemId;
            item.displayName = r.displayName;
            item.description = r.description;
            item.category = ItemCategory.Material;
            item.maxStack = Mathf.Max(1, r.maxStack);
            item.goldValue = r.goldValue;
            item.tint = ColourOf(r);
            inv.RegisterItem(item);
            return true;
        }

        public void Save()
        {
            if (Disabled || !loaded) return;
            var data = new GameSaveData();
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv != null)
            {
                foreach (ItemStack stack in inv.Slots)
                {
                    if (stack?.Item != null && stack.Amount > 0) data.inventory.Add(new ItemEntry(stack.Item.itemID, stack.Amount));
                }
            }

            data.pending.AddRange(pending);
            GameClock clock = GameClock.Instance;
            if (clock != null)
            {
                GameClock.ClockSaveData c = clock.ToSaveData();
                data.hasClock = true;
                data.clock = new ClockEntry { day = c.day, season = (int)c.season, year = c.year, minuteOfDay = c.minuteOfDay };
            }

            string tmp = SavePath + ".tmp";
            try
            {
                File.WriteAllText(tmp, data.ToJson());
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveGame] Not saved: " + e.Message);
            }
        }

        // ---------------- Module recipes and colours ----------------

        /// <summary>Garden seeds and compost are runtime items too; their names live in GardenTexts.</summary>
        private static void RegisterModuleRecipes()
        {
            ItemRecipes.Register(GardenRecipe);
        }

        private static ItemRecipe? GardenRecipe(string id)
        {
            if (id == GardenItems.CompostItemId)
            {
                return new ItemRecipe { itemId = id, displayName = GardenTexts.CompostName, description = GardenTexts.CompostDescription, goldValue = 1, maxStack = 99, kind = ItemKind.Other, baseId = id, tier = 1 };
            }

            if (!id.StartsWith("seed_", StringComparison.Ordinal)) return null;
            string plant = id.Substring("seed_".Length);
            if (StarterPlants.Create(plant) == null) return null;
            return new ItemRecipe { itemId = id, displayName = GardenTexts.SeedName(plant), description = GardenTexts.SeedDescription(plant), goldValue = 1, maxStack = 99, kind = ItemKind.Seed, baseId = plant, tier = 1 };
        }

        private static Color ColourOf(ItemRecipe r)
        {
            switch (r.kind)
            {
                case ItemKind.Tea:
                case ItemKind.Blend:
                    return r.baseId == SteepingSpirits.Ingredients.IngredientCatalog.WhiteTea ? LeafLook.TeaColor(TeaVariety.White)
                        : r.baseId == SteepingSpirits.Ingredients.IngredientCatalog.GreenTea ? LeafLook.TeaColor(TeaVariety.Green)
                        : r.baseId == SteepingSpirits.Ingredients.IngredientCatalog.BlackTea ? LeafLook.TeaColor(TeaVariety.Black)
                        : LeafLook.TeaColor(TeaVariety.DarkOolong);
                case ItemKind.DriedHerb: return new Color(0.55f, 0.6f, 0.4f);
                case ItemKind.Seed: return new Color(0.72f, 0.58f, 0.38f);
                case ItemKind.Ingredient: return PlantPalette.Ripe(r.baseId == SteepingSpirits.Ingredients.IngredientCatalog.TeaLeaf ? StarterPlants.Tea : r.baseId);
                default: return new Color(0.36f, 0.26f, 0.18f);
            }
        }
    }
}
