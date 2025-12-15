# Data Model & Entity Design

**Feature**: Context System Refactoring and Gameplay Enhancements
**Branch**: `001-context-refactor-features`
**Date**: 2025-10-27
**Status**: Design Complete

## Overview

This document defines all data structures, entity relationships, state machines, and data flow patterns for the context refactoring and gameplay enhancement features.

All entities follow POCO (Plain Old CLR Object) philosophy per CLAUDE.md guidelines: simple data structures with minimal logic, explicit typing, and configuration-driven behavior.

---

## 1. Context Management Entities

### 1.1 Message (MODIFIED)

**Purpose**: Atomic unit of context history representing a single communication or event in the game.

**File**: `Assets/Scripts/Models/Context/Message.cs`

```csharp
using System;

namespace ChatSystem.Models
{
    [Serializable]
    public class Message
    {
        public MessageRole role;
        public string content;
        public bool isPinned;           // NEW: Prevents removal by sliding window
        public string toolCallId;       // NEW: Links tool responses to requests
        public long timestamp;          // NEW: Unix timestamp for chronological ordering
        public MessageType messageType; // NEW: Categorization (Combat, Inventory, etc.)

        public Message(MessageRole role, string content)
        {
            this.role = role;
            this.content = content;
            this.isPinned = false;
            this.toolCallId = null;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.messageType = MessageType.General;
        }

        public Message(MessageRole role, string content, bool isPinned)
        {
            this.role = role;
            this.content = content;
            this.isPinned = isPinned;
            this.toolCallId = null;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.messageType = MessageType.General;
        }
    }
}
```

**Fields**:
- `role`: System, User, Assistant, or Tool (determines LLM API behavior)
- `content`: Text payload (JSON for structured data, plain text for dialogue)
- `isPinned`: If true, message is never removed by sliding window (system prompts, game rules)
- `toolCallId`: UUID linking tool responses to original tool call (for LLM context tracking)
- `timestamp`: Unix timestamp for chronological ordering and debugging
- `messageType`: Categorization for filtering/analytics (Combat, Inventory, Dialogue, etc.)

**Relationships**:
- Owned by: ConversationContext (1:N)
- Referenced by: ContextManager (for serialization/deserialization)

---

### 1.2 MessageRole (MODIFIED)

**Purpose**: Enum defining message sender/purpose for LLM API compatibility.

**File**: `Assets/Scripts/Enums/MessageRole.cs`

```csharp
namespace ChatSystem.Enums
{
    public enum MessageRole
    {
        System,    // Game rules, character personality, permanent instructions
        User,      // Player actions, NPC action requests
        Assistant, // LLM responses, NPC decisions
        Tool       // NEW: Tool execution results (attack damage, item pickup, etc.)
    }
}
```

**Usage**:
- `System`: Pinned messages (never removed); sent at conversation start
- `User`: Turn-specific requests ("What do you do?" prompts)
- `Assistant`: LLM-generated NPC decisions and dialogue
- `Tool`: Results from MCP tool executions (added to context for follow-up synthesis)

---

### 1.3 MessageType (NEW)

**Purpose**: Categorize messages for filtering, analytics, and debugging.

**File**: `Assets/Scripts/Enums/MessageType.cs`

```csharp
namespace ChatSystem.Enums
{
    public enum MessageType
    {
        General,      // Uncategorized messages
        Combat,       // Attack results, damage dealt, deaths
        Inventory,    // Item pickup, drop, give, consumption
        Dialogue,     // NPC-to-NPC or NPC-to-player conversations
        Movement,     // Teleport, flip, position changes
        System,       // Game state changes, turn start/end
        AI            // LLM reasoning, tool selection logs
    }
}
```

---

### 1.4 ConversationContext (MODIFIED)

**Purpose**: Container for a single character's conversation history with message retention logic.

