using System;

namespace InventorySystem.Enums
{
    [Flags]
    public enum ItemProperty
    {
        None = 0,
        Consumable = 1 << 0,  // Can be eaten/drunk
        Edible = 1 << 1,      // Food items
        Drinkable = 1 << 2,   // Beverage items
        Equipable = 1 << 3,   // Can be equipped (future: weapons, armor)
        Stackable = 1 << 4,   // Allows multiple units in inventory
        QuestItem = 1 << 5    // Cannot be dropped/sold (future)
    }
}
