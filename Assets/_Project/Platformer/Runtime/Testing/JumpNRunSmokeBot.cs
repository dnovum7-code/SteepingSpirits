using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.Hooks;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Outcome of one level run of the smoke test (written as JSON for the editor).</summary>
    [Serializable]
    public class SmokeLevelResult
    {
        public string scene = "";
        public string levelId = "";
        public bool reachedGoal;
        public bool isHub;
        public float seconds;
        public int catches;
        public int replans;
        public int collected;
        public int exceptions;
        public int errors;
        public int slowFrames;
        public float worstFrameMs;
        public float averageFrameMs;
        public List<string> messages = new List<string>();
        public List<string> route = new List<string>();
    }

    /// <summary>
    /// Plays the loaded level with real inputs along the route from the
    /// reachability check (<see cref="RoutePlanner"/> + <see cref="RouteFollower"/>).
    /// Watches exceptions, error logs, catches and frame times, then writes a
    /// result file and (in the editor) leaves play mode.
    /// </summary>
    public class JumpNRunSmokeBot : MonoBehaviour, IPlayerInputSource
    {
        public const float SlowFrameMs = 1000f / 30f;

        public string resultPath = "";
        public float maxSeconds = 180f;
        public float hubSeconds = 4f;

        private readonly SmokeLevelResult result = new SmokeLevelResult();
        private LevelLayout layout;
        private LevelReachability reach;
        private RouteFollower follower;
        private PlayerInputFrame frame;
        private float startTime = -1f;
        private int lastCatches;
        private bool finished;
        private double frameSum;
        private int frameCount;

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;
            JumpNRunHooks.InputOverride = this;
            JumpNRunSaveStore.ReadOnly = true;
            IngredientHandover.DryRun = true;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
            if (ReferenceEquals(JumpNRunHooks.InputOverride, this))
            {
                JumpNRunHooks.InputOverride = null;
            }
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception)
            {
                result.exceptions++;
                Note("Exception: " + message + " | " + FirstLine(stack));
            }
            else if (type == LogType.Error || type == LogType.Assert)
            {
                result.errors++;
                Note("Error: " + message);
            }
        }

        private static string FirstLine(string s) => string.IsNullOrEmpty(s) ? "" : s.Split('\n')[0];

        private void Note(string message)
        {
            if (result.messages.Count < 40) result.messages.Add(message);
        }

        public PlayerInputFrame Read()
        {
            PlayerInputFrame f = frame;
            frame.jumpPressed = false; // an edge is delivered once
            frame.dashPressed = false;
            return f;
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            float ms = Time.unscaledDeltaTime * 1000f;
            if (startTime >= 0f && Time.frameCount > 5)
            {
                frameSum += ms;
                frameCount++;
                if (ms > SlowFrameMs) result.slowFrames++;
                result.worstFrameMs = Mathf.Max(result.worstFrameMs, ms);
            }

            IJumpNRunProbe probe = JumpNRunHooks.Probe;
            if (startTime < 0f)
            {
                if (probe != null && probe.HasPlayer && Time.frameCount > 3)
                {
                    Begin(probe);
                }

                return;
            }

            float elapsed = Time.time - startTime;
            if (result.isHub)
            {
                if (elapsed >= hubSeconds) Finish(true, "hub idle check");
                return;
            }

            if (probe == null)
            {
                Finish(false, "level unloaded");
                return;
            }

            if (probe.Finished)
            {
                Finish(true, "goal reached");
                return;
            }

            if (elapsed > maxSeconds)
            {
                Finish(false, $"timeout after {maxSeconds:0}s at {probe.PlayerFeet}");
            }
        }

        private void FixedUpdate()
        {
            IJumpNRunProbe probe = JumpNRunHooks.Probe;
            if (finished || startTime < 0f || result.isHub || probe == null || probe.IsCatching)
            {
                frame = default;
                return;
            }

            RouteObservation o = probe.Observe();
            if (probe.Catches != lastCatches || follower == null || follower.Failed || (follower.Done && !probe.Finished))
            {
                if (follower != null)
                {
                    Note(probe.Catches != lastCatches ? $"caught during step {follower.Index} ({follower.Current})"
                        : follower.Failed ? "stuck: " + follower.FailReason : "route done but goal not touched");
                }

                lastCatches = probe.Catches;
                if (++result.replans > 12)
                {
                    Finish(false, "too many retries");
                    return;
                }

                Plan(probe);
                if (follower == null)
                {
                    return;
                }
            }

            MotorInput input = follower.Tick(Time.fixedDeltaTime, o);
            frame.move = new Vector2(input.moveX, input.moveY);
            frame.jumpHeld = input.jumpHeld;
            frame.jumpPressed |= input.jumpPressed;
        }

        private void Begin(IJumpNRunProbe probe)
        {
            startTime = Time.time;
            result.levelId = probe.LevelId;
            result.scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            try
            {
                layout = LevelLayout.Parse(probe.LayoutText);
            }
            catch (FormatException e)
            {
                Finish(false, "level text unreadable: " + e.Message);
                return;
            }

            result.isHub = layout.IsHub || layout.All(TileKind.Goal).Count == 0;
            MovementParams move = JumpNRunPlayer.Instance != null ? JumpNRunPlayer.Instance.Params : new MovementParams();
            reach = new LevelReachability(layout, move, SpiritElementsTuning.Current);
        }

        private void Plan(IJumpNRunProbe probe)
        {
            Vector2 feet = probe.PlayerFeet;
            LevelReachability.Cell? start = RoutePlanner.NearestStanding(reach, new Vec2(feet.x, feet.y));
            LevelMarker goal = layout.All(TileKind.Goal)[0];
            List<RouteStep> route = start.HasValue
                ? RoutePlanner.Plan(reach, layout, start.Value, c => c.y == goal.y && c.x == goal.x)
                : null;
            if (route == null)
            {
                follower = null;
                Finish(false, "no route from " + feet);
                return;
            }

            result.route.Clear();
            foreach (RouteStep s in route) result.route.Add(s.ToString());
            MovementParams move = JumpNRunPlayer.Instance != null ? JumpNRunPlayer.Instance.Params : new MovementParams();
            follower = new RouteFollower(route, reach, move) { SeatOf = SeatOf };
        }

        private static Vec2 SeatOf(Vec2 pivot)
        {
            IJumpNRunProbe probe = JumpNRunHooks.Probe;
            if (probe == null || probe.SwingCount == 0)
            {
                return pivot;
            }

            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < probe.SwingCount; i++)
            {
                Vector2 p = probe.SwingPivot(i);
                float d = (p - new Vector2(pivot.x, pivot.y)).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            Vector2 seat = probe.SwingSeat(best);
            return new Vec2(seat.x, seat.y);
        }

        private void Finish(bool ok, string reason)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            frame = default;
            IJumpNRunProbe probe = JumpNRunHooks.Probe;
            result.reachedGoal = ok && !result.isHub;
            result.seconds = startTime >= 0f ? Time.time - startTime : 0f;
            result.catches = probe != null ? probe.Catches : 0;
            result.collected = probe != null ? probe.BagTotal : 0;
            result.averageFrameMs = frameCount > 0 ? (float)(frameSum / frameCount) : 0f;
            Note((ok ? "OK: " : "FAILED: ") + reason);

            if (!string.IsNullOrEmpty(resultPath))
            {
                try
                {
                    File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[JumpNRun] Smoke result not written: " + e.Message);
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }
    }
}
