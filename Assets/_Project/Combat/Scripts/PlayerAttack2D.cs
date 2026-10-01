using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.Audio;
using SteepingSpirits.Player;

namespace SteepingSpirits.Combat
{
    /// <summary>
    /// Schwertschlag in Blickrichtung (Zelda-Stil). Ersetzt PlayerAttack +
    /// WalkSwordAnimation aus Everdawn.
    ///
    /// Ablauf: Taste → kurzer Hieb (Bewegung optional gesperrt) → nach
    /// hitDelay wird ein Trefferkreis vor dem Spieler geprüft → jede getroffene
    /// Health bekommt Schaden + Rückstoss (weg vom Spieler).
    ///
    /// Ohne WeaponData-Asset wird automatisch ein Holzschwert benutzt. Ohne
    /// Grafik wird ein Platzhalter-Hieb eingeblendet.
    /// </summary>
    [RequireComponent(typeof(PlayerController2D))]
    public class PlayerAttack2D : MonoBehaviour
    {
        [Header("Waffe")]
        [SerializeField] private WeaponData equippedWeapon;

        [Header("Treffer")]
        [SerializeField] private LayerMask hittableLayers = ~0;

        [Tooltip("Während des Hiebs stehen bleiben (Zelda/Stardew-Gefühl)")]
        [SerializeField] private bool lockMovementWhileAttacking = true;

        [Header("Darstellung")]
        [Tooltip("Optional eigener Hieb-Sprite (zeigt nach rechts). Leer = Platzhalter")]
        [SerializeField] private SpriteRenderer slashRenderer;
        [SerializeField] private Color slashColor = new Color(1f, 1f, 0.85f, 0.9f);
        [SerializeField] private Animator animator;

        [Header("Sound")]
        [SerializeField] private AudioClip swingClip;
        [SerializeField] private AudioClip hitClip;
        [Tooltip("Ohne eigene Clips: Code-Töne benutzen")]
        [SerializeField] private bool proceduralSfx = true;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;

        private PlayerController2D controller;
        private Health ownHealth;
        private AudioSource audioSource;
        private bool isAttacking;
        private readonly HashSet<Health> hitThisSwing = new HashSet<Health>();

        public WeaponData EquippedWeapon => equippedWeapon;
        public bool IsAttacking => isAttacking;

        /// <summary>Faktor auf den Waffenschaden (Cheats, Buffs).</summary>
        public float DamageMultiplier { get; set; } = 1f;

