using System;
using System.Collections.Generic;

namespace InventorySystem.Models
{
    [Serializable]
    public struct RemoveItemResult
    {
        public bool success;
        public int quantityRemoved;
        public List<string> removedItemIds;

        public RemoveItemResult(bool success, int quantityRemoved, List<string> removedItemIds)
        {
            this.success = success;
            this.quantityRemoved = quantityRemoved;
            this.removedItemIds = removedItemIds ?? new List<string>();
        }
    }
}
