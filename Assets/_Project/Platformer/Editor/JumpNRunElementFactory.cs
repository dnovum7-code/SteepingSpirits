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
    /// <summary>Which follow component the level camera gets (own camera from B2 on).</summary>
    public static class JumpNRunCameraSetup
    {
        public static void Attach(GameObject cameraObject)
        {
            cameraObject.AddComponent<SteepingSpirits.Player.CameraFollow2D>();
        }
    }
}