**File**: `Assets/Scripts/Models/Context/ConversationContext.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatSystem.Models
{
    [Serializable]
    public class ConversationContext
    {
        public string conversationId;
        public string participantId; // CharacterElement.Id
        public long createdAt;       // Unix timestamp

        private List<Message> pinnedMessages;
        private Queue<Message> slidingMessages;
        private int maxSlidingMessages;

        public ConversationContext(string conversationId, string participantId, int maxSlidingMessages)
        {
            this.conversationId = conversationId;
            this.participantId = participantId;
            this.createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.maxSlidingMessages = maxSlidingMessages;
            this.pinnedMessages = new List<Message>();
            this.slidingMessages = new Queue<Message>();
        }

        public void AddMessage(Message message)
        {
            if (message.isPinned)
            {
                pinnedMessages.Add(message);
            }
            else
            {
                if (slidingMessages.Count >= maxSlidingMessages)
                {
                    slidingMessages.Dequeue(); // Remove oldest
                }
                slidingMessages.Enqueue(message);
            }
        }

        public List<Message> GetAllMessages()
        {
            List<Message> all = new List<Message>(pinnedMessages);
            all.AddRange(slidingMessages);
            return all.OrderBy(m => m.timestamp).ToList(); // Chronological order
        }

        public int GetTotalMessageCount()
        {
            return pinnedMessages.Count + slidingMessages.Count;
        }

        public void ClearSlidingMessages()
        {
            slidingMessages.Clear();
        }
    }
}
```

**Key Behaviors**:
- **Pinned messages**: Stored in List (arbitrary order, never removed)
- **Sliding messages**: Stored in Queue (FIFO, oldest removed when limit exceeded)
- **GetAllMessages()**: Returns chronological merge of pinned + sliding

**State Transitions**:
```
[Empty] --AddMessage(pinned=true)--> [Has Pinned]
[Has Pinned] --AddMessage(pinned=false)--> [Has Pinned + Sliding]
[Sliding Full] --AddMessage(pinned=false)--> [Oldest Dequeued, New Enqueued]
```

---

### 1.5 ContextManager (MODIFIED)

**Purpose**: Global service managing all character conversations and context operations.

**File**: `Assets/Scripts/Services/Context/ContextManager.cs`

**Key Methods** (new/modified):

```csharp
public class ContextManager
{
    private Dictionary<string, ConversationContext> conversations;
    private GameplayConfiguration configuration;

    // NEW: Add system context (pinned message)
    public void AddSystemContextAsync(string characterId, string content, MessageType type)
    {
        ConversationContext context = GetOrCreateContext(characterId);
        Message message = new Message(MessageRole.System, content, isPinned: true);
        message.messageType = type;
        context.AddMessage(message);
    }

    // MODIFIED: Add user message (sliding)
    public void AddUserMessage(string characterId, string content, MessageType type)
    {
        ConversationContext context = GetOrCreateContext(characterId);
        Message message = new Message(MessageRole.User, content, isPinned: false);
        message.messageType = type;
        context.AddMessage(message);
    }

    // NEW: Add tool result
    public void AddToolResult(string characterId, string toolCallId, string result, MessageType type)
    {
        ConversationContext context = GetOrCreateContext(characterId);
        Message message = new Message(MessageRole.Tool, result, isPinned: false);
        message.toolCallId = toolCallId;
        message.messageType = type;
        context.AddMessage(message);
    }

    // NEW: Get full context for LLM request
    public List<Message> GetMessagesForLLM(string characterId)
    {
        ConversationContext context = GetOrCreateContext(characterId);
        return context.GetAllMessages(); // Chronologically ordered
    }

    private ConversationContext GetOrCreateContext(string characterId)
    {
        if (!conversations.ContainsKey(characterId))
        {
            int maxSliding = configuration.maxContextSlidingMessages;
            conversations[characterId] = new ConversationContext(
                Guid.NewGuid().ToString(),
                characterId,
                maxSliding
            );
        }
        return conversations[characterId];
    }
}
```

**Data Flow**:
```
GameEvent → ContextManager.AddUserMessage → ConversationContext.AddMessage → Queue/List
CharacterAgent.ExecuteTurnAsync() → ContextManager.GetMessagesForLLM → LLMRequest.messages
```

---

## 2. Consumable System Entities

### 2.1 ConsumableEffect (NEW)

**Purpose**: Enum defining types of effects consumable items can apply.

**File**: `Assets/Scripts/Enums/ConsumableEffect.cs`

