using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;

namespace SteepingSpirits.Quests.UI
{
    /// <summary>
    /// Quest-Journal als echtes uGUI (Canvas + Panel). Öffnen/Schliessen mit
    /// Taste B (oder ✕ / Esc). Solange es offen ist, steht die Spielwelt
    /// (GamePause). Aufbau:
    ///
    ///   • Suchfeld oben links,
    ///   • aufklappbare Sektionen (Hauptquests / Worldquests / Sidequests) –
    ///     Klick auf die Sektion listet die Quests darunter auf,
    ///   • Button unten führt auf eine eigene Seite „Charakterquests".
    ///
    /// Die komplette Oberfläche wird im Code gebaut (kein Prefab nötig). Die
    /// Sektionen sind über <see cref="MainSections"/> frei zuordenbar.
    /// Duplikatsicher: immer nur EINES aktiv.
    /// </summary>
    public class QuestJournalCanvas : MonoBehaviour
    {
        private static QuestJournalCanvas active;

        private struct Section
        {
            public string label;
            public QuestCategory[] cats;
        }

        // Zuordnung Sektion → Quest-Kategorien. Hier bei Bedarf umhängen.
        private static readonly Section[] MainSections =
        {
            new Section { label = "Hauptquests",    cats = new[] { QuestCategory.MAIN } },
            new Section { label = "Eventquests",    cats = new[] { QuestCategory.EVENT } },
            new Section { label = "Worldquests",    cats = new[] { QuestCategory.WORLD } },
            new Section { label = "Sidequests",     cats = new[] { QuestCategory.SIDE } },
            new Section { label = "Fraktionsquests", cats = new[] { QuestCategory.FACTION } },
        };

        // Die „Charakterquests"-Seite sammelt die restlichen Kategorien.
        private static readonly QuestCategory[] CharacterCats =
        {
            QuestCategory.TUTORIAL, QuestCategory.DAILY
        };

        // Zustände, die ins Quest-Archiv wandern (erledigte / beendete Quests).
        private static readonly QuestState[] ArchiveStates =
        {
            QuestState.COMPLETED, QuestState.FAILED, QuestState.ABANDONED
        };

        private GameObject root;          // Backdrop, wird ein-/ausgeblendet
        private GameObject mainPage;
        private GameObject characterPage;
        private GameObject archivePage;
        private RectTransform mainList;   // Scroll-Content Hauptseite
        private RectTransform charList;   // Scroll-Content Charakterseite
        private RectTransform archiveList; // Scroll-Content Archiv
        private InputField search;

        private bool open;
        private string filter = "";
        private readonly HashSet<string> expanded = new HashSet<string> { "Hauptquests" };

        // Cursor-Zustand vor dem Öffnen, damit wir ihn beim Schliessen
        // wiederherstellen statt blind auf „gefangen" zu setzen.
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;

        /// <summary>True, solange das Journal-Fenster offen ist.</summary>
        public static bool IsOpen => active != null && active.open;

        private void Awake()
        {
            if (active != null && active != this)
            {
                Destroy(this);
                return;
            }

            active = this;
            BuildUi();
        }

        private void OnEnable()
        {
            // Offenes Journal live aktualisieren, wenn sich Quests ändern.
            QuestEvents.OnQuestStateChanged += HandleQuestsChanged;
            QuestEvents.OnObjectiveUpdated += HandleObjectiveChanged;
        }

        private void OnDisable()
        {
            QuestEvents.OnQuestStateChanged -= HandleQuestsChanged;
            QuestEvents.OnObjectiveUpdated -= HandleObjectiveChanged;
        }

        private void OnDestroy()
        {
            GamePause.Set(this, false);
            if (active == this)
            {
                active = null;
            }
        }

        private void HandleQuestsChanged(QuestInstance quest)
        {
            if (open)
            {
                Rebuild();
            }
        }

        private void HandleObjectiveChanged(QuestInstance quest, ObjectiveData objective, int amount)
        {
            if (open)
            {
                Rebuild();
            }
        }

        private void Update()
        {
            bool toggle = GameInput.JournalTogglePressed;
            bool escape = GameInput.CancelPressed;

            // Nicht umschalten, während man ins Suchfeld tippt (sonst schliesst „b").
            if (toggle && !(search != null && search.isFocused))
            {
                SetOpen(!open);
            }
            else if (escape && open)
            {
                SetOpen(false);
            }
        }

        private void SetOpen(bool value)
        {
            open = value;
            GamePause.Set(this, open);
            if (root != null)
            {
                root.SetActive(open);
            }

            if (open)
            {
                ShowMainPage();
                if (search != null)
                {
                    search.text = "";
                }
                filter = "";
                Rebuild();

                // Cursor-Zustand merken und für das Menü freigeben.
                prevCursorLock = Cursor.lockState;
                prevCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Vorherigen Cursor-Zustand wiederherstellen.
                Cursor.lockState = prevCursorLock;
                Cursor.visible = prevCursorVisible;
            }
        }

