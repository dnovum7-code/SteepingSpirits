using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SteepingSpirits.Processing.EditorTools
{
    /// <summary>
    /// The single source for the tea workshop scene. Creates the tuning assets once (never overwrites
    /// them), regenerates the scene, adds it to the build list and prints the simulator numbers.
    /// Menu: SteepingSpirits → Processing → Build Workshop
    /// </summary>
    public static class ProcessingBuilder
    {
        public const string SceneFolder = "Assets/Scenes";
        public const string WorkshopScene = SceneFolder + "/Teewerkstatt.unity";
        public const string GardenScene = SceneFolder + "/Teegarten.unity";
        public const string DataFolder = "Assets/_Project/Processing/Data";
        public const string LayoutPath = "Assets/_Project/Processing/Layouts/Werkstatt.txt";
        public const string GardenDoorName = "TeewerkstattTuer";
        public const string WorkshopRootName = "TeaWorkshop";

        /// <summary>Menu of the brewing module that places the brewing table (called by name, no module reference).</summary>
        private const string BrewingTableMenu = "SteepingSpirits/Brewing/Add Brewing Table To Workshop";

        /// <summary>
        /// Where the door stands in the garden: on the hedge at the east end of the long path
        /// (garden layout cell 21,6); the player reaches it from the path cell next to it.
        /// </summary>
        public static readonly Vector2 GardenDoorPosition = new Vector2(21.5f, 6.5f);

        [MenuItem("SteepingSpirits/Processing/Build Workshop")]
        public static void BuildWorkshop()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ProcessingSettings settings = EnsureAsset<ProcessingSettings>(DataFolder + "/ProcessingSettings.asset");
            ProcessingAudioSet audio = EnsureAsset<ProcessingAudioSet>(DataFolder + "/ProcessingAudioSet.asset");
            AssetDatabase.SaveAssets();

            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (layout == null)
            {
                Debug.LogError("[Processing] Layout missing: " + LayoutPath);
                return;
            }

            try
            {
                WorkshopLayout.Parse(layout.text);
            }
            catch (System.FormatException e)
            {
                Debug.LogError("[Processing] Layout broken: " + e.Message);
                return;
            }

            EnsureFolder(SceneFolder);
            // Replace only our own root object, so other modules may add things to the workshop scene.
            Scene scene = SteepingSpirits.Core.EditorTools.OwnedSceneObjects.OpenForBuilder(WorkshopScene, WorkshopRootName);
            new GameObject(WorkshopRootName).AddComponent<TeaWorkshop>().Configure(layout, settings, audio, GardenScene);
            EditorSceneManager.SaveScene(scene, WorkshopScene);
            RegisterInBuildSettings(WorkshopScene);
            bool door = AddGardenDoor();
            // Brewing places its table into the workshop; it survives this rebuild, and is added here if it is missing.
            EditorApplication.ExecuteMenuItem(BrewingTableMenu);
            EditorSceneManager.OpenScene(WorkshopScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Processing] Built " + WorkshopScene + (door ? " (+ door in the garden)" : "") + "\n" + SimulatorSummary(settings.processing));
        }

        /// <summary>
        /// The only change to the garden: one door object in Teegarten.unity (added or moved). The
        /// garden builder regenerates that scene – run Build Workshop again afterwards (or this menu).
        /// </summary>
        [MenuItem("SteepingSpirits/Processing/Add Workshop Door To Garden")]
        public static void AddGardenDoorMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AddGardenDoor()) Debug.Log("[Processing] Workshop door added to " + GardenScene);
        }

        private static bool AddGardenDoor()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GardenScene) == null)
            {
                Debug.LogWarning("[Processing] " + GardenScene + " not found – no door added. Build the garden first (SteepingSpirits → Plantation → Build Garden).");
                return false;
            }

            Scene garden = EditorSceneManager.OpenScene(GardenScene, OpenSceneMode.Single);
            GameObject door = null;
            foreach (GameObject go in garden.GetRootGameObjects())
            {
                if (go.name == GardenDoorName) door = go;
            }

            if (door == null)
            {
                door = new GameObject(GardenDoorName);
                door.AddComponent<WorkshopDoor>();
            }

            door.transform.position = GardenDoorPosition;
            if (door.TryGetComponent(out WorkshopDoor d)) d.Configure(WorkshopScene);
            EditorSceneManager.SaveScene(garden, GardenScene);
            return true;
        }

        [MenuItem("SteepingSpirits/Processing/Simulate 100 Batches")]
        public static void Simulate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ProcessingSettings>(DataFolder + "/ProcessingSettings.asset");
            Debug.Log("[Processing] Simulator\n" + SimulatorSummary(settings != null ? settings.processing : new ProcessingParams()));
        }

        private static string SimulatorSummary(ProcessingParams p)
        {
            var sb = new System.Text.StringBuilder();
            foreach (WorkshopBot bot in new[] { WorkshopBot.Careful, WorkshopBot.Sloppy, WorkshopBot.Random })
            {
                WorkshopSimResult r = WorkshopSimulator.Run(p, bot, 100);
                sb.Append(r).Append('\n').Append(r.DurationLine()).Append('\n');
            }

            return sb.ToString();
        }

        public static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        public static void RegisterInBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
