using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>One particle to spawn. Pure data, so any backend can consume it.</summary>
    public struct JumpNRunParticle
    {
        public Vector2 position;
        public Vector2 velocity;
        public float size;
        public Color color;
        public float lifetime;

        /// <summary>Downward acceleration (leaves drift slowly, dust barely falls).</summary>
        public float gravity;

        /// <summary>Spin in degrees per second (leaves).</summary>
        public float spin;

        /// <summary>0 = circle, 1 = leaf (diamond).</summary>
        public int shape;
    }

    /// <summary>
    /// The narrow seam between feedback code and the particle backend. The
    /// placeholder is <see cref="JumpNRunParticles"/> (pooled sprites); a
    /// ParticleSystem backend only has to implement these two members.
    /// </summary>
    public interface IJumpNRunParticles
    {
        void Emit(in JumpNRunParticle particle);
        void Clear();
    }
}
