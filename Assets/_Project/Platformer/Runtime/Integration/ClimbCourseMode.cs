using UnityEngine;
using SteepingSpirits.Interaction;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Sets up the old climbing path (PlatformerCourse) with the Jump'n'Run
    /// player, camera, feedback and the gentle catch instead of the old
    /// controller and its spikes-reset. Used only when the course's switch is on.
    /// </summary>
    public static class ClimbCourseMode
    {
        public static readonly Vector2 PlayerSize = new Vector2(0.65f, 1.2f);

        /// <summary>Builds level services and the player; returns the player transform.</summary>
        public static Transform Setup(Transform parent, Vector2 spawnFeet, Rect bounds, MovementTuning tuning)
        {
            var levelRoot = new GameObject("JumpNRunServices");
            levelRoot.transform.SetParent(parent, false);
            levelRoot.SetActive(false); // configure before Awake runs
            var level = levelRoot.AddComponent<JumpNRunLevel>();
            level.levelId = "climb";
            level.displayName = "Kletterpfad";
            level.bounds = bounds;
            level.movementTuning = tuning;
            levelRoot.AddComponent<JumpNRunSession>();
            levelRoot.AddComponent<JumpNRunTime>();
            levelRoot.AddComponent<JumpNRunOptions>();
            levelRoot.AddComponent<JumpNRunDebugOverlay>();
            levelRoot.AddComponent<JumpNRunParticles>();
            levelRoot.AddComponent<JumpNRunSounds>();
            levelRoot.SetActive(true);

            var go = new GameObject("Player") { tag = "Player" };
            go.SetActive(false);
            go.transform.position = spawnFeet + Vector2.up * (PlayerSize.y * 0.5f + 0.02f);
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>().size = PlayerSize;
            var player = go.AddComponent<JumpNRunPlayer>();

            // The climbing path needs the dash; everything else uses the shared tuning.
            MovementParams p = tuning != null ? tuning.movement.Clone() : new MovementParams();
            p.dashEnabled = true;
            var climbTuning = ScriptableObject.CreateInstance<MovementTuning>();
            climbTuning.movement = p;
            player.Configure(climbTuning);

            go.AddComponent<JumpNRunFeedback>();
            go.AddComponent<ClimbGrab>();
            go.AddComponent<Interactor2D>();
            var visual = go.AddComponent<PlaceholderVisual>();
            visual.color = new Color(0.96f, 0.86f, 0.70f);
            visual.size = PlayerSize;
            visual.sortingOrder = 10;
            go.SetActive(true);
            player.Configure(climbTuning);
            return go.transform;
        }

        /// <summary>Puts the Jump'n'Run camera on the main camera.</summary>
        public static void SetupCamera(Camera cam)
        {
            var oldFollow = cam.GetComponent<SteepingSpirits.Player.CameraFollow2D>();
            if (oldFollow != null)
            {
                oldFollow.enabled = false; // one camera script at a time, and no shake
            }

            if (cam.GetComponent<JumpNRunCamera>() == null)
            {
                cam.gameObject.AddComponent<JumpNRunCamera>();
            }
        }
    }
}
