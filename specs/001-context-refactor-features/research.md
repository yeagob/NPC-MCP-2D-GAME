# Research & Technology Decisions

**Feature**: Context System Refactoring and Gameplay Enhancements
**Branch**: `001-context-refactor-features`
**Date**: 2025-10-27
**Status**: Research Complete

## Overview

This document captures technology research and architectural decisions for implementing unified context management, new gameplay mechanics (flip, end turn, consumables), visual enhancements (animation, logging), and editor tooling for the NPC-MCP-2D-Game.

All decisions align with CLAUDE.md constraints: no external dependencies, explicit typing, configuration-driven design, and SOLID principles.

---

## 1. Context Sliding Window Implementation

### Problem Statement

LLM providers impose token limits (GPT-4: 8k-128k tokens, QWEN: 32k-128k tokens). Without context management, message history grows unbounded, eventually exceeding limits and causing API failures. Need mechanism to retain critical context while discarding less important messages.

### Options Evaluated

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Fixed-size FIFO** | Simple: keep last N messages | Loses critical early-game events (NPC relationships, quest context) | ❌ Rejected |
| **Priority-based retention** | Keeps important messages (system prompts, combat events) | Complex scoring logic; subjective importance | ⚠️ Complex |
| **Pinned + Sliding Window** | System prompts pinned; user/assistant messages slide in FIFO | Balance simplicity and retention; predictable behavior | ✅ **Selected** |
| **Token-based windowing** | Tracks actual token count vs message count | Requires tokenizer integration (external dependency) | ❌ Violates no-dependency rule |

### Decision: Pinned + Sliding Window

**Implementation**:
```csharp
public class ConversationContext
{
    private List<Message> pinnedMessages;    // System prompts (never removed)
    private Queue<Message> slidingMessages;  // User/Assistant/Tool messages
    private int maxSlidingMessages = 50;     // Configurable via GameplayConfiguration

    public void AddMessage(Message message)
    {
        if (message.role == MessageRole.System && message.isPinned)
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
        return all;
    }
}
```

**Rationale**:
- System prompts (game rules, character personality) are permanent and critical
- Sliding window for user/assistant messages keeps recent history (50-100 turns)
- Queue provides O(1) enqueue/dequeue performance
- Predictable memory usage: ~50-100 messages × ~500 bytes = 25-50KB per NPC

**Alternatives Rejected**:
- **Fixed-size FIFO**: Early-game context loss breaks NPC relationship memory (requirement FR-003)
- **Token-based**: Requires external tokenizer library (violates CLAUDE.md no-dependencies)

**Configuration**:
```csharp
// Assets/Scripts/Configuration/GameplayConfiguration.cs
public int maxContextSlidingMessages = 50;
public int maxContextPinnedMessages = 10;
```

---

## 2. Unity Async Movement Patterns

### Problem Statement

Character movement must animate smoothly cell-by-cell over multiple frames. Unity's Update() loop runs once per frame, requiring yielding control between position updates. Need pattern compatible with existing async agent code and no external dependencies.

### Options Evaluated

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Coroutines (IEnumerator)** | Native Unity pattern; no dependencies | Poor error handling; hard to compose with async agent code | ⚠️ Fallback option |
| **async/await with Task.Yield()** | Composes with existing agent async; standard C# error handling | Requires careful Unity context management | ✅ **Selected** |
| **UniTask** | Best async performance for Unity | External dependency (NuGet package) | ❌ Violates no-dependency rule |
| **Manual state machine in Update()** | Full control; no async complexity | Boilerplate state tracking; hard to cancel/interrupt | ❌ Overly complex |

### Decision: async/await with Task.Yield()

**Implementation**:
```csharp
public class MovementAnimator
{
    private bool isAnimating = false;

    public async Task AnimateMovementAsync(CharacterElement character, GridCell target, float speed)
    {
        isAnimating = true;
        Vector3 start = character.transform.position;
        Vector3 end = gridSystem.GetWorldPosition(target);
        float elapsed = 0f;
        float duration = Vector3.Distance(start, end) / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            character.transform.position = Vector3.Lerp(start, end, t);

            await Task.Yield(); // Yield to next frame
        }

        character.transform.position = end; // Ensure exact position
        isAnimating = false;
    }
}
```

