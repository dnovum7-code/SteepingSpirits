using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteepingSpirits.Core
{
    /// <summary>
    /// Erzeugt einfache Platzhalter-Sprites per Code (weiss, mit leicht
    /// dunklerem Rand) – damit Test-Szenen ohne einen einzigen Grafik-Import
    /// funktionieren. Eingefärbt wird über SpriteRenderer.color.
    ///
    /// 1 Sprite = 1 Unity-Einheit (16 px, Point-Filter → Pixel-Look).
    /// Für das echte Spiel einfach eigene Sprites zuweisen.
    /// </summary>
    public static class PlaceholderSprites
    {
        public const int Size = 16;

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Square => Get("square", (x, y) => true);

        public static Sprite Circle => Get("circle", (x, y) => x * x + y * y <= 1f);

        public static Sprite Diamond => Get("diamond", (x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 1f);

        /// <summary>Herzform (für Lebensanzeige / Heil-Items).</summary>
        public static Sprite Heart => Get("heart", (x, y) =>
        {
            // Klassische Herzkurve (x²+y²−1)³ − x²y³ ≤ 0, leicht skaliert.
            float hx = x * 1.25f;
            float hy = y * 1.3f + 0.25f;
            float a = hx * hx + hy * hy - 1f;
            return a * a * a - hx * hx * hy * hy * hy <= 0f;
        });

        /// <summary>Sichelförmiger Schwerthieb, zeigt nach rechts (+X).</summary>
        public static Sprite Slash => Get("slash", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return r <= 1f && r >= 0.55f && x > -0.15f;
        });

        /// <summary>Kleines Dreieck nach oben (z.B. Bäume, Zelte).</summary>
        public static Sprite Triangle => Get("triangle", (x, y) => Mathf.Abs(x) <= (1f - y) * 0.5f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cache.Clear();
        }

        /// <summary>
        /// Baut (oder holt aus dem Cache) ein Sprite. inside(x, y) bekommt
        /// Koordinaten von −1..1 (y nach oben) und sagt, ob der Pixel gefüllt ist.
        /// </summary>
        public static Sprite Get(string key, Func<float, float, bool> inside)
        {
            if (cache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "Placeholder_" + key,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var filled = new bool[Size, Size];
            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    float x = (px + 0.5f) / Size * 2f - 1f;
                    float y = (py + 0.5f) / Size * 2f - 1f;
                    filled[px, py] = inside(x, y);
                }
            }

            var clear = new Color(0f, 0f, 0f, 0f);
            var edge = new Color(0.62f, 0.62f, 0.62f, 1f);
            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    if (!filled[px, py])
                    {
                        tex.SetPixel(px, py, clear);
                        continue;
                    }

                    bool border = px == 0 || py == 0 || px == Size - 1 || py == Size - 1
                                  || !filled[px - 1, py] || !filled[px + 1, py]
                                  || !filled[px, py - 1] || !filled[px, py + 1];
                    tex.SetPixel(px, py, border ? edge : Color.white);
                }
            }

            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.name = "Placeholder_" + key;
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Legt ein GameObject mit SpriteRenderer an (Platzhalter-Grafik).
        /// size = Grösse in Einheiten, sortingOrder = Zeichenreihenfolge.
        /// </summary>
        public static SpriteRenderer CreateSpriteObject(string name, Sprite sprite, Color color,
            Vector2 position, Vector2 size, int sortingOrder = 0, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
