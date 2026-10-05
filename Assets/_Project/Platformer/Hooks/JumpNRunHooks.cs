using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.Hooks
{
    /// <summary>One frame of player input (keyboard, pad, bot or a recording).</summary>
    public struct PlayerInputFrame
    {
        public Vector2 move;
        public bool jumpPressed;
        public bool jumpHeld;
        public bool dashPressed;
    }

    /// <summary>Anything that can steer the Jump'n'Run player instead of the real devices.</summary>
    public interface IPlayerInputSource
    {
        /// <summary>Called once per rendered frame by the player.</summary>
        PlayerInputFrame Read();
    }

    /// <summary>
    /// Read-only view on a running level plus a few test helpers. Implemented
    /// by the level (JumpNRunProbe) so that the smoke test, the input recorder
    /// and PlayMode tests do not need to reference game code directly.
    /// </summary>
    public interface IJumpNRunProbe
    {
        string LevelId { get; }

        /// <summary>The level text the scene was built from (for route planning).</summary>
        string LayoutText { get; }

        bool HasPlayer { get; }
        Vector2 PlayerFeet { get; }
        Vector2 PlayerVelocity { get; }
        bool Grounded { get; }
        bool Riding { get; }
        bool IsCatching { get; }
        int Catches { get; }
        int CurrentLantern { get; }
        bool Finished { get; }
        int BagTotal { get; }

        int LanternCount { get; }
        Vector2 LanternFeet(int index);
        int SwingCount { get; }
        Vector2 SwingSeat(int index);
        Vector2 SwingPivot(int index);

        /// <summary>The same observation the route bot gets in the core simulation.</summary>
        RouteObservation Observe();

        /// <summary>Places the player with the feet at a point and clears motion.</summary>
        void Teleport(Vector2 feet);
    }

    /// <summary>Static meeting point between the level and tools/tests.</summary>
    public static class JumpNRunHooks
    {
        /// <summary>When set, the player reads this instead of the keyboard/pad.</summary>
        public static IPlayerInputSource InputOverride;

        /// <summary>Set by the running level, cleared when it unloads.</summary>
        public static IJumpNRunProbe Probe;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            InputOverride = null;
            Probe = null;
        }
    }
}
