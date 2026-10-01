using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// Punkt, an dem sich der Spieler (PlatformerController2D) mit der
    /// Greif-Taste festhalten kann:
    ///  - <b>Hold</b>: festhalten und still hängen, dann abspringen oder dashen.
    ///  - <b>Swing</b>: an einem Seil schwingen. Links/rechts im Takt der
    ///    Bewegung drücken baut immer mehr Schwung auf; Loslassen/Springen
    ///    behält den Schwung.
    ///
    /// Kein Collider nötig – der Spieler sucht Punkte über die statische Liste.
    /// </summary>
    public class GrabPoint : MonoBehaviour
    {
        public enum Mode
        {
            Hold,
            Swing
        }

        private static readonly List<GrabPoint> all = new List<GrabPoint>();

        /// <summary>Alle aktiven Greifpunkte der Szene.</summary>
        public static IReadOnlyList<GrabPoint> All => all;

        [SerializeField] private Mode mode = Mode.Hold;

        [Tooltip("So nah muss der Spieler sein, um zuzugreifen (Einheiten)")]
        [SerializeField] private float catchRadius = 1.1f;

        [Header("Nur Swing")]
        [Tooltip("Maximale Seillänge. Greift man näher zu, wird das Seil kürzer (min. Min Rope Length)")]
        [SerializeField] private float ropeLength = 3.5f;
        [SerializeField] private float minRopeLength = 1.5f;

        public Mode GrabMode => mode;
        public float CatchRadius => catchRadius;
        public float RopeLength => ropeLength;
        public float MinRopeLength => minRopeLength;
        public Vector2 Position => transform.position;

        /// <summary>Konfiguration aus Code (z.B. Test-Parcours).</summary>
        public void Configure(Mode mode, float catchRadius, float ropeLength = 3.5f)
        {
            this.mode = mode;
            this.catchRadius = catchRadius;
            this.ropeLength = ropeLength;
            minRopeLength = Mathf.Min(minRopeLength, ropeLength);
        }

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = mode == Mode.Hold ? new Color(1f, 0.85f, 0.3f, 0.6f) : new Color(0.4f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, catchRadius);
            if (mode == Mode.Swing)
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * ropeLength);
            }
        }
    }
}
