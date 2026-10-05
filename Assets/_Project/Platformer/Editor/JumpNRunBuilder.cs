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
        public const string HubScene = SceneFolder + "/JumpNRun_Hub.unity";

        /// <summary>Where the portal to the clearing stands on the meadow (next to the climbing portal).</summary>
        public static readonly Vector2 MeadowHubPortalPosition = new Vector2(-17f, 3f);

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
            built.Add(BuildMeadowScene());
            built.Add(BuildClimbScene());
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
            EnsureAsset<JumpNRunAudioSet>(DataFolder + "/JumpNRunAudioSet.asset");
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

        /// <summary>
        /// The meadow builds itself at runtime (TestMeadow). The builder only adds
        /// one extra object next to it: the portal to the Jump'n'Run clearing.
        /// </summary>
        private static string BuildMeadowScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("TestMeadow").AddComponent<TestMeadow>();

            var portal = new GameObject("HubPortal");
            portal.transform.position = MeadowHubPortalPosition;
            var visual = portal.AddComponent<PlaceholderVisual>();
            visual.shape = PlaceholderVisual.Shape.Diamond;
            visual.color = new Color(0.55f, 0.85f, 0.8f);
            visual.size = new Vector2(1.2f, 1.6f);
            portal.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 1.6f);
            portal.AddComponent<SteepingSpirits.World.ScenePortal>().Configure(HubScene, JumpNRunTexts.MeadowPortalLabel);

            EditorSceneManager.SaveScene(scene, MeadowScene);
            return MeadowScene;
        }

        private const string ClimbSwitchPref = "SteepingSpirits.JumpNRun.ClimbUsesNewController";
        private const string ClimbSwitchMenu = "SteepingSpirits/JumpNRun/Climbing Path Uses New Controller";

        /// <summary>
        /// Switch for the old climbing path: off = PlatformerController2D (unchanged),
        /// on = Jump'n'Run player with assists and the gentle catch. Takes effect on the next build.
        /// </summary>
        [MenuItem(ClimbSwitchMenu)]
        private static void ToggleClimbController()
        {
            bool on = !EditorPrefs.GetBool(ClimbSwitchPref, false);
            EditorPrefs.SetBool(ClimbSwitchPref, on);
            Debug.Log($"[JumpNRun] Climbing path uses the {(on ? "new Jump'n'Run" : "old")} controller – run Build Levels to apply.");
        }

        [MenuItem(ClimbSwitchMenu, true)]
        private static bool ToggleClimbControllerValidate()
        {
            Menu.SetChecked(ClimbSwitchMenu, EditorPrefs.GetBool(ClimbSwitchPref, false));
            return true;
        }

        private static string BuildClimbScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var course = new GameObject("PlatformerCourse").AddComponent<PlatformerCourse>();
            course.ConfigureController(EditorPrefs.GetBool(ClimbSwitchPref, false),
                AssetDatabase.LoadAssetAtPath<MovementTuning>(DataFolder + "/MovementTuning.asset"));
            EditorSceneManager.SaveScene(scene, ClimbScene);
            return ClimbScene;
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
