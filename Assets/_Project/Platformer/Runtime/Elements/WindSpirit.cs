using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A wind spirit breathes a column of rising air. Inside the column the
    /// player is lifted (also out of a fall); the push fades near the top.
    /// The collider covers the column; its bottom is the spirit itself.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WindSpirit : MonoBehaviour
    {
        public float columnHeight = 6f;
        [SerializeField] private Color wispColor = new Color(0.85f, 0.95f, 1f, 0.35f);

        private BoxCollider2D column;
        private float wispTimer;
        private bool playerInside;

        private float Bottom => transform.position.y - 0.5f;

        private void Awake()
        {
            column = GetComponent<BoxCollider2D>();
            column.isTrigger = true;
        }

        /// <summary>Called by the builder: column above the spirit tile.</summary>
        public void Configure(float height, float width)
        {
            columnHeight = height;
            var box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(width, height);
            box.offset = new Vector2(0f, height * 0.5f - 0.5f);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            JumpNRunPlayer player = other.GetComponentInParent<JumpNRunPlayer>();
            if (player == null || player.IsControlled)
            {
                return;
            }

            WindParams p = SpiritElementsTuning.Current.wind;
            float a = SpiritMath.Updraft(p, columnHeight, player.Feet.y - Bottom, player.Velocity.y);
            player.AddAcceleration(new Vector2(0f, a));

            if (!playerInside)
            {
                playerInside = true;
                JumpNRunSounds.Play(JumpNRunSound.Wind, 0.12f);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<JumpNRunPlayer>() != null)
            {
                playerInside = false;
            }
        }

        private void Update()
        {
            wispTimer -= Time.deltaTime;
            if (wispTimer > 0f)
            {
                return;
            }

            wispTimer = 0.18f;
            float w = column.size.x * 0.4f;
            var pos = new Vector2(transform.position.x + Random.Range(-w, w), Bottom + Random.Range(0f, 1f));
            JumpNRunParticles.Burst(pos, 1, Vector2.up, 5f, 4.5f, wispColor, 0.12f, columnHeight / 4.5f, -0.2f);
        }
    }
}
