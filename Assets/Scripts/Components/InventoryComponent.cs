using UnityEngine;
using InventorySystem.Enums;
using InventorySystem.Models;
using InventorySystem.Configuration;
using System.Collections.Generic;

namespace InventorySystem.Components
{
    public class InventoryComponent : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private InventoryConfiguration _configuration;

        [Header("Current Items")]
        [SerializeField]
        private InventoryItem _keySlot;

        [SerializeField]
        private InventoryItem _moneySlot;

        [SerializeField]
        private InventoryItem _appleSlot;

        [Header("UI References")]
        [SerializeField]
        private GameObject _appleUI;

        [SerializeField]
        private GameObject _moneyUI;

        [SerializeField]
        private GameObject _keyUI;

        private void Awake()
        {
            _keySlot = InventoryItem.Empty();
            _moneySlot = InventoryItem.Empty();
            _appleSlot = InventoryItem.Empty();
            
            UpdateAllUIStates();
        }

        public bool HasItem(ItemType itemType)
        {
            return GetSlot(itemType).IsValid();
        }

        public AddItemResult AddItem(string itemId, ItemType itemType, int quantity = 1)
        {
            if (_configuration == null)
            {
                return new AddItemResult(false, 0, quantity, "Configuration not set");
            }

            InventoryItem slot = GetSlot(itemType);
            int maxStack = _configuration.GetMaxStackSize(itemType);

            if (!slot.IsValid())
            {
                List<string> ids = new List<string> { itemId };
                SetSlot(itemType, new InventoryItem(itemType, quantity, ids));
                UpdateUIState(itemType);
                return new AddItemResult(true, quantity, 0);
            }

            if (!slot.CanAddMore(maxStack))
            {
                return new AddItemResult(false, 0, quantity, "Stack full");
            }

            int spaceAvailable = maxStack - slot.quantity;
            int actualAdded = Mathf.Min(quantity, spaceAvailable);
            int overflow = quantity - actualAdded;

            slot.quantity += actualAdded;
            slot.itemIds.Add(itemId);
            SetSlot(itemType, slot);
            UpdateUIState(itemType);

            return new AddItemResult(true, actualAdded, overflow);
        }

        public RemoveItemResult RemoveItem(ItemType itemType, int quantity = 1)
        {
            InventoryItem slot = GetSlot(itemType);

            if (!slot.IsValid())
            {
                return new RemoveItemResult(false, 0, null);
            }

            int actualRemoved = Mathf.Min(quantity, slot.quantity);
            List<string> removedIds = new List<string>();

            for (int i = 0; i < actualRemoved; i++)
            {
                if (slot.itemIds.Count > 0)
                {
                    removedIds.Add(slot.itemIds[0]);
                    slot.itemIds.RemoveAt(0);
                }
            }

            slot.quantity -= actualRemoved;

            if (slot.quantity <= 0)
            {
                SetSlot(itemType, InventoryItem.Empty());
            }
            else
            {
                SetSlot(itemType, slot);
            }

            UpdateUIState(itemType);
            return new RemoveItemResult(true, actualRemoved, removedIds);
        }

        public InventoryItem GetItem(ItemType itemType)
        {
            return GetSlot(itemType);
        }

        public void ClearInventory()
        {
            _keySlot = InventoryItem.Empty();
            _moneySlot = InventoryItem.Empty();
            _appleSlot = InventoryItem.Empty();
            
            UpdateAllUIStates();
        }

        public string GetInventoryDescription()
        {
            List<string> items = new List<string>();

            if (_keySlot.IsValid())
                items.Add($"Key x{_keySlot.quantity}");
            if (_moneySlot.IsValid())
                items.Add($"Money x{_moneySlot.quantity}");
            if (_appleSlot.IsValid())
                items.Add($"Apple x{_appleSlot.quantity}");

            return items.Count == 0 ? "Empty inventory" : string.Join(", ", items);
        }

        public int GetItemQuantity(ItemType itemType)
        {
            InventoryItem slot = GetSlot(itemType);
            return slot.IsValid() ? slot.quantity : 0;
        }

        private InventoryItem GetSlot(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Key:
                    return _keySlot;
                case ItemType.Money:
                    return _moneySlot;
                case ItemType.Apple:
                    return _appleSlot;
                default:
                    return InventoryItem.Empty();
            }
        }

        private void SetSlot(ItemType itemType, InventoryItem item)
        {
            switch (itemType)
            {
                case ItemType.Key:
                    _keySlot = item;
                    break;
                case ItemType.Money:
                    _moneySlot = item;
                    break;
                case ItemType.Apple:
                    _appleSlot = item;
                    break;
            }
        }

        private void UpdateUIState(ItemType itemType)
        {
            InventoryItem slot = GetSlot(itemType);
            GameObject uiObject = GetUIObject(itemType);
            bool hasItem = slot.IsValid();

            if (uiObject != null)
            {
                uiObject.SetActive(hasItem);
            }
        }

        private void UpdateAllUIStates()
        {
            UpdateUIState(ItemType.Key);
            UpdateUIState(ItemType.Money);
            UpdateUIState(ItemType.Apple);
        }

        private GameObject GetUIObject(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Key:
                    return _keyUI;
                case ItemType.Money:
                    return _moneyUI;
                case ItemType.Apple:
                    return _appleUI;
                default:
                    return null;
            }
        }
    }
}