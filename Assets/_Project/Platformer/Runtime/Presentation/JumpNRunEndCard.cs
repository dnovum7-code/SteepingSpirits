using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Platforming.Core;
using SteepingSpirits.World;

namespace SteepingSpirits.Platformer.JumpNRun
{
    /// <summary>
    /// End-of-level card (uGUI): what is in the bag (found / placed), rare finds,
    /// lit lanterns, the lantern trail and where the ingredients went. No time,
    /// no falls – only what was gathered. Buttons work with mouse, keyboard and pad.
    /// </summary>
    public class JumpNRunEndCard : MonoBehaviour
    {
        public const string MeadowScene = "Assets/Scenes/TestMeadow.unity";

        private JumpNRunSession session;
        private JumpNRunLevel level;
        private GameObject card;
        private CanvasGroup group;
        private float shownAt = -1f;

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
        }

        private void Update()
        {
            if (group != null && group.alpha < 1f)
            {
                group.alpha = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.6f);
            }
        }

        private void OnFinish()
        {
            var available = new Dictionary<string, int>();
            IngredientBag placed = IngredientBag.Parse(level != null ? level.availableIngredients : "");
            foreach (string id in placed.Ids) available[id] = placed.Count(id);
            List<IngredientTallyLine> lines = IngredientTally.Lines(session.Bag, available);
            shownAt = Time.unscaledTime;
            Debug.Log("[JumpNRun] Bag: " + session.Bag.Serialize());
            Build(lines);
        }

        private void Build(List<IngredientTallyLine> lines)
        {
            RectTransform root = JumpNRunUi.Root;
            card = UiFactory.NewRect("EndCard", root).gameObject;
            UiFactory.FullStretch((RectTransform)card.transform);
            group = card.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            JumpNRunUi.Dim(card.transform, 0.4f);

            float height = 420f + Mathf.Max(1, lines.Count) * 40f;
            RectTransform content = JumpNRunUi.Window(card.transform, JumpNRunTexts.EndTitle, new Vector2(720f, height), out _);

            if (level != null && !string.IsNullOrEmpty(level.displayName))
            {
                JumpNRunUi.Line(content, level.displayName, 22, UiFactory.TextDim, TextAnchor.MiddleCenter);
            }

            if (session.Bag.Total == 0)
            {
                JumpNRunUi.Line(content, JumpNRunTexts.EndNothing, 24, UiFactory.TextMain, TextAnchor.MiddleCenter, 60f);
            }

            foreach (IngredientTallyLine line in lines)
            {
                AddIngredientRow(content, line);
            }

            int lanterns = level != null ? level.lanternCount : 0;
            if (lanterns > 0 && session.Checkpoints != null)
            {
                JumpNRunUi.Line(content, JumpNRunTexts.LanternsLit(session.Checkpoints.LitCount, lanterns), 20, UiFactory.TextDim,
                    TextAnchor.MiddleCenter);
            }

            LanternChallenge challenge = session.Challenge;
            if (challenge.Active)
            {
                JumpNRunUi.Line(content, challenge.Complete ? JumpNRunTexts.ChallengeDone : JumpNRunTexts.PathLanterns(challenge.Lit, challenge.Total),
                    20, UiFactory.TextDim, TextAnchor.MiddleCenter);
            }

            if (session.Handover != HandoverTarget.Nothing)
            {
                JumpNRunUi.Line(content, session.Handover == HandoverTarget.Inventory ? JumpNRunTexts.HandedToInventory : JumpNRunTexts.HandedToPantry,
                    20, UiFactory.TextDim, TextAnchor.MiddleCenter);
            }

            RectTransform buttons = UiFactory.NewRect("Buttons", content);
            UiFactory.HorizontalGroup(buttons, 14f, new RectOffset(0, 0, 8, 0)).childForceExpandWidth = true;
            UiFactory.Sizing(buttons.gameObject, 66f);

            var selectables = new List<Selectable>();
            bool hasNext = level != null && !string.IsNullOrEmpty(level.nextScenePath);
            if (hasNext)
            {
                Button next = JumpNRunUi.Button(buttons, JumpNRunTexts.NextLevel);
                next.onClick.AddListener(() => ScenePortal.Load(level.nextScenePath));
                selectables.Add(next);
            }

            Button back = JumpNRunUi.Button(buttons, level != null && !string.IsNullOrEmpty(level.backScenePath)
                ? JumpNRunTexts.BackToClearing : JumpNRunTexts.Back);
            back.onClick.AddListener(GoBack);
            selectables.Add(back);

            Button again = JumpNRunUi.Button(buttons, JumpNRunTexts.Again);
            again.onClick.AddListener(() => ScenePortal.Load(SceneManager.GetActiveScene().path));
            selectables.Add(again);

            JumpNRunUi.Chain(selectables, false);
            JumpNRunUi.Focus(selectables[0]);
        }

        private static void AddIngredientRow(Transform parent, IngredientTallyLine line)
        {
            RectTransform row = UiFactory.NewRect("Row", parent);
            UiFactory.HorizontalGroup(row, 12f, new RectOffset(40, 40, 0, 0));
            UiFactory.Sizing(row.gameObject, 34f);

            Image icon = UiFactory.Panel("Icon", row, Ingredient.ColorOf(line.id));
            icon.sprite = line.rare ? PlaceholderSprites.Diamond : PlaceholderSprites.Circle;
            icon.preserveAspect = true;
            UiFactory.Sizing(icon.gameObject, 26f, 26f);

            string name = JumpNRunTexts.IngredientName(line.id) + (line.rare ? $"  <i>({JumpNRunTexts.EndRare})</i>" : "");
            Text label = UiFactory.Label("Name", row, name, 24, UiFactory.TextMain, TextAnchor.MiddleLeft);
            UiFactory.Sizing(label.gameObject, 34f, -1f, 1f);

            Text count = UiFactory.Label("Count", row, JumpNRunTexts.Found(line.found, line.available), 24, UiFactory.Accent,
                TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.Sizing(count.gameObject, 34f, 120f);
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
    }
}
