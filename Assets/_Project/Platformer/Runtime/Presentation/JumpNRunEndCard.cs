using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.World;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// End-of-level card: what is in the bag (found / placed), rare finds and
    /// lit lanterns. No time, no falls – only what was gathered.
    /// </summary>
    public class JumpNRunEndCard : MonoBehaviour
    {
        public const string MeadowScene = "Assets/Scenes/TestMeadow.unity";

        private JumpNRunSession session;
        private JumpNRunLevel level;
        private List<IngredientTallyLine> lines;
        private float shownAt = -1f;
        private GUIStyle title, label, small, button;

        private void Start()
        {
            session = JumpNRunSession.Current;
            level = JumpNRunLevel.Current;
            if (session != null)
            {
                session.Finish += OnFinish;
            }
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.Finish -= OnFinish;
            }

            GamePause.Set(this, false);
        }

        private void OnFinish()
        {
            var available = new Dictionary<string, int>();
            IngredientBag placed = IngredientBag.Parse(level != null ? level.availableIngredients : "");
            foreach (string id in placed.Ids) available[id] = placed.Count(id);
            lines = IngredientTally.Lines(session.Bag, available);
            shownAt = Time.unscaledTime;
            Debug.Log("[JumpNRun] Bag: " + session.Bag.Serialize());
        }

        private void Update()
        {
            // Keyboard / pad: jump or interact takes the main way out.
            if (shownAt < 0f || Time.unscaledTime - shownAt < 0.8f)
            {
                return;
            }

            if (GameInput.JumpPressed || GameInput.InteractPressed)
            {
                bool hasNext = level != null && !string.IsNullOrEmpty(level.nextScenePath);
                if (hasNext) ScenePortal.Load(level.nextScenePath);
                else GoBack();
            }
        }

        private void GoBack()
        {
            if (level != null && !string.IsNullOrEmpty(level.backScenePath))
            {
                ScenePortal.Load(level.backScenePath);
                return;
            }

            string back = ScenePortal.PreviousScenePath;
            ScenePortal.Load(string.IsNullOrEmpty(back) || back == SceneManager.GetActiveScene().path ? MeadowScene : back);
        }

        private void OnGUI()
        {
            if (shownAt < 0f)
            {
                return;
            }

            if (title == null)
            {
                title = GuiDraw.Rich(22, new Color(1f, 0.92f, 0.78f), FontStyle.Bold, TextAnchor.MiddleCenter);
                label = GuiDraw.Rich(15, Color.white);
                small = GuiDraw.Rich(12, new Color(1f, 1f, 1f, 0.7f), FontStyle.Normal, TextAnchor.MiddleCenter);
                button = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            }

            float a = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.6f);
            GuiDraw.Solid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.05f, 0.06f, 0.1f, 0.4f * a));
            float h = 230f + Mathf.Max(1, lines.Count) * 26f;
            var r = new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f - h * 0.5f, 420f, h);
            GuiDraw.Panel(r, 0.94f * a);
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);

            GUI.Label(new Rect(r.x, r.y + 12f, r.width, 30f), JumpNRunTexts.EndTitle, title);
            if (level != null && !string.IsNullOrEmpty(level.displayName))
            {
                GUI.Label(new Rect(r.x, r.y + 40f, r.width, 20f), level.displayName, small);
            }

            float y = r.y + 70f;
            if (session.Bag.Total == 0)
            {
                GUI.Label(new Rect(r.x + 24f, y, r.width - 48f, 40f), JumpNRunTexts.EndNothing, label);
                y += 26f;
            }

            foreach (IngredientTallyLine line in lines)
            {
                Color c = Ingredient.ColorOf(line.id);
                GuiDraw.Sprite(new Rect(r.x + 28f, y + 4f, 14f, 14f), line.rare ? PlaceholderSprites.Diamond : PlaceholderSprites.Circle, c);
                string name = JumpNRunTexts.IngredientName(line.id) + (line.rare ? $"  <i>({JumpNRunTexts.EndRare})</i>" : "");
                GUI.Label(new Rect(r.x + 52f, y, 240f, 24f), name, label);
                GUI.Label(new Rect(r.xMax - 120f, y, 92f, 24f), JumpNRunTexts.Found(line.found, line.available),
                    GuiDraw.Rich(15, new Color(1f, 0.9f, 0.6f), FontStyle.Bold, TextAnchor.UpperRight));
                y += 26f;
            }

            int lanterns = level != null ? level.lanternCount : 0;
            if (lanterns > 0 && session.Checkpoints != null)
            {
                GUI.Label(new Rect(r.x, y + 6f, r.width, 20f), JumpNRunTexts.LanternsLit(session.Checkpoints.LitCount, lanterns), small);
                y += 20f;
            }

            if (session.Handover != HandoverTarget.Nothing)
            {
                string where = session.Handover == HandoverTarget.Inventory ? JumpNRunTexts.HandedToInventory : JumpNRunTexts.HandedToPantry;
                GUI.Label(new Rect(r.x, y + 6f, r.width, 20f), where, small);
                y += 20f;
            }

            LanternChallenge challenge = session.Challenge;
            if (challenge.Active)
            {
                string text = challenge.Complete
                    ? JumpNRunTexts.ChallengeDone
                    : JumpNRunTexts.PathLanterns(challenge.Lit, challenge.Total);
                GUI.Label(new Rect(r.x, y + 6f, r.width, 20f), text, small);
            }

            float by = r.yMax - 48f;
            bool hasNext = level != null && !string.IsNullOrEmpty(level.nextScenePath);
            float bw = hasNext ? 120f : 170f;
            float bx = r.x + (r.width - (hasNext ? 3 : 2) * (bw + 10f) + 10f) * 0.5f;
            if (hasNext && GUI.Button(new Rect(bx, by, bw, 32f), JumpNRunTexts.NextLevel, button))
            {
                ScenePortal.Load(level.nextScenePath);
            }

            if (hasNext) bx += bw + 10f;
            if (GUI.Button(new Rect(bx, by, bw, 32f), JumpNRunTexts.Again, button))
            {
                ScenePortal.Load(SceneManager.GetActiveScene().path);
            }

            bx += bw + 10f;
            if (GUI.Button(new Rect(bx, by, bw, 32f), level != null && !string.IsNullOrEmpty(level.backScenePath)
                    ? JumpNRunTexts.BackToClearing : JumpNRunTexts.Back, button))
            {
                GoBack();
            }

            GUI.color = old;
        }
    }
}
