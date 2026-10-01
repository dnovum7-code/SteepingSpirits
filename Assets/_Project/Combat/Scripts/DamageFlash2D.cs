using System.Collections;
using UnityEngine;

namespace SteepingSpirits.Combat
{
    /// <summary>
    /// Treffer-Feedback: Sprite blitzt kurz rot auf und flackert, solange die
    /// Health unverwundbar ist (klassisches Zelda-Blinken).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private float flashSeconds = 0.12f;
        [SerializeField] private float blinkInterval = 0.08f;

        private Health health;
        private Color baseColor;
        private Coroutine routine;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= HandleDamaged;
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = baseColor;
                }
            }
        }

        private void HandleDamaged(float amount, Vector2 knockback)
        {
            if (spriteRenderer == null || !isActiveAndEnabled)
            {
                return;
            }

            if (routine == null)
            {
                baseColor = spriteRenderer.color;
            }
            else
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashSeconds);
            spriteRenderer.color = baseColor;

            // Flackern während der Unverwundbarkeit.
            bool visible = true;
            while (health != null && health.IsInvulnerable && !health.IsDead)
            {
                visible = !visible;
                Color c = baseColor;
                c.a = visible ? baseColor.a : baseColor.a * 0.25f;
                spriteRenderer.color = c;
                yield return new WaitForSeconds(blinkInterval);
            }

            spriteRenderer.color = baseColor;
            routine = null;
        }
    }
}
