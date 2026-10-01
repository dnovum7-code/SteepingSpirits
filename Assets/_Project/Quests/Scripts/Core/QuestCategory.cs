namespace SteepingSpirits.Quests
{
    /// <summary>
    /// Quest-Kategorie gemäss Namenskonvention Q_[KATEGORIE]_[Name].
    /// </summary>
    public enum QuestCategory
    {
        MAIN,      // Hauptstory-Quest
        SIDE,      // Nebenquest
        DAILY,     // Tägliche Wiederholungsquest
        TUTORIAL,  // Einführungsquest
        EVENT,     // Zeitlich begrenzte Event-Quest
        // Neue Werte immer HINTEN anhängen, damit gespeicherte Assets ihre
        // Zahlenwerte behalten (Enum wird als Index serialisiert).
        WORLD,     // Weltquest (offene Welt, ortsgebunden)
        FACTION    // Fraktions-/Gildenquest (Ruf, Zugehörigkeit)
    }
}