**Rationale**:
- Composes with existing `CharacterAgent.ExecuteTurnAsync()` pattern
- Standard C# try/catch error handling
- Cancellation tokens for interrupted animations
- No external dependencies (Task is in System.Threading.Tasks)

**Alternatives Rejected**:
- **UniTask**: External dependency violates CLAUDE.md
- **Coroutines**: Incompatible with async agent code; would require callbacks or polling

**Input Blocking Pattern**:
```csharp
// In PlayerController.cs
if (MovementAnimator.isAnimating)
{
    return; // Block all input during animation
}
```

**Performance Consideration**: Task.Yield() adds ~1ms overhead per frame. With 10 concurrent animations at 60 FPS, total overhead = 10ms (acceptable within 16.6ms frame budget).

---

## 3. MCP Tool Schema Extensions

### Problem Statement

New tools (flip, end_turn, eat_item) must follow existing MCP function definition format while adding parameter validation and clear error responses.

### Existing Pattern Analysis

Current tools use this structure:
```json
{
  "name": "attack",
  "description": "Attack a nearby character",
  "parameters": {
    "type": "object",
    "properties": {
      "targetCharacterId": {
        "type": "string",
        "description": "ID of character to attack"
      }
    },
    "required": ["targetCharacterId"]
  }
}
```

### Decision: Extend with Enum Validation and Error Codes

**New Tool Schemas**:

#### flip Tool
```json
{
  "name": "flip",
  "description": "Flip character orientation 180 degrees without moving",
  "parameters": {
    "type": "object",
    "properties": {},
    "required": []
  },
  "annotations": {
    "readOnly": false,
    "destructive": false,
    "actionPointCost": 1
  }
}
```

#### end_turn Tool
```json
{
  "name": "end_turn",
  "description": "End turn early before consuming all action points",
  "parameters": {
    "type": "object",
    "properties": {},
    "required": []
  },
  "annotations": {
    "readOnly": false,
    "destructive": false,
    "terminatesTurn": true
  }
}
```

#### eat_item Tool
```json
{
  "name": "eat_item",
  "description": "Consume an edible item from inventory to restore health",
  "parameters": {
    "type": "object",
    "properties": {
      "itemType": {
        "type": "string",
        "description": "Type of item to eat (Apple, Bread, etc.)",
        "enum": ["Apple"]  // Future: extend with more consumables
      }
    },
    "required": ["itemType"]
  },
  "annotations": {
    "readOnly": false,
    "destructive": true,
    "actionPointCost": 1,
    "removesFromInventory": true
  }
}
```

**Error Response Standardization**:
```csharp
public class ToolResponse
{
    public bool success;
    public string errorCode;  // NEW: Standardized codes
    public string message;
    public object data;       // Optional: health restored, new orientation, etc.
}

// Error codes enum
public enum ToolErrorCode
{
    None,
    NotInInventory,
    ItemNotConsumable,
    InsufficientActionPoints,
    NotYourTurn,
    InvalidTarget,
    OutOfRange
}
```

**Rationale**:
- Annotations provide LLM hints (actionPointCost, terminatesTurn)
- Enum validation prevents invalid itemType values
- Standardized error codes enable consistent LLM error handling

---

## 4. Log Panel UI Performance

### Problem Statement

Log panel displays 50+ entries with color coding, auto-scroll, and real-time updates. Must maintain 60 FPS even when receiving 10 events/second.

### Options Evaluated

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **GameObject per entry + Destroy()** | Simple implementation | Garbage collection spikes; allocation overhead | ❌ Performance risk |
| **Object Pooling** | Reuses GameObjects; no GC spikes | More complex pool management | ✅ **Selected** |
| **Virtual Scrolling (UI Toolkit)** | Only renders visible entries | Requires Unity UI Toolkit (Unity 2021+) | ⚠️ Available but overkill for 50 entries |
| **Single TextMeshPro with rich text** | Zero GameObjects after initial | Hard to color individual lines; no per-entry control | ❌ Inflexible |

### Decision: Object Pooling with TextMeshProUGUI

