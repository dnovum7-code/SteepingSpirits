using UnityEngine;
using SteepingSpirits.Interaction;

namespace SteepingSpirits.Dialogue
{
    /// <summary>
    /// Einfaches „Ansprechen → Text"-Objekt: Schilder, Briefkästen, Grabsteine,
    /// stumme Dorfbewohner. Braucht einen Collider2D (Trigger reicht).
    /// Für NPCs mit Quests stattdessen QuestNpc benutzen.
    /// </summary>
    public class InteractableDialogue : MonoBehaviour, IInteractable, IInteractLabel
    {
        [SerializeField] private string speakerName = "Schild";
        [TextArea] [SerializeField] private string[] lines = { "..." };
        [SerializeField] private string interactLabel = "Lesen";
        [SerializeField] private Sprite portrait;

        public string InteractLabel => interactLabel;

        /// <summary>Konfiguration aus Code (z.B. Test-Szene).</summary>
        public void Configure(string speakerName, string[] lines, string interactLabel = "Lesen")
        {
            this.speakerName = speakerName;
            this.lines = lines;
            this.interactLabel = interactLabel;
        }

        public void Interact()
        {
            DialogueHUD.Instance.Play(speakerName, lines, null, portrait);
        }
    }
}
