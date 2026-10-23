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
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("TeaWorkshop").AddComponent<TeaWorkshop>().Configure(layout, settings, audio, GardenScene);
            EditorSceneManager.SaveScene(scene, WorkshopScene);
            RegisterInBuildSettings(WorkshopScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Processing] Built " + WorkshopScene + "\n" + SimulatorSummary(settings.processing));
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
