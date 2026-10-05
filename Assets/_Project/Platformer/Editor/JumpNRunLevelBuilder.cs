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

        /// <summary>Level id → display name, filled before building so doors can show names.</summary>
        private static readonly Dictionary<string, string> levelNames = new Dictionary<string, string>();

        public static string ScenePathFor(string target)
        {
            switch (target)
            {
                case "meadow": return JumpNRunBuilder.MeadowScene;
                case "climb": return JumpNRunBuilder.ClimbScene;
                default: return JumpNRunBuilder.SceneFolder + "/JumpNRun_" + target + ".unity";
            }
        }

        public static string DisplayNameFor(string target)
        {
            switch (target)
            {
                case "meadow": return JumpNRunTexts.DoorMeadow;
                case "climb": return JumpNRunTexts.DoorClimb;
                default: return JumpNRunTexts.DoorLabel(levelNames.TryGetValue(target, out string n) ? n : target);
            }
        }

        public static List<string> BuildAllLevels()
        {
            var built = new List<string>();
            if (!Directory.Exists(LevelFolder))
            {
                return built;
            }

            string[] files = Directory.GetFiles(LevelFolder, "*.txt");
            System.Array.Sort(files);
            var layouts = new List<KeyValuePair<string, LevelLayout>>();
            levelNames.Clear();
            foreach (string file in files)
            {
                string path = file.Replace('\\', '/');
                try
                {
                    LevelLayout parsed = LevelLayout.Parse(File.ReadAllText(path));
                    levelNames[parsed.Setting("id", Path.GetFileNameWithoutExtension(path))] = parsed.Name;
                    layouts.Add(new KeyValuePair<string, LevelLayout>(path, parsed));
                }
                catch (System.FormatException e)
                {
                    Debug.LogError($"[JumpNRun] {path}: {e.Message}");
                }
            }

            foreach (var entry in layouts)
            {
                string path = entry.Key;
                LevelLayout layout = entry.Value;
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
            root.AddComponent<JumpNRunHud>();
            root.AddComponent<JumpNRunDebugOverlay>();
            root.AddComponent<JumpNRunProbe>();
            root.AddComponent<JumpNRunInputRecorder>();
            if (!layout.IsHub)
            {
                root.AddComponent<JumpNRunEndCard>();
                root.AddComponent<JumpNRunTelemetry>();
            }
            level.levelId = id;
            level.displayName = layout.Name;
            level.movementTuning = AssetDatabase.LoadAssetAtPath<MovementTuning>(JumpNRunBuilder.DataFolder + "/MovementTuning.asset");
            level.cameraTuning = AssetDatabase.LoadAssetAtPath<CameraTuning>(JumpNRunBuilder.DataFolder + "/CameraTuning.asset");
            level.feedbackTuning = AssetDatabase.LoadAssetAtPath<FeedbackTuning>(JumpNRunBuilder.DataFolder + "/FeedbackTuning.asset");
            level.elementsTuning = AssetDatabase.LoadAssetAtPath<SpiritElementsTuning>(JumpNRunBuilder.DataFolder + "/SpiritElementsTuning.asset");
            level.bounds = new Rect(0f, 0f, layout.Width, layout.Height);
            level.lanternCount = layout.All(TileKind.Lantern).Count;
            level.layoutText = layout.SourceText;
            level.pathLanternCount = layout.All(TileKind.PathLantern).Count;
            var placed = new IngredientBag();
            foreach (var pair in IngredientTally.Available(layout)) placed.Add(pair.Key, pair.Value);
            level.availableIngredients = placed.Serialize();
            string next = layout.Setting("next");
            level.nextScenePath = string.IsNullOrEmpty(next) ? "" : JumpNRunBuilder.SceneFolder + "/JumpNRun_" + next + ".unity";

            Transform services = Child(root.transform, "Feedback");
            services.gameObject.AddComponent<JumpNRunParticles>();
            services.gameObject.AddComponent<JumpNRunSounds>();

            LevelMood.Palette mood = LevelMood.For(layout.Setting("mood", "morning"));
            BuildBackground(root.transform, layout, mood);

            Transform geometry = Child(root.transform, "Geometry");
            foreach (TileRect r in layout.Solids)
            {
                Block(geometry, "Ground", r, mood.ground, false);
            }

            foreach (TileRect r in layout.OneWays)
            {
                Block(geometry, "OneWay", r, OneWayColor, true);
            }

            foreach (TileRect r in layout.GhostPlatforms)
            {
                GameObject g = Block(geometry, "GhostPlatform", r, new Color(0.72f, 0.8f, 1f, 0.15f), false);
                g.AddComponent<GhostPlatform>();
                g.AddComponent<UnsafeGround>();
            }

            foreach (TileRect r in layout.LeafPlatforms)
            {
                GameObject leaf = Block(geometry, "LeafPlatform", r, new Color(0.48f, 0.68f, 0.36f), false);
                ShapeAsThinTop(leaf, 0.35f);
                var rb = leaf.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                leaf.AddComponent<LeafPlatform>();
                leaf.AddComponent<UnsafeGround>();
            }

            Transform objects = Child(root.transform, "Objects");
            JumpNRunElementFactory.Build(layout, level, objects);

            Camera cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = mood.sky;
            }

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        /// <summary>Three parallax layers of soft hills/bushes plus a faint light veil.</summary>
        private static void BuildBackground(Transform root, LevelLayout layout, LevelMood.Palette mood)
        {
            Transform bg = Child(root, "Background");
            var rng = new System.Random(layout.Name.GetHashCode());
            AddHills(bg, "Far", layout, 0.8f, mood.far, 9f, 6f, -40, rng, layout.Height * 0.35f);
            AddHills(bg, "Mid", layout, 0.55f, mood.mid, 6f, 4f, -30, rng, layout.Height * 0.2f);
            AddHills(bg, "Near", layout, 0.25f, mood.near, 3.5f, 2.5f, -20, rng, 0f);

            var veil = new GameObject("LightVeil");
            veil.transform.SetParent(bg, false);
            veil.transform.position = new Vector3(layout.Width * 0.5f, layout.Height * 0.5f, 0f);
            var v = veil.AddComponent<PlaceholderVisual>();
            v.shape = PlaceholderVisual.Shape.Square;
            v.color = mood.glow;
            v.size = new Vector2(layout.Width + 60f, layout.Height + 40f);
            v.sortingOrder = 20;
            veil.AddComponent<ParallaxLayer>().factor = 1f;
        }

        private static void AddHills(Transform parent, string name, LevelLayout layout, float factor, Color color,
            float size, float spacing, int order, System.Random rng, float baseY)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            layer.AddComponent<ParallaxLayer>().factor = factor;

            // Wide enough to cover the level while the layer lags behind the camera.
            float span = layout.Width * (1f - factor) + 40f;
            for (float x = -20f; x < span; x += spacing * (0.7f + (float)rng.NextDouble() * 0.6f))
            {
                var hill = new GameObject("Hill");
                hill.transform.SetParent(layer.transform, false);
                float s = size * (0.7f + (float)rng.NextDouble() * 0.6f);
                hill.transform.position = new Vector3(x, baseY, 0f);
                var vis = hill.AddComponent<PlaceholderVisual>();
                vis.shape = PlaceholderVisual.Shape.Circle;
                vis.color = color;
                vis.size = new Vector2(s * 1.6f, s);
                vis.sortingOrder = order;
            }
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

        /// <summary>Turns a block into a thin slab at the top of its tiles (leaves, dew leaves).</summary>
        public static void ShapeAsThinTop(GameObject go, float thickness)
        {
            var box = go.GetComponent<BoxCollider2D>();
            float top = go.transform.position.y + box.size.y * 0.5f;
            box.size = new Vector2(box.size.x, thickness);
            go.transform.position = new Vector3(go.transform.position.x, top - thickness * 0.5f, 0f);
            var visual = go.GetComponent<PlaceholderVisual>();
            if (visual != null)
            {
                visual.size = box.size;
                visual.shape = PlaceholderVisual.Shape.Circle;
            }
        }

        /// <summary>Center of a tile in world units.</summary>
        public static Vector3 TileCenter(int x, int y) => new Vector3(x + 0.5f, y + 0.5f, 0f);
    }
}
