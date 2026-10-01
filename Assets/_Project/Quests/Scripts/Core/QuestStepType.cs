namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Verfügbare Step-Templates. Neue Typen hier ergänzen und in
    /// QuestStepFactory einen passenden Step registrieren.
    /// </summary>
    public enum QuestStepType
    {
        Collect,  // Sammle X (targetID = Item-ID)
        Defeat,   // Besiege X (targetID = Enemy-ID)
        Visit,    // Besuche X (targetID = Location-ID)
        Bring,    // Bringe etwas zu X (targetID = NPC-ID)
        Talk      // Sprich mit X (targetID = NPC-ID)
    }
}
