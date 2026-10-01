using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Platformer
{
    /// <summary>
    /// Liane: eine echte Physik-Kette aus Segmenten (Rigidbody2D + HingeJoint2D),
    /// oben fest verankert. Der Spieler greift ein Segment, sein Gewicht zieht
    /// die Liane, links/rechts schaukelt sie auf, hoch/runter klettert er.
    /// Beim Loslassen fliegt er mit der Geschwindigkeit des Segments weiter.
    ///
    /// Einfach ein leeres GameObject an den Aufhängepunkt legen und diese
    /// Komponente drauf – die Segmente werden beim Start erzeugt.
    /// </summary>
    public class Vine : MonoBehaviour
    {
        [SerializeField] private int segmentCount = 12;
        [SerializeField] private float segmentLength = 0.6f;
        [SerializeField] private float segmentMass = 0.25f;
        [Tooltip("Schwerkraft-Faktor der Segmente (Physics2D.gravity × Wert)")]
        [SerializeField] private float gravityScale = 2.5f;
        [SerializeField] private Color color = new Color(0.3f, 0.62f, 0.25f);

        [Tooltip("Die obersten N Segmente sind nicht greifbar (zu nah am Ast)")]
        [SerializeField] private int ungrabbableTop = 2;

        private readonly List<VineSegment> segments = new List<VineSegment>();

        public IReadOnlyList<VineSegment> Segments => segments;
        public int FirstGrabbableIndex => Mathf.Clamp(ungrabbableTop, 0, Mathf.Max(0, segments.Count - 1));

        /// <summary>Konfiguration aus Code – vor Start() aufrufen.</summary>
        public void Configure(int segmentCount, float segmentLength)
        {
            this.segmentCount = Mathf.Max(2, segmentCount);
            this.segmentLength = Mathf.Max(0.1f, segmentLength);
        }

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            // Unbewegter Anker (statischer Körper) am Aufhängepunkt.
            Rigidbody2D anchor = gameObject.GetComponent<Rigidbody2D>();
            if (anchor == null)
            {
                anchor = gameObject.AddComponent<Rigidbody2D>();
            }
            anchor.bodyType = RigidbodyType2D.Static;

            PlaceholderSprites.CreateSpriteObject("Ast", PlaceholderSprites.Circle, new Color(0.4f, 0.27f, 0.15f),
                transform.position, new Vector2(0.5f, 0.5f), 2, transform);

            Rigidbody2D previous = anchor;
            Vector2 top = transform.position;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 center = top + Vector2.down * segmentLength * (i + 0.5f);
                SpriteRenderer sr = PlaceholderSprites.CreateSpriteObject("Liane_" + i, PlaceholderSprites.Square,
                    i % 2 == 0 ? color : color * 0.9f, center, new Vector2(0.16f, segmentLength), 1, transform);
                sr.transform.position = center; // Weltposition (Eltern-Skalierung egal)

                var body = sr.gameObject.AddComponent<Rigidbody2D>();
                body.mass = segmentMass;
                body.gravityScale = gravityScale;
                body.linearDamping = 0.15f;
                body.angularDamping = 0.6f;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                // Trigger-Collider nur für eine sinnvolle Massenverteilung – kollidiert mit nichts.
                var col = sr.gameObject.AddComponent<CapsuleCollider2D>();
                col.isTrigger = true;

                var hinge = sr.gameObject.AddComponent<HingeJoint2D>();
                hinge.connectedBody = previous;
                hinge.autoConfigureConnectedAnchor = false;
                hinge.anchor = new Vector2(0f, 0.5f); // obere Kante (lokal, Sprite = 1 Einheit hoch)
                hinge.connectedAnchor = previous == anchor ? Vector2.zero : new Vector2(0f, -0.5f);

                VineSegment segment = sr.gameObject.AddComponent<VineSegment>();
                segment.Init(this, i, body);
                segments.Add(segment);
                previous = body;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * segmentCount * segmentLength);
        }
    }
}
