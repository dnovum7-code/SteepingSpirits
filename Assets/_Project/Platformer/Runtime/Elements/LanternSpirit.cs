using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A small lantern spirit. It waits at its spot; once the player touches it,
    /// it follows them and its light makes nearby ghost platforms solid.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LanternSpirit : MonoBehaviour
    {
        private static readonly List<LanternSpirit> active = new List<LanternSpirit>();
        private static readonly List<Vec2> lights = new List<Vec2>();

        [SerializeField] private Color lightColor = new Color(1f, 0.85f, 0.55f, 0.12f);

        private bool following;
        private Vector3 home;
        private SpriteRenderer halo;
        private float phase;

        /// <summary>Positions of all lantern spirits (for ghost platforms).</summary>
        public static IReadOnlyList<Vec2> Lights
        {
            get
            {
                lights.Clear();
                foreach (LanternSpirit s in active) lights.Add(new Vec2(s.transform.position.x, s.transform.position.y));
                return lights;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            active.Clear();
            lights.Clear();
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            home = transform.position;
            phase = transform.position.x;
        }

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        private void Start()
        {
            float r = SpiritElementsTuning.Current.lanternSpirit.lightRadius;
            halo = SteepingSpirits.Core.PlaceholderSprites.CreateSpriteObject("Light",
                SteepingSpirits.Core.PlaceholderSprites.Circle, lightColor, transform.position, Vector2.one * r * 2f, -2, transform);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!following && other.GetComponentInParent<JumpNRunPlayer>() != null)
            {
                following = true;
                JumpNRunSounds.Play(JumpNRunSound.Lantern, 0.14f);
            }
        }

        private void Update()
        {
            LanternSpiritParams p = SpiritElementsTuning.Current.lanternSpirit;
            float bob = Mathf.Sin(Time.time * 1.7f + phase) * 0.12f;
            Vector3 target = home;
            JumpNRunPlayer player = JumpNRunPlayer.Instance;
            if (following && player != null)
            {
                target = (Vector3)player.Feet + new Vector3(p.followOffsetX * player.Facing, p.followOffsetY, 0f);
            }

            Vector3 pos = transform.position;
            pos.x = PMath.Damp(pos.x, target.x, p.followRate, Time.deltaTime);
            pos.y = PMath.Damp(pos.y, target.y + bob, p.followRate, Time.deltaTime);
            transform.position = pos;

            if (halo != null)
            {
                halo.transform.localScale = Vector3.one * p.lightRadius * 2f;
            }
        }

        /// <summary>A catch brings the spirit back to the player (it stays a companion).</summary>
        public static void GatherAll(Vector2 near)
        {
            foreach (LanternSpirit s in active)
            {
                if (s.following)
                {
                    s.transform.position = near + Vector2.up * 1.5f;
                }
            }
        }
    }
}