        private void Awake()
        {
            controller = GetComponent<PlayerController2D>();
            ownHealth = GetComponent<Health>();

            if (equippedWeapon == null)
            {
                equippedWeapon = WeaponData.CreateRuntime("Holzschwert", 1f);
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            EnsureSlashRenderer();
            EnsureAudio();
        }

        private void OnDisable()
        {
            // Abgebrochener Hieb (Objekt deaktiviert) darf nichts gesperrt hinterlassen.
            if (isAttacking)
            {
                isAttacking = false;
                controller.MovementLocked = false;
                if (slashRenderer != null)
                {
                    slashRenderer.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (GamePause.IsBlocked || (ownHealth != null && ownHealth.IsDead))
            {
                return;
            }

            if (GameInput.AttackPressed)
            {
                RequestAttack();
            }
        }

        public void EquipWeapon(WeaponData weapon)
        {
            if (isAttacking)
            {
                Debug.LogWarning("[PlayerAttack2D] Waffenwechsel während eines Angriffs nicht möglich.");
                return;
            }

            equippedWeapon = weapon;
        }

        /// <summary>Angriff auslösen (auch per Skript, z.B. Touch-Button).</summary>
        public void RequestAttack()
        {
            if (isAttacking || equippedWeapon == null)
            {
                return;
            }

            StartCoroutine(Attack(equippedWeapon));
        }

        private IEnumerator Attack(WeaponData weapon)
        {
            isAttacking = true;
            hitThisSwing.Clear();

            if (lockMovementWhileAttacking)
            {
                controller.MovementLocked = true;
            }

            if (animator != null && !string.IsNullOrEmpty(weapon.AttackTrigger) && HasTrigger(weapon.AttackTrigger))
            {
                animator.SetTrigger(weapon.AttackTrigger);
            }

            Vector2 facing = controller.Facing;
            ShowSlash(weapon, facing, true);
            Play(swingClip, proceduralSwing);

            yield return new WaitForSeconds(weapon.HitDelay);

            TryHit(weapon, facing);

            float rest = weapon.AttackDuration - weapon.HitDelay;
            if (rest > 0f)
            {
                yield return new WaitForSeconds(rest);
            }

            ShowSlash(weapon, facing, false);
            controller.MovementLocked = false;
            isAttacking = false;
        }

        private void TryHit(WeaponData weapon, Vector2 facing)
        {
            Vector2 origin = transform.position;
            Vector2 center = origin + facing * weapon.Reach;

            bool hitSomething = false;
            foreach (Collider2D col in Physics2D.OverlapCircleAll(center, weapon.HitRadius, hittableLayers))
            {
                if (col.transform.IsChildOf(transform))
                {
                    continue; // nicht sich selbst treffen
                }

                Health target = col.GetComponentInParent<Health>();
                if (target == null || target == ownHealth || target.IsDead || !hitThisSwing.Add(target))
                {
                    continue;
                }

                Vector2 away = (Vector2)target.transform.position - origin;
                away = away.sqrMagnitude > 0.0001f ? away.normalized : facing;
                target.TakeDamage(weapon.Damage * DamageMultiplier, away * weapon.Knockback);
                hitSomething = true;
            }

            if (hitSomething)
            {
                Play(hitClip, proceduralHit);
            }
        }

        // ---------------- Darstellung ----------------

        private void EnsureSlashRenderer()
        {
            if (slashRenderer != null)
            {
                slashRenderer.enabled = false;
                return;
            }

            var go = new GameObject("Slash");
            go.transform.SetParent(transform, false);
            slashRenderer = go.AddComponent<SpriteRenderer>();
            slashRenderer.sprite = PlaceholderSprites.Slash;
            slashRenderer.color = slashColor;
            slashRenderer.sortingOrder = 50;
            slashRenderer.enabled = false;
        }

        private void ShowSlash(WeaponData weapon, Vector2 facing, bool on)
        {
            if (slashRenderer == null)
            {
                return;
            }

            slashRenderer.enabled = on;
            if (!on)
            {
                return;
            }

            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            Transform t = slashRenderer.transform;
            t.localPosition = facing * weapon.Reach;
            t.localRotation = Quaternion.Euler(0f, 0f, angle);
            float size = weapon.HitRadius * 2f;
            t.localScale = new Vector3(size, size, 1f);
        }

        private bool HasTrigger(string trigger)
        {
            if (animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == trigger)
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------- Sound ----------------

        private AudioClip proceduralSwing;
        private AudioClip proceduralHit;

        private void EnsureAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (proceduralSfx)
            {
                // Code-Töne einmal erzeugen – greifen nur, wenn kein eigener Clip gesetzt ist.
                proceduralSwing = ProceduralSfx.Melody("sfx_swing", new[] { 520f, 340f }, 0.08f, 0.35f);
                proceduralHit = ProceduralSfx.Tone("sfx_hit", 160f, 0.09f, 0.5f);
            }
        }

        private void Play(AudioClip custom, AudioClip fallback)
        {
            AudioClip clip = custom != null ? custom : fallback;
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private void OnDrawGizmosSelected()
        {
            WeaponData w = equippedWeapon;
            float reach = w != null ? w.Reach : 0.75f;
            float radius = w != null ? w.HitRadius : 0.6f;
            Vector2 facing = Application.isPlaying && controller != null ? controller.Facing : Vector2.down;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere((Vector2)transform.position + facing * reach, radius);
        }
    }
}
