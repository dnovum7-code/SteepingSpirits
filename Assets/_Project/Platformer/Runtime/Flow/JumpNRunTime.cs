using UnityEngine;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// Owns Time.timeScale inside Jump'n'Run levels: the assist game speed
    /// times an optional short slow motion (measured in unscaled time).
    /// Restores normal speed when the level unloads.
    /// </summary>
    public class JumpNRunTime : MonoBehaviour
    {
        private static JumpNRunTime instance;

        private float baseSpeed = 1f;
        private float slowScale = 1f;
        private float slowLeft;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                Time.timeScale = 1f;
            }
        }

        public static void SetBaseSpeed(float speed)
        {
            if (instance != null)
            {
                instance.baseSpeed = Mathf.Clamp(speed, 0.1f, 1f);
                instance.Apply();
            }
        }

        /// <summary>Short slow motion; scale 1 or seconds 0 does nothing.</summary>
        public static void SlowMotion(float scale, float unscaledSeconds)
        {
            if (instance == null || scale >= 0.999f || unscaledSeconds <= 0f)
            {
                return;
            }

            instance.slowScale = Mathf.Clamp(scale, 0.1f, 1f);
            instance.slowLeft = unscaledSeconds;
            instance.Apply();
        }

        private void Update()
        {
            if (slowLeft <= 0f)
            {
                return;
            }

            slowLeft -= Time.unscaledDeltaTime;
            if (slowLeft <= 0f)
            {
                slowScale = 1f;
            }

            Apply();
        }

        private void Apply()
        {
            // Ease back in during the last 40 % of the slow motion.
            float s = slowScale;
            if (slowLeft > 0f && slowScale < 1f)
            {
                s = Mathf.Lerp(1f, slowScale, Mathf.Clamp01(slowLeft / 0.15f));
            }

            Time.timeScale = baseSpeed * s;
        }
    }
}
