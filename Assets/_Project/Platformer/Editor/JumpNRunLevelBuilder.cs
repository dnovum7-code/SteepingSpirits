using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.JumpNRun;

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>
    /// Turns a level text (Assets/_Project/Platformer/Levels/*.txt, see
    /// LevelLayout for the legend) into a scene Assets/Scenes/JumpNRun_&lt;id&gt;.unity.
    /// Every run rebuilds the scene completely; nothing in it is edited by hand.
    /// </summary>
    public static class JumpNRunLevelBuilder
    {
        public const string LevelFolder = "Assets/_Project/Platformer/Levels";

        public static readonly Color GroundColor = new Color(0.36f, 0.30f, 0.26f);
        public static readonly Color OneWayColor = new Color(0.55f, 0.42f, 0.30f);

        public static List<string> BuildAllLevels()
        {
            var built = new List<string>();
            if (!Directory.Exists(LevelFolder))
            {
                return built;
            }

            string[] files = Directory.GetFiles(LevelFolder, "*.txt");
            System.Array.Sort(files);
            foreach (string file in files)
            {
                string path = file.Replace('\\', '/');
                LevelLayout layout;
                try
                {
                    layout = LevelLayout.Parse(File.ReadAllText(path));
                }
                catch (System.FormatException e)
                {
                    Debug.LogError($"[JumpNRun] {path}: {e.Message}");
                    continue;
                }

                string id = layout.Setting("id", Path.GetFileNameWithoutExtension(path));
                string scenePath = JumpNRunBuilder.SceneFolder + "/JumpNRun_" + id + ".unity";
                BuildLevelScene(layout, id, scenePath);
                built.Add(scenePath);
            }

            return built;
        }

        public static void BuildLevelScene(LevelLayout layout, string id, string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("Level_" + id);
            var level = root.AddComponent<JumpNRunLevel>();
            root.AddComponent<JumpNRunSession>();
            root.AddComponent<JumpNRunTime>();
            root.AddComponent<JumpNRunOptions>();
            level.levelId = id;
            level.displayName = layout.Name;
            level.movementTuning = AssetDatabase.LoadAssetAtPath<MovementTuning>(JumpNRunBuilder.DataFolder + "/MovementTuning.asset");
            level.cameraTuning = AssetDatabase.LoadAssetAtPath<CameraTuning>(JumpNRunBuilder.DataFolder + "/CameraTuning.asset");
            level.feedbackTuning = AssetDatabase.LoadAssetAtPath<FeedbackTuning>(JumpNRunBuilder.DataFolder + "/FeedbackTuning.asset");
            level.bounds = new Rect(0f, 0f, layout.Width, layout.Height);

            Transform services = Child(root.transform, "Feedback");
            services.gameObject.AddComponent<JumpNRunParticles>();
            services.gameObject.AddComponent<JumpNRunSounds>();

            Transform geometry = Child(root.transform, "Geometry");
            foreach (TileRect r in layout.Solids)
            {
                Block(geometry, "Ground", r, GroundColor, false);
            }

            foreach (TileRect r in layout.OneWays)
            {
                Block(geometry, "OneWay", r, OneWayColor, true);
            }

            Transform objects = Child(root.transform, "Objects");
            JumpNRunElementFactory.Build(layout, level, objects);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        public static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>A collider block for a tile rect. One-way blocks are thin and use a PlatformEffector2D.</summary>
        public static GameObject Block(Transform parent, string name, TileRect r, Color color, bool oneWay)
        {
            var go = new GameObject($"{name} {r}");
            go.transform.SetParent(parent, false);

            float height = oneWay ? 0.35f : r.height;
            float top = r.y + r.height;
            go.transform.position = new Vector3(r.CenterX, top - height * 0.5f, 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(r.width, height);

            if (oneWay)
            {
                box.usedByEffector = true;
                var effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 160f;
                go.AddComponent<OneWayPlatform>();
            }

            var visual = go.AddComponent<PlaceholderVisual>();
            visual.color = color;
            visual.size = box.size;
            visual.sortingOrder = -5;
            return go;
        }

        /// <summary>Center of a tile in world units.</summary>
        public static Vector3 TileCenter(int x, int y) => new Vector3(x + 0.5f, y + 0.5f, 0f);
    }
}
