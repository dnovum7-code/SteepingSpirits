using System;
using UnityEngine;

namespace SteepingSpirits.Combat
{
    /// <summary>
    /// Lebenspunkte für Spieler, Gegner und alles Zerstörbare (aus Everdawn
    /// übernommen, für 2D erweitert):
    ///  - Trefferrichtung/Rückstoss wird mitgeschickt (Knockback2D hört zu),
    ///  - kurze Unverwundbarkeit nach einem Treffer (Zelda-„i-Frames"),
    ///  - Wiederbeleben (Spieler) und Maximalwert ändern (Herzcontainer).
    ///
    /// Einheit: frei wählbar. Beim Spieler gilt 1 HP = ein halbes Herz
    /// (6 HP = 3 Herzen, siehe GameHUD).
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 6f;

        [SerializeField] private bool destroyOnDeath = true;

        [SerializeField] private float destroyDelay;

        [Tooltip("Sekunden ohne Schaden nach einem Treffer (0 = aus). Spieler: ~1s wie in Zelda")]
        [SerializeField] private float invulnerableSeconds;

        [Tooltip("Treffer in der Konsole loggen")]
        [SerializeField] private bool logDamage;

        private float invulnerableUntil;

        public float CurrentHealth { get; private set; }

        public float MaxHealth => maxHealth;

        public bool IsDead => CurrentHealth <= 0f;

        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>Schaden erhalten: Menge, Rückstoss-Vektor (Richtung × Stärke, kann 0 sein).</summary>
        public event Action<float, Vector2> Damaged;

        /// <summary>Geheilt: tatsächlich geheilte Menge.</summary>
        public event Action<float> Healed;

        public event Action Died;

        /// <summary>Nach Revive().</summary>
        public event Action Revived;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        /// <summary>Konfiguration aus Code (z.B. Test-Szene).</summary>
        public void Configure(float maxHealth, bool destroyOnDeath, float invulnerableSeconds = 0f, float destroyDelay = 0f)
        {
            this.maxHealth = Mathf.Max(0.01f, maxHealth);
            this.destroyOnDeath = destroyOnDeath;
            this.invulnerableSeconds = Mathf.Max(0f, invulnerableSeconds);
            this.destroyDelay = Mathf.Max(0f, destroyDelay);
            CurrentHealth = this.maxHealth;
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, Vector2.zero);
        }

        /// <summary>Schaden mit Rückstoss (knockback = Richtung × Stärke).</summary>
        public void TakeDamage(float amount, Vector2 knockback)
        {
            if (IsDead || amount <= 0f || IsInvulnerable)
            {
                return;
            }

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
            if (invulnerableSeconds > 0f)
            {
                invulnerableUntil = Time.time + invulnerableSeconds;
            }

            if (logDamage)
            {
                Debug.Log($"{gameObject.name} nimmt {amount} Schaden. Rest: {CurrentHealth}/{maxHealth}");
            }

            Damaged?.Invoke(amount, knockback);

            if (IsDead)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            float before = CurrentHealth;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
            if (CurrentHealth > before)
            {
                Healed?.Invoke(CurrentHealth - before);
            }
        }

        /// <summary>Ändert das Maximum (z.B. Herzcontainer). refill = gleich voll heilen.</summary>
        public void SetMaxHealth(float value, bool refill)
        {
            maxHealth = Mathf.Max(0.01f, value);
            CurrentHealth = refill ? maxHealth : Mathf.Min(CurrentHealth, maxHealth);
        }

        /// <summary>Holt einen Toten zurück (Spieler-Respawn). fraction = Anteil des Maximums.</summary>
        public void Revive(float fraction = 1f)
        {
            CurrentHealth = Mathf.Clamp(maxHealth * fraction, 0.01f, maxHealth);
            invulnerableUntil = Time.time + Mathf.Max(invulnerableSeconds, 1f);
            Revived?.Invoke();
        }

        private void Die()
        {
            if (logDamage)
            {
                Debug.Log($"{gameObject.name} ist besiegt.");
            }

            Died?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }
}
