using System;
using System.Collections.Generic;
using InventorySystem.Enums;

namespace InventorySystem.Models
{
    [Serializable]
    public class InventoryItem
    {
        public ItemType itemType;
        public int quantity;
        public List<string> itemIds;

        public InventoryItem(ItemType itemType, int quantity, List<string> itemIds)
        {
            this.itemType = itemType;
            this.quantity = quantity;
            this.itemIds = itemIds ?? new List<string>();
        }

        public static InventoryItem Empty()
        {
            return new InventoryItem(ItemType.Key, 0, new List<string>());
        }

        public bool IsValid()
        {
            return quantity > 0;
        }

        public bool CanAddMore(int maxStackSize)
        {
            return quantity < maxStackSize;
        }
    }
}