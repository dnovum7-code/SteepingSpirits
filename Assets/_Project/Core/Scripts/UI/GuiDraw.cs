using UnityEngine;

namespace SteepingSpirits.Core.UI
{
    /// <summary>
    /// Kleine Helfer für die OnGUI-HUDs (Flächen, Rahmen, Text-Stile), damit
    /// nicht jedes HUD seine eigene 1×1-Textur und Styles baut.
    /// </summary>
    public static class GuiDraw
    {
        private static Texture2D solidTex;

        public static void Solid(Rect rect, Color color)
        {
            if (solidTex == null)
            {
                solidTex = new Texture2D(1, 1);
                solidTex.SetPixel(0, 0, Color.white);
                solidTex.Apply();
            }

            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, solidTex);
            GUI.color = prev;
        }

        public static void Border(Rect rect, Color color, float thickness = 2f)
        {
            Solid(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Solid(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Solid(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Solid(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        /// <summary>Holzfarbener Kasten mit hellem Rand (cozy HUD-Look).</summary>
        public static void Panel(Rect rect, float alpha = 0.85f)
        {
            Solid(rect, new Color(0.20f, 0.13f, 0.08f, alpha));
            Border(rect, new Color(0.62f, 0.45f, 0.25f, alpha), 2f);
        }

        /// <summary>Zeichnet ein Sprite (z.B. Platzhalter-Herz) eingefärbt in ein GUI-Rechteck.</summary>
        public static void Sprite(Rect rect, Sprite sprite, Color color, float fill01 = 1f)
        {
            if (sprite == null || fill01 <= 0f)
            {
                return;
            }

            Texture2D tex = sprite.texture;
            Rect tr = sprite.textureRect;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height,
                tr.width / tex.width * fill01, tr.height / tex.height);
            var target = new Rect(rect.x, rect.y, rect.width * fill01, rect.height);

            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(target, tex, uv);
            GUI.color = prev;
        }

        public static GUIStyle Rich(int size = 13, Color? color = null, FontStyle style = FontStyle.Normal,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = size,
                fontStyle = style,
                alignment = anchor
            };
            s.normal.textColor = color ?? Color.white;
            return s;
        }

        /// <summary>Text mit leichtem Schatten (gut lesbar über bunter Spielwelt).</summary>
        public static void ShadowLabel(Rect rect, string text, GUIStyle style)
        {
            Color main = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.65f * main.a);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = main;
            GUI.Label(rect, text, style);
        }
    }
}
