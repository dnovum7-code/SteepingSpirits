using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Ingredients;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// A shelf on the clearing with one place per rare ingredient. Found ones
    /// sit there glowing softly, missing ones are pale outlines. Reads the save.
    /// </summary>
    public class RareShelf : MonoBehaviour
    {
        [SerializeField] private float spacing = 1.2f;
        [SerializeField] private Color woodColor = new Color(0.45f, 0.32f, 0.22f);
        [SerializeField] private Color missingColor = new Color(1f, 1f, 1f, 0.15f);

        private WorldLabel caption;
        private SpriteRenderer[] items;
        private float time;

        private void Start()
        {
            int i = 0;
            int found = 0;
            var ids = new System.Collections.Generic.List<string>(IngredientCatalog.Ids(Rarity.Rare));
            items = new SpriteRenderer[ids.Count];
            float width = (ids.Count - 1) * spacing;
            PlaceholderSprites.CreateSpriteObject("Board", PlaceholderSprites.Square, woodColor,
                (Vector2)transform.position + new Vector2(width * 0.5f, -0.1f), new Vector2(width + 1.2f, 0.18f), 1, transform);

            foreach (string id in ids)
            {
                bool has = JumpNRunSaveStore.Current.HasEverFound(id);
                if (has) found++;
                Vector2 pos = (Vector2)transform.position + new Vector2(i * spacing, 0.35f);
                Color c = has ? Ingredient.ColorOf(id) : missingColor;
                items[i] = PlaceholderSprites.CreateSpriteObject(id, PlaceholderSprites.Diamond, c, pos, Vector2.one * 0.55f, 3, transform);
                if (has)
                {
                    PlaceholderSprites.CreateSpriteObject("Glow", PlaceholderSprites.Circle, new Color(1f, 0.95f, 0.75f, 0.15f),
                        pos, Vector2.one * 1.1f, 2, transform);
                }

                i++;
            }

            caption = new WorldLabel("ShelfCaption", 18, new Color(1f, 0.93f, 0.8f), new Color(0.2f, 0.13f, 0.08f, 0.6f), 320f);
            caption.SetText(JumpNRunTexts.ShelfCount(found, ids.Count));
            caption.SetAlpha(1f);
        }

        private void OnDestroy()
        {
            caption?.Destroy();
        }

        private void LateUpdate()
        {
            if (caption != null)
            {
                caption.Follow(transform.position + new Vector3((items.Length - 1) * spacing * 0.5f, 1.2f, 0f));
            }

            // Found items breathe a little.
            time += Time.deltaTime;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                float s = 0.55f * (1f + 0.05f * Mathf.Sin(time * 1.5f + i));
                items[i].transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
