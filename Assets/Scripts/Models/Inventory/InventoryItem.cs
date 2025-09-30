using System;
using InventorySystem.Enums;

namespace InventorySystem.Models
{
    [Serializable]
    public struct InventoryItem
    {
        public string itemId;
        public ItemType itemType;

        public InventoryItem(string itemId, ItemType itemType)
        {
            this.itemId = itemId;
            this.itemType = itemType;
        }

        public static InventoryItem Empty()
        {
            return new InventoryItem(string.Empty, ItemType.Key);
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(itemId);
        }
    }
}