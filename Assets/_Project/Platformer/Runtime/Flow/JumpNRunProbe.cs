using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.Platformer.Hooks;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>Publishes the running level through <see cref="JumpNRunHooks.Probe"/> (tools and tests).</summary>
    public class JumpNRunProbe : MonoBehaviour, IJumpNRunProbe
    {
        private readonly RaycastHit2D[] hits = new RaycastHit2D[4];
        private readonly List<Lantern> lanterns = new List<Lantern>();
        private ContactFilter2D solid;

        private void Awake()
        {
            JumpNRunHooks.Probe = this;
            solid = new ContactFilter2D { useTriggers = false };
        }

        private void Start()
        {
            lanterns.AddRange(FindObjectsByType<Lantern>(FindObjectsSortMode.None));
            lanterns.Sort((a, b) => a.index.CompareTo(b.index));
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(JumpNRunHooks.Probe, this))
            {
                JumpNRunHooks.Probe = null;
            }
        }

        private static JumpNRunPlayer Player => JumpNRunPlayer.Instance;
        private static JumpNRunSession Session => JumpNRunSession.Current;

        public string LevelId => JumpNRunLevel.Current != null ? JumpNRunLevel.Current.levelId : "";
        public string LayoutText => JumpNRunLevel.Current != null ? JumpNRunLevel.Current.layoutText : "";
        public bool HasPlayer => Player != null;
        public Vector2 PlayerFeet => Player != null ? Player.Feet : Vector2.zero;
        public Vector2 PlayerVelocity => Player != null ? Player.Velocity : Vector2.zero;
        public bool Grounded => Player != null && Player.IsGrounded;
        public bool Riding => RiddenSwing() != null;
        public bool IsCatching => Session != null && Session.IsCatching;
        public int Catches => Session != null ? Session.Catches : 0;
        public int CurrentLantern => Session != null && Session.Checkpoints != null ? Session.Checkpoints.CurrentLantern : -1;
        public bool Finished => Session != null && Session.Finished;
        public int BagTotal => Session != null ? Session.Bag.Total : 0;

        public int LanternCount => lanterns.Count;
        public Vector2 LanternFeet(int index) => lanterns[index].RespawnFeet;
        public int SwingCount => PlaygroundSwing.All.Count;
        public Vector2 SwingSeat(int index) => PlaygroundSwing.All[index].Seat;
        public Vector2 SwingPivot(int index) => PlaygroundSwing.All[index].transform.position;

        public void Teleport(Vector2 feet)
        {
            if (Player != null)
            {
                Player.Teleport(feet);
            }
        }

        public RouteObservation Observe()
        {
            var o = new RouteObservation();
            JumpNRunPlayer p = Player;
            if (p == null)
            {
                return o;
            }

            Vector2 feet = p.Feet;
            Vector2 v = p.Velocity;
            o.feet = new Vec2(feet.x, feet.y);
            o.velocity = new Vec2(v.x, v.y);
            o.grounded = p.IsGrounded;
            o.groundAheadRight = GroundBelow(feet + new Vector2(0.6f, 0.1f));
            o.groundAheadLeft = GroundBelow(feet + new Vector2(-0.6f, 0.1f));
            o.catches = Catches;

            PlaygroundSwing s = RiddenSwing();
            if (s != null)
            {
                o.riding = true;
                o.swingAngle = s.Model.Angle;
                o.swingAngularVelocity = s.Model.AngularVelocity;
                o.swingAmplitude = s.Model.Amplitude;
                o.swingMaxAngle = s.Model.MaxAngle;
                o.swingPivot = new Vec2(s.transform.position.x, s.transform.position.y);
            }

            return o;
        }

        private bool GroundBelow(Vector2 from)
        {
            int n = Physics2D.Raycast(from, Vector2.down, solid, hits, 0.7f);
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider != null && hits[i].collider.GetComponentInParent<JumpNRunPlayer>() == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static PlaygroundSwing RiddenSwing()
        {
            foreach (PlaygroundSwing s in PlaygroundSwing.All)
            {
                if (s.Occupied) return s;
            }

            return null;
        }
    }
}
