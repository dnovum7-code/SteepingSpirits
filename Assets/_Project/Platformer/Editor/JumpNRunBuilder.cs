using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.DevTools;

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

        [MenuItem("SteepingSpirits/JumpNRun/Build Levels")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var built = new List<string>();
            built.Add(BuildSingleComponentScene<TestMeadow>(MeadowScene, "TestMeadow"));
            built.Add(BuildSingleComponentScene<PlatformerCourse>(ClimbScene, "PlatformerCourse"));

            RegisterInBuildSettings(built);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[JumpNRun] Built scenes:\n" + string.Join("\n", built));
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
