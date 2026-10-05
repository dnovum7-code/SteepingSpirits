using UnityEngine;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.JumpNRun;

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>Creates the scene objects for the markers of a level layout.</summary>
    public static class JumpNRunElementFactory
    {
        public static void Build(LevelLayout layout, JumpNRunLevel level, Transform parent)
        {
            foreach (LevelMarker m in layout.Markers)
            {
                BuildMarker(layout, level, parent, m);
            }
        }

        private static void BuildMarker(LevelLayout layout, JumpNRunLevel level, Transform parent, LevelMarker m)
        {
            switch (m.kind)
            {
                case TileKind.Start:
                    BuildStart(parent, m);
                    break;
                case TileKind.Lantern:
                    BuildLantern(parent, m);
                    break;
                case TileKind.Bramble:
                    BuildBramble(parent, m);
                    break;
                case TileKind.Ingredient:
                    BuildIngredient(parent, m);
                    break;
                case TileKind.Goal:
                    BuildGoal(parent, m);
                    break;
                default:
                    Placeholder(parent, m);
                    break;
            }
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
            JumpNRunCameraSetup.Attach(cam);
        }

        private static void BuildLantern(Transform parent, LevelMarker m)
        {
            GameObject go = Element(parent, "Lantern", m, PlaceholderVisual.Shape.Circle, new Color(0.45f, 0.42f, 0.38f),
                new Vector2(0.45f, 0.6f));
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.2f, 2.5f);
            go.AddComponent<Lantern>().index = m.index;
        }

        private static void BuildBramble(Transform parent, LevelMarker m)
        {
            GameObject go = Element(parent, "Bramble", m, PlaceholderVisual.Shape.Triangle, new Color(0.42f, 0.30f, 0.45f),
                new Vector2(1f, 0.7f));
            go.transform.position += Vector3.down * 0.15f;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 0.5f);
            go.AddComponent<Bramble>();
        }

        private static void BuildIngredient(Transform parent, LevelMarker m)
        {
            string id = IngredientIds.FromSymbol(m.symbol);
            bool rare = IngredientIds.IsRare(id);
            GameObject go = Element(parent, "Ingredient " + id, m, rare ? PlaceholderVisual.Shape.Diamond : PlaceholderVisual.Shape.Circle,
                Ingredient.ColorOf(id), Vector2.one * (rare ? 0.55f : 0.42f));
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.45f;
            var ing = go.AddComponent<Ingredient>();
            ing.ingredientId = id;
            ing.rare = rare;
        }

        private static void BuildGoal(Transform parent, LevelMarker m)
        {
            GameObject go = Element(parent, "Goal", m, PlaceholderVisual.Shape.Triangle, new Color(0.95f, 0.82f, 0.55f),
                new Vector2(1.4f, 2f));
            go.transform.position += Vector3.up * 0.5f;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.2f, 2f);
            go.AddComponent<Goal>();
        }

        /// <summary>An element object at a marker's tile centre with a placeholder look.</summary>
        public static GameObject Element(Transform parent, string name, LevelMarker m, PlaceholderVisual.Shape shape,
            Color color, Vector2 size)
        {
            var go = new GameObject($"{name} #{m.index}");
            go.transform.SetParent(parent, false);
            go.transform.position = JumpNRunLevelBuilder.TileCenter(m.x, m.y);
            var v = go.AddComponent<PlaceholderVisual>();
            v.shape = shape;
            v.color = color;
            v.size = size;
            v.sortingOrder = 2;
            return go;
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

namespace SteepingSpirits.Platformer.EditorTools
{
    /// <summary>Which follow component the level camera gets.</summary>
    public static class JumpNRunCameraSetup
    {
        public static void Attach(GameObject cameraObject)
        {
            cameraObject.AddComponent<JumpNRunCamera>();
        }
    }
}
