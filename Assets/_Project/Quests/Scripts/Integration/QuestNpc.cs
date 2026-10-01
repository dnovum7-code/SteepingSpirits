using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Dialogue;
using SteepingSpirits.Interaction;
using SteepingSpirits.Quests.Steps;

namespace SteepingSpirits.Quests.Integration
{
    /// <summary>
    /// NPC-Controller &amp; -Bridge (das „NPC.cs" des Projekts, aus Everdawn
    /// übernommen und auf 2D umgestellt). Fasst alles NPC-seitige zusammen:
    ///  - Identität: npcID (Anker) + displayName (Namensschild über dem Kopf).
    ///  - Interaktion: ansprechbar über den Interactor2D (Spieler, Taste E).
    ///  - Dialog (DialogueHUD), Quest-Vergabe (wenn AVAILABLE).
    ///  - Meldet GameplayEvents.NpcTalkedTo; schliesst Bring-Objectives ab.
    ///  - „!"/„?"-Marker (QuestMarkerHUD) und Blick zum Spieler (Sprite spiegeln).
    ///
    /// Braucht einen Collider2D (Trigger reicht, etwas grösser als die Figur).
    /// Bewusst NICHT enthalten: Ziel-/Input-Logik – die lebt einmalig auf dem
    /// Spieler (Interactor2D), nicht pro NPC.
    ///
    /// NPC-ID: leeres Feld → QuestTarget → GameObject-Name. Für „sprich mit
    /// genau diesem NPC" das GameObject in einen QuestTargetLink ziehen.
    /// </summary>
    public class QuestNpc : MonoBehaviour, IInteractable, IInteractLabel
    {
        [Tooltip("Leer lassen = QuestTarget-Id bzw. GameObject-Name")]
        [SerializeField] private string npcID;

        [Tooltip("Optional: Quest die dieser NPC vergibt (questID)")]
        [SerializeField] private string questIDToStart;

        [Tooltip("Quest-Zeilen: Angebot bzw. beim Abgeben/Ansprechen im Quest-Kontext")]
        [TextArea] [SerializeField] private string[] dialogueLines;

        [Tooltip("Standard-Zeilen ohne aktive Quest (z.B. Smalltalk). Werden von Quests überschrieben")]
        [TextArea] [SerializeField] private string[] idleLines;

        [Tooltip("Name der über dem Dialog angezeigt wird")]
        [SerializeField] private string speakerName = "NPC";

        [Tooltip("Optional: Portrait im Dialogfenster")]
        [SerializeField] private Sprite portrait;

        [Header("Identität / Namensschild")]
        [Tooltip("Name über dem Kopf. Leer = speakerName")]
        [SerializeField] private string displayName;

        [SerializeField] private bool showNameplate = true;

        [Tooltip("Höhe des Namensschilds über dem NPC-Ursprung (Einheiten)")]
        [SerializeField] private float nameplateHeight = 0.95f;

        [Tooltip("Namensschild nur zeigen, wenn der Spieler näher ist (Einheiten)")]
        [SerializeField] private float nameplateRange = 6f;

        [Header("Verhalten")]
        [Tooltip("NPC schaut zum Spieler (Sprite wird gespiegelt), wenn dieser nah ist")]
        [SerializeField] private bool facePlayer = true;
        [SerializeField] private float faceRange = 3f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool spriteFacesRight = true;

        [Tooltip("Statt nur das Sprite das ganze Objekt spiegeln (inkl. Kinder wie Hut, Werkzeug)")]
        [SerializeField] private bool flipWholeObject;

        private GUIStyle nameplateStyle;

        /// <summary>Verb für den Interaktions-Prompt („[E] Reden").</summary>
        public string InteractLabel => "Reden";

        /// <summary>Anzeigename (Namensschild/Dialog): displayName, sonst speakerName.</summary>
        public string DisplayName =>
            !string.IsNullOrEmpty(displayName) ? displayName : speakerName;

        /// <summary>Spiegeln über transform.localScale.x statt SpriteRenderer.flipX.</summary>
        public bool FlipWholeObject
        {
            get => flipWholeObject;
            set => flipWholeObject = value;
        }

        /// <summary>Die aufgelöste NPC-ID (Feld → QuestTarget → GameObject-Name).</summary>
        public string NpcID => ResolveId();

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (facePlayer)
            {
                FaceNearbyPlayer();
            }
        }

        private void FaceNearbyPlayer()
        {
            Transform player = PlayerLocator.Find();
            if (player == null || (spriteRenderer == null && !flipWholeObject))
            {
                return;
            }

            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f || ((Vector2)(player.position - transform.position)).sqrMagnitude > faceRange * faceRange)
            {
                return;
            }

