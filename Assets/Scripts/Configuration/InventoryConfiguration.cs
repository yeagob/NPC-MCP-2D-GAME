using UnityEngine;
using InventorySystem.Enums;

namespace InventorySystem.Configuration
{
    [CreateAssetMenu(fileName = "InventoryConfiguration", menuName = "Game/Configuration/Inventory")]
    public class InventoryConfiguration : ScriptableObject
    {
        [Header("Stack Limits")]
        [SerializeField] private int keyMaxStack = 5;
        [SerializeField] private int moneyMaxStack = 999;
        [SerializeField] private int appleMaxStack = 20;

        [Header("UI Settings")]
        [SerializeField] private bool showQuantityAlways = false;
        [SerializeField] private string quantityFormat = "x{0}";

        public bool ShowQuantityAlways => showQuantityAlways;
        public string QuantityFormat => quantityFormat;

        public int GetMaxStackSize(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Key => keyMaxStack,
                ItemType.Money => moneyMaxStack,
                ItemType.Apple => appleMaxStack,
                _ => 1
            };
        }
    }
}