```csharp
namespace InventorySystem.Enums
{
    public enum ConsumableEffect
    {
        None,
        RestoreHealth,    // Increases HealthPoints up to maximum
        RestoreMana,      // Future: mana restoration (out of scope Phase 1)
        ApplyBuff,        // Future: temporary stat boosts (out of scope)
        RemoveDebuff      // Future: cure poison, etc. (out of scope)
    }
}
```

---

### 2.2 ItemProperty (NEW)

**Purpose**: Flags defining item capabilities/classifications.

**File**: `Assets/Scripts/Enums/ItemProperty.cs`

```csharp
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
```

**Usage**:
```csharp
ItemProperty appleProperties = ItemProperty.Consumable | ItemProperty.Edible | ItemProperty.Stackable;
if ((appleProperties & ItemProperty.Consumable) != 0)
{
    // Item can be consumed
}
```

---

### 2.3 ConsumableData (NEW)

**Purpose**: Configuration structure defining consumable item effects and metadata.

**File**: `Assets/Scripts/Models/Inventory/ConsumableData.cs`

```csharp
using System;
using InventorySystem.Enums;

namespace InventorySystem.Models
{
    [Serializable]
    public class ConsumableData
    {
        public ItemType itemType;
        public ConsumableEffect effect;
        public int value;                // HP restored, mana restored, etc.
        public string consumeMessage;    // Logged to context and UI
        public float consumeDelay;       // Animation delay before effect applies (future)

        public ConsumableData(ItemType itemType, ConsumableEffect effect, int value, string message)
        {
            this.itemType = itemType;
            this.effect = effect;
            this.value = value;
            this.consumeMessage = message;
            this.consumeDelay = 0f;
        }
    }
}
```

**Example Configuration** (via ScriptableObject):
```yaml
Apple:
  itemType: Apple
  effect: RestoreHealth
  value: 20
  consumeMessage: "You ate an apple and restored 20 HP"
  consumeDelay: 0.5
```

---

### 2.4 ItemElement (MODIFIED)

**Purpose**: Represents a physical item on the game map with consumable properties.

**File**: `Assets/Scripts/Map/Elements/ItemElement.cs`

**New Fields**:
```csharp
public class ItemElement : MapElement
{
    public ItemType ItemType;
    public ItemProperty properties; // NEW: Consumable, Edible, etc.

    // NEW: Check if item is consumable
    public bool IsConsumable()
    {
        return (properties & ItemProperty.Consumable) != 0;
    }

    public bool IsEdible()
    {
        return (properties & ItemProperty.Edible) != 0;
    }
}
```

---

### 2.5 ConsumeItemResult (NEW)

**Purpose**: Result struct for consume item operations.

**File**: `Assets/Scripts/Models/Inventory/ConsumeItemResult.cs`

```csharp
using System;

namespace InventorySystem.Models
{
    [Serializable]
    public struct ConsumeItemResult
    {
        public bool success;
        public int healthRestored;
        public int newHealthValue;
        public string message;
        public ItemType consumedItem;

        public ConsumeItemResult(bool success, int healthRestored, int newHealth, string message, ItemType item)
        {
            this.success = success;
            this.healthRestored = healthRestored;
            this.newHealthValue = newHealth;
            this.message = message;
            this.consumedItem = item;
        }
    }
}
```

---

## 3. Logging System Entities

### 3.1 LogCategory (NEW)

**Purpose**: Enum classifying log events for color coding and filtering.

**File**: `Assets/Scripts/Enums/LogCategory.cs`

```csharp
namespace UISystem.Enums
{
    public enum LogCategory
    {
        System,      // Game state, turn changes, errors
        Action,      // Player/NPC actions (move, flip, end turn)
        Combat,      // Attacks, damage, deaths
        Inventory,   // Pickup, drop, give, consume
        Dialogue,    // NPC conversations, player talk
        AI           // LLM tool calls, reasoning (debug mode)
    }
}
```

---

### 3.2 LogEntry (NEW)

**Purpose**: Data structure representing a single log event.

**File**: `Assets/Scripts/Models/UI/LogEntry.cs`

