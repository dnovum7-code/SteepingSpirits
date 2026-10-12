using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Plantation;

namespace SteepingSpirits.Plantation.EditorTools
{
    /// <summary>
    /// The single source for the tea garden scene. Creates the tuning assets once (existing ones
    /// are never overwritten), regenerates the scene from code and the text layout, adds it to the
    /// build list and prints the simulator numbers for the layout.
    /// Menu: SteepingSpirits → Plantation → Build Garden
    /// </summary>
    public static class PlantationBuilder
    {
        public const string SceneFolder = "Assets/Scenes";
        public const string GardenScene = SceneFolder + "/Teegarten.unity";
        public const string MeadowScene = SceneFolder + "/TestMeadow.unity";
        public const string DataFolder = "Assets/_Project/Plantation/Data";
        public const string PlantFolder = DataFolder + "/Plants";
        public const string LayoutPath = "Assets/_Project/Plantation/Layouts/Teegarten.txt";

        [MenuItem("SteepingSpirits/Plantation/Build Garden")]
        public static void BuildGarden()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GardenSettings settings = EnsureAssets(out PlantationAudioSet audio);
            AssetDatabase.SaveAssets();

            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            if (layout == null)
            {
                Debug.LogError("[Plantation] Layout missing: " + LayoutPath);
                return;
            }

            GardenLayout parsed;
            try
            {
                parsed = GardenLayout.Parse(layout.text);
            }
            catch (System.FormatException e)
            {
                Debug.LogError("[Plantation] Layout broken: " + e.Message);
                return;
            }

            EnsureFolder(SceneFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TeaGarden").AddComponent<TeaGarden>();
            root.Configure(layout, settings, audio, MeadowScene);
            EditorSceneManager.SaveScene(scene, GardenScene);

            RegisterInBuildSettings(new[] { GardenScene });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Plantation] Built " + GardenScene + "\n" + SimulatorSummary(parsed, settings));
        }

        [MenuItem("SteepingSpirits/Plantation/Simulate 28 Days")]
        public static void Simulate()
        {
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            GardenSettings settings = AssetDatabase.LoadAssetAtPath<GardenSettings>(DataFolder + "/GardenSettings.asset");
            if (layout == null) return;
            Debug.Log("[Plantation] Simulator\n" + SimulatorSummary(GardenLayout.Parse(layout.text), settings != null ? settings : GardenSettings.CreateDefault()));
        }

        private static string SimulatorSummary(GardenLayout layout, GardenSettings settings)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(layout.All(GardenTile.Bed).Count).Append(" bed cells, ").Append(layout.BedBlocks().Count).Append(" blocks\n");
            foreach (BotStyle style in new[] { BotStyle.Careful, BotStyle.Sloppy, BotStyle.NeverWater })
            {
                SimResult r = GardenSimulator.Run(layout, settings.garden, settings.BuildBook(), style, 28);
                sb.Append(r).Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>Plant definitions, settings and audio set: created once with the start values, then tuned by hand.</summary>
        public static GardenSettings EnsureAssets(out PlantationAudioSet audio)
        {
            EnsureFolder(PlantFolder);
            var plants = new List<PlantDefinition>();
            foreach (string id in StarterPlants.Ids)
            {
                string path = PlantFolder + "/" + id + ".asset";
                var def = AssetDatabase.LoadAssetAtPath<PlantDefinition>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<PlantDefinition>();
                    def.plant = StarterPlants.Create(id);
                    def.leafColor = PlantPalette.Leaf(id);
                    def.ripeColor = PlantPalette.Ripe(id);
                    AssetDatabase.CreateAsset(def, path);
                }

                plants.Add(def);
            }

            string settingsPath = DataFolder + "/GardenSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<GardenSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GardenSettings>();
                settings.plants = plants.ToArray();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            else if (settings.plants == null || settings.plants.Length == 0)
            {
                settings.plants = plants.ToArray();
                EditorUtility.SetDirty(settings);
            }

            string audioPath = DataFolder + "/PlantationAudioSet.asset";
            audio = AssetDatabase.LoadAssetAtPath<PlantationAudioSet>(audioPath);
            if (audio == null)
            {
                audio = ScriptableObject.CreateInstance<PlantationAudioSet>();
                AssetDatabase.CreateAsset(audio, audioPath);
            }

            return settings;
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

        /// <summary>Adds scenes to the build list (needed for portals outside the editor). Never removes any.</summary>
        public static void RegisterInBuildSettings(IEnumerable<string> paths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in paths)
            {
                if (scenes.Exists(s => s.path == path)) continue;
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
