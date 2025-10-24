using System;

namespace InventorySystem.Models
{
    [Serializable]
    public struct AddItemResult
    {
        public bool success;
        public int quantityAdded;
        public int overflow;
        public string message;

        public AddItemResult(bool success, int quantityAdded, int overflow, string message = "")
        {
            this.success = success;
            this.quantityAdded = quantityAdded;
            this.overflow = overflow;
            this.message = message;
        }
    }
}
