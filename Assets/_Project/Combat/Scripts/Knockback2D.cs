using UnityEngine;

namespace SteepingSpirits.Combat
{
    /// <summary>
    /// Rückstoss bei Treffern (Zelda-Gefühl): hört auf Health.Damaged, schubst
    /// den Rigidbody2D in Trefferrichtung und „betäubt" kurz. Solange
    /// <see cref="IsStunned"/> true ist, lassen Spieler- und Gegner-Steuerung
    /// die Geschwindigkeit in Ruhe.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class Knockback2D : MonoBehaviour
    {
        [Tooltip("Faktor auf den eingehenden Rückstoss (0 = immun, z.B. schwere Gegner)")]
        [SerializeField] private float strengthMultiplier = 1f;

        [Tooltip("So lange keine eigene Bewegung nach einem Treffer")]
        [SerializeField] private float stunSeconds = 0.18f;

        [Tooltip("Wie schnell der Schwung abgebremst wird (Einheiten/s²)")]
        [SerializeField] private float deceleration = 40f;

        private Rigidbody2D body;
        private Health health;
        private float stunnedUntil;

        public bool IsStunned => Time.time < stunnedUntil;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= HandleDamaged;
        }

        private void HandleDamaged(float amount, Vector2 knockback)
        {
            Vector2 push = knockback * strengthMultiplier;
            if (push.sqrMagnitude < 0.0001f)
            {
                return;
            }

            body.linearVelocity = push;
            stunnedUntil = Time.time + stunSeconds;
        }

        private void FixedUpdate()
        {
            if (IsStunned)
            {
                body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            }
        }
    }
}
