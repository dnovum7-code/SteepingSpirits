using UnityEngine;
using SteepingSpirits.Core.Audio;

namespace SteepingSpirits.Quests.Integration
{
    /// <summary>
    /// Spielt kleine Sounds zu Quest-Ereignissen (Start, Fortschritt, Ziel
    /// erreicht, Abschluss, Fehlschlag) und beim Aufsammeln von Items.
    /// Die Clips werden per Code erzeugt (siehe ProceduralSfx) – kein Asset
    /// nötig. Zero-Wiring: einfach in die Szene legen.
    /// Duplikatsicher: immer nur EINES aktiv.
    /// </summary>
    public class QuestAudioHooks : MonoBehaviour
    {
        private static QuestAudioHooks active;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.6f;

        [Header("Eigene Sounds")]
        [Tooltip("Resources-Unterordner mit eigenen AudioClips (siehe README im Ordner)")]
        [SerializeField] private string audioFolder = "QuestSfx";

        [Tooltip("Wenn kein eigener Clip gefunden wird: Code-Ton als Ersatz (aus = kein Sound)")]
        [SerializeField] private bool useProceduralFallback = true;

        /// <summary>Ist ein aktives QuestAudioHooks in der Szene?</summary>
        public static bool HasInstance => active != null;

        /// <summary>Lautstärke der Quest-SFX (0..1) – z.B. für einen Regler.</summary>
        public static float MasterVolume
        {
            get => active != null ? active.volume : 0f;
            set { if (active != null) active.volume = Mathf.Clamp01(value); }
        }

        private AudioSource source;
        private AudioClip pickupClip;
        private AudioClip startClip;
        private AudioClip progressClip;
        private AudioClip objectiveDoneClip;
        private AudioClip completeClip;
        private AudioClip failClip;

        private void Awake()
        {
            EnsureAudioListener();

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D
            BuildClips();
        }

        // Ohne AudioListener bleibt ALLES stumm – häufigste Ursache für „kein Sound".
        private static void EnsureAudioListener()
        {
            if (Object.FindAnyObjectByType<AudioListener>() != null)
            {
                return;
            }

            Camera cam = Camera.main;
            GameObject host = cam != null ? cam.gameObject : new GameObject("AudioListener");
            host.AddComponent<AudioListener>();
            Debug.Log("[QuestAudioHooks] Kein AudioListener gefunden – automatisch einen hinzugefügt.");
        }

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
            QuestEvents.OnQuestStarted += HandleStarted;
            QuestEvents.OnObjectiveUpdated += HandleObjective;
            QuestEvents.OnQuestCompleted += HandleCompleted;
            QuestEvents.OnQuestFailed += HandleFailed;
            GameplayEvents.OnItemCollected += HandlePickup;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                QuestEvents.OnQuestStarted -= HandleStarted;
                QuestEvents.OnObjectiveUpdated -= HandleObjective;
                QuestEvents.OnQuestCompleted -= HandleCompleted;
                QuestEvents.OnQuestFailed -= HandleFailed;
                GameplayEvents.OnItemCollected -= HandlePickup;
                active = null;
            }
        }

        private void BuildClips()
        {
            // Reihenfolge pro Sound: 1) eigener Clip aus dem Resources-Ordner
            // (nach Dateiname), sonst 2) Code-Ton – oder gar nichts, wenn der
            // Fallback ausgeschaltet ist.
            pickupClip = Resolve("item_pickup", () => ProceduralSfx.Tone("sfx_pickup", 880f, 0.09f, 0.6f));
            startClip = Resolve("quest_start", () => ProceduralSfx.Melody("sfx_start", new[] { 523f, 784f }, 0.22f, 0.6f));
            progressClip = Resolve("quest_progress", () => ProceduralSfx.Tone("sfx_progress", 620f, 0.07f, 0.5f));
            objectiveDoneClip = Resolve("quest_objective", () => ProceduralSfx.Tone("sfx_objdone", 988f, 0.12f, 0.6f));
            completeClip = Resolve("quest_complete", () => ProceduralSfx.Melody("sfx_complete", new[] { 523f, 659f, 784f, 1046f }, 0.5f, 0.6f));
            failClip = Resolve("quest_fail", () => ProceduralSfx.Melody("sfx_fail", new[] { 392f, 294f, 196f }, 0.42f, 0.55f));
        }

        /// <summary>
        /// Eigener Clip aus Resources/&lt;audioFolder&gt;/&lt;name&gt; – wenn vorhanden.
        /// Sonst der Code-Ton (falls Fallback an), sonst null (kein Sound).
        /// </summary>
        private AudioClip Resolve(string name, System.Func<AudioClip> procedural)
        {
            AudioClip custom = string.IsNullOrEmpty(audioFolder)
                ? Resources.Load<AudioClip>(name)
                : Resources.Load<AudioClip>(audioFolder + "/" + name);

            if (custom != null)
            {
                return custom;
            }

            return useProceduralFallback ? procedural() : null;
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }

        private void HandleStarted(QuestInstance quest) => Play(startClip);
        private void HandleCompleted(QuestInstance quest) => Play(completeClip);
        private void HandleFailed(QuestInstance quest) => Play(failClip);
        private void HandlePickup(string itemID, GameObject source) => Play(pickupClip);

        private void HandleObjective(QuestInstance quest, ObjectiveData objective, int currentAmount)
        {
            Play(currentAmount >= objective.requiredAmount ? objectiveDoneClip : progressClip);
        }
    }
}
