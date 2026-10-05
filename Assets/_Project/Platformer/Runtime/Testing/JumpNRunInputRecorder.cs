using System;
using System.IO;
using UnityEngine;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.Hooks;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// F5 starts/stops recording the player's inputs, F6 replays the latest
    /// recording of this level from its start point. Files land in
    /// persistentDataPath/jumpnrun_recordings/ (attach them to bug reports).
    /// A developer tool: the small status label is drawn with OnGUI.
    /// </summary>
    public class JumpNRunInputRecorder : MonoBehaviour, IPlayerInputSource
    {
        public const string FolderName = "jumpnrun_recordings";

        private enum Mode
        {
            Idle,
            Recording,
            Playing
        }

        private Mode mode;
        private InputRecording recording;
        private InputPlayback playback;
        private float startTime;
        private string lastFile = "";
        private string status = "";
        private float statusUntil;
        private GUIStyle style;

        public static string Folder => Path.Combine(Application.persistentDataPath, FolderName);

        private void Update()
        {
            if (KeyF5())
            {
                if (mode == Mode.Recording) StopRecording();
                else if (mode == Mode.Idle) StartRecording();
            }

            if (KeyF6())
            {
                if (mode == Mode.Playing) StopPlayback("Wiedergabe abgebrochen");
                else if (mode == Mode.Idle) StartPlayback();
            }

            if (mode == Mode.Playing && playback.Finished)
            {
                StopPlayback("Wiedergabe fertig");
            }
        }

        private void OnDisable()
        {
            if (mode == Mode.Recording) StopRecording();
            if (ReferenceEquals(JumpNRunHooks.InputOverride, this)) JumpNRunHooks.InputOverride = null;
        }

        // ------------------------------------------------------------------
        // IPlayerInputSource: wraps the devices while recording, replays while playing.
        // ------------------------------------------------------------------

        public PlayerInputFrame Read()
        {
            float t = Time.time - startTime;
            if (mode == Mode.Playing)
            {
                RecordedInput s = playback.Sample(t);
                return new PlayerInputFrame
                {
                    move = new Vector2(s.moveX, s.moveY),
                    jumpHeld = s.jumpHeld,
                    jumpPressed = s.jumpPressed,
                    dashPressed = s.dashPressed
                };
            }

            JumpNRunHooks.InputOverride = null;
            PlayerInputFrame live = JumpNRunPlayer.ReadInput();
            JumpNRunHooks.InputOverride = this;
            if (mode == Mode.Recording)
            {
                recording.Record(new RecordedInput
                {
                    time = t,
                    moveX = live.move.x,
                    moveY = live.move.y,
                    jumpHeld = live.jumpHeld,
                    jumpPressed = live.jumpPressed,
                    dashPressed = live.dashPressed
                });
            }

            return live;
        }

        private void StartRecording()
        {
            IJumpNRunProbe probe = JumpNRunHooks.Probe;
            if (probe == null || !probe.HasPlayer || JumpNRunHooks.InputOverride != null)
            {
                return;
            }

            Vector2 feet = probe.PlayerFeet;
            recording = new InputRecording
            {
                levelId = probe.LevelId,
                start = new Vec2(feet.x, feet.y),
                assists = JumpNRunOptions.Instance != null ? JumpNRunOptions.Instance.Options.Serialize() : ""
            };
            startTime = Time.time;
            mode = Mode.Recording;
            JumpNRunHooks.InputOverride = this;
            Show("● Aufnahme läuft (F5 stoppt)");
        }

        private void StopRecording()
        {
            JumpNRunHooks.InputOverride = null;
            mode = Mode.Idle;
            recording.End(Time.time - startTime);
            try
            {
                Directory.CreateDirectory(Folder);
                string name = $"{recording.levelId}_{DateTime.Now:yyyyMMdd-HHmmss}.jnrrec";
                lastFile = Path.Combine(Folder, name);
                File.WriteAllText(lastFile, recording.Serialize());
                File.WriteAllText(LatestPath(recording.levelId), recording.Serialize());
                Show("Aufnahme gespeichert: " + name);
                Debug.Log("[JumpNRun] Input recording saved: " + lastFile);
            }
            catch (Exception e)
            {
                Show("Aufnahme nicht gespeichert");
                Debug.LogWarning("[JumpNRun] Recording not saved: " + e.Message);
            }
        }

        private void StartPlayback()
        {
            IJumpNRunProbe probe = JumpNRunHooks.Probe;
            if (probe == null || !probe.HasPlayer || JumpNRunHooks.InputOverride != null)
            {
                return;
            }

            string file = LatestPath(probe.LevelId);
            if (!File.Exists(file))
            {
                Show("Keine Aufnahme für dieses Level");
                return;
            }

            try
            {
                InputRecording r = InputRecording.Parse(File.ReadAllText(file));
                probe.Teleport(new Vector2(r.start.x, r.start.y));
                playback = new InputPlayback(r);
                startTime = Time.time;
                mode = Mode.Playing;
                JumpNRunHooks.InputOverride = this;
                Show($"▶ Wiedergabe ({r.Duration:0.0} s, F6 bricht ab)");
            }
            catch (FormatException e)
            {
                Show("Aufnahme unlesbar");
                Debug.LogWarning("[JumpNRun] Recording unreadable: " + e.Message);
            }
        }

        private void StopPlayback(string message)
        {
            mode = Mode.Idle;
            if (ReferenceEquals(JumpNRunHooks.InputOverride, this)) JumpNRunHooks.InputOverride = null;
            Show(message);
        }

        private static string LatestPath(string levelId) => Path.Combine(Folder, $"latest_{levelId}.jnrrec");

        private void Show(string text)
        {
            status = text;
            statusUntil = Time.unscaledTime + 3f;
        }

        private void OnGUI()
        {
            string text = mode == Mode.Recording ? "● REC " + (Time.time - startTime).ToString("0.0") + " s"
                : mode == Mode.Playing ? "▶ PLAY " + (Time.time - startTime).ToString("0.0") + " s"
                : Time.unscaledTime < statusUntil ? status : "";
            if (text.Length == 0)
            {
                return;
            }

            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            style.normal.textColor = mode == Mode.Recording ? new Color(1f, 0.6f, 0.55f) : new Color(0.85f, 0.95f, 1f);
            GUI.Label(new Rect(Screen.width - 330f, 10f, 320f, 22f), text, style);
        }

        private static bool KeyF5()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F5);
#endif
        }

        private static bool KeyF6()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F6);
#endif
        }
    }
}
