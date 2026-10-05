using UnityEngine;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>One particle to spawn. Pure data, so any backend can consume it.</summary>
    public struct ParticleSpawn
    {
        public Vector2 position;
        public Vector2 velocity;
        public float size;
        public Color color;
        public float lifetime;

        /// <summary>Size multiplier reached at the end of the lifetime (steam grows, bubbles don't).</summary>
        public float endSizeFactor;
    }

    /// <summary>
    /// The narrow seam between brewing views and the particle backend. The
    /// prototype uses <see cref="SpriteParticleEmitter"/>; a ParticleSystem or
    /// VFX Graph backend only has to implement these two members.
    /// </summary>
    public interface IBrewParticles
    {
        void Emit(in ParticleSpawn spawn);
        void Clear();
    }
}
