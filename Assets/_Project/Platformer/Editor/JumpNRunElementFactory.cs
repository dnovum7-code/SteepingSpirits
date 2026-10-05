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
                default:
                    Placeholder(parent, m);
                    break;
            }
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
