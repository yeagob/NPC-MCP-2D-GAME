# Developer Quickstart Guide

**Feature**: Context System Refactoring and Gameplay Enhancements
**Branch**: `001-context-refactor-features`
**Date**: 2025-10-27
**Audience**: Unity developers implementing or extending this feature

## Overview

This quickstart guide provides practical instructions for developers working with the context refactoring and gameplay enhancement systems. It covers common development tasks, debugging techniques, and extension patterns.

---

## Table of Contents

1. [Adding New Consumable Items](#1-adding-new-consumable-items)
2. [Extending Context with New Event Types](#2-extending-context-with-new-event-types)
3. [Adding New Log Categories](#3-adding-new-log-categories)
4. [Debugging Context Flow](#4-debugging-context-flow)
5. [Creating New MCP Tools](#5-creating-new-mcp-tools)
6. [Testing Context Persistence](#6-testing-context-persistence)
7. [Performance Profiling](#7-performance-profiling)
8. [Common Pitfalls](#8-common-pitfalls)

---

## 1. Adding New Consumable Items

### Step 1: Add ItemType Enum Value

**File**: `Assets/Scripts/Enums/ItemType.cs`

```csharp
public enum ItemType
{
    Key,
    Money,
    Apple,
    Bread     // NEW: Add your new item
}
```

### Step 2: Create ConsumableData Entry

**Location**: Unity Editor → Project → Assets/Configuration/ConsumableConfiguration.asset

**Inspector Steps**:
1. Click "+" button under "Consumables" list
2. Set fields:
   - **Item Type**: `Bread` (from dropdown)
   - **Effect**: `RestoreHealth`
   - **Value**: `30` (HP restored)
   - **Consume Message**: `"You ate bread and restored 30 HP"`
   - **Consume Delay**: `0.0`

**Validation**: OnValidate will warn if duplicate itemType exists.

### Step 3: Update ItemElement Prefab

**Location**: `Assets/Prefabs/Items/BreadPrefab.prefab`

**Inspector Steps**:
1. Set `ItemType` field to `Bread`
2. Set `properties` flags:
   - ✅ Consumable
   - ✅ Edible
   - ✅ Stackable
   - ❌ Drinkable
   - ❌ Equipable

### Step 4: Update eat_item Tool Schema

**File**: `Assets/Agents/ToolSets/-2DGameTools-/inventory/EatItemToolConfig.asset`

**Modify FunctionDefinition**:
```json
{
  "parameters": {
    "properties": {
      "itemType": {
        "enum": ["Apple", "Bread"]  // Add "Bread"
      }
    }
  }
}
```

### Step 5: Test in Play Mode

**Scene**: `Assets/Scenes/NpcMcp2DGame.unity`

**Test Steps**:
1. Place Bread item on map
2. Give character low health (e.g., 50/100)
3. Pickup Bread via player or NPC
4. Execute `eat_item(itemType: "Bread")`
5. **Verify**:
   - Health increases by 30
   - Bread removed from inventory
   - Log panel shows "ate Bread" message (green)
   - Context contains eat event (check ContextManager in Inspector)

---

## 2. Extending Context with New Event Types

### Scenario: Add "Trade" Event Type

Trade events occur when characters exchange items for currency.

### Step 1: Add MessageType Enum Value

**File**: `Assets/Scripts/Enums/MessageType.cs`

```csharp
public enum MessageType
{
    General,
    Combat,
    Inventory,
    Dialogue,
    Movement,
    System,
    AI,
    Trade       // NEW: For item-for-money exchanges
}
```

### Step 2: Add Context Messages in Trade Logic

**File**: `Assets/Scripts/Services/Trading/TradeService.cs` (hypothetical)

```csharp
public bool ExecuteTrade(CharacterElement buyer, CharacterElement seller, ItemType item, int price)
{
    // ... trade logic ...

    // Log to context for both characters
    string buyerMessage = $"{buyer.name} bought {item} from {seller.name} for {price} gold";
    string sellerMessage = $"{seller.name} sold {item} to {buyer.name} for {price} gold";

    contextManager.AddUserMessage(buyer.Id, buyerMessage, MessageType.Trade);
    contextManager.AddUserMessage(seller.Id, sellerMessage, MessageType.Trade);

    return true;
}
```

### Step 3: (Optional) Add Log Category for Trade

**File**: `Assets/Scripts/Enums/LogCategory.cs`

```csharp
public enum LogCategory
{
    System,
    Action,
    Combat,
    Inventory,
    Dialogue,
    AI,
    Trade       // NEW
}
```

**Configuration**: `Assets/Configuration/GameplayConfiguration.asset`

Add new color field:
```csharp
[Header("Logging")]
public Color logColorTrade = new Color(1.0f, 0.84f, 0.0f); // Gold color

public Color GetLogColor(LogCategory category)
{
    // ... existing cases ...
    case LogCategory.Trade: return logColorTrade;
    // ...
}
```

### Step 4: Log to UI

```csharp
UniversalLogUI.Instance.LogMessage(buyerMessage, LogCategory.Trade);
```

---

## 3. Adding New Log Categories

### Scenario: Add "Quest" Log Category

Quest events track quest start, progress, and completion.

### Step 1: Add LogCategory Enum Value

**File**: `Assets/Scripts/Enums/LogCategory.cs`

```csharp
public enum LogCategory
{
    System,
    Action,
    Combat,
    Inventory,
    Dialogue,
    AI,
    Quest       // NEW
}
```

### Step 2: Add Color Configuration

**File**: `Assets/Scripts/Configuration/GameplayConfiguration.cs`

```csharp
[Header("Logging")]
public Color logColorQuest = new Color(1.0f, 0.65f, 0.0f); // Orange

public Color GetLogColor(LogCategory category)
{
    switch (category)
    {
        // ... existing cases ...
        case LogCategory.Quest: return logColorQuest;
        default: return Color.white;
    }
}
```

### Step 3: Set Color in Unity Inspector

**Location**: `Assets/Configuration/GameplayConfiguration.asset`

**Inspector**:
- **Log Color Quest**: Set to orange (R:255, G:165, B:0, A:255)

### Step 4: Use in Code

```csharp
// When quest starts
UniversalLogUI.Instance.LogMessage("Quest started: Find the Ancient Key", LogCategory.Quest);

// When quest progresses
UniversalLogUI.Instance.LogMessage("Quest progress: 2/3 keys collected", LogCategory.Quest);

// When quest completes
UniversalLogUI.Instance.LogMessage("Quest completed: Find the Ancient Key (Reward: 100 gold)", LogCategory.Quest);
```

**Result**: Log panel displays quest messages in orange color.

---

## 4. Debugging Context Flow

### Viewing Context in Unity Inspector

**Runtime Debugging**:

1. **Enter Play Mode**
2. **Hierarchy** → Find CharacterAgent GameObject (e.g., "Pablo")
3. **Inspector** → CharacterAgent component
4. **Expand "Context Manager" field**
5. **View**:
   - `conversations` dictionary (character ID → ConversationContext)
   - `pinnedMessages` list (system prompts, never removed)
   - `slidingMessages` queue (recent events, FIFO)

**Pinned Messages Example**:
```
[0] Role: System, Content: "You are Pablo, a brave warrior...", isPinned: true
[1] Role: System, Content: "Game Rules: Attack costs 1 AP, range 1 cell...", isPinned: true
```

**Sliding Messages Example**:
```
[0] Role: User, Content: "Turn 5 started", timestamp: 1730035100
[1] Role: Assistant, Content: "I will attack the goblin", timestamp: 1730035105
[2] Role: Tool, Content: "Attack dealt 15 damage", timestamp: 1730035106
[3] Role: User, Content: "Pablo ate Apple (+20 HP)", timestamp: 1730035110
...
[49] Role: User, Content: "Turn 54 started", timestamp: 1730035500
```

### Logging Context to Console

**Add Debug Helper Method**:

```csharp
// In ContextManager.cs
public void DebugPrintContext(string characterId)
{
    ConversationContext context = GetOrCreateContext(characterId);
    List<Message> messages = context.GetAllMessages();

    Debug.Log($"=== CONTEXT for {characterId} ===");
    Debug.Log($"Total messages: {messages.Count}");
    Debug.Log($"Pinned: {context.pinnedMessages.Count}, Sliding: {context.slidingMessages.Count}");

    foreach (Message msg in messages)
    {
        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(msg.timestamp).DateTime;
        Debug.Log($"[{dt:HH:mm:ss}] {msg.role} ({msg.messageType}): {msg.content.Substring(0, Math.Min(50, msg.content.Length))}...");
    }

    Debug.Log($"=== END CONTEXT ===");
}
```

**Usage**:
```csharp
// In CharacterAgent.ExecuteTurnAsync()
contextManager.DebugPrintContext(GetCharacterElement().Id);
```

### Visualizing Context in JSON

**Export to File** (for LLM debugging):

```csharp
public void ExportContextToJson(string characterId, string filePath)
{
    ConversationContext context = GetOrCreateContext(characterId);
    List<Message> messages = context.GetAllMessages();

    string json = JsonUtility.ToJson(new { messages = messages }, prettyPrint: true);
    System.IO.File.WriteAllText(filePath, json);

    Debug.Log($"Context exported to {filePath}");
}

// Usage
contextManager.ExportContextToJson("pablo-001", "D:/context_debug.json");
```

**Output** (`context_debug.json`):
```json
{
  "messages": [
    {
      "role": "System",
      "content": "You are Pablo...",
      "isPinned": true,
      "timestamp": 1730035000
    },
    {
      "role": "User",
      "content": "Turn 5 started",
      "isPinned": false,
      "messageType": "System",
      "timestamp": 1730035100
    }
  ]
}
```

---

## 5. Creating New MCP Tools

### Scenario: Add "heal_ally" Tool

Allow NPCs to heal adjacent allies using a medkit item.

### Step 1: Define Tool Schema

**Location**: `Assets/Agents/ToolSets/-2DGameTools-/charactertools/HealAllyToolConfig.asset`

**Create ScriptableObject**:
1. **Right-click** in Project → Create → MCP → ToolConfig
2. **Set Fields**:
   - **Tool Id**: `heal_ally`
   - **Tool Name**: `heal_ally`
   - **Function Definition**:
     ```json
     {
       "name": "heal_ally",
       "description": "Use medkit to heal adjacent ally. Restores 30 HP. Consumes medkit from inventory.",
       "parameters": {
         "type": "object",
         "properties": {
           "targetCharacterId": {
             "type": "string",
             "description": "ID of ally to heal (must be adjacent)"
           }
         },
         "required": ["targetCharacterId"]
       }
     }
     ```

### Step 2: Implement Tool in ToolSet

**File**: `Assets/Scripts/Services/Tools/CharacterToolSet.cs`

```csharp
public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall)
{
    switch (toolCall.name)
    {
        case "flip":
            return await ExecuteFlipAsync();
        case "end_turn":
            return await ExecuteEndTurnAsync();
        case "heal_ally":  // NEW
            return await ExecuteHealAllyAsync(toolCall);
        default:
            return new ToolResponse(false, "TOOL_NOT_FOUND", $"Unknown tool: {toolCall.name}");
    }
}

private async Task<ToolResponse> ExecuteHealAllyAsync(ToolCall toolCall)
{
    // Parse arguments
    dynamic args = JsonHelper.Deserialize(toolCall.arguments);
    string targetId = args.targetCharacterId;

    // Validate target
    CharacterElement target = _mapSystem.GetElementById(targetId) as CharacterElement;
    if (target == null)
    {
        return new ToolResponse(false, "INVALID_TARGET", "Target not found");
    }

    // Validate distance (must be adjacent)
    int distance = _mapSystem.GetDistanceBetweenElements(_characterAgent.GetCharacterElement(), target);
    if (distance > 1)
    {
        return new ToolResponse(false, "OUT_OF_RANGE", "Target must be adjacent");
    }

    // Validate medkit in inventory
    if (!_inventoryComponent.HasItem(ItemType.Medkit))
    {
        return new ToolResponse(false, "NO_MEDKIT", "No medkit in inventory");
    }

    // Remove medkit
    RemoveItemResult removeResult = _inventoryComponent.RemoveItem(ItemType.Medkit, 1);
    if (!removeResult.success)
    {
        return new ToolResponse(false, "INVENTORY_ERROR", "Failed to remove medkit");
    }

    // Heal target
    CombatComponent targetCombat = target.GetComponent<CombatComponent>();
    int healthBefore = targetCombat.HealthPoints;
    int healAmount = 30;
    int newHealth = Mathf.Min(healthBefore + healAmount, targetCombat.MaxHealthPoints);
    int actualHealed = newHealth - healthBefore;
    targetCombat.ModifyHealth(actualHealed);

    // Consume action point
    _characterAgent.ConsumeActionPoint();

    // Log
    string message = $"{_characterAgent.name} used medkit on {target.name}, healing {actualHealed} HP";
    _contextManager.AddUserMessage(_characterAgent.GetCharacterElement().Id, message, MessageType.System);
    UniversalLogUI.Instance.LogMessage(message, LogCategory.Action);

    return new ToolResponse(true, "SUCCESS", message, new
    {
        targetId = targetId,
        healthRestored = actualHealed,
        targetCurrentHealth = newHealth
    });
}
```

### Step 3: Register Tool

**File**: `Assets/Scripts/Services/Tools/CharacterToolSet.cs` (constructor)

```csharp
public CharacterToolSet(CharacterAgent agent, InventoryComponent inventory, MapSystem mapSystem)
{
    _characterAgent = agent;
    _inventoryComponent = inventory;
    _mapSystem = mapSystem;

    RegisterTool("flip");
    RegisterTool("end_turn");
    RegisterTool("heal_ally");  // NEW
}
```

### Step 4: Add Tool to Agent Configuration

**Location**: `Assets/Agents/-2DGameAgents-/PabloAgentConfig.asset`

**Inspector**:
1. Expand "Tool Configs" array
2. Click "+" to add element
3. Drag `HealAllyToolConfig.asset` into new slot

### Step 5: Test

**Play Mode**:
1. Place 2 characters adjacent (Pablo, Maria)
2. Give Pablo a Medkit
3. Reduce Maria's health to 50/100
4. Execute Pablo's turn (NPC should use heal_ally tool)
5. **Verify**:
   - Maria's health increases by 30
   - Medkit removed from Pablo's inventory
   - Log shows heal action

---

## 6. Testing Context Persistence

### Manual Test Scenario: Multi-Turn Memory

**Goal**: Verify NPC remembers events from 5+ turns ago.

**Setup**:
1. Enter Play Mode
2. Turn 1: Player attacks Pablo (reduce Pablo to 70/100 HP)
3. Turn 2: Player gives Pablo an Apple
4. Turn 3-7: NPCs take turns (no interaction with Pablo)
5. Turn 8: Pablo's turn

**Expected Behavior**:
Pablo's context should include:
- Message from Turn 1: "Player attacked Pablo for 30 damage"
- Message from Turn 2: "Player gave Apple to Pablo"

**Validation**:
```csharp
// In Pablo's CharacterAgent.ExecuteTurnAsync()
List<Message> context = contextManager.GetMessagesForLLM(GetCharacterElement().Id);
foreach (Message msg in context)
{
    Debug.Log($"Pablo Context: {msg.content}");
}

// Should print:
// Pablo Context: Player attacked Pablo for 30 damage (timestamp: 1730035000)
// Pablo Context: Player gave Apple to Pablo (timestamp: 1730035050)
// ... other messages ...
```

**LLM Reasoning Test**:
Pablo's next action should reference these events (e.g., "I will eat the apple the player gave me to heal from the attack").

### Automated Test (Unity Test Framework)

**File**: `Assets/Tests/PlayMode/ContextPersistenceTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class ContextPersistenceTests
{
    [UnityTest]
    public IEnumerator ContextRetains5TurnsHistory()
    {
        // Arrange
        ContextManager contextManager = new ContextManager(mockConfiguration);
        string characterId = "pablo-001";

        // Act: Add 5 messages over 5 "turns"
        for (int turn = 1; turn <= 5; turn++)
        {
            contextManager.AddUserMessage(characterId, $"Turn {turn} event", MessageType.System);
            yield return null; // Simulate turn delay
        }

        // Assert: All 5 messages present
        List<Message> messages = contextManager.GetMessagesForLLM(characterId);
        Assert.AreEqual(5, messages.Count(m => m.content.Contains("Turn")));
        Assert.IsTrue(messages.Any(m => m.content.Contains("Turn 1")));
        Assert.IsTrue(messages.Any(m => m.content.Contains("Turn 5")));
    }

    [UnityTest]
    public IEnumerator ContextSlidingWindow_RemovesOldestWhenFull()
    {
        // Arrange
        GameplayConfiguration config = ScriptableObject.CreateInstance<GameplayConfiguration>();
        config.maxContextSlidingMessages = 3; // Small window for testing
        ContextManager contextManager = new ContextManager(config);
        string characterId = "pablo-001";

        // Act: Add 5 messages (exceeds max of 3)
        for (int i = 1; i <= 5; i++)
        {
            contextManager.AddUserMessage(characterId, $"Message {i}", MessageType.System);
            yield return null;
        }

        // Assert: Only last 3 messages retained
        List<Message> messages = contextManager.GetMessagesForLLM(characterId);
        Assert.AreEqual(3, messages.Count);
        Assert.IsTrue(messages.Any(m => m.content.Contains("Message 3")));
        Assert.IsTrue(messages.Any(m => m.content.Contains("Message 4")));
        Assert.IsTrue(messages.Any(m => m.content.Contains("Message 5")));
        Assert.IsFalse(messages.Any(m => m.content.Contains("Message 1"))); // Removed
        Assert.IsFalse(messages.Any(m => m.content.Contains("Message 2"))); // Removed
    }
}
```

---

## 7. Performance Profiling

### Unity Profiler Setup

**Steps**:
1. **Window** → **Analysis** → **Profiler**
2. **Enter Play Mode**
3. **Profile CPU**:
   - Expand "CharacterAgent.ExecuteTurnAsync()"
   - Check "ContextManager.GetMessagesForLLM()" time
   - **Target**: <100ms per turn
4. **Profile Memory**:
   - Monitor "Managed Heap"
   - After 100 turns, verify growth <20%
   - Check for memory leaks in LogPanel (pooled objects should not increase)

### Custom Timing Logs

**Add Stopwatch to Critical Paths**:

```csharp
// In ContextManager.cs
using System.Diagnostics;

public List<Message> GetMessagesForLLM(string characterId)
{
    Stopwatch sw = Stopwatch.StartNew();

    ConversationContext context = GetOrCreateContext(characterId);
    List<Message> messages = context.GetAllMessages();

    sw.Stop();
    if (sw.ElapsedMilliseconds > 100)
    {
        Debug.LogWarning($"[ContextManager] GetMessagesForLLM took {sw.ElapsedMilliseconds}ms (threshold: 100ms)");
    }

    return messages;
}
```

### Performance Test Scenario

**Goal**: Verify 60 FPS with 10 simultaneous animations.

**Setup**:
1. Create 10 NPCs in scene
2. Command all to move 5 cells simultaneously
3. Observe Unity Profiler FPS counter

**Expected**: FPS ≥ 60 (frame time ≤ 16.6ms)

**If FPS drops**:
- Profile MovementAnimator.AnimateMovementAsync()
- Check Task.Yield() overhead
- Consider animation queue (serialize animations instead of parallel)

---

## 8. Common Pitfalls

### Pitfall 1: Context Not Updating

**Symptom**: NPC makes decisions without awareness of recent events.

**Cause**: Forgot to call `contextManager.AddUserMessage()` after game event.

**Fix**:
```csharp
// After ANY significant game event:
contextManager.AddUserMessage(characterId, eventDescription, MessageType.Appropriate);
```

**Checklist**:
- ✅ After combat
- ✅ After item pickup/drop/give
- ✅ After dialogue
- ✅ After movement
- ✅ After consumable use

### Pitfall 2: Pinned Messages Growing Unbounded

**Symptom**: Memory usage increases despite sliding window.

**Cause**: Incorrectly marking dynamic messages as pinned.

**Fix**: Only pin **permanent** system prompts:
```csharp
// CORRECT (pinned):
contextManager.AddSystemContextAsync(characterId, "You are Pablo, a brave warrior...", MessageType.System);

// INCORRECT (should NOT be pinned):
contextManager.AddUserMessage(characterId, "Turn 5 started", MessageType.System); // isPinned=false (default)
```

**Rule**: If message is turn-specific or event-specific, do NOT pin.

### Pitfall 3: Tool Not Registered

**Symptom**: LLM calls tool, but tool execution fails with "TOOL_NOT_FOUND".

**Cause**: Tool not registered in ToolSet constructor.

**Fix**:
```csharp
public CharacterToolSet(CharacterAgent agent)
{
    RegisterTool("flip");
    RegisterTool("end_turn");
    RegisterTool("heal_ally"); // Ensure this line exists
}
```

### Pitfall 4: Enum Mismatch

**Symptom**: eat_item fails with "INVALID_ITEM_TYPE".

**Cause**: LLM passes item name not in ItemType enum.

**Fix**: Update tool schema enum values:
```json
{
  "parameters": {
    "properties": {
      "itemType": {
        "enum": ["Apple", "Bread"] // Must match ItemType enum exactly
      }
    }
  }
}
```

### Pitfall 5: Health Capping Not Applied

**Symptom**: Character health exceeds MaxHealthPoints after eating.

**Cause**: Forgot to cap health in consumable logic.

**Fix**:
```csharp
int newHealth = Mathf.Min(healthBefore + consumableData.value, combatComponent.MaxHealthPoints);
combatComponent.ModifyHealth(newHealth - healthBefore); // Only restore delta
```

### Pitfall 6: Log Panel Memory Leak

**Symptom**: LogPanel GameObjects increase over time, never destroyed.

**Cause**: Not returning entries to pool.

**Fix**: Always call ReturnToPool() when removing old entries:
```csharp
if (entries.Count >= maxEntries)
{
    LogEntry oldest = entries.Dequeue();
    ReturnToPool(oldest.gameObject); // CRITICAL: Return to pool, not Destroy()
}
```

### Pitfall 7: Async Animation Blocking Forever

**Symptom**: Character stuck mid-movement, game freezes.

**Cause**: MovementAnimator.AnimateMovementAsync() loop never exits (duration calculation error).

**Fix**: Add safety timeout:
```csharp
float elapsed = 0f;
float maxDuration = 10f; // Safety timeout

while (elapsed < duration && elapsed < maxDuration)
{
    elapsed += Time.deltaTime;
    // ... interpolation ...
    await Task.Yield();
}

if (elapsed >= maxDuration)
{
    Debug.LogError($"MovementAnimator timeout after {maxDuration}s");
}
```

---

## Quick Reference Commands

### View Context in Console
```csharp
contextManager.DebugPrintContext("pablo-001");
```

### Export Context to JSON
```csharp
contextManager.ExportContextToJson("pablo-001", "D:/context_debug.json");
```

### Force Clear Sliding Window
```csharp
contextManager.GetOrCreateContext("pablo-001").ClearSlidingMessages();
```

### Manually Add Context Message
```csharp
contextManager.AddUserMessage("pablo-001", "DEBUG: Test event", MessageType.System);
```

### Check if Item is Consumable
```csharp
ItemElement item = mapSystem.GetElementById("apple-001") as ItemElement;
bool canEat = item.IsConsumable(); // Checks ItemProperty.Consumable flag
```

### Get Consumable Data
```csharp
ConsumableData data = gameplayConfiguration.consumableConfiguration.GetConsumableData(ItemType.Apple);
Debug.Log($"Apple restores {data.value} HP");
```

---

## Next Steps

After familiarizing with these patterns:

1. **Review** `plan.md` for implementation sequence (Sprint 1-5)
2. **Check** `tasks.md` (generated by `/speckit.tasks`) for dependency-ordered task breakdown
3. **Start** with Sprint 1: Context Foundation (highest priority)
4. **Test** incrementally after each sprint
5. **Profile** performance at Sprint 4 completion

**Questions?** Review:
- `spec.md` for requirements and acceptance criteria
- `research.md` for technology decisions and rationale
- `data-model.md` for entity relationships and state machines
- `contracts/` for MCP tool specifications

Good luck with implementation!
