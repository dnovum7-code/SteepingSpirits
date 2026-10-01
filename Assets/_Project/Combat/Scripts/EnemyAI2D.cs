using System.Collections;
using UnityEngine;
using SteepingSpirits.Core;

namespace SteepingSpirits.Combat
{
    /// <summary>
    /// Einfache Top-Down-Gegner-KI (ersetzt SimpleEnemyAI aus Everdawn):
    ///  - Umherstreifen um den Startpunkt,
    ///  - Spieler in Sichtweite → verfolgen,
    ///  - in Angriffsreichweite → kurz ausholen, dann Treffer (mit Rückstoss),
    ///  - optional Kontaktschaden (Zelda: Berührung tut weh).
    /// Steht still, solange ein Dialog/Menü offen ist (GamePause) oder er
    /// gerade zurückgestossen wird (Knockback2D).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyAI2D : MonoBehaviour
    {
        [Header("Ziel")]
        [SerializeField] private Transform target;

        [Header("Wahrnehmung")]
        [SerializeField] private float detectionRange = 4.5f;
        [Tooltip("Ab dieser Distanz gibt der Gegner die Verfolgung auf")]
        [SerializeField] private float loseRange = 7f;

        [Header("Bewegung")]
        [SerializeField] private float chaseSpeed = 1.9f;
        [SerializeField] private float wanderSpeed = 0.8f;
        [SerializeField] private float wanderRadius = 2f;
        [SerializeField] private float acceleration = 20f;

        [Header("Angriff")]
        [SerializeField] private float attackRange = 0.8f;
        [SerializeField] private float attackDamage = 1f;
        [SerializeField] private float attackKnockback = 6f;
        [SerializeField] private float attackWindup = 0.35f;
        [SerializeField] private float attackCooldown = 1.2f;

        [Tooltip("Berührung verursacht Schaden (nutzt die i-Frames des Spielers)")]
        [SerializeField] private bool contactDamage = true;

        private Rigidbody2D body;
        private Health ownHealth;
        private Health targetHealth;
        private Knockback2D knockback;
        private Vector2 home;
        private Vector2 wanderGoal;
        private float nextWanderPick;
        private bool chasing;
        private bool isAttacking;
        private float nextAttackTime;
        private Vector2 desiredVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownHealth = GetComponent<Health>();
            knockback = GetComponent<Knockback2D>();

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Start()
        {
            home = transform.position;
            wanderGoal = home;

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target != null)
            {
                targetHealth = target.GetComponent<Health>();
            }
        }

        /// <summary>Konfiguration aus Code (z.B. Test-Szene).</summary>
        public void Configure(float chaseSpeed, float detectionRange, float attackDamage)
        {
            this.chaseSpeed = chaseSpeed;
            this.detectionRange = detectionRange;
            this.loseRange = Mathf.Max(loseRange, detectionRange + 2f);
            this.attackDamage = attackDamage;
        }

        private void Update()
        {
            desiredVelocity = Vector2.zero;

            if (ownHealth.IsDead || GamePause.IsBlocked || isAttacking)
            {
                return;
            }

            bool targetAlive = target != null && (targetHealth == null || !targetHealth.IsDead);
            float distance = targetAlive ? Vector2.Distance(transform.position, target.position) : float.MaxValue;

            chasing = targetAlive && (chasing ? distance <= loseRange : distance <= detectionRange);

            if (chasing)
            {
                if (distance > attackRange)
                {
                    desiredVelocity = ((Vector2)target.position - (Vector2)transform.position).normalized * chaseSpeed;
                }
                else if (Time.time >= nextAttackTime)
                {
                    StartCoroutine(Attack());
                }
            }
            else
            {
                Wander();
            }
        }

        private void Wander()
        {
            if (Time.time >= nextWanderPick)
            {
                nextWanderPick = Time.time + Random.Range(1.5f, 3.5f);
                // Manchmal einfach stehen bleiben (wirkt lebendiger).
                wanderGoal = Random.value < 0.35f ? (Vector2)transform.position : home + Random.insideUnitCircle * wanderRadius;
            }

            Vector2 to = wanderGoal - (Vector2)transform.position;
            if (to.sqrMagnitude > 0.04f)
            {
                desiredVelocity = to.normalized * wanderSpeed;
            }
        }

        private void FixedUpdate()
        {
            if (knockback != null && knockback.IsStunned)
            {
                return;
            }

            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
        }

        private IEnumerator Attack()
        {
            isAttacking = true;
            nextAttackTime = Time.time + attackCooldown;

            // Ausholen: kurz anhalten (sichtbares „Telegraphing").
            float until = Time.time + attackWindup;
            while (Time.time < until)
            {
                if (ownHealth.IsDead)
                {
                    isAttacking = false;
                    yield break;
                }

                yield return null;
            }

            TryDamageTarget(attackRange + 0.3f);
            isAttacking = false;
        }

        private void TryDamageTarget(float maxDistance)
        {
            // Kein Treffer, während ein Dialog/Menü offen ist (Ausholen lief evtl. weiter).
            if (target == null || targetHealth == null || targetHealth.IsDead || ownHealth.IsDead || GamePause.IsBlocked)
            {
                return;
            }

            Vector2 to = (Vector2)target.position - (Vector2)transform.position;
            if (to.magnitude > maxDistance)
            {
                return; // ausgewichen
            }

            Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.down;
            targetHealth.TakeDamage(attackDamage, dir * attackKnockback);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!contactDamage || ownHealth.IsDead || GamePause.IsBlocked || target == null)
            {
                return;
            }

            if (collision.transform == target || collision.transform.IsChildOf(target))
            {
                TryDamageTarget(float.MaxValue);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