        // ---------------- Aufbau ----------------

        private void BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas("QuestJournalCanvas", 100);
            canvas.transform.SetParent(transform, false);

            root = UiFactory.Panel("Backdrop", canvas.transform, UiFactory.Backdrop).gameObject;
            UiFactory.FullStretch(root.GetComponent<RectTransform>());

            Image window = UiFactory.Panel("Window", root.transform, UiFactory.WindowBg);
            Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1120f, 720f));

            BuildHeader(window.rectTransform);

            // Körper (unter dem Header).
            Image body = UiFactory.Panel("Body", window.rectTransform, new Color(0f, 0f, 0f, 0f));
            UiFactory.FullStretch(body.rectTransform, 0f, 68f, 0f, 0f);

            BuildMainPage(body.rectTransform);
            characterPage = BuildDetailPage(body.rectTransform, "CharacterPage", "◇ Charakterquests", out charList);
            archivePage = BuildDetailPage(body.rectTransform, "ArchivePage", "❐ Quest-Archiv", out archiveList);

            SetOpenImmediate(false);
        }

        private void BuildHeader(RectTransform window)
        {
            Image header = UiFactory.Panel("Header", window, UiFactory.HeaderBg);
            Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 68f));

            Image accent = UiFactory.Panel("Accent", header.rectTransform, UiFactory.Accent);
            Place(accent.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 3f));

            // Suchfeld oben links.
            search = UiFactory.MakeInput("Search", header.rectTransform, "Quest suchen…", 15);
            Place(search.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -15f), new Vector2(360f, 38f));
            search.onValueChanged.AddListener(value =>
            {
                filter = value == null ? "" : value.Trim().ToLowerInvariant();
                Rebuild();
            });

            // Titel mittig.
            Text title = UiFactory.Label("Title", header.rectTransform, "Questlog", 22,
                UiFactory.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(420f, 36f));

            // Schliessen oben rechts.
            Button close = UiFactory.MakeButton("Close", header.rectTransform, "✕", 18,
                new Color(0.5f, 0.2f, 0.2f, 1f), UiFactory.TextMain);
            Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-14f, -15f), new Vector2(40f, 38f));
            close.onClick.AddListener(() => SetOpen(false));

            // Kleiner Hinweis links vom ✕.
            Text hint = UiFactory.Label("Hint", header.rectTransform, "B / Esc", 12,
                UiFactory.TextDim, TextAnchor.MiddleRight);
            Place(hint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-62f, -16f), new Vector2(120f, 34f));
        }

        private void BuildMainPage(RectTransform body)
        {
            mainPage = UiFactory.Panel("MainPage", body, new Color(0f, 0f, 0f, 0f)).gameObject;
            UiFactory.FullStretch(mainPage.GetComponent<RectTransform>());

            mainList = UiFactory.MakeScroll("List", mainPage.transform, out ScrollRect mainScroll);
            UiFactory.FullStretch((RectTransform)mainScroll.transform, 14f, 12f, 14f, 64f);

            // Kleiner quadratischer Icon-Button ganz links → Quest-Archiv.
            Button archiveBtn = UiFactory.MakeButton("ArchiveIcon", mainPage.transform, "❐", 20,
                new Color(0.42f, 0.34f, 0.20f, 1f), UiFactory.TextMain);
            Place(archiveBtn.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(14f, 14f), new Vector2(42f, 42f));
            archiveBtn.onClick.AddListener(ShowArchivePage);

            // Footer-Button → Charakterquests (rechts vom Archiv-Icon).
            Button toChar = UiFactory.MakeButton("ToCharacter", mainPage.transform,
                "Charakterquests  ▶", 16, UiFactory.AccentSoft, UiFactory.TextMain);
            RectTransform fr = toChar.GetComponent<RectTransform>();
            fr.anchorMin = new Vector2(0f, 0f);
            fr.anchorMax = new Vector2(1f, 0f);
            fr.pivot = new Vector2(0.5f, 0f);
            fr.offsetMin = new Vector2(64f, 14f);
            fr.offsetMax = new Vector2(-14f, 56f);
            toChar.onClick.AddListener(ShowCharacterPage);
        }

        /// <summary>Baut eine Unterseite (Zurück-Knopf + Titel + Scroll-Liste).</summary>
        private GameObject BuildDetailPage(RectTransform body, string name, string title, out RectTransform list)
        {
            GameObject page = UiFactory.Panel(name, body, new Color(0f, 0f, 0f, 0f)).gameObject;
            UiFactory.FullStretch(page.GetComponent<RectTransform>());

            Button back = UiFactory.MakeButton("Back", page.transform, "◀ Zurück", 15,
                UiFactory.AccentSoft, UiFactory.TextMain);
            Place(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(150f, 38f));
            back.onClick.AddListener(ShowMainPage);

            Text titleText = UiFactory.Label("Title", page.transform, title, 18,
                UiFactory.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform tt = titleText.rectTransform;
            tt.anchorMin = new Vector2(0f, 1f);
            tt.anchorMax = new Vector2(1f, 1f);
            tt.pivot = new Vector2(0f, 1f);
            tt.offsetMin = new Vector2(180f, -52f);
            tt.offsetMax = new Vector2(-16f, -14f);

            list = UiFactory.MakeScroll("List", page.transform, out ScrollRect scroll);
            UiFactory.FullStretch((RectTransform)scroll.transform, 14f, 58f, 14f, 14f);
            return page;
        }

        // ---------------- Seiten / Inhalt ----------------

        private void ShowMainPage()
        {
            SetPage(mainPage);
        }

        private void ShowCharacterPage()
        {
            SetPage(characterPage);
            BuildCharacterList();
        }

        private void ShowArchivePage()
        {
            SetPage(archivePage);
            BuildArchiveList();
        }

        private void SetPage(GameObject page)
        {
            if (mainPage != null)
            {
                mainPage.SetActive(page == mainPage);
            }
            if (characterPage != null)
            {
                characterPage.SetActive(page == characterPage);
            }
            if (archivePage != null)
            {
                archivePage.SetActive(page == archivePage);
            }
        }

        private void SetOpenImmediate(bool value)
        {
            open = value;
            if (root != null)
            {
                root.SetActive(value);
            }
        }

        private void Rebuild()
        {
            if (mainList == null)
            {
                return;
            }

            Clear(mainList);

            if (QuestManager.Instance == null)
            {
                MakeInfo(mainList, "Kein QuestManager in der Szene.");
                return;
            }

            foreach (Section section in MainSections)
            {
                BuildSection(mainList, section.label, section.cats);
            }

            if (characterPage != null && characterPage.activeSelf)
            {
                BuildCharacterList();
            }
            if (archivePage != null && archivePage.activeSelf)
            {
                BuildArchiveList();
            }
        }

        private void BuildSection(RectTransform content, string label, QuestCategory[] cats)
        {
            List<QuestInstance> quests = QuestsIn(cats);
            bool show = expanded.Contains(label) || filter.Length > 0;
            string arrow = show ? "▾" : "▸";

            Button header = UiFactory.MakeButton("Sec_" + label, content,
                $"{arrow}   ◇  {label}    <color=#9aa0ac>({quests.Count})</color>", 17,
                UiFactory.HeaderBg, UiFactory.TextMain);
            UiFactory.Sizing(header.gameObject, 46f);
            LeftAlign(header, 16f);

            string lbl = label;
            header.onClick.AddListener(() =>
            {
                if (!expanded.Remove(lbl))
                {
                    expanded.Add(lbl);
                }
                Rebuild();
            });

            if (!show)
            {
                return;
            }

            if (quests.Count == 0)
            {
                MakeInfo(content, "     (keine passenden Quests)");
                return;
            }

            foreach (QuestInstance quest in quests)
            {
                BuildQuestRow(content, quest);
            }
        }

        private void BuildCharacterList()
        {
            if (charList == null)
            {
                return;
            }

            Clear(charList);

            List<QuestInstance> quests = QuestsIn(CharacterCats);
            if (quests.Count == 0)
            {
                MakeInfo(charList, "Noch keine Charakterquests vorhanden.");
                return;
            }

            foreach (QuestInstance quest in quests)
            {
                BuildQuestRow(charList, quest);
            }
        }

        private void BuildArchiveList()
        {
            if (archiveList == null)
            {
                return;
            }

            Clear(archiveList);

            List<QuestInstance> quests = QuestsInStates(ArchiveStates);
            if (quests.Count == 0)
            {
                MakeInfo(archiveList, "Noch keine abgeschlossenen Quests im Archiv.");
                return;
            }

            foreach (QuestInstance quest in quests)
            {
                BuildQuestRow(archiveList, quest);
            }
        }

        private void BuildQuestRow(RectTransform content, QuestInstance quest)
        {
            Image row = UiFactory.Panel("Q_" + quest.Data.questID, content, UiFactory.RowBg);
            UiFactory.Sizing(row.gameObject, 52f);

            Text t = UiFactory.Label("Text", row.transform, RowText(quest), 14,
                UiFactory.TextMain, TextAnchor.MiddleLeft);
            t.verticalOverflow = VerticalWrapMode.Overflow;
            UiFactory.FullStretch(t.rectTransform, 22f, 6f, 12f, 6f);
        }

        // ---------------- Daten / Text ----------------

        private List<QuestInstance> QuestsIn(QuestCategory[] cats)
        {
            var list = new List<QuestInstance>();
            QuestManager qm = QuestManager.Instance;
            if (qm == null)
            {
                return list;
            }

            foreach (QuestInstance quest in qm.GetAllQuests())
            {
                if (InCategories(quest.Data.category, cats) && PassesFilter(quest))
                {
                    list.Add(quest);
                }
            }

            SortByStateThenName(list);
            return list;
        }

        private List<QuestInstance> QuestsInStates(QuestState[] states)
        {
            var list = new List<QuestInstance>();
            QuestManager qm = QuestManager.Instance;
            if (qm == null)
            {
                return list;
            }

            foreach (QuestInstance quest in qm.GetAllQuests())
            {
                if (InStates(quest.State, states) && PassesFilter(quest))
                {
                    list.Add(quest);
                }
            }

            SortByStateThenName(list);
            return list;
        }

        private bool PassesFilter(QuestInstance quest)
        {
            if (filter.Length == 0)
            {
                return true;
            }

            string name = quest.Data.displayName != null ? quest.Data.displayName.ToLowerInvariant() : "";
            return name.IndexOf(filter, System.StringComparison.Ordinal) >= 0;
        }

        private static void SortByStateThenName(List<QuestInstance> list)
        {
            list.Sort((a, b) =>
            {
                int pa = StatePriority(a.State);
                int pb = StatePriority(b.State);
                if (pa != pb)
                {
                    return pa.CompareTo(pb);
                }
                return string.Compare(a.Data.displayName, b.Data.displayName, System.StringComparison.Ordinal);
            });
        }

        private static bool InStates(QuestState state, QuestState[] states)
        {
            foreach (QuestState s in states)
            {
                if (s == state)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool InCategories(QuestCategory cat, QuestCategory[] cats)
        {
            foreach (QuestCategory c in cats)
            {
                if (c == cat)
                {
                    return true;
                }
            }
            return false;
        }

        private static string RowText(QuestInstance quest)
        {
            string line1 = $"–  <b>{quest.Data.displayName}</b>    {StateLabel(quest.State)}";
            string line2 = $"<color=#8a90a0>{ObjectivesInline(quest)}</color>";
            return line1 + "\n" + line2;
        }

        private static string ObjectivesInline(QuestInstance quest)
        {
            if (quest.Data.objectives == null || quest.Data.objectives.Length == 0)
            {
                return "—";
            }

            var sb = new System.Text.StringBuilder();
            foreach (ObjectiveData obj in quest.Data.objectives)
            {
                if (sb.Length > 0)
                {
                    sb.Append("   ·   ");
                }
                int cur = quest.GetProgress(obj.objectiveID);
                sb.Append(obj.description).Append(": ").Append(cur).Append('/').Append(obj.requiredAmount);
            }

            return sb.ToString();
        }

        private static int StatePriority(QuestState state)
        {
            switch (state)
            {
                case QuestState.ACTIVE: return 0;
                case QuestState.AVAILABLE: return 1;
                case QuestState.COMPLETED: return 2;
                case QuestState.FAILED: return 3;
                default: return 4;
            }
        }

        private static string StateLabel(QuestState state)
        {
            string hex;
            string txt;
            switch (state)
            {
                case QuestState.ACTIVE: hex = "#ffcf40"; txt = "Aktiv"; break;
                case QuestState.COMPLETED: hex = "#7fe07f"; txt = "Abgeschlossen"; break;
                case QuestState.AVAILABLE: hex = "#7fc8ff"; txt = "Verfügbar"; break;
                case QuestState.FAILED: hex = "#ff6a5a"; txt = "Fehlgeschlagen"; break;
                default: hex = "#9aa0ac"; txt = state.ToString(); break;
            }

            return $"<color={hex}><b>[{txt}]</b></color>";
        }

        // ---------------- Kleinkram ----------------

        private static void MakeInfo(RectTransform content, string text)
        {
            Text t = UiFactory.Label("Info", content, text, 13, UiFactory.TextDim, TextAnchor.MiddleLeft);
            UiFactory.Sizing(t.gameObject, 28f);
        }

        private static void LeftAlign(Button button, float leftPad)
        {
            Transform textTransform = button.transform.Find("Text");
            if (textTransform == null)
            {
                return;
            }

            var t = textTransform.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            UiFactory.FullStretch(t.rectTransform, leftPad, 2f, 8f, 2f);
        }

        private static void Clear(RectTransform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