**Implementation**:
```csharp
public class LogPanel : MonoBehaviour
{
    private Queue<LogEntry> entries = new Queue<LogEntry>();
    private Queue<GameObject> pool = new Queue<GameObject>();
    private int maxEntries = 50; // From GameplayConfiguration

    public void AddLogEntry(LogEntry entry)
    {
        GameObject entryGO = GetPooledObject();
        TextMeshProUGUI text = entryGO.GetComponent<TextMeshProUGUI>();
        text.text = $"[{entry.category}] {entry.message}";
        text.color = GetCategoryColor(entry.category);

        if (entries.Count >= maxEntries)
        {
            LogEntry oldest = entries.Dequeue();
            ReturnToPool(oldest.gameObject);
        }

        entries.Enqueue(entry);

        if (autoScroll)
        {
            scrollRect.verticalNormalizedPosition = 0f; // Scroll to bottom
        }
    }

    private GameObject GetPooledObject()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(logEntryPrefab, contentTransform);
    }

    private void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

**Performance Profile**:
- Initial allocation: 50 GameObjects × 200 bytes = ~10KB
- Per-frame overhead: Text update (~0.5ms) + color change (~0.1ms) = 0.6ms per entry
- 10 entries/second = 6ms/second (negligible)

**Rationale**:
- Object pooling eliminates GC spikes from Instantiate/Destroy
- TextMeshPro batching handles color changes efficiently
- 50 entries small enough to not need virtual scrolling complexity

**Alternatives Rejected**:
- **Destroy() per entry**: 10 events/sec × 60 frames = 600 allocations/min → GC pressure
- **Virtual scrolling**: Overkill for 50 entries; adds UI Toolkit dependency

**Color Configuration**:
```csharp
// In GameplayConfiguration.cs
public Color logColorSystem = Color.gray;
public Color logColorAction = Color.yellow;
public Color logColorCombat = Color.red;
public Color logColorInventory = Color.green;
public Color logColorDialogue = Color.cyan;
public Color logColorAI = Color.magenta;
```

---

## 5. ScriptableObject Configuration Patterns

### Problem Statement

ConsumableData must be serializable in Unity Inspector without custom editors. Need pattern for enum-keyed lookups (ItemType → ConsumableData) that works with ScriptableObjects.

### Existing Pattern Analysis

Current configuration uses:
```csharp
[CreateAssetMenu(fileName = "ToolConfig", menuName = "MCP/ToolConfig")]
public class ToolConfig : ScriptableObject
{
    public string toolId;
    public FunctionDefinition functionDefinition;
}
```

### Decision: Dictionary Pattern with SerializableKeyValuePair

**Implementation**:
```csharp
[System.Serializable]
public class ConsumableData
{
    public ItemType itemType;
    public ConsumableEffect effect;
    public int value;
    public string consumeMessage;
}

[System.Serializable]
public class ItemTypeConsumableDataPair
{
    public ItemType itemType;
    public ConsumableData data;
}

[CreateAssetMenu(fileName = "ConsumableConfiguration", menuName = "Game/ConsumableConfiguration")]
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

        // Validation: Check for duplicate itemTypes
        HashSet<ItemType> seen = new HashSet<ItemType>();
        foreach (ItemTypeConsumableDataPair pair in consumables)
        {
            if (seen.Contains(pair.itemType))
            {
                Debug.LogError($"Duplicate consumable definition for {pair.itemType}");
            }
            seen.Add(pair.itemType);
        }
    }
}
```

**Rationale**:
- Unity can serialize `List<CustomClass>` without custom editor
- Dictionary built at runtime for O(1) lookup performance
- OnValidate provides immediate feedback on duplicate entries
- No external dependencies (UnityEngine only)

**Alternatives Rejected**:
- **Direct Dictionary serialization**: Unity doesn't serialize Dictionary<TKey, TValue> by default
- **Custom PropertyDrawer**: Adds complexity; violates "no custom editors" preference from CLAUDE.md simplicity principle

**Example Configuration in Inspector**:
```yaml
Consumables:
  - Item Type: Apple
    Effect: RestoreHealth
    Value: 20
    Consume Message: "You ate an apple and restored 20 HP"
  - Item Type: Bread
    Effect: RestoreHealth
    Value: 30
    Consume Message: "You ate bread and restored 30 HP"
