using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;
using SteepingSpirits.Brewing.Flow;
using SteepingSpirits.Brewing.Presentation;
using Shape = SteepingSpirits.Brewing.Presentation.PlaceholderSpriteShape.Shape;

namespace SteepingSpirits.Brewing.EditorTools
{
    /// <summary>
    /// Creates the default brewing assets (three teas, tuning, audio set) and the
    /// playable sandbox scene. Existing assets are kept, so tuning survives a rebuild;
    /// the scene is regenerated every time.
    /// Menu: SteepingSpirits → Brewing → Build Sandbox
    /// </summary>
    public static class BrewingSandboxBuilder
    {
        private const string DataFolder = "Assets/_Project/Brewing/Data";
        private const string SceneFolder = "Assets/_Project/Brewing/Scenes";
        private const string ScenePath = SceneFolder + "/BrewingSandbox.unity";

        [MenuItem("SteepingSpirits/Brewing/Build Sandbox")]
        public static void BuildSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(DataFolder);
            EnsureFolder(SceneFolder);

            TeaDefinition[] teas =
            {
                LoadOrCreateTea("Tea_Black", "Schwarztee", "Standhaft und kräftig – ideal zum Lernen.",
                    TeaPresets.Black(), new Color(0.62f, 0.25f, 0.08f), new Color(0.2f, 0.13f, 0.08f)),
                LoadOrCreateTea("Tea_White", "Weißtee", "Scheu und langsam, aber verzeihend.",
                    TeaPresets.White(), new Color(0.95f, 0.83f, 0.5f), new Color(0.75f, 0.75f, 0.65f)),
                LoadOrCreateTea("Tea_Green", "Grüntee", "Nervös, mit schmalem Fenster – mag kein kochendes Wasser.",
                    TeaPresets.Green(), new Color(0.72f, 0.78f, 0.3f), new Color(0.25f, 0.45f, 0.18f))
            };
            BrewingTuning tuning = LoadOrCreate<BrewingTuning>(DataFolder + "/BrewingTuning.asset");
            BrewAudioSet audioSet = LoadOrCreate<BrewAudioSet>(DataFolder + "/BrewAudioSet.asset");
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene(tuning, teas, audioSet);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Brewing] Sandbox built: {ScenePath}. Press Play.");
        }

        // ---------------- Assets ----------------

        private static TeaDefinition LoadOrCreateTea(string file, string displayName, string description,
            TeaParams parameters, Color liquor, Color leaf)
        {
            string path = $"{DataFolder}/{file}.asset";
            var tea = AssetDatabase.LoadAssetAtPath<TeaDefinition>(path);
            if (tea != null)
            {
                return tea; // keep tuned values
            }

            tea = ScriptableObject.CreateInstance<TeaDefinition>();
            tea.displayName = displayName;
            tea.description = description;
            tea.parameters = parameters;
            tea.liquorColor = TeaDefinition.DefaultLiquor(liquor);
            tea.leafColor = leaf;
            AssetDatabase.CreateAsset(tea, path);
            return tea;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        // ---------------- Scene ----------------

        private static void BuildScene(BrewingTuning tuning, TeaDefinition[] teas, BrewAudioSet audioSet)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.09f, 0.07f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            var world = new GameObject("Teehaus").transform;

            // Room and table
            Sprite(world, "Wand", Shape.Square, new Color(0.2f, 0.14f, 0.1f), new Vector2(0f, 0.5f), new Vector2(20f, 10f), -100);
            Sprite(world, "Tisch", Shape.Square, new Color(0.38f, 0.25f, 0.15f), new Vector2(0f, -3.3f), new Vector2(18f, 2.4f), -50);

            // Fire and kettle (glass, so the bubbles are visible)
            Sprite(world, "Feuerstelle", Shape.Square, new Color(0.35f, 0.33f, 0.32f), new Vector2(-4f, -2.0f), new Vector2(2.6f, 0.35f), -10);
            SpriteRenderer fire = Sprite(world, "Feuer", Shape.Triangle, new Color(1f, 0.55f, 0.2f, 0f), new Vector2(-4f, -1.6f), new Vector2(1.4f, 0.7f), -5);
            Sprite(world, "Kessel", Shape.Square, new Color(0.8f, 0.9f, 1f, 0.22f), new Vector2(-4f, -0.45f), new Vector2(2.6f, 2.4f), 0);
            Sprite(world, "Wasser", Shape.Square, new Color(0.6f, 0.8f, 1f, 0.3f), new Vector2(-4f, -0.8f), new Vector2(2.4f, 1.7f), 1);
            Sprite(world, "Deckel", Shape.Square, new Color(0.45f, 0.4f, 0.38f), new Vector2(-4f, 0.82f), new Vector2(1.4f, 0.16f), 2);
            Sprite(world, "Tülle", Shape.Square, new Color(0.8f, 0.9f, 1f, 0.3f), new Vector2(-2.45f, 0.1f), new Vector2(0.9f, 0.18f), 0)
                .transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            Transform kettleTop = Marker(world, "Dampfpunkt Kessel", new Vector2(-4f, 1.0f));

            // Steeping vessel (glass) with liquor and leaves
            SpriteRenderer glass = Sprite(world, "Glaskanne", Shape.Square, new Color(0.85f, 0.92f, 1f, 0.2f), new Vector2(1.2f, -1.1f), new Vector2(1.8f, 1.9f), 6);
            Transform liquorPivot = Marker(world, "Aufguss (Füllstand)", new Vector2(1.2f, -2.0f));
            SpriteRenderer liquor = Sprite(liquorPivot, "Aufguss", Shape.Square, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one, 5);
            liquor.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            liquor.transform.localScale = new Vector3(1.6f, 1.7f, 1f);
            Transform leafRoot = Marker(world, "Blätter", new Vector2(1.2f, -1.85f));
            leafRoot.localScale = new Vector3(0.35f, 0.35f, 1f);
            var leaves = new Transform[5];
            for (int i = 0; i < leaves.Length; i++)
            {
                SpriteRenderer leaf = Sprite(leafRoot, "Blatt " + (i + 1), Shape.Diamond, new Color(0.25f, 0.35f, 0.15f), Vector2.zero, Vector2.one, 7);
                leaf.transform.localPosition = new Vector3(-1.6f + i * 0.8f, (i % 2) * 0.35f, 0f);
                leaves[i] = leaf.transform;
            }
            Transform vesselTop = Marker(world, "Dampfpunkt Gefäß", new Vector2(1.2f, -0.05f));

            // Cup
            Sprite(world, "Schale", Shape.Square, new Color(0.92f, 0.88f, 0.8f), new Vector2(4.3f, -1.95f), new Vector2(1.6f, 0.6f), 4);
            SpriteRenderer cupLiquor = Sprite(world, "Tee in der Schale", Shape.Square, new Color(0f, 0f, 0f, 0f), new Vector2(4.3f, -1.8f), new Vector2(1.35f, 0.3f), 5);

            // Particles
            SpriteParticleEmitter bubbles = Emitter("Blasen", 220, Vector2.zero, 0f, 3);
            SpriteParticleEmitter steam = Emitter("Dampf", 160, new Vector2(0f, 0.08f), 0.4f, 20);
            SpriteParticleEmitter aroma = Emitter("Duft", 140, new Vector2(0f, 0.05f), 0.3f, 21);

            // Controller and views
            var brewing = new GameObject("Brewing");
            var controller = brewing.AddComponent<BrewSessionController>();
            controller.Configure(tuning, teas);

            brewing.AddComponent<KettleView>().Configure(fire);
            brewing.AddComponent<BubbleStageView>().Configure(bubbles, new Rect(-5.1f, -1.6f, 2.2f, 1.6f));
            brewing.AddComponent<SteamView>().Configure(steam, kettleTop, vesselTop);
            brewing.AddComponent<SteepVesselView>().Configure(glass, liquorPivot, liquor, cupLiquor, leaves);
            brewing.AddComponent<AromaWispView>().Configure(aroma, vesselTop);
            brewing.AddComponent<BrewAudioController>().Configure(audioSet);
            brewing.AddComponent<BrewHaptics>();
            brewing.AddComponent<BrewPromptView>();
            brewing.AddComponent<ThermometerView>();
            brewing.AddComponent<BrewResultView>();
            brewing.AddComponent<BrewDebugOverlay>();

            foreach (BrewView view in brewing.GetComponents<BrewView>())
            {
                view.Bind(controller);
            }
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Shape shape, Color color,
            Vector2 position, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color;
            sr.sortingOrder = order;
            go.AddComponent<PlaceholderSpriteShape>().Configure(shape);
            return sr;
        }

        private static Transform Marker(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            return go.transform;
        }

        private static SpriteParticleEmitter Emitter(string name, int capacity, Vector2 acceleration, float drag, int order)
        {
            var go = new GameObject("Partikel " + name);
            var emitter = go.AddComponent<SpriteParticleEmitter>();
            emitter.Configure(capacity, acceleration, drag, order);
            return emitter;
        }
    }
}
