using UnityEngine;
using SteepingSpirits.Core;
using SteepingSpirits.Core.UI;
using SteepingSpirits.Player;

namespace SteepingSpirits.Interaction
{
    /// <summary>
    /// Spieler-Komponente (ersetzt QuestInteractor aus Everdawn): sucht jeden
    /// Frame das beste <see cref="IInteractable"/> in Reichweite – bevorzugt
    /// das, wo der Spieler hinschaut – und zeigt unten „[E] …". Taste E (oder
    /// Gamepad A) ruft Interact() auf. Eine Komponente bedient alle NPCs.
    ///
    /// Interaktive Objekte brauchen einen Collider2D (gern als Trigger etwas
    /// grösser als die Figur).
    /// </summary>
    public class Interactor2D : MonoBehaviour
    {
        [SerializeField] private float interactRange = 1.3f;

        [Tooltip("Wie stark die Blickrichtung zählt (0 = nur Distanz)")]
        [SerializeField] private float facingBias = 0.6f;

        [SerializeField] private LayerMask interactLayers = ~0;

        [Tooltip("„[E] …\"-Hinweis einblenden, wenn ein Ziel in Reichweite ist")]
        [SerializeField] private bool showPrompt = true;

        private PlayerController2D controller;
        private IInteractable current;
        private Component currentComponent;
        private GUIStyle promptStyle;

        /// <summary>Das gerade ausgewählte Ziel (oder null).</summary>
        public IInteractable Current => current;

        private void Awake()
        {
            controller = GetComponent<PlayerController2D>();
        }

        private void Update()
        {
            if (GamePause.IsBlocked)
            {
                current = null;
                currentComponent = null;
                return;
            }

            FindTarget();

            if (current != null && GameInput.InteractPressed)
            {
                if (controller != null && currentComponent != null)
                {
                    controller.FaceTowards(currentComponent.transform.position);
                }

                current.Interact();
            }
        }

        /// <summary>Interaktion per Skript auslösen (z.B. Touch-Button).</summary>
        public void TryInteract()
        {
            if (GamePause.IsBlocked)
            {
                return;
            }

            FindTarget();
            current?.Interact();
        }

        private void FindTarget()
        {
            current = null;
            currentComponent = null;

            Vector2 origin = transform.position;
            Vector2 facing = controller != null ? controller.Facing : Vector2.down;
            float bestScore = float.MaxValue;

            foreach (Collider2D col in Physics2D.OverlapCircleAll(origin, interactRange, interactLayers))
            {
                if (col.transform.IsChildOf(transform))
                {
                    continue;
                }

                IInteractable interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    continue;
                }

                // Nächster Punkt des Colliders zählt (grosse Objekte wie Häuser).
                Vector2 closest = col.ClosestPoint(origin);
                Vector2 to = closest - origin;
                float distance = to.magnitude;
                float facingDot = distance > 0.001f ? Vector2.Dot(facing, to / distance) : 1f;
                float score = distance - facingDot * facingBias;

                if (score < bestScore)
                {
                    bestScore = score;
                    current = interactable;
                    currentComponent = interactable as Component;
                }
            }
        }

        private void OnGUI()
        {
            if (!showPrompt || current == null || GamePause.IsBlocked)
            {
                return;
            }

            string verb = current is IInteractLabel labelled ? labelled.InteractLabel : null;
            string key = GameInput.InteractKeyLabel;
            string text = string.IsNullOrEmpty(verb) ? $"[ {key} ]" : $"[ {key} ]   {verb}";

            if (promptStyle == null)
            {
                promptStyle = GuiDraw.Rich(16, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                promptStyle.wordWrap = false;
            }

            var content = new GUIContent(text);
            Vector2 size = promptStyle.CalcSize(content);
            float w = size.x + 28f;
            float h = size.y + 14f;
            var rect = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 70f, w, h);

            GuiDraw.Panel(rect, 0.8f);
            GUI.Label(rect, content, promptStyle);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
