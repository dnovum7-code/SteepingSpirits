using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>A big floating leaf: sinks slowly while the player stands on it and drifts back up afterwards.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class LeafPlatform : MonoBehaviour
    {
        private Rigidbody2D body;
        private BoxCollider2D box;
        private LeafSpring spring;
        private float restY;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            box = GetComponent<BoxCollider2D>();
            restY = body.position.y;
            spring = new LeafSpring(SpiritElementsTuning.Current.leaf);
        }

        private void FixedUpdate()
        {
            spring.Params = SpiritElementsTuning.Current.leaf;
            JumpNRunPlayer player = JumpNRunPlayer.Instance;
            bool loaded = player != null && player.Ground == box;
            float v = spring.Step(Time.fixedDeltaTime, loaded);

            // Kinematic velocity moves the leaf and lets the player ride along;
            // a gentle correction keeps it from drifting away from the spring state.
            float drift = restY + spring.Offset - body.position.y;
            body.linearVelocity = new Vector2(0f, v + drift * 5f);
        }
    }
}
