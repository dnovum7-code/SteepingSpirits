using UnityEngine;
using SteepingSpirits.Combat;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Economy;
using SteepingSpirits.Progression;
using SteepingSpirits.Quests.UI;
using SteepingSpirits.World;

namespace SteepingSpirits.HUD
{
    /// <summary>
    /// Haupt-HUD (OnGUI, Zero-Wiring – einfach in die Szene legen):
    ///  - oben links: Herzen (Zelda), darunter Gold und Level/XP-Balken,
    ///  - oben rechts: Datum, Jahreszeit und Uhrzeit (Stardew),
    ///  - Mitte: Hinweis, wenn der Spieler umgekippt ist.
    /// Liest Health (Spieler), Wallet, PlayerProgression und GameClock – fehlt
    /// eins davon, wird der Teil einfach weggelassen.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        private static GameHUD active;

        [Tooltip("HP pro Herz (2 = halbe Herzen möglich, wie Zelda)")]
        [SerializeField] private float healthPerHeart = 2f;

        [SerializeField] private float heartSize = 28f;

        [SerializeField] private Color heartColor = new Color(0.92f, 0.18f, 0.22f);
        [SerializeField] private Color heartEmptyColor = new Color(0.18f, 0.08f, 0.08f, 0.85f);

        private Health playerHealth;
        private GUIStyle smallStyle;
        private GUIStyle clockTimeStyle;
        private GUIStyle clockDateStyle;
        private GUIStyle bigStyle;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                active = null;
            }
        }

        private void OnGUI()
        {
            if (QuestJournalCanvas.IsOpen)
            {
                return;
            }

            EnsureStyles();
            DrawHearts();
            DrawStats();
            DrawClock();
            DrawDeathNotice();
        }

        private Health PlayerHealth()
        {
            if (playerHealth == null)
            {
                Transform p = PlayerLocator.Find();
                playerHealth = p != null ? p.GetComponent<Health>() : null;
            }

            return playerHealth;
        }

        private void DrawHearts()
        {
            Health h = PlayerHealth();
            if (h == null)
            {
                return;
            }

            int hearts = Mathf.CeilToInt(h.MaxHealth / healthPerHeart);
            const float x0 = 14f, y0 = 12f;
            float gap = heartSize + 4f;
            Sprite heart = PlaceholderSprites.Heart;

            for (int i = 0; i < hearts; i++)
            {
                // Max. 10 Herzen pro Reihe, dann neue Reihe.
                float x = x0 + (i % 10) * gap;
                float y = y0 + (i / 10) * gap;
                var rect = new Rect(x, y, heartSize, heartSize);

                float fill = Mathf.Clamp01((h.CurrentHealth - i * healthPerHeart) / healthPerHeart);
                GuiDraw.Sprite(rect, heart, heartEmptyColor);
                GuiDraw.Sprite(rect, heart, heartColor, fill);
            }
        }

        private void DrawStats()
        {
            float y = 12f + heartSize + 10f;
            Health h = PlayerHealth();
            if (h != null && h.MaxHealth > healthPerHeart * 10f)
            {
                y += heartSize + 4f; // zweite Herzreihe
            }

            Wallet wallet = Wallet.Instance;
            if (wallet != null)
            {
                GuiDraw.ShadowLabel(new Rect(16f, y, 220f, 22f), $"<color=#ffd75e>●</color> {wallet.Gold} Gold", smallStyle);
                y += 22f;
            }

            PlayerProgression prog = PlayerProgression.Instance;
            if (prog != null)
            {
                GuiDraw.ShadowLabel(new Rect(16f, y, 220f, 22f), $"Lv {prog.Level}", smallStyle);
                var bar = new Rect(62f, y + 7f, 110f, 8f);
                float frac = prog.XpToNext > 0 ? Mathf.Clamp01((float)prog.Xp / prog.XpToNext) : 0f;
                GuiDraw.Solid(bar, new Color(0f, 0f, 0f, 0.55f));
                GuiDraw.Solid(new Rect(bar.x, bar.y, bar.width * frac, bar.height), new Color(0.45f, 0.85f, 0.4f));
                GuiDraw.Border(bar, new Color(0f, 0f, 0f, 0.7f), 1f);
            }
        }

        private void DrawClock()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null)
            {
                return;
            }

            const float w = 190f, h = 66f;
            var box = new Rect(Screen.width - w - 12f, 12f, w, h);
            GuiDraw.Panel(box);

            GUI.Label(new Rect(box.x, box.y + 6f, w, 20f), $"{clock.DateLabel}  ·  J{clock.Year}", clockDateStyle);
            string icon = clock.Hour >= 6 && clock.Hour < 19 ? "<color=#ffd75e>☀</color>" : "<color=#b8c4ff>☾</color>";
            GUI.Label(new Rect(box.x, box.y + 26f, w, 34f), $"{icon} {clock.TimeLabel}", clockTimeStyle);
        }

        private void DrawDeathNotice()
        {
            Health h = PlayerHealth();
            if (h == null || !h.IsDead)
            {
                return;
            }

            var rect = new Rect(0f, Screen.height * 0.4f, Screen.width, 40f);
            GuiDraw.Solid(new Rect(0f, rect.y - 10f, Screen.width, 60f), new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(rect, "Du bist umgekippt …", bigStyle);
        }

        private void EnsureStyles()
        {
            if (smallStyle != null)
            {
                return;
            }

            smallStyle = GuiDraw.Rich(14, Color.white, FontStyle.Bold);
            smallStyle.wordWrap = false;
            clockDateStyle = GuiDraw.Rich(13, new Color(0.98f, 0.92f, 0.8f), FontStyle.Bold, TextAnchor.MiddleCenter);
            clockDateStyle.wordWrap = false;
            clockTimeStyle = GuiDraw.Rich(22, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            clockTimeStyle.wordWrap = false;
            bigStyle = GuiDraw.Rich(24, new Color(1f, 0.85f, 0.85f), FontStyle.Bold, TextAnchor.MiddleCenter);
        }
    }
}
