using System;
using InventorySystem.Enums;
using MapSystem.Elements;

namespace InventorySystem.Models
{
    [Serializable]
    public struct InventoryItem
    {
        public string itemId;
        public ItemElement itemElement;
        public ItemType itemType;

        public InventoryItem(string itemId, ItemType itemType, ItemElement itemElement)
        {
            this.itemId = itemId;
            this.itemType = itemType;
            this.itemElement = itemElement;
        }

        public static InventoryItem Empty()
        {
            return new InventoryItem(string.Empty, ItemType.Key, null);
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(itemId);
        }
    }
}