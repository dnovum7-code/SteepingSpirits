using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Inventory;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// Zero-Wiring Quest-HUD (OnGUI/IMGUI). Braucht keine Inspector-Referenzen –
    /// einfach die Komponente in die Szene legen und es funktioniert. Ideal für
    /// die Test-Arena, wo man sofort sehen will, ob Quests laufen:
    ///
    ///  • grosses „✔ Quest abgeschlossen"-Banner (mittig) beim Abschluss,
    ///  • kurze Toasts (unten) bei Start / Fortschritt / Fehlschlag,
    ///  • dauerhafter Tracker (oben rechts) mit allen laufenden Quests + Fortschritt.
    ///
    /// Duplikatsicher: immer nur EINES aktiv (weitere deaktivieren sich selbst),
    /// damit Banner und Toasts nicht doppelt erscheinen.
    /// </summary>
    public class QuestToastHUD : MonoBehaviour
    {
        private static QuestToastHUD active;

        private const float ToastSeconds = 3.5f;
        private const float BannerSeconds = 4.5f;

        private struct Toast
        {
            public string text;
            public Color color;
            public float expires;
        }

        [Tooltip("Eigenen Tracker oben rechts zeigen (aus, wenn QuestTrackerHUD links genutzt wird)")]
        [SerializeField] private bool showTracker = true;

        private readonly List<Toast> toasts = new List<Toast>();

        /// <summary>Tracker oben rechts ein-/ausschalten (z.B. aus der Test-Szene).</summary>
        public bool ShowTracker
        {
            get => showTracker;
            set => showTracker = value;
        }
        private readonly GuiDraggablePanel trackerPanel = new GuiDraggablePanel();

        private string bannerTitle;
        private string bannerRewards;
        private float bannerExpires;

        private GUIStyle bannerTitleStyle;
        private GUIStyle bannerRewardStyle;
        private GUIStyle trackerStyle;
        private GUIStyle trackerHeadStyle;
        private GUIStyle toastStyle;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                enabled = false;
                return;
            }

            active = this;
            QuestEvents.OnQuestStarted += HandleQuestStarted;
            QuestEvents.OnObjectiveUpdated += HandleObjectiveUpdated;
            QuestEvents.OnQuestCompleted += HandleQuestCompleted;
            QuestEvents.OnQuestFailed += HandleQuestFailed;
            GameplayEvents.OnItemCollected += HandleItemCollected;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                QuestEvents.OnQuestStarted -= HandleQuestStarted;
                QuestEvents.OnObjectiveUpdated -= HandleObjectiveUpdated;
                QuestEvents.OnQuestCompleted -= HandleQuestCompleted;
                QuestEvents.OnQuestFailed -= HandleQuestFailed;
                GameplayEvents.OnItemCollected -= HandleItemCollected;
                active = null;
            }
        }

        // ---------------- Events ----------------

        private void HandleQuestStarted(QuestInstance quest)
        {
            Push($"Quest angenommen: {quest.Data.displayName}", new Color(.55f, .8f, 1f));
        }

        private void HandleObjectiveUpdated(QuestInstance quest, ObjectiveData objective, int currentAmount)
        {
            // Fertige Objectives extra hervorheben, sonst normaler Fortschritt.
            bool done = currentAmount >= objective.requiredAmount;
            string text = $"{objective.description}: {currentAmount}/{objective.requiredAmount}";
            Push(done ? "✔ " + text : text,
                done ? new Color(.5f, .95f, .5f) : new Color(.95f, .92f, .6f));
        }

        private void HandleQuestCompleted(QuestInstance quest)
        {
            bannerTitle = quest.Data.displayName;
            bannerRewards = BuildRewardSummary(quest.Data.rewards);
            bannerExpires = Time.unscaledTime + BannerSeconds;
        }

        private void HandleQuestFailed(QuestInstance quest)
        {
            Push($"✘ Quest fehlgeschlagen: {quest.Data.displayName}", new Color(1f, .5f, .45f));
        }

        private void HandleItemCollected(string itemID, GameObject source)
        {
            Push($"+1  {ResolveItemName(itemID)}", new Color(.8f, .95f, .65f));
        }

        private static string ResolveItemName(string itemID)
        {
            if (PlayerInventory.Instance != null)
            {
                ItemData data = PlayerInventory.Instance.Resolve(itemID);
                if (data != null && !string.IsNullOrEmpty(data.displayName))
                {
                    return data.displayName;
                }
            }

            return itemID;
        }

        private void Push(string text, Color color)
        {
            toasts.Add(new Toast { text = text, color = color, expires = Time.unscaledTime + ToastSeconds });
        }

        // ---------------- Zeichnen ----------------

        private void OnGUI()
        {
            EnsureStyles();
            PruneToasts();

            DrawTracker();
            DrawToasts();
            DrawBanner();
        }

        private void PruneToasts()
        {
            float now = Time.unscaledTime;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (toasts[i].expires <= now)
                {
                    toasts.RemoveAt(i);
                }
            }
        }

        private void DrawTracker()
        {
            // Das ausführliche Quest-Journal übernimmt bei geöffnetem Fenster.
            if (!showTracker || QuestJournalCanvas.IsOpen)
            {
                return;
            }

            QuestManager qm = QuestManager.Instance;
            if (qm == null)
            {
                return;
            }

            List<QuestInstance> activeQuests = qm.GetActiveQuests();
            if (activeQuests.Count == 0)
            {
                return;
            }

            const float width = 300f;
            float x = Screen.width - width - 12f;
            float y = 86f; // unter der Uhr (GameHUD)

            var body = new StringBuilder();
            foreach (QuestInstance quest in activeQuests)
            {
                body.Append("<b>").Append(quest.Data.displayName).Append("</b>\n");
                if (quest.Data.objectives != null)
                {
                    foreach (ObjectiveData objective in quest.Data.objectives)
                    {
                        int cur = quest.GetProgress(objective.objectiveID);
                        bool done = cur >= objective.requiredAmount;
                        string mark = done ? "<color=#7fe07f>✔</color>" : "•";
                        body.Append("   ").Append(mark).Append(' ')
                            .Append(objective.description).Append(": ")
                            .Append(cur).Append('/').Append(objective.requiredAmount).Append('\n');
                    }
                }
            }

            string text = body.ToString().TrimEnd('\n');
            float height = trackerStyle.CalcHeight(new GUIContent(text), width - 20f) + 34f;

            var box = trackerPanel.Apply(new Rect(x, y, width, height));
            x = box.x; y = box.y; // verschobene Position übernehmen
            GuiDraw.Panel(box);
            GuiDraggablePanel.DrawGrip(box);
            GameInput.ClaimGuiArea(box);
            GUI.Label(new Rect(x + 22f, y + 6f, width - 32f, 20f), "Laufende Quests", trackerHeadStyle);
            GUI.Label(new Rect(x + 10f, y + 28f, width - 20f, height - 34f), text, trackerStyle);
        }

        private void DrawToasts()
        {
            if (toasts.Count == 0)
            {
                return;
            }

            const float width = 460f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 150f; // über dem Interaktions-Prompt

            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                Toast t = toasts[i];
                float remaining = t.expires - Time.unscaledTime;
                float alpha = Mathf.Clamp01(remaining / 0.6f); // letzte 0.6s ausblenden

                var rect = new Rect(x, y, width, 26f);
                GuiDraw.Solid(rect, new Color(0f, 0f, 0f, .6f * alpha));

                Color c = t.color;
                c.a = alpha;
                toastStyle.normal.textColor = c;
                GUI.Label(rect, t.text, toastStyle);

                y -= 30f;
            }
        }

        private void DrawBanner()
        {
            if (Time.unscaledTime >= bannerExpires || string.IsNullOrEmpty(bannerTitle))
            {
                return;
            }

            float remaining = bannerExpires - Time.unscaledTime;
            float alpha = Mathf.Clamp01(remaining / 0.8f);

            const float width = 560f;
            const float height = 96f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.28f;

            var box = new Rect(x, y, width, height);
            GuiDraw.Solid(box, new Color(.09f, .11f, .09f, .92f * alpha));
            GuiDraw.Border(box, new Color(.5f, .95f, .5f, alpha));

            Color title = new Color(.6f, 1f, .6f, alpha);
            bannerTitleStyle.normal.textColor = title;
            GUI.Label(new Rect(x, y + 16f, width, 34f),
                "✔ Quest abgeschlossen:  " + bannerTitle, bannerTitleStyle);

            if (!string.IsNullOrEmpty(bannerRewards))
            {
                Color rew = new Color(1f, .92f, .55f, alpha);
                bannerRewardStyle.normal.textColor = rew;
                GUI.Label(new Rect(x, y + 54f, width, 28f), "Belohnung:  " + bannerRewards, bannerRewardStyle);
            }
        }

        // ---------------- Helfer ----------------

        private static string BuildRewardSummary(Reward[] rewards)
        {
            if (rewards == null || rewards.Length == 0)
            {
                return "";
            }

            var builder = new StringBuilder();
            foreach (Reward reward in rewards)
            {
                if (builder.Length > 0)
                {
                    builder.Append("   ·   ");
                }

                switch (reward.type)
                {
                    case RewardType.XP:
                        builder.Append("+").Append(reward.amount).Append(" XP");
                        break;
                    case RewardType.Gold:
                        builder.Append("+").Append(reward.amount).Append(" Gold");
                        break;
                    case RewardType.Item:
                        builder.Append(reward.itemID);
                        if (reward.amount > 1)
                        {
                            builder.Append(" x").Append(reward.amount);
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        private void EnsureStyles()
        {
            if (bannerTitleStyle != null)
            {
                return;
            }

            bannerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            bannerRewardStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };

            trackerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                wordWrap = true
            };
            trackerStyle.normal.textColor = new Color(.92f, .94f, .98f);

            trackerHeadStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            trackerHeadStyle.normal.textColor = new Color(1f, .85f, .4f);

            toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
