using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A platform from the spirit world: only a faint outline until a lantern
    /// spirit's light touches it, then it becomes solid. It never becomes
    /// solid while the player is inside it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class GhostPlatform : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(0.72f, 0.80f, 1f, 1f);
        [SerializeField] private float hiddenAlpha = 0.12f;

        private BoxCollider2D box;
        private PlaceholderVisual visual;
        private float visibility;

        public bool IsSolid => box.enabled;

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            visual = GetComponent<PlaceholderVisual>();
            box.enabled = false;
        }

        private void FixedUpdate()
        {
            SpiritElementParams p = SpiritElementsTuning.Current;
            Bounds b = box.bounds;
            bool lit = SpiritMath.IsLit(LanternSpirit.Lights, p.lanternSpirit.lightRadius, b.min.x, b.min.y, b.max.x, b.max.y);

            if (lit && !box.enabled)
            {
                JumpNRunPlayer player = JumpNRunPlayer.Instance;
                Collider2D pc = player != null ? player.GetComponent<Collider2D>() : null;
                if (pc == null || !pc.bounds.Intersects(b))
                {
                    box.enabled = true;
                }
            }
            else if (!lit && box.enabled)
            {
                box.enabled = false;
            }

            visibility = PMath.Damp(visibility, box.enabled ? 1f : 0f, p.lanternSpirit.fadeRate, Time.fixedDeltaTime);
        }

        [SerializeField] private Color highContrastColor = new Color(1f, 0.95f, 0.4f, 1f);

        private void Update()
        {
            if (visual == null || visual.Renderer == null)
            {
                return;
            }

            // Comfort option "hoher Kontrast": bright outline colour, clearly visible even when unlit.
            bool contrast = JumpNRunOptions.Instance != null && JumpNRunOptions.Instance.Options.highContrast;
            Color c = contrast ? highContrastColor : color;
            float hidden = contrast ? 0.45f : hiddenAlpha;
            visual.Renderer.color = new Color(c.r, c.g, c.b, Mathf.Lerp(hidden, contrast ? 1f : 0.9f, visibility));
        }
    }
}