            bool left = dx < 0f;
            bool flip = spriteFacesRight ? left : !left;
            if (flipWholeObject)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (flip ? -1f : 1f);
                transform.localScale = scale;
            }
            else
            {
                spriteRenderer.flipX = flip;
            }
        }

        /// <summary>Konfiguration aus Code (z.B. für die Test-Szene).</summary>
        public void Configure(string npcID, string questIDToStart, string[] dialogueLines,
            string speakerName = null, string[] idleLines = null, string displayName = null)
        {
            this.npcID = npcID;
            this.questIDToStart = questIDToStart;
            this.dialogueLines = dialogueLines;
            this.idleLines = idleLines;
            if (!string.IsNullOrEmpty(speakerName))
            {
                this.speakerName = speakerName;
            }
            if (!string.IsNullOrEmpty(displayName))
            {
                this.displayName = displayName;
            }
        }

        private string ResolveId()
        {
            if (!string.IsNullOrEmpty(npcID))
            {
                return npcID;
            }

            QuestTarget target = GetComponent<QuestTarget>();
            return target != null ? target.Id : gameObject.name;
        }

        /// <summary>
        /// Liefert das Quest-Marker-Symbol über diesem NPC:
        ///  „!" (gelb)  = NPC bietet eine annehmbare Quest,
        ///  „?" (grün)  = NPC ist Ziel eines laufenden, offenen Objectives.
        /// Gibt false zurück, wenn kein Marker angezeigt werden soll.
        /// </summary>
        public bool TryGetMarker(out string glyph, out Color color)
        {
            glyph = null;
            color = Color.white;

            QuestManager manager = QuestManager.Instance;
            if (manager == null)
            {
                return false;
            }

            // „!" – dieser NPC vergibt eine Quest, die man annehmen kann.
            if (!string.IsNullOrEmpty(questIDToStart))
            {
                QuestInstance offered = manager.GetQuest(questIDToStart);
                if (offered != null && (offered.State == QuestState.AVAILABLE || offered.State == QuestState.ABANDONED))
                {
                    glyph = "!";
                    color = new Color(1f, 0.85f, 0.2f);
                    return true;
                }
            }

            // „?" – dieser NPC ist Ziel eines laufenden, noch offenen Objectives.
            if (ActiveTargetObjective() != null)
            {
                glyph = "?";
                color = new Color(0.5f, 0.95f, 0.5f);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Das NÄCHSTE offene Objective einer laufenden Quest, dessen Ziel dieser
        /// NPC ist (per ID oder verlinktem GameObject) – oder null. Bewusst nur
        /// der nächste Schritt: so zeigen „?"-Marker und Tracker dasselbe an.
        /// </summary>
        private ObjectiveData ActiveTargetObjective()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null)
            {
                return null;
            }

            string id = ResolveId();
            foreach (QuestInstance quest in manager.GetActiveQuests())
            {
                ObjectiveData next = FirstOpenObjective(quest);
                if (next == null)
                {
                    continue;
                }

                if (next.targetID == id || QuestTargetRegistry.IsTargetFor(next.objectiveID, gameObject))
                {
                    return next;
                }
            }

            return null;
        }

        /// <summary>Erstes noch offenes Objective der Quest (= nächster Schritt).</summary>
        private static ObjectiveData FirstOpenObjective(QuestInstance quest)
        {
            if (quest.Data.objectives == null)
            {
                return null;
            }

            foreach (ObjectiveData obj in quest.Data.objectives)
            {
                if (!quest.IsObjectiveComplete(obj.objectiveID))
                {
                    return obj;
                }
            }

            return null;
        }

        public void Interact()
        {
            string id = ResolveId();

            // Dialog ZUERST bestimmen – vor Talk-Meldung/Abgabe, sonst gilt das
            // Ziel schon als erledigt und der NPC würde nur noch Smalltalk sagen.
            string[] lines = BuildDialogueLines();

            GameplayEvents.NpcTalkedTo(id, gameObject);

            if (QuestManager.Instance != null)
            {
                TryDeliverBringObjectives(QuestManager.Instance, id);
            }

            // Mit Dialog: Quest erst nach der letzten Zeile starten.
            if (lines != null && lines.Length > 0)
            {
                DialogueHUD.Instance.Play(DisplayName, lines, StartQuestIfAvailable, portrait);
            }
            else
            {
                StartQuestIfAvailable();
            }
        }

        /// <summary>
        /// Baut die Dialogzeilen passend zum aktuellen Kontext:
        ///  - Quest-Geber: zustandsabhängig (Angebot / läuft / erledigt).
        ///  - Ziel-NPC: nennt die Aufgabe, wenn er gerade Ziel einer laufenden
        ///    Quest ist – sonst die Standard-/Idle-Zeilen.
        /// </summary>
        private string[] BuildDialogueLines()
        {
            QuestManager manager = QuestManager.Instance;

            // Quest-Geber (hat eine eigene Quest): Zustand entscheidet.
            if (manager != null && !string.IsNullOrEmpty(questIDToStart))
            {
                QuestInstance quest = manager.GetQuest(questIDToStart);
                if (quest != null)
                {
                    switch (quest.State)
                    {
                        case QuestState.ACTIVE:
                            return WithExtra("(Die Quest läuft schon – komm zurück, wenn du sie erledigt hast.)");
                        case QuestState.COMPLETED:
                            return quest.Data.isRepeatable
                                ? new[] { "Danke für heute! Morgen hab ich bestimmt wieder was für dich." }
                                : new[] { "Stark! Die Aufgabe ist erledigt – danke dir!" };
                        case QuestState.FAILED:
                            return new[] { "Schade, das ist leider misslungen." };
                        default:
                            return dialogueLines; // AVAILABLE / INACTIVE / ABANDONED → Quest anbieten
                    }
                }
            }

            // Ziel-NPC: nennt die Aufgabe des nächsten Schritts.
            ObjectiveData active = ActiveTargetObjective();
            if (active != null)
            {
                return BuildTargetLines(active);
            }

            // Sonst Standardantwort (Idle) – Fallback auf dialogueLines, falls keine gesetzt.
            return (idleLines != null && idleLines.Length > 0) ? idleLines : dialogueLines;
        }

        /// <summary>Charakter-Zeile(n) + die konkrete Aufgabe dieses NPCs.</summary>
        private string[] BuildTargetLines(ObjectiveData active)
        {
            string[] baseLines =
                (dialogueLines != null && dialogueLines.Length > 0) ? dialogueLines :
                (idleLines != null && idleLines.Length > 0) ? idleLines : new string[0];

            string task = !string.IsNullOrEmpty(active.description)
                ? active.description
                : active.stepType + " " + active.targetID;

            var result = new string[baseLines.Length + 1];
            System.Array.Copy(baseLines, result, baseLines.Length);
            result[baseLines.Length] = "(Aufgabe: " + task + ")";
            return result;
        }

        /// <summary>Hängt eine Zusatzzeile an die konfigurierten Dialogzeilen an.</summary>
        private string[] WithExtra(string extra)
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                return new[] { extra };
            }

            var result = new string[dialogueLines.Length + 1];
            System.Array.Copy(dialogueLines, result, dialogueLines.Length);
            result[dialogueLines.Length] = extra;
            return result;
        }

        private void StartQuestIfAvailable()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null || string.IsNullOrEmpty(questIDToStart))
            {
                return;
            }

            QuestInstance quest = manager.GetQuest(questIDToStart);
            if (quest != null && (quest.State == QuestState.AVAILABLE || quest.State == QuestState.ABANDONED))
            {
                manager.QuestStart(questIDToStart);
            }
        }

        /// <summary>
        /// Einfache Regel ohne Item-Übergabe: Wer den NPC anspricht, übergibt das
        /// Paket. Ausgelöst wird ein Bring-Objective, das diesen NPC per ID ODER
        /// per verlinktem GameObject als Ziel hat.
        /// </summary>
        private void TryDeliverBringObjectives(QuestManager manager, string id)
        {
            foreach (QuestInstance quest in manager.GetActiveQuests())
            {
                if (quest.Data.objectives == null)
                {
                    continue;
                }

                foreach (ObjectiveData objective in quest.Data.objectives)
                {
                    bool matches = objective.targetID == id
                                   || QuestTargetRegistry.IsTargetFor(objective.objectiveID, gameObject);

                    if (objective.stepType == QuestStepType.Bring
                        && matches
                        && !quest.IsObjectiveComplete(objective.objectiveID))
                    {
                        GameplayEvents.ItemDelivered(id, string.Empty, gameObject);
                        return;
                    }
                }
            }
        }

        // ---------------------------------------------------------------
        // Namensschild (Name über dem Kopf) – gehört zum NPC, kein extra HUD nötig.
        // ---------------------------------------------------------------
        private void OnGUI()
        {
            if (!showNameplate || DialogueHUD.IsActive)
            {
                return;
            }

            string label = DisplayName;
            Camera cam = Camera.main;
            if (string.IsNullOrEmpty(label) || cam == null)
            {
                return;
            }

            Transform player = PlayerLocator.Find();
            if (player != null &&
                ((Vector2)(player.position - transform.position)).sqrMagnitude > nameplateRange * nameplateRange)
            {
                return;
            }

            Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * nameplateHeight);
            if (sp.z <= 0f)
            {
                return; // hinter der Kamera
            }

            if (nameplateStyle == null)
            {
                nameplateStyle = GuiDraw.Rich(13, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                nameplateStyle.wordWrap = false;
            }

            var content = new GUIContent(label);
            Vector2 size = nameplateStyle.CalcSize(content);
            float w = size.x + 14f;
            float h = size.y + 4f;
            var rect = new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h, w, h);

            GuiDraw.Solid(rect, new Color(0f, 0f, 0f, 0.5f));
            GUI.Label(rect, content, nameplateStyle);
        }
    }
}