```csharp
using System;
using UISystem.Enums;
using UnityEngine;

namespace UISystem.Models
{
    [Serializable]
    public class LogEntry
    {
        public LogCategory category;
        public string message;
        public long timestamp;        // Unix timestamp
        public Color color;           // Assigned from GameplayConfiguration
        public GameObject gameObject; // Reference to UI element (for pooling)

        public LogEntry(LogCategory category, string message, Color color)
        {
            this.category = category;
            this.message = message;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.color = color;
            this.gameObject = null;
        }

        public string GetFormattedMessage()
        {
            DateTime dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            return $"[{dt:HH:mm:ss}] [{category}] {message}";
        }
    }
}
```

---

## 4. Animation Entities

### 4.1 MovementAnimator (NEW)

**Purpose**: Service class handling smooth character movement interpolation.

**File**: `Assets/Scripts/Services/Animation/MovementAnimator.cs`

```csharp
using System.Threading.Tasks;
using Grid.Models.Grid;
using MapSystem.Elements;
using UnityEngine;

namespace AnimationSystem.Services
{
    public class MovementAnimator
    {
        private GridSystem gridSystem;
        private GameplayConfiguration configuration;
        public bool isAnimating { get; private set; }

        public MovementAnimator(GridSystem gridSystem, GameplayConfiguration configuration)
        {
            this.gridSystem = gridSystem;
            this.configuration = configuration;
            this.isAnimating = false;
        }

        public async Task AnimateMovementAsync(CharacterElement character, GridCell targetCell)
        {
            isAnimating = true;

            Vector3 startPosition = character.transform.position;
            Vector3 endPosition = gridSystem.GetWorldPosition(targetCell);
            float distance = Vector3.Distance(startPosition, endPosition);
            float duration = distance / configuration.movementAnimationSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                character.transform.position = Vector3.Lerp(startPosition, endPosition, t);

                await Task.Yield(); // Yield to next frame
            }

            character.transform.position = endPosition; // Ensure exact positioning
            isAnimating = false;
        }
    }
}
```

**State Machine**:
```
[Idle] --AnimateMovementAsync()-->  [Animating]
[Animating] --duration elapsed-->   [Idle]
[Animating] --exception-->          [Idle] (with error log)
```

---

## 5. Configuration Entities

### 5.1 GameplayConfiguration (NEW)

**Purpose**: ScriptableObject consolidating all gameplay constants.

**File**: `Assets/Scripts/Configuration/GameplayConfiguration.cs`

```csharp
using UnityEngine;

namespace GameSystem.Configuration
{
    [CreateAssetMenu(fileName = "GameplayConfiguration", menuName = "Game/GameplayConfiguration")]
    public class GameplayConfiguration : ScriptableObject
    {
        [Header("Context Management")]
        public int maxContextSlidingMessages = 50;
        public int maxContextPinnedMessages = 10;

        [Header("Action Points")]
        public int flipActionPointCost = 1;
        public int eatActionPointCost = 1;
        public int endTurnActionPointCost = 0;

        [Header("Animation")]
        public float movementAnimationSpeed = 5.0f; // cells per second
        public bool enableMovementAnimation = true;

        [Header("Logging")]
        public int maxLogEntries = 50;
        public bool autoScrollLogs = true;
        public Color logColorSystem = Color.gray;
        public Color logColorAction = Color.yellow;
        public Color logColorCombat = Color.red;
        public Color logColorInventory = Color.green;
        public Color logColorDialogue = Color.cyan;
        public Color logColorAI = Color.magenta;

        [Header("Consumables")]
        public ConsumableConfiguration consumableConfiguration;

        public Color GetLogColor(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.System: return logColorSystem;
                case LogCategory.Action: return logColorAction;
                case LogCategory.Combat: return logColorCombat;
                case LogCategory.Inventory: return logColorInventory;
                case LogCategory.Dialogue: return logColorDialogue;
                case LogCategory.AI: return logColorAI;
                default: return Color.white;
            }
        }
    }
}
```

---

### 5.2 ConsumableConfiguration (NEW)

**Purpose**: ScriptableObject mapping ItemType to ConsumableData.

**File**: `Assets/Scripts/Configuration/ConsumableConfiguration.cs`

