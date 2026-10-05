using UnityEngine;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.JumpNRun;

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>
    /// Element templates (saved as prefabs by <see cref="JumpNRunPrefabs"/>) and
    /// placement of prefab instances for the markers of a level layout.
    /// Per-instance values (lantern index, wind column height, ingredient id)
    /// are set as overrides on the placed instance.
    /// </summary>
    public static class JumpNRunElementFactory
    {
        // ------------------------------------------------------------------
        // Templates (one prefab each)
        // ------------------------------------------------------------------

        public static GameObject CreateLantern()
        {
            GameObject go = Template("Lantern", PlaceholderVisual.Shape.Circle, new Color(0.45f, 0.42f, 0.38f), new Vector2(0.45f, 0.6f));
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.2f, 2.5f);
            go.AddComponent<Lantern>();
            return go;
        }

        public static GameObject CreateBramble()
        {
            GameObject go = Template("Bramble", PlaceholderVisual.Shape.Triangle, new Color(0.42f, 0.30f, 0.45f), new Vector2(1f, 0.7f));
            go.GetComponent<PlaceholderVisual>().offset = new Vector2(0f, -0.15f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 0.5f);
            box.offset = new Vector2(0f, -0.2f);
            go.AddComponent<Bramble>();
            return go;
        }

        public static GameObject CreateIngredient()
        {
            GameObject go = Template("Ingredient", PlaceholderVisual.Shape.Circle, Ingredient.ColorOf(IngredientIds.TeaLeaf), Vector2.one * 0.42f);
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.45f;
            go.AddComponent<Ingredient>();
            return go;
        }

        public static GameObject CreateGoal()
        {
            GameObject go = Template("Goal", PlaceholderVisual.Shape.Triangle, new Color(0.95f, 0.82f, 0.55f), new Vector2(1.4f, 2f));
            go.GetComponent<PlaceholderVisual>().offset = new Vector2(0f, 0.5f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.2f, 2f);
            box.offset = new Vector2(0f, 0.5f);
            go.AddComponent<Goal>();
            return go;
        }

        public static GameObject CreateWindSpirit()
        {
            GameObject go = Template("WindSpirit", PlaceholderVisual.Shape.Circle, new Color(0.85f, 0.95f, 1f, 0.8f), new Vector2(0.7f, 0.5f));
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<WindSpirit>().Configure(6f, new WindParams().width);
            return go;
        }

        public static GameObject CreateLanternSpirit()
        {
            GameObject go = Template("LanternSpirit", PlaceholderVisual.Shape.Circle, new Color(1f, 0.86f, 0.55f), Vector2.one * 0.45f);
            go.GetComponent<PlaceholderVisual>().sortingOrder = 11;
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.6f;
            go.AddComponent<LanternSpirit>();
            return go;
        }

        public static GameObject CreateDewLeaf()
        {
            GameObject go = Template("DewLeaf", PlaceholderVisual.Shape.Circle, new Color(0.62f, 0.85f, 0.95f), new Vector2(1.6f, 0.35f));
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.6f, 0.35f);
            go.AddComponent<DewLeaf>();
            go.AddComponent<UnsafeGround>();
            return go;
        }

        public static GameObject CreatePathLantern()
        {
            GameObject go = Template("PathLantern", PlaceholderVisual.Shape.Diamond, new Color(0.40f, 0.38f, 0.42f), new Vector2(0.3f, 0.45f));
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.9f, 1.4f);
            go.AddComponent<PathLantern>();
            return go;
        }

        public static GameObject CreateDoor()
        {
            GameObject go = Template("LevelDoor", PlaceholderVisual.Shape.Square, new Color(0.42f, 0.30f, 0.22f), new Vector2(1.2f, 2f));
            go.GetComponent<PlaceholderVisual>().offset = new Vector2(0f, 0.5f);
            go.GetComponent<PlaceholderVisual>().sortingOrder = 0;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.2f, 2f);
            box.offset = new Vector2(0f, 0.5f);
            go.AddComponent<LevelDoor>();
            return go;
        }

        public static GameObject CreateSpiritNpc()
        {
            GameObject go = Template("SpiritNpc", PlaceholderVisual.Shape.Circle, new Color(0.88f, 0.93f, 1f, 0.75f), new Vector2(0.8f, 1f));
            go.GetComponent<PlaceholderVisual>().sortingOrder = 3;
            go.AddComponent<SpiritNpc>();
            return go;
        }

        public static GameObject CreateSwing()
        {
            var go = new GameObject("PlaygroundSwing");
            var branch = go.AddComponent<PlaceholderVisual>();
            branch.shape = PlaceholderVisual.Shape.Square;
            branch.color = new Color(0.45f, 0.34f, 0.25f);
            branch.size = new Vector2(2.2f, 0.25f);
            branch.sortingOrder = 1;
            go.AddComponent<PlaygroundSwing>();
            return go;
        }

        private static GameObject Template(string name, PlaceholderVisual.Shape shape, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            var v = go.AddComponent<PlaceholderVisual>();
            v.shape = shape;
            v.color = color;
            v.size = size;
            v.sortingOrder = 2;
            return go;
        }

        // ------------------------------------------------------------------
        // Placement
        // ------------------------------------------------------------------

        public static void Build(LevelLayout layout, JumpNRunLevel level, Transform parent)
        {
            foreach (LevelMarker m in layout.Markers)
            {
                BuildMarker(layout, level, parent, m);
            }

            // Values set on prefab instances must be recorded as overrides to be saved.
            foreach (Component c in parent.GetComponentsInChildren<Component>(true))
            {
                if (c != null && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(c))
                {
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                }
            }
        }

        private static void BuildMarker(LevelLayout layout, JumpNRunLevel level, Transform parent, LevelMarker m)
        {
            Vector3 center = JumpNRunLevelBuilder.TileCenter(m.x, m.y);
            GameObject go;
            switch (m.kind)
            {
                case TileKind.Start:
                    BuildStart(parent, m);
                    return;
                case TileKind.Lantern:
                    go = Place("Lantern", parent, center, m);
                    go.GetComponent<Lantern>().index = m.index;
                    break;
                case TileKind.Bramble:
                    Place("Bramble", parent, center, m);
                    break;
                case TileKind.Ingredient:
                    go = Place("Ingredient", parent, center, m);
                    string id = IngredientIds.FromSymbol(m.symbol);
                    var ing = go.GetComponent<Ingredient>();
                    ing.ingredientId = id;
                    ing.rare = IngredientIds.IsRare(id);
                    ing.spotX = m.x;
                    ing.spotY = m.y;
                    var v = go.GetComponent<PlaceholderVisual>();
                    v.color = Ingredient.ColorOf(id);
                    v.shape = ing.rare ? PlaceholderVisual.Shape.Diamond : PlaceholderVisual.Shape.Circle;
                    v.size = Vector2.one * (ing.rare ? 0.55f : 0.42f);
                    go.name = "Ingredient " + id;
                    break;
                case TileKind.Goal:
                    Place("Goal", parent, center, m);
                    break;
                case TileKind.WindSpirit:
                    WindParams wind = level.elementsTuning != null ? level.elementsTuning.elements.wind : new WindParams();
                    int height = 1;
                    while (height < wind.maxHeight && !layout.IsStandable(m.x, m.y + height))
                    {
                        height++;
                    }

                    go = Place("WindSpirit", parent, center, m);
                    go.GetComponent<WindSpirit>().Configure(height, wind.width);
                    break;
                case TileKind.LanternSpirit:
                    Place("LanternSpirit", parent, center, m);
                    break;
                case TileKind.PathLantern:
                    go = Place("PathLantern", parent, center, m);
                    go.GetComponent<PathLantern>().index = m.index;
                    break;
                case TileKind.Door:
                    go = Place("LevelDoor", parent, center, m);
                    var door = go.GetComponent<LevelDoor>();
                    string target = layout.DoorTarget(m);
                    door.targetScenePath = JumpNRunLevelBuilder.ScenePathFor(target);
                    door.label = JumpNRunLevelBuilder.DisplayNameFor(target);
                    door.levelId = target == "meadow" || target == "climb" ? "" : target;
                    go.name = "Door " + target;
                    break;
                case TileKind.Npc:
                    go = Place("SpiritNpc", parent, center + Vector3.up * 0.3f, m);
                    go.GetComponent<SpiritNpc>().lineKey = layout.Setting("npc" + m.index);
                    break;
                case TileKind.Swing:
                    go = Place("PlaygroundSwing", parent, new Vector3(m.x + 0.5f, m.y + 0.5f, 0f), m);
                    var swing = go.GetComponent<PlaygroundSwing>();
                    if (swing != null)
                    {
                        var r = new LevelReachability(layout, null, level.elementsTuning != null ? level.elementsTuning.elements : null);
                        string ropeSetting = layout.Setting("rope" + m.index), angleSetting = layout.Setting("angle" + m.index);
                        swing.ropeLengthOverride = string.IsNullOrEmpty(ropeSetting) ? 0f : r.RopeLength(m);
                        swing.maxAngleOverride = string.IsNullOrEmpty(angleSetting) ? 0f : r.MaxAngleDeg(m);
                    }

                    break;
                case TileKind.DewLeaf:
                    Place("DewLeaf", parent, new Vector3(m.x + 0.5f, m.y + 0.175f, 0f), m);
                    break;
                default:
                    Placeholder(parent, m);
                    break;
            }
        }

        private static GameObject Place(string prefabName, Transform parent, Vector3 position, LevelMarker m)
        {
            GameObject prefab = JumpNRunPrefabs.Element(prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"[JumpNRun] Prefab '{prefabName}' missing ({JumpNRunPrefabs.ElementPath(prefabName)}) – placeholder used for {m}.");
                return Placeholder(parent, m);
            }

            GameObject go = JumpNRunPrefabs.Place(prefab, parent, position);
            go.name = $"{prefabName} #{m.index}";
            return go;
        }

        /// <summary>Feet of the player rest on the bottom edge of the start tile.</summary>
        private static void BuildStart(Transform parent, LevelMarker m)
        {
            Vector3 feet = new Vector3(m.x + 0.5f, m.y, 0f);
            Vector3 pos = feet + Vector3.up * (JumpNRunPrefabs.PlayerSize.y * 0.5f + 0.02f);
            GameObject player = JumpNRunPrefabs.Place(JumpNRunPrefabs.Player, parent.parent, pos);
            player.name = "Player";

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(pos.x, pos.y, -10f);
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.72f, 0.80f);
            cam.AddComponent<AudioListener>();
            cam.AddComponent<JumpNRunCamera>();
        }

        /// <summary>Visible stand-in for markers whose element is not built yet.</summary>
        private static GameObject Placeholder(Transform parent, LevelMarker m)
        {
            var go = new GameObject($"{m.kind} #{m.index}");
            go.transform.SetParent(parent, false);
            go.transform.position = JumpNRunLevelBuilder.TileCenter(m.x, m.y);
            var v = go.AddComponent<PlaceholderVisual>();
            v.shape = PlaceholderVisual.Shape.Diamond;
            v.color = new Color(1f, 0.4f, 0.8f, 0.8f);
            v.size = Vector2.one * 0.5f;
            return go;
        }
    }
}
