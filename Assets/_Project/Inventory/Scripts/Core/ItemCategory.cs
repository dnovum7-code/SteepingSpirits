namespace SteepingSpirits.Inventory
{
    /// <summary>
    /// Grobe Einordnung eines Items – steuert Sortierung, Filter und was
    /// „Benutzen" bedeutet. Neue Kategorien hier ergänzen.
    /// </summary>
    public enum ItemCategory
    {
        Consumable,  // Trank, Essen … kann benutzt/verbraucht werden
        Equipment,   // Waffe, Rüstung
        Material,    // Crafting-Zutat (z.B. Heilkraut)
        Quest,       // wird von einer Quest benötigt
        Currency,    // Gold, Münzen
        Misc         // alles andere
    }
}