```

---

## 6. Message Role Classification

### Problem Statement

Context messages need role-based categorization for LLM API compatibility (OpenAI/QWEN expect specific role values).

### Decision: Extend Existing MessageRole Enum

**Current Implementation** (from existing codebase):
```csharp
public enum MessageRole
{
    System,
    User,
    Assistant
}
```

**Extension Required**:
```csharp
public enum MessageRole
{
    System,     // System prompts (game rules, personality)
    User,       // Player actions, tool call requests
    Assistant,  // LLM responses, NPC decisions
    Tool        // NEW: Tool execution results
}
```

**Rationale**:
- OpenAI API supports `system`, `user`, `assistant`, `tool` roles
- QWEN API mirrors OpenAI format (compatibility maintained)
- Tool role separates execution results from user input (clearer context)

**Message Structure Extension**:
```csharp
public class Message
{
    public MessageRole role;
    public string content;
    public bool isPinned;          // NEW: For sliding window
    public string toolCallId;      // NEW: For tool responses
    public long timestamp;         // NEW: For chronological ordering
}
```

---

## 7. Performance Validation Strategy

### Measurement Tools

| Metric | Tool | Threshold | Action if Exceeded |
|--------|------|-----------|-------------------|
| FPS | Unity Profiler (CPU Usage) | 60 FPS minimum | Optimize animation queue; reduce concurrent animations |
| Memory Growth | Unity Profiler (Memory) | <20% over 100 turns | Verify sliding window; check for leaked GameObjects |
| Context Operations | Custom Stopwatch | <100ms per turn | Cache Message.GetAllMessages(); optimize JSON serialization |
| LLM Latency | AgentExecutor timing logs | <2 seconds | Not actionable (external API); user feedback only |

### Test Scenarios

1. **Context Stress Test**: 100 consecutive turns with combat, inventory, dialogue → verify memory stable
2. **Animation Stress Test**: Spawn 15 characters, move all simultaneously → verify FPS ≥ 60
3. **Log Panel Stress Test**: Generate 100 events in 1 second → verify no frame drops
4. **Integration Test**: 50-turn game with all features active → verify success criteria met

---

## 8. Migration Strategy: Context Refactoring

### Problem

CharacterAgent currently uses split prompts (system, context payloads). Need to migrate to unified message-based context without breaking existing NPC behavior.

### Decision: Incremental Migration with Parallel Systems

**Phase 1**: Add message-based context **alongside** existing prompts
```csharp
// In CharacterAgent.ExecuteTurnAsync()
LLMRequest request = new LLMRequest
{
    messages = contextManager.GetAllMessages(),        // NEW
    systemPrompt = agentConfig.systemPrompt.content,   // EXISTING (parallel)
    // ...
};
```

**Phase 2**: Validate equivalence (manual testing, NPC behavior unchanged)

**Phase 3**: Remove old prompt system after validation
```csharp
// Remove systemPrompt, contextPayloads from LLMRequest
```

**Rationale**:
- Parallel systems allow A/B comparison during testing
- Rollback possible if message-based context causes issues
- Low risk to existing functionality

---

## Summary of Decisions

| Area | Decision | Key Rationale |
|------|----------|---------------|
| Context Window | Pinned + Sliding Window (Queue) | Retains system prompts; predictable memory |
| Movement Animation | async/await with Task.Yield() | Composes with async agents; no dependencies |
| Tool Schemas | Extend with annotations and error codes | Standardized LLM error handling |
| Log Panel | Object Pooling with TextMeshPro | Eliminates GC spikes; performant for 50 entries |
| ScriptableObjects | List + Runtime Dictionary | Unity-serializable; O(1) lookup |
| Message Roles | Add Tool role to existing enum | OpenAI/QWEN compatibility |
| Performance | Unity Profiler + Custom timing | Validates 60 FPS, <100ms ops, <20% memory |
| Migration | Incremental (parallel then remove) | Low risk; rollback possible |

All decisions comply with CLAUDE.md: no external dependencies, explicit types, configuration-driven, SOLID principles maintained.

**Next Phase**: Generate data-model.md with entity designs and state machines.