```csharp
using System.Collections.Generic;
using InventorySystem.Enums;
using InventorySystem.Models;
using UnityEngine;

namespace InventorySystem.Configuration
{
    [System.Serializable]
    public class ItemTypeConsumableDataPair
    {
        public ItemType itemType;
        public ConsumableData data;
    }

    [CreateAssetMenu(fileName = "ConsumableConfiguration", menuName = "Inventory/ConsumableConfiguration")]
    public class ConsumableConfiguration : ScriptableObject
    {
        public List<ItemTypeConsumableDataPair> consumables;

        private Dictionary<ItemType, ConsumableData> lookup;

        public ConsumableData GetConsumableData(ItemType itemType)
        {
            if (lookup == null)
            {
                BuildLookup();
            }
            lookup.TryGetValue(itemType, out ConsumableData data);
            return data;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<ItemType, ConsumableData>();
            foreach (ItemTypeConsumableDataPair pair in consumables)
            {
                lookup[pair.itemType] = pair.data;
            }
        }

        private void OnValidate()
        {
            lookup = null; // Rebuild on next access

            // Validate for duplicates
            HashSet<ItemType> seen = new HashSet<ItemType>();
            foreach (ItemTypeConsumableDataPair pair in consumables)
            {
                if (seen.Contains(pair.itemType))
                {
                    Debug.LogError($"[ConsumableConfiguration] Duplicate entry for {pair.itemType}");
                }
                seen.Add(pair.itemType);
            }
        }
    }
}
```

---

## 6. Entity Relationship Diagram

```
                          ┌─────────────────────┐
                          │  GameplayConfig     │
                          │  (ScriptableObject) │
                          └──────────┬──────────┘
                                     │ configures
                      ┌──────────────┴──────────────┐
                      │                             │
          ┌───────────▼──────────┐      ┌──────────▼────────────┐
          │  ContextManager      │      │  ConsumableConfig     │
          │  (Singleton Service) │      │  (ScriptableObject)   │
          └───────────┬──────────┘      └──────────┬────────────┘
                      │ manages                     │ provides data
                      │ 1:N                         │
          ┌───────────▼──────────────┐              │
          │  ConversationContext     │              │
          │  (Per-Character)         │              │
          └───────────┬──────────────┘              │
                      │ contains                    │
                      │ 1:N                         │
          ┌───────────▼──────────┐                  │
          │  Message             │                  │
          │  (Context Event)     │                  │
          └──────────────────────┘                  │
                                                    │
┌─────────────────────────────────────────────────────┐
│                  CharacterAgent                     │
│                  (NPC Controller)                   │
└──────────┬──────────────────────────┬───────────────┘
           │ uses                     │ uses
           │                          │
┌──────────▼──────────┐   ┌───────────▼────────────┐
│  CharacterElement   │   │  InventoryComponent    │
│  (Map Entity)       │   │  (Game Component)      │
└──────────┬──────────┘   └───────────┬────────────┘
           │ positioned on             │ contains
           │                           │ 1:N
┌──────────▼──────────┐   ┌───────────▼────────────┐
│  GridCell           │   │  InventoryItem         │
│  (Map Position)     │   │  (Slot Data)           │
└─────────────────────┘   └────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                  LogPanel (UI View)                 │
└──────────┬──────────────────────────────────────────┘
           │ displays
           │ 1:N (pooled)
┌──────────▼──────────┐
│  LogEntry           │
│  (UI Model)         │
└─────────────────────┘
```

---

## 7. State Machines

### 7.1 ConversationContext State Machine

```
[Empty Context]
    │
    │ AddMessage(isPinned=true)
    ▼
[Pinned Messages Only] ─────────────────┐
    │                                   │
    │ AddMessage(isPinned=false)        │ AddMessage(isPinned=true)
    ▼                                   │
[Pinned + Sliding (< max)]              │
    │                                   │
    │ AddMessage (sliding full)         │
    ▼                                   │
[Sliding Window Full] ──────────────────┘
    │
    │ Dequeue oldest, Enqueue new
    └─► [Sliding Window Full] (steady state)
```

### 7.2 MovementAnimator State Machine

```
[Idle] ──────────────────────────────────┐
    │                                    │
    │ AnimateMovementAsync()             │
    ▼                                    │
[Animating]                              │
    │                                    │
    ├─► elapsed < duration ─► Task.Yield() ─► [Animating]
    │                                    │
    └─► elapsed >= duration ─────────────┘
```

### 7.3 InventoryComponent Consumable Flow

