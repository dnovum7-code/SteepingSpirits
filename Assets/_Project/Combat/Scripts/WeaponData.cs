using UnityEngine;

namespace SteepingSpirits.Combat
{
    public enum WeaponCategory
    {
        Sword,
        Dagger,
        Axe,
        Spear,
        Hammer
    }

    /// <summary>
    /// Waffen-Bauplan (aus Everdawn übernommen, auf 2D-Top-Down umgestellt):
    /// statt 3D-Reichweite/Animations-States gibt es Reichweite vor dem
    /// Spieler, Trefferradius und Rückstoss.
    ///
    /// Treffer-Kreis: Mittelpunkt = Spieler + Blickrichtung × reach, Radius = hitRadius.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "SteepingSpirits/Combat/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identität")]
        [SerializeField] private string displayName = "Holzschwert";
        [SerializeField] private WeaponCategory category = WeaponCategory.Sword;
        [SerializeField] private Sprite icon;

        [Header("Kampf")]
        [Min(0f)] [SerializeField] private float damage = 1f;

        [Tooltip("Abstand des Trefferkreises vor dem Spieler (Einheiten)")]
        [Min(0f)] [SerializeField] private float reach = 0.75f;

        [Tooltip("Radius des Trefferkreises (Einheiten)")]
        [Min(0.05f)] [SerializeField] private float hitRadius = 0.6f;

        [Tooltip("Rückstoss auf getroffene Ziele (Einheiten/s)")]
        [Min(0f)] [SerializeField] private float knockback = 7f;

        [Header("Timing")]
        [Min(0f)] [SerializeField] private float hitDelay = 0.06f;
        [Min(0.01f)] [SerializeField] private float attackDuration = 0.28f;

        [Header("Animation (optional)")]
        [Tooltip("Trigger-Parameter im Animator des Spielers (leer = keiner)")]
        [SerializeField] private string attackTrigger = "Attack";

        public string DisplayName => displayName;
        public WeaponCategory Category => category;
        public Sprite Icon => icon;
        public float Damage => damage;
        public float Reach => reach;
        public float HitRadius => hitRadius;
        public float Knockback => knockback;
        public float HitDelay => hitDelay;
        public float AttackDuration => attackDuration;
        public string AttackTrigger => attackTrigger;

        /// <summary>Erzeugt eine Waffe zur Laufzeit (ohne Asset) – z.B. Standard-Schwert.</summary>
        public static WeaponData CreateRuntime(string name, float damage, float reach = 0.75f, float hitRadius = 0.6f,
            float knockback = 7f, float hitDelay = 0.06f, float attackDuration = 0.28f)
        {
            var w = CreateInstance<WeaponData>();
            w.name = name;
            w.displayName = name;
            w.damage = damage;
            w.reach = reach;
            w.hitRadius = hitRadius;
            w.knockback = knockback;
            w.hitDelay = hitDelay;
            w.attackDuration = attackDuration;
            w.OnValidate();
            return w;
        }

        private void OnValidate()
        {
            attackDuration = Mathf.Max(attackDuration, 0.01f);
            hitDelay = Mathf.Clamp(hitDelay, 0f, attackDuration);
        }
    }
}
