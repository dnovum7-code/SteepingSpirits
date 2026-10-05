using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>A leaf holding a big dew drop: landing on it bounces the player up (higher while holding jump).</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DewLeaf : MonoBehaviour
    {
        private BoxCollider2D box;
        private float cooldown;
        private PlaceholderVisual visual;
        private float squish;

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            visual = GetComponent<PlaceholderVisual>();
        }

        private void FixedUpdate()
        {
            cooldown -= Time.fixedDeltaTime;
            JumpNRunPlayer player = JumpNRunPlayer.Instance;
            if (player == null || cooldown > 0f || player.Ground != box || player.IsControlled)
            {
                return;
            }

            DewParams p = SpiritElementsTuning.Current.dew;
            float vy = SpiritMath.DewBounceVelocity(p, player.Params.Gravity, player.JumpHeld);
            player.Launch(new Vector2(player.Velocity.x, vy));
            cooldown = p.cooldown;
            squish = 1f;

            var feedback = player.GetComponent<JumpNRunFeedback>();
            if (feedback != null) feedback.Stretch();
            JumpNRunSounds.Play(JumpNRunSound.Bounce, 0.2f);
            JumpNRunParticles.Burst(player.Feet, 6, Vector2.up, 60f, 2.5f, new Color(0.75f, 0.9f, 1f, 0.6f), 0.12f, 0.5f, 3f);
        }

        private void Update()
        {
            squish = Mathf.MoveTowards(squish, 0f, Time.deltaTime * 4f);
            if (visual != null && visual.Renderer != null)
            {
                Vector2 s = visual.size;
                visual.Renderer.transform.localScale = new Vector3(s.x * (1f + 0.15f * squish), s.y * (1f - 0.3f * squish), 1f);
            }
        }
    }
}