```
[Has Item in Inventory]
    │
    │ ConsumeItem(itemType)
    ▼
[Validate Item Exists] ──no──► [Return Error: Not in Inventory]
    │ yes
    ▼
[Check Consumable Property] ──no──► [Return Error: Not Consumable]
    │ yes
    ▼
[Get ConsumableData]
    │
    ▼
[Remove from Inventory] ──fail──► [Return Error: Remove Failed]
    │ success
    ▼
[Apply Effect (e.g., Restore Health)]
    │
    ▼
[Cap at Maximum (if health)]
    │
    ▼
[Log to Context]
    │
    ▼
[Return ConsumeItemResult(success=true)]
```

---

## 8. Data Flow Diagrams

### 8.1 Context Flow (Turn Execution)

```
TurnSystemController
    │
    │ ExecuteTurn(characterAgent)
    ▼
CharacterAgent.ExecuteTurnAsync()
    │
    ├─► ContextManager.GetMessagesForLLM(characterId)
    │       │
    │       └─► ConversationContext.GetAllMessages()
    │               │
    │               └─► [Pinned Messages] + [Sliding Messages]
    │
    ├─► LLMOrchestrator.ProcessMessageAsync(messages)
    │       │
    │       └─► LLM API (OpenAI/QWEN)
    │
    ├─► Parse ToolCalls from LLM Response
    │
    ├─► AgentExecutor.ExecuteToolCallsAsync(toolCalls)
    │       │
    │       └─► CharacterToolSet.ExecuteToolAsync()
    │               │
    │               └─► ToolResponse (success, data)
    │
    └─► ContextManager.AddToolResult(characterId, toolCallId, result)
            │
            └─► ConversationContext.AddMessage(toolMessage)
```

### 8.2 Consumable Flow (Player/NPC)

```
PlayerController / InventoryToolSet
    │
    │ EatItem(itemType)
    ▼
InventoryComponent.HasItem(itemType)
    │ yes
    ▼
ConsumableConfiguration.GetConsumableData(itemType)
    │
    ▼
InventoryComponent.RemoveItem(itemType, quantity=1)
    │ success
    ▼
CombatComponent.ModifyHealth(+data.value)
    │
    ▼
ContextManager.AddUserMessage(characterId, "Ate Apple, restored 20 HP", MessageType.Inventory)
    │
    ▼
LogPanel.AddLogEntry(LogCategory.Inventory, "Player ate Apple", Color.green)
    │
    ▼
Return ConsumeItemResult(success=true, healthRestored=20)
```

### 8.3 Log Panel Flow

```
GameEvent (anywhere in code)
    │
    │ LogPanel.AddLogEntry(category, message)
    ▼
[Check if entries.Count >= maxEntries]
    │ yes
    ├─► Dequeue oldest LogEntry
    │   └─► ReturnToPool(oldEntry.gameObject)
    │
    │ no
    └─► Continue
        │
        ▼
    GetPooledObject() ──pool empty──► Instantiate(logEntryPrefab)
        │                                  │
        │ pool has object                  │
        └─► pool.Dequeue() ◄───────────────┘
            │
            ▼
        Set Text, Color, Parent
            │
            ▼
        Enqueue to entries
            │
            ▼
        [AutoScroll enabled?] ──yes──► ScrollRect.verticalPosition = 0
            │
            │ no
            └─► Skip scroll
```

---

## 9. Validation Rules

### Context System
- **FR-003**: Complete turn history must be preserved (pinned messages never removed)
- **FR-007**: Sliding window must enforce max 50-100 messages
- **FR-008**: System prompts must be pinned (separate from dynamic context)

### Consumables
- **FR-024**: Validate item in inventory before consumption
- **FR-026**: Health restoration must cap at maximum health
- **FR-028**: Non-consumable items must return clear error

### Logging
- **FR-042**: Log panel must limit to max 50 entries
- **FR-045**: Each entry must have timestamp

---

## Summary

All entities follow POCO philosophy with explicit types, no external dependencies, and configuration-driven behavior. State machines ensure predictable data flow. Entity relationships maintain SOLID principles with clear separation of concerns.

**Next Phase**: Generate contracts/ documentation for MCP tools and quickstart.md developer guide.
