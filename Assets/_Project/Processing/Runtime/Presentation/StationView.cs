using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SteepingSpirits.Core;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// A station in the room: its placeholder furniture plus small leaf heaps for the batches lying
    /// on it, coloured and shaped by <see cref="LeafLook"/> (green → copper → brown, flat → rolled,
    /// glossy while moist).
    /// </summary>
    public class StationView : MonoBehaviour
    {
        private const int MaxHeaps = 4;

        private readonly List<SpriteRenderer> leaves = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> gloss = new List<SpriteRenderer>();
        private SpriteRenderer glow;

        public WorkshopTile Tile { get; private set; }
        public Vector2 Center => transform.position;

        public void Build(WorkshopTile tile)
        {
            Tile = tile;
            gameObject.AddComponent<SortingGroup>();
            Color wood = new Color(0.50f, 0.34f, 0.20f), dark = new Color(0.30f, 0.20f, 0.12f), wicker = new Color(0.72f, 0.58f, 0.36f);
            switch (tile)
            {
                case WorkshopTile.Shelf:
                    Part(PlaceholderSprites.Square, dark, Vector2.zero, new Vector2(0.95f, 0.95f), 0);
                    for (int i = 0; i < 3; i++) Part(PlaceholderSprites.Square, wood, new Vector2(0f, -0.3f + i * 0.3f), new Vector2(0.9f, 0.06f), 1);
                    break;
                case WorkshopTile.Store:
                    Part(PlaceholderSprites.Circle, wicker, new Vector2(-0.2f, -0.05f), new Vector2(0.5f, 0.45f), 0);
                    Part(PlaceholderSprites.Circle, wicker * 0.9f, new Vector2(0.22f, 0.05f), new Vector2(0.45f, 0.4f), 0);
                    Part(PlaceholderSprites.Circle, new Color(0.4f, 0.65f, 0.32f), new Vector2(-0.2f, 0.02f), new Vector2(0.36f, 0.22f), 1);
                    break;
                case WorkshopTile.Rack:
                    Part(PlaceholderSprites.Square, new Color(0.80f, 0.70f, 0.45f), Vector2.zero, new Vector2(0.95f, 0.8f), 0);
                    for (int i = 0; i < 4; i++) Part(PlaceholderSprites.Square, new Color(0.6f, 0.5f, 0.3f), new Vector2(0f, -0.3f + i * 0.2f), new Vector2(0.92f, 0.03f), 1);
                    break;
                case WorkshopTile.RollTable:
                case WorkshopTile.BlendTable:
                    Part(PlaceholderSprites.Square, wood, Vector2.zero, new Vector2(0.95f, 0.7f), 0);
                    Part(PlaceholderSprites.Square, wood * 1.15f, new Vector2(0f, 0.05f), new Vector2(0.85f, 0.55f), 1);
                    if (tile == WorkshopTile.BlendTable)
                    {
                        Part(PlaceholderSprites.Circle, new Color(0.85f, 0.85f, 0.9f), new Vector2(-0.25f, 0.15f), new Vector2(0.2f, 0.26f), 2);
                        Part(PlaceholderSprites.Circle, new Color(0.9f, 0.8f, 0.6f), new Vector2(0.25f, 0.15f), new Vector2(0.2f, 0.26f), 2);
                    }

                    break;
                case WorkshopTile.Basket:
                    Part(PlaceholderSprites.Circle, wicker, Vector2.zero, new Vector2(0.85f, 0.75f), 0);
                    Part(PlaceholderSprites.Circle, wicker * 0.8f, Vector2.zero, new Vector2(0.65f, 0.55f), 1);
                    break;
                case WorkshopTile.Wok:
                    Part(PlaceholderSprites.Square, new Color(0.55f, 0.30f, 0.22f), Vector2.zero, new Vector2(0.95f, 0.8f), 0);
                    glow = Part(PlaceholderSprites.Circle, new Color(1f, 0.55f, 0.25f, 0.0f), new Vector2(0f, -0.05f), new Vector2(0.8f, 0.6f), 1);
                    Part(PlaceholderSprites.Circle, new Color(0.18f, 0.18f, 0.2f), new Vector2(0f, 0.05f), new Vector2(0.7f, 0.5f), 2);
                    break;
                case WorkshopTile.Dryer:
                    Part(PlaceholderSprites.Square, new Color(0.45f, 0.35f, 0.3f), new Vector2(0f, -0.25f), new Vector2(0.9f, 0.35f), 0);
                    glow = Part(PlaceholderSprites.Circle, new Color(1f, 0.5f, 0.2f, 0.0f), new Vector2(0f, -0.25f), new Vector2(0.7f, 0.3f), 1);
                    Part(PlaceholderSprites.Circle, wicker, new Vector2(0f, 0.12f), new Vector2(0.85f, 0.5f), 2);
                    break;
                case WorkshopTile.Door:
                    Part(PlaceholderSprites.Square, dark, new Vector2(0f, 0.1f), new Vector2(0.9f, 1.1f), 0);
                    Part(PlaceholderSprites.Square, new Color(0.55f, 0.75f, 0.45f, 0.6f), new Vector2(0f, 0.1f), new Vector2(0.7f, 0.9f), 1);
                    break;
            }

            if (tile == WorkshopTile.Door) return;
            gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.8f);
            for (int i = 0; i < MaxHeaps; i++)
            {
                SpriteRenderer leaf = Part(PlaceholderSprites.Circle, Color.green, HeapOffset(i), Vector2.one * 0.2f, 5);
                SpriteRenderer shine = Part(PlaceholderSprites.Circle, new Color(1f, 1f, 1f, 0f), HeapOffset(i) + new Vector2(-0.03f, 0.03f), Vector2.one * 0.07f, 6);
                leaf.enabled = shine.enabled = false;
                leaves.Add(leaf);
                gloss.Add(shine);
            }
        }

        /// <summary>Shows up to four batches lying here.</summary>
        public void Show(IList<LeafBatch> batches)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                bool on = batches != null && i < batches.Count;
                leaves[i].enabled = on;
                gloss[i].enabled = on;
                if (!on) continue;
                LeafBatch b = batches[i];
                Vector2 s = LeafLook.Shape(b) * (1.1f + 0.08f * Mathf.Min(6, b.amount));
                leaves[i].color = LeafLook.Colour(b);
                leaves[i].transform.localScale = new Vector3(s.x, s.y, 1f);
                Color g = gloss[i].color;
                g.a = LeafLook.Gloss(b) * 0.7f;
                gloss[i].color = g;
            }
        }

        /// <summary>Glow of embers (wok heat, dryer).</summary>
        public void SetGlow(float amount)
        {
            if (glow == null) return;
            Color c = glow.color;
            c.a = Mathf.Clamp01(amount) * 0.6f;
            glow.color = c;
        }

        private static Vector2 HeapOffset(int i) => new Vector2(-0.22f + (i % 2) * 0.44f, 0.12f - (i / 2) * 0.24f);

        private SpriteRenderer Part(Sprite sprite, Color c, Vector2 offset, Vector2 size, int order)
        {
            SpriteRenderer sr = PlaceholderSprites.CreateSpriteObject("Teil", sprite, c, Vector2.zero, size, order, transform);
            sr.transform.localPosition = offset;
            return sr;
        }
    }
}
