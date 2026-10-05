using System.Text;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Developer overlay (F1): speed, ground state, assist timers, gravity band,
    /// the zone the player is in, section and last checkpoint. Not for players,
    /// so its labels are plain English.
    /// </summary>
    public class JumpNRunDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible;

        private readonly StringBuilder sb = new StringBuilder(512);
        private GUIStyle style;

        public static bool Visible { get; private set; }

        private void Update()
        {
            if (GameInput.CheatTogglePressed)
            {
                visible = !visible;
            }

            Visible = visible;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            JumpNRunPlayer player = JumpNRunPlayer.Instance;
            JumpNRunSession session = JumpNRunSession.Current;
            if (player == null)
            {
                return;
            }

            if (style == null)
            {
                style = GuiDraw.Rich(12, new Color(0.85f, 1f, 0.85f));
                style.font = Font.CreateDynamicFontFromOSFont("Consolas", 12);
            }

            PlayerMotor m = player.Motor;
            MovementParams p = m.Params;
            Vector2 v = player.Velocity;
            sb.Length = 0;
            sb.Append("<b>Jump'n'Run debug</b>  (F1)\n");
            sb.AppendFormat("speed     {0,6:0.00}  x {1,6:0.00}  y {2,6:0.00}\n", v.magnitude, v.x, v.y);
            sb.AppendFormat("grounded  {0}   rising {1}   facing {2}\n", m.Grounded, m.Rising, m.Facing);
            sb.AppendFormat("coyote    {0}\n", Bar(m.CoyoteTimer, p.coyoteTime));
            sb.AppendFormat("buffer    {0}\n", Bar(m.BufferTimer, p.jumpBufferTime));
            sb.AppendFormat("gravity   {0}  (g {1:0.0}, v0 {2:0.0}, max fall {3:0.0})\n", m.GravityBand, p.Gravity, p.JumpVelocity, p.MaxFallSpeed);
            sb.AppendFormat("air jumps {0}/{1}   dash {2}\n", m.AirJumpsLeft, m.AirJumpCapacity, p.dashEnabled ? (m.DashReady ? "ready" : "used") : "off");
            sb.AppendFormat("zone      {0}\n", Zone(player));
            if (session != null && session.Checkpoints != null)
            {
                CheckpointTracker c = session.Checkpoints;
                Vec2 lp = c.LanternPoint;
                sb.AppendFormat("section   {0}   lantern {1} at ({2:0.0}, {3:0.0})\n", c.CurrentLantern + 1,
                    c.CurrentLantern < 0 ? "start" : c.CurrentLantern.ToString(), lp.x, lp.y);
                sb.AppendFormat("safe pt   ({0:0.0}, {1:0.0})   catches {2}   bag {3}\n", c.SafePoint.x, c.SafePoint.y,
                    session.Catches, session.Bag.Total);
            }

            JumpNRunOptions o = JumpNRunOptions.Instance;
            if (o != null)
            {
                sb.AppendFormat("assists   {0}   timeScale {1:0.00}\n", o.Options.Serialize(), Time.timeScale);
            }

            var r = new Rect(10f, Screen.height - 220f, 430f, 210f);
            GuiDraw.Solid(r, new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(r.x + 8f, r.y + 6f, r.width - 16f, r.height - 12f), sb.ToString(), style);
        }

        private static string Bar(float value, float max)
        {
            int n = max > 0f ? Mathf.Clamp(Mathf.RoundToInt(value / max * 10f), 0, 10) : 0;
            return "[" + new string('#', n) + new string('.', 10 - n) + $"] {Mathf.Max(0f, value):0.000}s";
        }

        private static string Zone(JumpNRunPlayer player)
        {
            JumpNRunSession session = JumpNRunSession.Current;
            if (session != null && session.IsCatching) return "catch";
            if (player.IsControlled) return "swing";
            if (Time.time - WindSpirit.LastPushTime < 0.1f) return "wind";
            Collider2D g = player.Ground;
            if (g == null) return "air";
            if (g.GetComponent<DewLeaf>() != null) return "dew leaf";
            if (g.GetComponent<LeafPlatform>() != null) return "leaf platform";
            if (g.GetComponent<GhostPlatform>() != null) return "ghost platform";
            if (g.GetComponent<OneWayPlatform>() != null) return "one-way";
            return "ground";
        }
    }
}
