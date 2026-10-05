using System;
using System.Collections.Generic;

namespace SteepingSpirits.Platforming.Core
{
    /// <summary>
    /// Lantern checkpoints and the "safe ground" memory used by the
    /// fall-protection assist. Lanterns are identified by their index in the
    /// level; lighting an earlier lantern again never moves the respawn back.
    /// </summary>
    public sealed class CheckpointTracker
    {
        private readonly HashSet<int> lit = new HashSet<int>();

        public Vec2 StartPoint { get; private set; }

        /// <summary>Respawn point of the furthest lit lantern (or the start).</summary>
        public Vec2 LanternPoint { get; private set; }
        public int CurrentLantern { get; private set; } = -1;

        /// <summary>Last position where the player stood still enough on solid ground.</summary>
        public Vec2 SafePoint { get; private set; }

        /// <summary>Seconds the player must stand on ground before it counts as safe.</summary>
        public float SafeDelay = 0.25f;

        private float groundedTime;

        public CheckpointTracker(Vec2 start)
        {
            StartPoint = start;
            LanternPoint = start;
            SafePoint = start;
        }

        public bool IsLit(int lantern) => lit.Contains(lantern);
        public int LitCount => lit.Count;

        /// <summary>Lights a lantern. Returns true if it was not lit before.</summary>
        public bool Light(int lantern, Vec2 respawnPoint)
        {
            if (!lit.Add(lantern))
            {
                return false;
            }

            // Only move forward: the furthest lantern (highest index) wins.
            if (lantern > CurrentLantern)
            {
                CurrentLantern = lantern;
                LanternPoint = respawnPoint;
            }

            return true;
        }

        /// <summary>
        /// Called every physics step. safeSurface = standing on static ground
        /// (not on a sinking leaf, ghost platform or swing).
        /// </summary>
        public void TrackGround(float dt, bool grounded, bool safeSurface, Vec2 feet)
        {
            if (!grounded || !safeSurface)
            {
                groundedTime = 0f;
                return;
            }

            groundedTime += dt;
            if (groundedTime >= SafeDelay)
            {
                SafePoint = feet;
            }
        }

        public Vec2 RespawnPoint(bool fallProtection) => fallProtection ? SafePoint : LanternPoint;

        /// <summary>After a respawn the safe point is where the player is placed.</summary>
        public void OnRespawned(Vec2 point)
        {
            SafePoint = point;
            groundedTime = 0f;
        }
    }

    public enum CatchPhase
    {
        Idle,
        FadeOut,
        Hold,
        FadeIn
    }

    /// <summary>
    /// The gentle catch when the player falls: fade to a soft dark, move the
    /// player while hidden, fade back. Runs on unscaled time.
    /// </summary>
    public sealed class CatchSequence
    {
        public FeedbackParams Params;
        public CatchPhase Phase { get; private set; } = CatchPhase.Idle;
        public float Darkness { get; private set; }
        public bool Active => Phase != CatchPhase.Idle;

        private float timer;

        public CatchSequence(FeedbackParams parameters)
        {
            Params = parameters ?? new FeedbackParams();
        }

        public bool Begin()
        {
            if (Active)
            {
                return false;
            }

            Phase = CatchPhase.FadeOut;
            timer = 0f;
            return true;
        }

        /// <summary>Advances the sequence. Returns true exactly once: when the player should be moved.</summary>
        public bool Tick(float unscaledDt)
        {
            bool moveNow = false;
            timer += unscaledDt;
            switch (Phase)
            {
                case CatchPhase.FadeOut:
                    Darkness = Smooth(timer / Math.Max(0.001f, Params.catchFadeOut));
                    if (timer >= Params.catchFadeOut)
                    {
                        Phase = CatchPhase.Hold;
                        timer = 0f;
                        Darkness = 1f;
                        moveNow = true;
                    }

                    break;
                case CatchPhase.Hold:
                    if (timer >= Params.catchHold)
                    {
                        Phase = CatchPhase.FadeIn;
                        timer = 0f;
                    }

                    break;
                case CatchPhase.FadeIn:
                    Darkness = 1f - Smooth(timer / Math.Max(0.001f, Params.catchFadeIn));
                    if (timer >= Params.catchFadeIn)
                    {
                        Phase = CatchPhase.Idle;
                        Darkness = 0f;
                    }

                    break;
            }

            return moveNow;
        }

        public float TotalSeconds => Params.catchFadeOut + Params.catchHold + Params.catchFadeIn;

        private static float Smooth(float t)
        {
            t = PMath.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
