namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Lebenszyklus einer Quest.
    /// </summary>
    public enum QuestState
    {
        INACTIVE,    // Quest existiert, wurde aber noch nicht gestartet
        AVAILABLE,   // Quest sichtbar auf Karte, noch nicht angenommen
        ACTIVE,      // Quest läuft (QuestStep In Progress)
        COMPLETED,   // Quest erfolgreich abgeschlossen
        FAILED,      // Bedingung nicht erfüllt (z.B. Timer abgelaufen, NPC gestorben)
        ABANDONED    // Spieler hat Quest manuell abgebrochen
    }
}
