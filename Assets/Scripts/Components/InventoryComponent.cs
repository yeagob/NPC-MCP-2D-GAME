using UnityEngine;
using InventorySystem.Enums;
using InventorySystem.Models;
using MapSystem.Elements;

namespace InventorySystem.Components
{
    public class InventoryComponent : MonoBehaviour
    {
        [Header("Current Items")]
        [SerializeField] private InventoryItem keySlot;
        [SerializeField] private InventoryItem moneySlot;
        [SerializeField] private InventoryItem appleSlot;

        private void Awake()
        {
            keySlot = InventoryItem.Empty();
            moneySlot = InventoryItem.Empty();
            appleSlot = InventoryItem.Empty();
        }

        public bool HasItem(ItemType itemType)
        {
            return GetSlot(itemType).IsValid();
        }

        public bool AddItem(string itemId, ItemType itemType, ItemElement item)
        {
            if (HasItem(itemType))
            {
                return false;
            }

            SetSlot(itemType, new InventoryItem(itemId, itemType, item));
            return true;
        }

        public bool RemoveItem(ItemType itemType)
        {
            if (!HasItem(itemType))
            {
                return false;
            }

            SetSlot(itemType, InventoryItem.Empty());
            return true;
        }

        public InventoryItem GetItem(ItemType itemType)
        {
            return GetSlot(itemType);
        }

        public void ClearInventory()
        {
            keySlot = InventoryItem.Empty();
            moneySlot = InventoryItem.Empty();
            appleSlot = InventoryItem.Empty();
        }

        public string GetInventoryDescription()
        {
            System.Collections.Generic.List<string> items = new System.Collections.Generic.List<string>();

            if (keySlot.IsValid()) items.Add("Key");
            if (moneySlot.IsValid()) items.Add("Money");
            if (appleSlot.IsValid()) items.Add("Apple");

            return items.Count == 0 ? "Empty inventory" : string.Join(", ", items);
        }

        private InventoryItem GetSlot(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Key:
                    return keySlot;
                case ItemType.Money:
                    return moneySlot;
                case ItemType.Apple:
                    return appleSlot;
                default:
                    return InventoryItem.Empty();
            }
        }

        private void SetSlot(ItemType itemType, InventoryItem item)
        {
            switch (itemType)
            {
                case ItemType.Key:
                    keySlot = item;
                    break;
                case ItemType.Money:
                    moneySlot = item;
                    break;
                case ItemType.Apple:
                    appleSlot = item;
                    break;
            }
        }
    }
}