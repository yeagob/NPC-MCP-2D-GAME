using UnityEngine;
using UISystem.Enums;
using InventorySystem.Configuration;

namespace GameSystem.Configuration
{
    [CreateAssetMenu(fileName = "GameplayConfiguration", menuName = "Game/GameplayConfiguration")]
    public class GameplayConfiguration : ScriptableObject
    {
        [Header("Context Management")]
        [Tooltip("Maximum number of sliding messages in context history (50-100 recommended)")]
        public int maxContextSlidingMessages = 50;

        [Tooltip("Maximum number of pinned messages (system prompts, game rules)")]
        public int maxContextPinnedMessages = 10;

        [Header("Action Points")]
        [Tooltip("Action point cost for flip action")]
        public int flipActionPointCost = 1;

        [Tooltip("Action point cost for eat action")]
        public int eatActionPointCost = 1;

        [Tooltip("Action point cost for end turn (0 = free action)")]
        public int endTurnActionPointCost = 0;

        [Header("Animation")]
        [Tooltip("Movement speed in cells per second")]
        public float movementAnimationSpeed = 5.0f;

        [Tooltip("Enable smooth movement animation between cells")]
        public bool enableMovementAnimation = true;

        [Header("Logging")]
        [Tooltip("Maximum number of log entries to retain (50 recommended)")]
        public int maxLogEntries = 50;

        [Tooltip("Auto-scroll log panel to most recent entry")]
        public bool autoScrollLogs = true;

        [Tooltip("Color for System category logs")]
        public Color logColorSystem = Color.gray;

        [Tooltip("Color for Action category logs")]
        public Color logColorAction = Color.yellow;

        [Tooltip("Color for Combat category logs")]
        public Color logColorCombat = Color.red;

        [Tooltip("Color for Inventory category logs")]
        public Color logColorInventory = Color.green;

        [Tooltip("Color for Dialogue category logs")]
        public Color logColorDialogue = Color.cyan;

        [Tooltip("Color for AI category logs")]
        public Color logColorAI = Color.magenta;

        [Header("Consumables")]
        [Tooltip("Configuration for consumable items")]
        public ConsumableConfiguration consumableConfiguration;

        public Color GetLogColor(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.System:
                    return logColorSystem;
                case LogCategory.Action:
                    return logColorAction;
                case LogCategory.Combat:
                    return logColorCombat;
                case LogCategory.Inventory:
                    return logColorInventory;
                case LogCategory.Dialogue:
                    return logColorDialogue;
                case LogCategory.AI:
                    return logColorAI;
                default:
                    return Color.white;
            }
        }
    }
}
