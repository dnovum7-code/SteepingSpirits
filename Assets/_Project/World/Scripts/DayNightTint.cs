using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.World
{
    /// <summary>
    /// Tag/Nacht-Stimmung für 2D: ein bildschirmfüllendes, halbtransparentes
    /// Farb-Overlay, dessen Farbe der GameClock folgt – morgens warm-hell,
    /// mittags klar, abends orange (Abendrot), nachts dunkelblau.
    /// (Das Morgen-/Abendrot-Gefühl aus Everdawns DayNightCycle, ohne Skybox.)
    ///
    /// Funktioniert mit jeder Render-Pipeline. Wer später URP-2D-Lichter nutzt,
    /// kann stattdessen das Global Light2D einfärben.
    /// </summary>
    public class DayNightTint : MonoBehaviour
    {
        [System.Serializable]
        public struct TintKey
        {
            [Tooltip("Uhrzeit in Stunden (über 24 = nach Mitternacht)")]
            public float hour;
            public Color color;

            public TintKey(float hour, Color color)
            {
                this.hour = hour;
                this.color = color;
            }
        }

        [Tooltip("Farbe + Deckkraft über den Tag (aufsteigend nach Uhrzeit)")]
        [SerializeField] private TintKey[] keys =
        {
            new TintKey(6f,  new Color(1.00f, 0.80f, 0.60f, 0.16f)), // Morgendämmerung
            new TintKey(8f,  new Color(1.00f, 0.95f, 0.85f, 0.00f)), // Tag
            new TintKey(16f, new Color(1.00f, 0.95f, 0.85f, 0.00f)),
            new TintKey(18f, new Color(1.00f, 0.55f, 0.25f, 0.18f)), // Abendrot
            new TintKey(20f, new Color(0.10f, 0.10f, 0.35f, 0.42f)), // Dämmerung
            new TintKey(22f, new Color(0.03f, 0.04f, 0.18f, 0.55f)), // Nacht
            new TintKey(26f, new Color(0.02f, 0.02f, 0.12f, 0.62f)),
        };

        [Tooltip("Gesamtstärke des Effekts (0 = aus)")]
        [Range(0f, 1f)] [SerializeField] private float intensity = 1f;

        private Image overlay;

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            // Unter allen anderen Canvases (Dialog 90, Questlog 100), über der Welt.
            Canvas canvas = UiFactory.CreateCanvas("DayNightOverlay", -50);
            canvas.transform.SetParent(transform, false);
            overlay = UiFactory.Panel("Tint", canvas.transform, new Color(0f, 0f, 0f, 0f));
            overlay.raycastTarget = false;
            UiFactory.FullStretch(overlay.rectTransform);
        }

        private void LateUpdate()
        {
            GameClock clock = GameClock.Instance;
            if (overlay == null)
            {
                return;
            }

            Color c = clock != null ? Evaluate(clock.TimeOfDayHours) : new Color(0f, 0f, 0f, 0f);
            c.a *= intensity;
            overlay.color = c;
        }

        /// <summary>Farbe zur Uhrzeit (linear zwischen den Keys).</summary>
        public Color Evaluate(float hour)
        {
            if (keys == null || keys.Length == 0)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            if (hour <= keys[0].hour)
            {
                return keys[0].color;
            }

            for (int i = 1; i < keys.Length; i++)
            {
                if (hour <= keys[i].hour)
                {
                    float t = Mathf.InverseLerp(keys[i - 1].hour, keys[i].hour, hour);
                    return Color.Lerp(keys[i - 1].color, keys[i].color, t);
                }
            }

            return keys[keys.Length - 1].color;
        }
    }
}
