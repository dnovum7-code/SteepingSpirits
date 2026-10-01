namespace SteepingSpirits.Interaction
{
    /// <summary>
    /// Alles, womit der Spieler per Interaktionstaste (E) interagieren kann:
    /// NPCs, Schilder, Quest-Marker, später Truhen, Türen, Betten, Felder …
    /// Der Interactor2D auf dem Spieler sucht nach dieser Schnittstelle.
    /// (In Everdawn hiess sie IQuestInteractable.)
    /// </summary>
    public interface IInteractable
    {
        void Interact();
    }

    /// <summary>
    /// Optional zusätzlich zu <see cref="IInteractable"/>: kurzes Verb für den
    /// Prompt („[E] Reden", „[E] Lesen"). Ohne zeigt der Prompt nur „[E]".
    /// </summary>
    public interface IInteractLabel
    {
        string InteractLabel { get; }
    }
}
