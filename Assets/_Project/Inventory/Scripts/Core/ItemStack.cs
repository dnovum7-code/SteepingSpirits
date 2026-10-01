namespace SteepingSpirits.Inventory
{
    /// <summary>
    /// Ein Stapel gleicher Items in einem Inventar-Slot: Referenz auf die
    /// Definition + aktuelle Menge. Reines C#-Objekt (lebt im RAM).
    /// </summary>
    public class ItemStack
    {
        public ItemData Item { get; }
        public int Amount { get; private set; }

        public ItemStack(ItemData item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public bool IsFull => Amount >= Item.maxStack;
        public int SpaceLeft => Item.maxStack - Amount;

        /// <summary>Füllt den Stapel auf; gibt zurück, wie viel nicht mehr passte.</summary>
        public int Add(int amount)
        {
            int fits = System.Math.Min(amount, SpaceLeft);
            Amount += fits;
            return amount - fits;
        }

        /// <summary>Entnimmt bis zu amount; gibt die tatsächlich entnommene Menge zurück.</summary>
        public int Remove(int amount)
        {
            int taken = System.Math.Min(amount, Amount);
            Amount -= taken;
            return taken;
        }
    }
}
