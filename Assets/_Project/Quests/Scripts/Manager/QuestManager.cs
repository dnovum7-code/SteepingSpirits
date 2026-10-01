using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SteepingSpirits.Quests.Data;
using SteepingSpirits.Quests.Markers;
using SteepingSpirits.Quests.Steps;

namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Zentraler Verwaltungspunkt und einziger Einstiegspunkt für alle
    /// quest-bezogenen Operationen. UI, NPCs und Gameplay kommunizieren
    /// ausschliesslich über ihn – nach aussen feuert er QuestEvents.
    ///
    /// Quellen für Quests: QuestData-Assets, JSON-Dateien (TextAsset) und
    /// Resources-Ordner – oder zur Laufzeit per RegisterQuest / RegisterQuestFromJson.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Quest-Definitionen")]
        [Tooltip("QuestData-Assets die beim Start registriert werden")]
        [SerializeField] private QuestData[] questAssets;

        [Tooltip("Quest-JSON-Dateien (TextAsset), werden beim Start validiert und registriert")]
        [SerializeField] private TextAsset[] questJsonFiles;

        [Tooltip("Zusätzlich alle QuestData + Quest-JSONs aus einem Resources-Ordner laden")]
        [SerializeField] private bool loadFromResources;
        [SerializeField] private string resourcesFolder = "Quests";

        [Header("World-Marker")]
        [Tooltip("Optional: Marker-Prefab, wird pro Quest an startLocation gespawnt")]
        [SerializeField] private QuestWorldMarker markerPrefab;

        [Header("Speichern")]
        [SerializeField] private string saveFileName = "quest_save.json";
        [SerializeField] private bool loadSaveOnStart;
        [SerializeField] private bool saveOnQuit = true;

        // Alle registrierten Quests, Zugriff per questID – keine Duplikate.
        private readonly Dictionary<string, QuestInstance> activeQuests = new Dictionary<string, QuestInstance>();

        // Quest-GameObjects mit den laufenden Step-Templates (nur für ACTIVE Quests).
        private readonly Dictionary<string, GameObject> questStepObjects = new Dictionary<string, GameObject>();

        private string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, saveFileName); }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[QuestManager] Es existiert bereits eine Instanz – Duplikat wird zerstört.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Registrierung bewusst in Start(): UI und Marker abonnieren
            // QuestEvents in OnEnable und verpassen so keine Events.
            if (questAssets != null)
            {
                foreach (QuestData data in questAssets)
                {
                    // Leere Inspector-Slots still überspringen.
                    if (data != null)
                    {
                        RegisterQuest(data);
                    }
                }
            }

            if (questJsonFiles != null)
            {
                foreach (TextAsset file in questJsonFiles)
                {
                    if (file != null)
                    {
                        RegisterQuestFromJson(file.text, file.name);
                    }
                }
            }

            if (loadFromResources)
            {
                foreach (QuestData data in Resources.LoadAll<QuestData>(resourcesFolder))
                {
                    RegisterQuest(data);
                }

                foreach (TextAsset file in Resources.LoadAll<TextAsset>(resourcesFolder))
                {
                    RegisterQuestFromJson(file.text, file.name);
                }
            }

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
        // Registrierung (Editor-Assets und Runtime-Generierung)
        // ---------------------------------------------------------------

        /// <summary>
        /// Nimmt eine Quest-Definition entgegen – ein Asset oder ein zur
        /// Laufzeit erzeugtes Objekt – und macht sie als AVAILABLE sichtbar.
        /// </summary>
        public QuestInstance RegisterQuest(QuestData data)
        {
            if (data == null || string.IsNullOrEmpty(data.questID))
            {
                Debug.LogError("[QuestManager] RegisterQuest: QuestData ist null oder hat keine questID.");
                return null;
            }

            if (activeQuests.ContainsKey(data.questID))
            {
                Debug.LogWarning($"[QuestManager] Quest '{data.questID}' ist bereits registriert.");
                return activeQuests[data.questID];
            }

            var instance = new QuestInstance(data);
            activeQuests.Add(data.questID, instance);

            SetState(instance, QuestState.AVAILABLE);
            SpawnWorldMarker(instance);
            QuestEvents.RaiseQuestRegistered(instance);
            return instance;
        }

        /// <summary>
        /// JSON validieren, QuestData im RAM erzeugen (kein Asset auf Disk) und
        /// direkt registrieren. source = Name für Fehlermeldungen (z.B. Dateiname).
        /// </summary>
        public QuestInstance RegisterQuestFromJson(string json, string source = null)
        {
            QuestData data = QuestJsonLoader.CreateFromJson(json, out List<string> errors);

            if (data == null)
            {
                string from = string.IsNullOrEmpty(source) ? "" : $" ({source})";
                Debug.LogError($"[QuestManager] Quest-JSON ungültig{from}:\n- " + string.Join("\n- ", errors.ToArray()));
                return null;
            }

            return RegisterQuest(data);
        }

        // ---------------------------------------------------------------
        // Quest-Lebenszyklus
        // ---------------------------------------------------------------

        /// <summary>Lädt QuestData, aktiviert die QuestInstance und spawnt die Step-Templates.</summary>
        public bool QuestStart(string questID)
        {
            QuestInstance instance = GetQuest(questID);
            if (instance == null)
            {
                Debug.LogError($"[QuestManager] QuestStart: Quest '{questID}' ist nicht registriert.");
                return false;
            }

            switch (instance.State)
            {
                case QuestState.ACTIVE:
                    Debug.LogWarning($"[QuestManager] Quest '{questID}' läuft bereits.");
                    return false;

                case QuestState.COMPLETED:
                case QuestState.FAILED:
                    if (!instance.Data.isRepeatable)
                    {
                        Debug.LogWarning($"[QuestManager] Quest '{questID}' ist nicht wiederholbar.");
                        return false;
                    }
                    instance.ResetProgress();
                    break;

                case QuestState.ABANDONED:
                    // Abgebrochene Quests dürfen immer neu angenommen werden.
                    instance.ResetProgress();
                    break;
            }

            SetState(instance, QuestState.ACTIVE);
            SpawnQuestSteps(instance);
            QuestEvents.RaiseQuestStarted(instance);
            return true;
        }

        /// <summary>
        /// Aktualisiert den Fortschritt eines Objectives. Sucht die passende
        /// aktive Quest anhand der Objective-ID (z.B. QuestContinue("collect_herbs", 1)).
        /// </summary>
        public void QuestContinue(string objectiveID, int amount = 1)
        {
            foreach (QuestInstance instance in activeQuests.Values)
            {
                ObjectiveData objective;
                if (instance.State == QuestState.ACTIVE && instance.TryGetObjective(objectiveID, out objective))
                {
                    QuestContinue(instance.Data.questID, objectiveID, amount);
                    return;
                }
            }

            Debug.LogWarning($"[QuestManager] QuestContinue: Kein aktives Objective '{objectiveID}' gefunden.");
        }

        /// <summary>Aktualisiert den Fortschritt eines Objectives einer bestimmten Quest.</summary>
        public void QuestContinue(string questID, string objectiveID, int amount)
        {
            QuestInstance instance = GetQuest(questID);
            if (instance == null || instance.State != QuestState.ACTIVE)
            {
                Debug.LogWarning($"[QuestManager] QuestContinue: Quest '{questID}' ist nicht aktiv.");
                return;
            }

            ObjectiveData objective;
            if (!instance.TryGetObjective(objectiveID, out objective))
            {
                Debug.LogWarning($"[QuestManager] QuestContinue: Objective '{objectiveID}' existiert nicht in '{questID}'.");
                return;
            }

            int delta = instance.AddProgress(objectiveID, amount);
            if (delta == 0)
            {
                return;
            }

            QuestEvents.RaiseObjectiveUpdated(instance, objective, instance.GetProgress(objectiveID));

            // Alle Ziele erfüllt → Quest automatisch abschliessen.
            if (instance.AreAllObjectivesComplete())
            {
                QuestFinish(questID);
            }
        }

        /// <summary>
        /// Schliesst die Quest ab und feuert OnQuestCompleted. Die Rewards
        /// verteilen die Listener (Inventory, XP-System, ...) anhand von
        /// instance.Data.rewards.
        /// </summary>
        public void QuestFinish(string questID)
        {
            QuestInstance instance = GetQuest(questID);
            if (instance == null || instance.State != QuestState.ACTIVE)
            {
                Debug.LogWarning($"[QuestManager] QuestFinish: Quest '{questID}' ist nicht aktiv.");
                return;
            }

            SetState(instance, QuestState.COMPLETED);
            DestroyQuestSteps(questID);
            QuestEvents.RaiseQuestCompleted(instance);
        }

        /// <summary>Markiert eine aktive Quest als gescheitert (Timer abgelaufen, NPC gestorben, ...).</summary>
        public void QuestFail(string questID)
        {
            QuestInstance instance = GetQuest(questID);
            if (instance == null || instance.State != QuestState.ACTIVE)
            {
                Debug.LogWarning($"[QuestManager] QuestFail: Quest '{questID}' ist nicht aktiv.");
                return;
            }

            SetState(instance, QuestState.FAILED);
            DestroyQuestSteps(questID);
            QuestEvents.RaiseQuestFailed(instance);
        }

        /// <summary>Spieler bricht die Quest manuell ab.</summary>
        public void AbandonQuest(string questID)
        {
            QuestInstance instance = GetQuest(questID);
            if (instance == null || instance.State != QuestState.ACTIVE)
            {
                Debug.LogWarning($"[QuestManager] AbandonQuest: Quest '{questID}' ist nicht aktiv.");
                return;
            }

            SetState(instance, QuestState.ABANDONED);
            DestroyQuestSteps(questID);
            QuestEvents.RaiseQuestAbandoned(instance);
        }

        /// <summary>
        /// Setzt eine Quest auf AVAILABLE zurück (Fortschritt 0) – z.B. jeden
        /// Morgen für Daily-Quests (siehe DailyQuestReset). Läuft sie gerade,
        /// werden ihre Steps beendet.
        /// </summary>
        public bool ResetQuest(string questID)
        {
            QuestInstance instance = GetQuest(questID);
            if (instance == null)
            {
                return false;
            }

            DestroyQuestSteps(questID);
            instance.ResetProgress();
            SetState(instance, QuestState.AVAILABLE);
            return true;
        }

        /// <summary>
        /// Entfernt eine Quest komplett aus dem Manager (inkl. Step-Objekte).
        /// Nützlich fürs Dev-Menü / zum Aufräumen von Testquests.
        /// </summary>
        public bool RemoveQuest(string questID)
        {
            if (!activeQuests.TryGetValue(questID, out QuestInstance instance))
            {
                return false;
            }

            DestroyQuestSteps(questID);
            activeQuests.Remove(questID);
            QuestEvents.RaiseQuestStateChanged(instance); // Listener (Log/Tracker) aktualisieren
            return true;
        }

        // ---------------------------------------------------------------
        // Abfragen (z.B. für das Quest-Log)
        // ---------------------------------------------------------------

        public QuestInstance GetQuest(string questID)
        {
            QuestInstance instance;
            return activeQuests.TryGetValue(questID, out instance) ? instance : null;
        }

        public IEnumerable<QuestInstance> GetAllQuests()
        {
            return activeQuests.Values;
        }

        public List<QuestInstance> GetActiveQuests()
        {
            var result = new List<QuestInstance>();
            foreach (QuestInstance instance in activeQuests.Values)
            {
                if (instance.State == QuestState.ACTIVE)
                {
                    result.Add(instance);
                }
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Speichern / Laden (JSON auf Disk)
        // ---------------------------------------------------------------

        public void SaveToDisk()
        {
            var file = new QuestSaveFile();
            foreach (QuestInstance instance in activeQuests.Values)
            {
                file.quests.Add(instance.ToSaveData());
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(file, true));
            Debug.Log($"[QuestManager] {file.quests.Count} Quests gespeichert: {SavePath}");
        }

        public bool LoadFromDisk()
        {
            if (!File.Exists(SavePath))
            {
                return false;
            }

            var file = JsonUtility.FromJson<QuestSaveFile>(File.ReadAllText(SavePath));
            if (file == null || file.quests == null)
            {
                return false;
            }

            foreach (QuestInstanceSaveData save in file.quests)
            {
                QuestInstance instance = GetQuest(save.questID);
                if (instance == null)
                {
                    // Zur Laufzeit erzeugte Quests (z.B. aus Code) müssen vor
                    // dem Laden erneut registriert sein.
                    Debug.LogWarning($"[QuestManager] Gespeicherte Quest '{save.questID}' ist nicht registriert – übersprungen.");
                    continue;
                }

                DestroyQuestSteps(save.questID);
                instance.ApplySaveData(save);

                if (instance.State == QuestState.ACTIVE)
                {
                    SpawnQuestSteps(instance);
                }

                QuestEvents.RaiseQuestStateChanged(instance);
            }

            return true;
        }

        [Serializable]
        private class QuestSaveFile
        {
            public List<QuestInstanceSaveData> quests = new List<QuestInstanceSaveData>();
        }

        // ---------------------------------------------------------------
        // Intern
        // ---------------------------------------------------------------

        private void SetState(QuestInstance instance, QuestState state)
        {
            instance.State = state;
            QuestEvents.RaiseQuestStateChanged(instance);
        }

        /// <summary>
        /// Erzeugt das Quest-GameObject und legt pro Objective das passende
        /// Step-Template darauf ab (siehe Systemdiagramm: "Quest GO").
        /// </summary>
        private void SpawnQuestSteps(QuestInstance instance)
        {
            if (questStepObjects.ContainsKey(instance.Data.questID))
            {
                return;
            }

            var questGO = new GameObject("Quest_" + instance.Data.questID);
            questGO.transform.SetParent(transform);

            if (instance.Data.objectives != null)
            {
                foreach (ObjectiveData objective in instance.Data.objectives)
                {
                    QuestStepFactory.AddStep(questGO, instance, objective);
                }
            }

            questStepObjects.Add(instance.Data.questID, questGO);
        }

        private void DestroyQuestSteps(string questID)
        {
            GameObject questGO;
            if (questStepObjects.TryGetValue(questID, out questGO))
            {
                questStepObjects.Remove(questID);
                Destroy(questGO);
            }
        }

        private void SpawnWorldMarker(QuestInstance instance)
        {
            // Kein Prefab ODER startLocation 0,0 (bzw. im JSON weggelassen) → kein Marker.
            if (markerPrefab == null || instance.Data.startLocation == Vector2.zero)
            {
                return;
            }

            Vector2 at = instance.Data.startLocation;
            QuestWorldMarker marker = Instantiate(markerPrefab, new Vector3(at.x, at.y, 0f), Quaternion.identity);
            marker.name = "QuestMarker_" + instance.Data.questID;
            marker.Bind(instance);
        }
    }
}
