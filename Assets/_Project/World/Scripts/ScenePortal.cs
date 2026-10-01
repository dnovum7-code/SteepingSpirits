using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Interaction;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace SteepingSpirits.World
{
    /// <summary>
    /// Portal in eine andere Szene (z.B. Wiese ↔ Jump'n'Run). Ansprechen mit E.
    ///
    /// Im Editor wird die Szene direkt über ihren Pfad geladen – sie muss dafür
    /// NICHT in den Build-Einstellungen stehen. Im fertigen Build wird über den
    /// Namen geladen (dann Szene unter File → Build Profiles eintragen).
    ///
    /// „Zurück"-Portale (returnToPrevious) laden die Szene, aus der man kam.
    /// Systeme mit DontDestroyOnLoad (Quests, Inventar, Gold, XP, Uhr) bleiben
    /// dabei erhalten – Quest-Fortschritt geht über Szenen hinweg.
    /// </summary>
    public class ScenePortal : MonoBehaviour, IInteractable, IInteractLabel
    {
        [Tooltip("Asset-Pfad, z.B. Assets/Scenes/JumpAndRun.unity (für den Editor)")]
        [SerializeField] private string scenePath = "Assets/Scenes/JumpAndRun.unity";

        [Tooltip("Szenen-Name für Builds (leer = aus dem Pfad)")]
        [SerializeField] private string sceneName;

        [Tooltip("Statt scenePath zurück in die vorherige Szene")]
        [SerializeField] private bool returnToPrevious;

        [SerializeField] private string label = "Betreten";

        /// <summary>Pfad der Szene, aus der zuletzt ein Portal benutzt wurde.</summary>
        public static string PreviousScenePath { get; private set; }

        public string InteractLabel => label;

        public void Configure(string scenePath, string label, bool returnToPrevious = false)
        {
            this.scenePath = scenePath;
            this.label = label;
            this.returnToPrevious = returnToPrevious;
        }

        public void Interact()
        {
            string path = returnToPrevious ? PreviousScenePath : scenePath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[ScenePortal] Keine Ziel-Szene bekannt. Startszene speichern (z.B. Assets/Scenes/…) " +
                                 "und über ein Portal hierher kommen, oder scenePath setzen.");
                return;
            }

            Load(path, returnToPrevious ? null : sceneName);
        }

        /// <summary>Lädt eine Szene per Pfad (Editor) bzw. Name (Build).</summary>
        public static void Load(string path, string nameOverride = null)
        {
            PreviousScenePath = SceneManager.GetActiveScene().path;

#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            string name = string.IsNullOrEmpty(nameOverride) ? System.IO.Path.GetFileNameWithoutExtension(path) : nameOverride;
            SceneManager.LoadScene(name);
#endif
        }
    }
}
