using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>One recorded input change (time since the start of the recording).</summary>
    public struct RecordedInput
    {
        public float time;
        public float moveX;
        public float moveY;
        public bool jumpHeld;

        /// <summary>Press edges that happened since the previous entry.</summary>
        public bool jumpPressed;
        public bool dashPressed;
    }

    /// <summary>
    /// A replayable input recording (F5/F6 in the levels). Stores only changes,
    /// so files stay small. Text format, version 1:
    /// <code>
    /// jnrrec 1
    /// level Level1
    /// start 2.50 2.00
    /// assists speed=1.0;air=0;fall=0
    /// 0.000 1 0 0 0 0      (time moveX moveY jumpHeld jumpPressed dashPressed)
    /// </code>
    /// </summary>
    public sealed class InputRecording
    {
        public const int Version = 1;

        public string levelId = "";
        public Vec2 start;
        public string assists = "";
        public readonly List<RecordedInput> entries = new List<RecordedInput>();

        public float Duration => entries.Count > 0 ? entries[entries.Count - 1].time : 0f;

        /// <summary>Adds a sample; it is stored only if something changed or a press happened.</summary>
        public void Record(RecordedInput sample)
        {
            sample.moveX = Quantize(sample.moveX);
            sample.moveY = Quantize(sample.moveY);
            if (entries.Count > 0 && !sample.jumpPressed && !sample.dashPressed)
            {
                RecordedInput last = entries[entries.Count - 1];
                if (last.moveX == sample.moveX && last.moveY == sample.moveY && last.jumpHeld == sample.jumpHeld)
                {
                    return;
                }
            }

            entries.Add(sample);
        }

        /// <summary>Closes the recording at a time so trailing idle time is kept.</summary>
        public void End(float time)
        {
            RecordedInput last = entries.Count > 0 ? entries[entries.Count - 1] : default;
            last.time = time;
            last.jumpPressed = false;
            last.dashPressed = false;
            entries.Add(last);
        }

        private static float Quantize(float v) => (float)Math.Round(PMath.Clamp(v, -1f, 1f) * 100f) / 100f;

        public string Serialize()
        {
            var sb = new StringBuilder();
            var c = CultureInfo.InvariantCulture;
            sb.Append("jnrrec ").Append(Version).Append('\n');
            sb.Append("level ").Append(levelId).Append('\n');
            sb.Append("start ").Append(start.x.ToString("0.000", c)).Append(' ').Append(start.y.ToString("0.000", c)).Append('\n');
            sb.Append("assists ").Append(assists).Append('\n');
            foreach (RecordedInput e in entries)
            {
                sb.Append(e.time.ToString("0.000", c)).Append(' ')
                    .Append(e.moveX.ToString("0.##", c)).Append(' ')
                    .Append(e.moveY.ToString("0.##", c)).Append(' ')
                    .Append(e.jumpHeld ? '1' : '0').Append(' ')
                    .Append(e.jumpPressed ? '1' : '0').Append(' ')
                    .Append(e.dashPressed ? '1' : '0').Append('\n');
            }

            return sb.ToString();
        }

        public static InputRecording Parse(string text)
        {
            var r = new InputRecording();
            var c = CultureInfo.InvariantCulture;
            string[] lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 || !lines[0].StartsWith("jnrrec "))
            {
                throw new FormatException("not a jnrrec file");
            }

            if (!int.TryParse(lines[0].Substring(7).Trim(), out int version) || version > Version)
            {
                throw new FormatException("unsupported recording version");
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                string key = line.Split(' ')[0];
                string rest = line.Length > key.Length ? line.Substring(key.Length).Trim() : "";
                if (key == "level") { r.levelId = rest; continue; }
                if (key == "assists") { r.assists = rest; continue; }
                if (key == "start")
                {
                    string[] s = rest.Split(' ');
                    r.start = new Vec2(float.Parse(s[0], c), float.Parse(s[1], c));
                    continue;
                }

                string[] p = line.Split(' ');
                if (p.Length < 6) throw new FormatException($"line {i + 1}: expected 6 values");
                r.entries.Add(new RecordedInput
                {
                    time = float.Parse(p[0], c),
                    moveX = float.Parse(p[1], c),
                    moveY = float.Parse(p[2], c),
                    jumpHeld = p[3] == "1",
                    jumpPressed = p[4] == "1",
                    dashPressed = p[5] == "1"
                });
            }

            return r;
        }
    }

    /// <summary>Reads a recording back by time; press edges are never lost, even if frames are skipped.</summary>
    public sealed class InputPlayback
    {
        private readonly InputRecording recording;
        private int next;
        private RecordedInput current;

        public InputPlayback(InputRecording recording)
        {
            this.recording = recording;
        }

        public bool Finished => next >= recording.entries.Count && recording.entries.Count > 0 || recording.entries.Count == 0;

        /// <summary>The input at time t (seconds since playback start).</summary>
        public RecordedInput Sample(float t)
        {
            bool jump = false, dash = false;
            while (next < recording.entries.Count && recording.entries[next].time <= t)
            {
                current = recording.entries[next];
                jump |= current.jumpPressed;
                dash |= current.dashPressed;
                next++;
            }

            RecordedInput r = current;
            r.time = t;
            r.jumpPressed = jump;
            r.dashPressed = dash;
            return r;
        }
    }
}
