using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Follow camera for the Jump'n'Run levels. The math is in
    /// <see cref="CameraRig"/>; this component runs it in LateUpdate (after the
    /// interpolated player transform moved, so no jitter) and never shakes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class JumpNRunCamera : MonoBehaviour
    {
        [SerializeField] private CameraTuning tuning;

        private Camera cam;
        private CameraRig rig;
        private JumpNRunPlayer player;

        public CameraRig Rig => rig;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (tuning == null && JumpNRunLevel.Current != null)
            {
                tuning = JumpNRunLevel.Current.cameraTuning;
            }

            rig = new CameraRig(tuning != null ? tuning.camera : new CameraParams());
        }

        private void Start()
        {
            Snap();
        }

        /// <summary>Jump straight to the player (level start, after a catch).</summary>
        public void Snap()
        {
            player = JumpNRunPlayer.Instance;
            if (player == null)
            {
                return;
            }

            ApplyView();
            rig.Snap(Focus());
            Write();
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                Snap();
                return;
            }

            ApplyView();
            rig.Update(Time.deltaTime, Focus(), player.Velocity.x, player.IsGrounded);
            Write();
        }

        private Vec2 Focus()
        {
            Vector2 f = player.Feet;
            return new Vec2(f.x, f.y);
        }

        private void ApplyView()
        {
            cam.orthographicSize = rig.Params.orthographicSize;
            rig.HalfView = new Vec2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
            JumpNRunLevel level = JumpNRunLevel.Current;
            if (level != null)
            {
                Rect b = level.bounds;
                rig.BoundsMin = new Vec2(b.xMin, b.yMin - 2f);
                rig.BoundsMax = new Vec2(b.xMax, b.yMax + 6f);
            }
        }

        private void Write()
        {
            transform.position = new Vector3(rig.Position.x, rig.Position.y, transform.position.z);
        }
    }
}
