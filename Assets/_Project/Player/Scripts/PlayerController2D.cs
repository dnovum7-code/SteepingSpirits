using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Core;

namespace SteepingSpirits.Player
{
    /// <summary>
    /// Top-Down-Steuerung (Zelda/Stardew): WASD/Pfeile/Stick, 4-Wege-Blickrichtung,
    /// physikbasiert über Rigidbody2D (Kollision mit Wänden, Gegnern, NPCs).
    ///
    /// Ersetzt den FirstPersonController aus Everdawn. Optionaler Animator:
    /// existieren die Float-Parameter „MoveX", „MoveY" und „Speed", werden sie
    /// gesetzt (Blend-Tree für 4 Richtungen) – fehlen sie, passiert nichts.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Bewegung")]
        [SerializeField] private float moveSpeed = 4.5f;

        [Tooltip("Beschleunigung (Einheiten/s²). Hoch = knackig wie Zelda")]
        [SerializeField] private float acceleration = 60f;

        [Tooltip("Blickrichtung nur in 4 Richtungen (oben/unten/links/rechts)")]
        [SerializeField] private bool fourWayFacing = true;

        [Header("Darstellung (optional)")]
        [SerializeField] private Animator animator;

        [Tooltip("Ohne Animator: Sprite für »nach links« spiegeln")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool spriteFacesRight = true;

        private Rigidbody2D body;
        private Knockback2D knockback;
        private Health health;
        private Vector2 input;
        private bool hasMoveX, hasMoveY, hasSpeed;

        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveYId = Animator.StringToHash("MoveY");
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        /// <summary>Aktuelle Blickrichtung (Länge 1). Startet nach unten (Richtung Kamera).</summary>
        public Vector2 Facing { get; private set; } = Vector2.down;

        public bool IsMoving => input.sqrMagnitude > 0.01f;

        /// <summary>Grundtempo (Einheiten/s).</summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>Faktor aufs Tempo (Cheats, Items, Untergrund …).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Andere Systeme (Angriff, Zwischensequenz) können die Bewegung kurz sperren.</summary>
        public bool MovementLocked { get; set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            knockback = GetComponent<Knockback2D>();
            health = GetComponent<Health>();

            // Top-Down: keine Schwerkraft, nicht umkippen, weiche Darstellung.
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CacheAnimatorParameters();
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type != AnimatorControllerParameterType.Float) continue;
                if (p.nameHash == MoveXId) hasMoveX = true;
                if (p.nameHash == MoveYId) hasMoveY = true;
                if (p.nameHash == SpeedId) hasSpeed = true;
            }
        }

        private void Update()
        {
            bool canAct = !GamePause.IsBlocked && !MovementLocked && (health == null || !health.IsDead);
            input = canAct ? GameInput.Move : Vector2.zero;

            if (IsMoving && !MovementLocked)
            {
                Facing = ToFacing(input);
            }

            UpdateVisuals();
        }

        private void FixedUpdate()
        {
            if (knockback != null && knockback.IsStunned)
            {
                return; // Rückstoss gehört gerade der Physik
            }

            Vector2 target = input * moveSpeed * SpeedMultiplier;
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, target, acceleration * Time.fixedDeltaTime);
        }

        /// <summary>Blick in eine Richtung drehen (z.B. zum NPC beim Ansprechen).</summary>
        public void FaceTowards(Vector2 worldPoint)
        {
            Vector2 dir = worldPoint - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Facing = ToFacing(dir);
                UpdateVisuals();
            }
        }

        /// <summary>Teleport (Respawn, Türen). Setzt auch die Geschwindigkeit zurück.</summary>
        public void Teleport(Vector2 position)
        {
            body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            body.linearVelocity = Vector2.zero;
        }

        private Vector2 ToFacing(Vector2 dir)
        {
            if (!fourWayFacing)
            {
                return dir.normalized;
            }

            // Dominante Achse; bei Gleichstand die bisherige Achse behalten
            // (verhindert Flackern beim Diagonal-Laufen).
            float ax = Mathf.Abs(dir.x);
            float ay = Mathf.Abs(dir.y);
            bool horizontal = Mathf.Approximately(ax, ay) ? Mathf.Abs(Facing.x) > 0.5f : ax > ay;
            return horizontal ? new Vector2(Mathf.Sign(dir.x), 0f) : new Vector2(0f, Mathf.Sign(dir.y));
        }

        private void UpdateVisuals()
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                if (hasMoveX) animator.SetFloat(MoveXId, Facing.x);
                if (hasMoveY) animator.SetFloat(MoveYId, Facing.y);
                if (hasSpeed) animator.SetFloat(SpeedId, input.magnitude);
            }
            else if (spriteRenderer != null && Mathf.Abs(Facing.x) > 0.01f)
            {
                bool left = Facing.x < 0f;
                spriteRenderer.flipX = spriteFacesRight ? left : !left;
            }
        }
    }
}
