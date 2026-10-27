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
        public const string MeadowPortalName = "TeegartenPortal";
        public const string GardenRootName = "TeaGarden";

        /// <summary>Where the garden entrance stands on the meadow: below the climbing portal (-17, 0) and the clearing portal (-17, 3).</summary>
        public static readonly Vector2 MeadowPortalPosition = new Vector2(-17f, -3f);

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
            // The garden scene is shared (the workshop adds its door): replace only our own object.
            Scene scene = SteepingSpirits.Core.EditorTools.OwnedSceneObjects.OpenForBuilder(GardenScene, GardenRootName);
            var root = new GameObject(GardenRootName).AddComponent<TeaGarden>();
            root.Configure(layout, settings, audio, MeadowScene);
            EditorSceneManager.SaveScene(scene, GardenScene);

            RegisterInBuildSettings(new[] { GardenScene });
            bool entrance = AddMeadowEntrance();
            EditorSceneManager.OpenScene(GardenScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Plantation] Built " + GardenScene + (entrance ? " (+ entrance on the meadow)" : "") + "\n" + SimulatorSummary(parsed, settings));
        }

        /// <summary>
        /// The only change to the meadow: one portal object to the garden, added (or updated) in
        /// TestMeadow.unity. The Jump'n'Run builder regenerates that scene – run Build Garden again
        /// afterwards (or this menu) to put the entrance back.
        /// </summary>
        [MenuItem("SteepingSpirits/Plantation/Add Garden Entrance To Meadow")]
        public static void AddMeadowEntranceMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AddMeadowEntrance()) Debug.Log("[Plantation] Garden entrance added to " + MeadowScene);
        }

        private static bool AddMeadowEntrance()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MeadowScene) == null)
            {
                Debug.LogWarning("[Plantation] " + MeadowScene + " not found – no entrance added. Build the meadow first (SteepingSpirits → JumpNRun → Build Levels).");
                return false;
            }

            Scene meadow = EditorSceneManager.OpenScene(MeadowScene, OpenSceneMode.Single);
            GameObject portal = null;
            foreach (GameObject go in meadow.GetRootGameObjects())
            {
                if (go.name == MeadowPortalName) portal = go;
            }

            if (portal == null)
            {
                portal = new GameObject(MeadowPortalName);
                portal.AddComponent<GardenPlaceholderVisual>();
                portal.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 1.4f);
                portal.AddComponent<SteepingSpirits.World.ScenePortal>();
            }

            portal.transform.position = MeadowPortalPosition;
            if (portal.TryGetComponent(out SteepingSpirits.World.ScenePortal sp)) sp.Configure(GardenScene, GardenTexts.MeadowPortalLabel);
            EditorSceneManager.SaveScene(meadow, MeadowScene);
            return true;
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
                else
                {
                    MigrateFirstFlush(def);
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

        /// <summary>
        /// Data migration for plant assets created before the tea shrub gave the first flush as its own
        /// ingredient: only touches the new field (and the old +1 first-harvest bonus that it replaces).
        /// Other tuned values stay as they are.
        /// </summary>
        private static void MigrateFirstFlush(PlantDefinition def)
        {
            PlantParams start = StarterPlants.Create(def.plant.id);
            if (start == null || string.IsNullOrEmpty(start.firstHarvestIngredientId) || !string.IsNullOrEmpty(def.plant.firstHarvestIngredientId)) return;
            def.plant.firstHarvestIngredientId = start.firstHarvestIngredientId;
            if (def.plant.firstHarvestQualityBonus == 1) def.plant.firstHarvestQualityBonus = start.firstHarvestQualityBonus;
            EditorUtility.SetDirty(def);
            Debug.Log("[Plantation] " + def.name + ": first harvest now gives " + start.firstHarvestIngredientId + ".");
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
