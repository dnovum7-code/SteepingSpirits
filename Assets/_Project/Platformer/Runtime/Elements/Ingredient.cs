using UnityEngine;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>A collectible ingredient. Floats gently; touching it puts it into the bag.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Ingredient : MonoBehaviour
    {
        public string ingredientId = IngredientIds.TeaLeaf;
        public bool rare;

        /// <summary>Tile of the marker (rare finds are remembered per spot).</summary>
        public int spotX;
        public int spotY;

        private Vector3 basePosition;
        private float phase;
        private bool collected;
        private PlaceholderVisual visual;
        private SpriteRenderer halo;

        public static Color ColorOf(string id)
        {
            switch (IngredientIds.BaseOf(id))
            {
                case IngredientIds.TeaLeaf: return new Color(0.45f, 0.70f, 0.38f);
                case IngredientIds.Herb: return new Color(0.62f, 0.78f, 0.45f);
                case IngredientIds.Blossom: return new Color(0.95f, 0.66f, 0.78f);
                case IngredientIds.MorningDew: return new Color(0.70f, 0.88f, 0.98f);
                default: return new Color(0.52f, 0.72f, 0.95f);
            }
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            visual = GetComponent<PlaceholderVisual>();
            basePosition = transform.position;
            phase = (transform.position.x * 0.37f) % 6.28f;
        }

        private void Start()
        {
            // A rare spot that was already harvested grows the common variant on later visits.
            JumpNRunLevel level = JumpNRunLevel.Current;
            if (rare && level != null && JumpNRunSaveStore.Current.HasFoundRareAt(level.levelId, spotX, spotY))
            {
                rare = false;
                ingredientId = IngredientIds.BaseOf(ingredientId);
                if (visual != null && visual.Renderer != null)
                {
                    visual.Renderer.sprite = PlaceholderVisual.SpriteFor(PlaceholderVisual.Shape.Circle);
                    visual.Renderer.color = ColorOf(ingredientId);
                }
            }

            if (rare && visual != null)
            {
                halo = SteepingSpirits.Core.PlaceholderSprites.CreateSpriteObject("Halo",
                    SteepingSpirits.Core.PlaceholderSprites.Circle, new Color(1f, 0.95f, 0.7f, 0.18f),
                    transform.position, Vector2.one * 1.3f, 1, transform);
            }
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            float t = Time.time * 1.8f + phase;
            transform.position = basePosition + Vector3.up * (Mathf.Sin(t) * 0.08f);
            if (halo != null)
            {
                float a = 0.14f + 0.06f * Mathf.Sin(t * 0.7f);
                halo.color = new Color(1f, 0.95f, 0.7f, a);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponentInParent<JumpNRunPlayer>() == null)
            {
                return;
            }

            collected = true;
            JumpNRunSession.Current?.Collect(ingredientId);

            FeedbackTuning ft = JumpNRunLevel.Current != null ? JumpNRunLevel.Current.feedbackTuning : null;
            FeedbackParams fp = ft != null ? ft.feedback : new FeedbackParams();
            Color c = ColorOf(ingredientId);
            JumpNRunParticles.Burst(transform.position, rare ? 14 : 7, Vector2.up, 180f, rare ? 2f : 1.4f, c, 0.14f, 0.7f, -0.3f);
            JumpNRunSounds.Play(rare ? JumpNRunSound.CollectRare : JumpNRunSound.Collect, fp.collectVolume);
            if (rare)
            {
                JumpNRunTime.SlowMotion(fp.rareSlowMoScale, fp.rareSlowMoSeconds);
                JumpNRunLevel level = JumpNRunLevel.Current;
                if (level != null)
                {
                    JumpNRunSaveStore.Current.FoundRare(level.levelId, spotX, spotY, ingredientId);
                    JumpNRunSaveStore.Save();
                }
            }

            gameObject.SetActive(false);
        }
    }
}
