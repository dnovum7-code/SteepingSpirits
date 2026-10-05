using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.DevTools;
using SteepingSpirits.Platformer.JumpNRun;

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>
    /// The single source for the Jump'n'Run scenes. Regenerates every scene
    /// (and later the prefabs) from code and data; tuning and level assets
    /// that already exist are kept.
    /// Menu: SteepingSpirits → JumpNRun → Build Levels
    /// </summary>
    public static class JumpNRunBuilder
    {
        public const string SceneFolder = "Assets/Scenes";
        public const string MeadowScene = SceneFolder + "/TestMeadow.unity";
        public const string ClimbScene = SceneFolder + "/JumpAndRun.unity";
        public const string DataFolder = "Assets/_Project/Platformer/Data";

        [MenuItem("SteepingSpirits/JumpNRun/Build Levels")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureTuningAssets();
            JumpNRunPrefabs.BuildAll();
            AssetDatabase.SaveAssets();
            JumpNRunPrefabs.CheckAll();

            var built = new List<string>();
            built.Add(BuildSingleComponentScene<TestMeadow>(MeadowScene, "TestMeadow"));
            built.Add(BuildSingleComponentScene<PlatformerCourse>(ClimbScene, "PlatformerCourse"));
            built.AddRange(JumpNRunLevelBuilder.BuildAllLevels());

            RegisterInBuildSettings(built);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[JumpNRun] Built scenes:\n" + string.Join("\n", built));
        }

        /// <summary>Tuning assets are created once and then only edited by hand in the Inspector.</summary>
        public static void EnsureTuningAssets()
        {
            EnsureAsset<MovementTuning>(DataFolder + "/MovementTuning.asset");
            EnsureAsset<CameraTuning>(DataFolder + "/CameraTuning.asset");
            EnsureAsset<FeedbackTuning>(DataFolder + "/FeedbackTuning.asset");
            EnsureAsset<SpiritElementsTuning>(DataFolder + "/SpiritElementsTuning.asset");
        }

        /// <summary>Loads an asset or creates it with defaults. Existing assets are never overwritten.</summary>
        public static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        /// <summary>Scenes whose content is created at runtime by one builder component.</summary>
        private static string BuildSingleComponentScene<T>(string path, string objectName) where T : Component
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(objectName).AddComponent<T>();
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        /// <summary>Adds the scenes to the build list (needed for portals outside the editor).</summary>
        public static void RegisterInBuildSettings(IEnumerable<string> paths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in paths)
            {
                if (scenes.Exists(s => s.path == path))
                {
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
