# MCP Tool Contract: eat_item

**Feature**: Context System Refactoring and Gameplay Enhancements
**Tool ID**: `eat_item`
**Version**: 1.0.0
**Date**: 2025-10-27

## Overview

The `eat_item` tool allows NPCs to consume edible items from their inventory to restore health or apply other effects. Phase 1 focuses on health restoration only (apples, bread, etc.). This enables resource management gameplay and survival mechanics.

## Function Definition (MCP Format)

```json
{
  "name": "eat_item",
  "description": "Consume an edible item from your inventory to restore health. Item must be in inventory and marked as consumable. Removes item from inventory permanently. Phase 1 supports health restoration only (Apple: +20 HP).",
  "parameters": {
    "type": "object",
    "properties": {
      "itemType": {
        "type": "string",
        "description": "Type of item to consume. Must be a valid ItemType enum value (Apple, Bread, etc.)",
        "enum": ["Apple"]
      }
    },
    "required": ["itemType"]
  }
}
```

**Note**: The `enum` values will expand as new consumable items are added to the game. Phase 1 includes only "Apple".

## Tool Annotations

```json
{
  "readOnly": false,
  "destructive": true,
  "actionPointCost": 1,
  "changesGameState": true,
  "removesFromInventory": true,
  "restoresHealth": true
}
```

**Annotation Meanings**:
- `readOnly: false` - Tool modifies game state (inventory, health)
- `destructive: true` - Permanently removes item from inventory (irreversible)
- `actionPointCost: 1` - Consumes exactly 1 action point per execution
- `changesGameState: true` - Alters inventory and character health
- `removesFromInventory: true` - Decrements item quantity or removes slot
- `restoresHealth: true` - Increases character HealthPoints (capped at maximum)

## Parameters

### itemType (required)

**Type**: `string` (enum)
**Description**: The type of item to consume from inventory.
**Valid Values**: `"Apple"` (Phase 1)
**Future Values**: `"Bread"`, `"Potion"`, `"Berry"`, etc.

**Validation Rules**:
1. Must be a valid ItemType enum value
2. Must correspond to an item with `Consumable` property flag
3. Item must exist in character's inventory (quantity ≥ 1)

**Example**:
```json
{
  "itemType": "Apple"
}
```

## Preconditions

1. **Action Points Available**: Character must have ≥1 action point remaining
2. **Item in Inventory**: Character's inventory must contain the specified itemType with quantity ≥ 1
3. **Item is Consumable**: ItemElement must have `ItemProperty.Consumable` flag set
4. **Character Alive**: Character's HealthPoints > 0 (cannot eat while dead)
5. **Character's Turn**: Must be called during the character's active turn

## Execution Logic

```csharp
// In InventoryToolSet.cs
public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall)
{
    if (toolCall.name != "eat_item")
    {
        return new ToolResponse(false, "INVALID_TOOL", "Tool not supported");
    }

    // Parse parameters
    dynamic args = JsonHelper.Deserialize(toolCall.arguments);
    string itemTypeStr = args.itemType;

    if (!Enum.TryParse(itemTypeStr, out ItemType itemType))
    {
        return new ToolResponse(false, "INVALID_ITEM_TYPE",
            $"Invalid itemType: {itemTypeStr}");
    }

    // Precondition: Check inventory
    if (!_inventoryComponent.HasItem(itemType))
    {
        return new ToolResponse(false, "ITEM_NOT_IN_INVENTORY",
            $"Cannot eat {itemType}: not in inventory");
    }

    // Precondition: Check consumable property
    ConsumableData consumableData = _gameplayConfiguration.consumableConfiguration.GetConsumableData(itemType);
    if (consumableData == null)
    {
        return new ToolResponse(false, "ITEM_NOT_CONSUMABLE",
            $"Cannot eat {itemType}: item is not consumable");
    }

    // Precondition: Check action points
    if (_characterAgent.CurrentActionPoints < 1)
    {
        return new ToolResponse(false, "INSUFFICIENT_ACTION_POINTS",
            "Cannot eat: no action points remaining");
    }

    // Execute: Remove item from inventory
    RemoveItemResult removeResult = _inventoryComponent.RemoveItem(itemType, quantity: 1);
    if (!removeResult.success)
    {
        return new ToolResponse(false, "INVENTORY_ERROR",
            $"Failed to remove {itemType} from inventory");
    }

    // Execute: Apply consumable effect
    CombatComponent combatComponent = _characterAgent.GetComponent<CombatComponent>();
    int healthBefore = combatComponent.HealthPoints;
    int healthRestored = 0;

    if (consumableData.effect == ConsumableEffect.RestoreHealth)
    {
        int newHealth = Mathf.Min(healthBefore + consumableData.value, combatComponent.MaxHealthPoints);
        healthRestored = newHealth - healthBefore;
        combatComponent.ModifyHealth(healthRestored);
    }

    // Consume action point
    _characterAgent.ConsumeActionPoint();

    // Log to context
    string contextMessage = $"{_characterAgent.name} ate {itemType} and restored {healthRestored} HP (now {combatComponent.HealthPoints}/{combatComponent.MaxHealthPoints})";
    _contextManager.AddUserMessage(_characterAgent.GetCharacterElement().Id, contextMessage, MessageType.Inventory);

    // Log to UI
    UniversalLogUI.Instance.LogMessage(contextMessage, LogCategory.Inventory);

    // Return success
    return new ToolResponse(true, "SUCCESS", consumableData.consumeMessage, new
    {
        itemConsumed = itemType.ToString(),
        healthRestored = healthRestored,
        currentHealth = combatComponent.HealthPoints,
        maxHealth = combatComponent.MaxHealthPoints
    });
}
```

## Response Format

### Success Response

```json
{
  "success": true,
  "errorCode": "SUCCESS",
  "message": "You ate an apple and restored 20 HP",
  "data": {
    "itemConsumed": "Apple",
    "healthRestored": 20,
    "currentHealth": 80,
    "maxHealth": 100
  }
}
```

**Scenario**: Character at 60/100 HP eats Apple (+20 HP) → 80/100 HP

### Success Response (Health Capped)

```json
{
  "success": true,
  "errorCode": "SUCCESS",
  "message": "You ate an apple and restored 10 HP",
  "data": {
    "itemConsumed": "Apple",
    "healthRestored": 10,
    "currentHealth": 100,
    "maxHealth": 100
  }
}
```

**Scenario**: Character at 90/100 HP eats Apple (+20 HP potential) → 100/100 HP (capped, only +10 restored)

### Error Responses

#### Item Not in Inventory
```json
{
  "success": false,
  "errorCode": "ITEM_NOT_IN_INVENTORY",
  "message": "Cannot eat Apple: not in inventory",
  "data": null
}
```

#### Item Not Consumable
```json
{
  "success": false,
  "errorCode": "ITEM_NOT_CONSUMABLE",
  "message": "Cannot eat Key: item is not consumable",
  "data": null
}
```

**Scenario**: NPC attempts to eat a Key (not consumable)

#### Insufficient Action Points
```json
{
  "success": false,
  "errorCode": "INSUFFICIENT_ACTION_POINTS",
  "message": "Cannot eat: no action points remaining",
  "data": null
}
```

#### Invalid Item Type
```json
{
  "success": false,
  "errorCode": "INVALID_ITEM_TYPE",
  "message": "Invalid itemType: InvalidItem",
  "data": null
}
```

**Scenario**: LLM passes non-existent item type (e.g., "Banana" not in ItemType enum)

## Side Effects

1. **Item Removed from Inventory**: `InventoryComponent.RemoveItem()` decrements quantity or removes slot
2. **Health Restored**: `CombatComponent.HealthPoints` increased (capped at MaxHealthPoints)
3. **Action Point Consumed**: `CharacterAgent.CurrentActionPoints` decremented by 1
4. **Context Updated**: Message added to ConversationContext (role: User, type: Inventory)
5. **Log Entry Created**: LogPanel displays consumption action with LogCategory.Inventory (green)
6. **UI Inventory Updated**: InventoryComponent.UpdateUIState() refreshes display

## Context Integration

**Message Added to Context**:
```json
{
  "role": "User",
  "content": "Pablo ate Apple and restored 20 HP (now 80/100)",
  "isPinned": false,
  "messageType": "Inventory",
  "timestamp": 1730035200
}
```

This message is visible to the NPC in subsequent turns, enabling resource management decisions (e.g., "I have no more apples, need to find food").

## Use Cases

### Combat Healing
**Scenario**: NPC at low health during combat, has Apple in inventory.

**LLM Reasoning**:
```
My health is 30/100 (critical). I have 1 Apple in inventory.
Eating Apple will restore ~20 HP → 50/100 (safer).
Priority: Eat Apple before attacking.
```

**Tool Call Sequence**:
1. `eat_item(itemType: "Apple")` → Health: 30 → 50
2. `attack(targetCharacterId: "enemy-001")` → Fight from safer position

### Resource Management
**Scenario**: NPC has 2 Apples, health 70/100, no immediate threats.

**LLM Reasoning**:
```
Health is not critical (70/100).
I have 2 Apples (scarce resource).
Best strategy: Save Apples for emergencies.
Do NOT eat now.
```

**Decision**: Skip eat_item, use action points for exploration/positioning.

### Health Capping Awareness
**Scenario**: NPC at 95/100 HP, has Apple (+20 potential).

**LLM Reasoning**:
```
Health is 95/100 (nearly full).
Eating Apple will only restore 5 HP (waste).
Best strategy: Save Apple for when health is lower.
```

**Decision**: Skip eat_item, avoid wasting consumable.

**Context Integration**: LLM learns this through past experiences logged in context (e.g., "I ate Apple at 95 HP and only gained 5 HP, wasted resource").

## Integration Points

### InventoryToolSet Registration
```csharp
public InventoryToolSet(CharacterAgent agent, MapSystem mapSystem)
{
    _characterAgent = agent;
    _mapSystem = mapSystem;
    _inventoryComponent = agent.GetComponent<InventoryComponent>();
    RegisterTool("pickup_item");
    RegisterTool("drop_item");
    RegisterTool("give_item");
    RegisterTool("eat_item"); // NEW
}
```

### ToolConfig ScriptableObject
**Asset Path**: `Assets/Agents/ToolSets/-2DGameTools-/inventory/EatItemToolConfig.asset`

```yaml
toolId: "eat_item"
toolName: "eat_item"
functionDefinition:
  name: "eat_item"
  description: "Consume an edible item from your inventory to restore health..."
  parameters:
    type: "object"
    properties:
      itemType:
        type: "string"
        description: "Type of item to consume..."
        enum: ["Apple"]
    required: ["itemType"]
annotations:
  - readOnly: false
  - destructive: true
  - actionPointCost: 1
  - removesFromInventory: true
  - restoresHealth: true
```

### ConsumableConfiguration ScriptableObject
**Asset Path**: `Assets/Configuration/ConsumableConfiguration.asset`

```yaml
consumables:
  - itemType: Apple
    effect: RestoreHealth
    value: 20
    consumeMessage: "You ate an apple and restored 20 HP"
    consumeDelay: 0.0
```

### PlayerController Integration
```csharp
// Add "Eat" action to InventoryUI
private void OnEatItemSelected(ItemType itemType)
{
    if (!CanExecuteAction())
    {
        return;
    }

    ConsumeItemResult result = _inventoryComponent.ConsumeItem(itemType);

    if (!result.success)
    {
        Debug.LogWarning($"Failed to eat {itemType}: {result.message}");
        return;
    }

    ConsumeActionPoint();
    Debug.Log(result.message);
}
```

## Testing Validation

### Unit Test: Successful Consumption
```csharp
[Test]
public void EatItem_WithAppleInInventory_RestoresHealth()
{
    // Arrange
    CharacterAgent agent = CreateTestAgent(health: 60, maxHealth: 100);
    InventoryComponent inventory = agent.GetComponent<InventoryComponent>();
    inventory.AddItem("apple-001", ItemType.Apple, quantity: 1);
    InventoryToolSet toolSet = new InventoryToolSet(agent, mockMapSystem);

    // Act
    ToolResponse response = await toolSet.ExecuteToolAsync(
        new ToolCall("eat_item", "{\"itemType\":\"Apple\"}")
    );

    // Assert
    Assert.IsTrue(response.success);
    Assert.AreEqual(80, agent.GetComponent<CombatComponent>().HealthPoints); // 60 + 20
    Assert.IsFalse(inventory.HasItem(ItemType.Apple)); // Item removed
}
```

### Integration Test: Health Capping
```csharp
[Test]
public void EatItem_AtNearMaxHealth_CapsAtMaximum()
{
    // Arrange
    CharacterAgent agent = CreateTestAgent(health: 95, maxHealth: 100);
    InventoryComponent inventory = agent.GetComponent<InventoryComponent>();
    inventory.AddItem("apple-001", ItemType.Apple, quantity: 1);
    InventoryToolSet toolSet = new InventoryToolSet(agent, mockMapSystem);

    // Act
    ToolResponse response = await toolSet.ExecuteToolAsync(
        new ToolCall("eat_item", "{\"itemType\":\"Apple\"}")
    );

    // Assert
    Assert.IsTrue(response.success);
    Assert.AreEqual(100, agent.GetComponent<CombatComponent>().HealthPoints); // Capped
    Assert.AreEqual(5, ((dynamic)response.data).healthRestored); // Only 5 HP restored
}
```

### Integration Test: Item Not Consumable
```csharp
[Test]
public void EatItem_WithNonConsumableItem_ReturnsError()
{
    // Arrange
    CharacterAgent agent = CreateTestAgent();
    InventoryComponent inventory = agent.GetComponent<InventoryComponent>();
    inventory.AddItem("key-001", ItemType.Key, quantity: 1); // Key is NOT consumable
    InventoryToolSet toolSet = new InventoryToolSet(agent, mockMapSystem);

    // Act
    ToolResponse response = await toolSet.ExecuteToolAsync(
        new ToolCall("eat_item", "{\"itemType\":\"Key\"}")
    );

    // Assert
    Assert.IsFalse(response.success);
    Assert.AreEqual("ITEM_NOT_CONSUMABLE", response.errorCode);
    Assert.IsTrue(inventory.HasItem(ItemType.Key)); // Item NOT removed
}
```

## Performance Considerations

- **Execution Time**: <5ms (inventory lookup + health modification + logging)
- **Memory Impact**: Negligible (decrements counter, no allocations)
- **Context Growth**: +1 message per consumption (~500 bytes)

## Error Handling Best Practices

### For LLMs
When the eat_item tool fails, the LLM should:
1. **Parse error code** from `ToolResponse.errorCode`
2. **Adjust strategy**:
   - `ITEM_NOT_IN_INVENTORY` → Search for food items, ask allies for items
   - `ITEM_NOT_CONSUMABLE` → Choose correct item (Apple vs Key)
   - `INSUFFICIENT_ACTION_POINTS` → Defer eating to next turn or end turn
3. **Learn from context**: Previous failed attempts logged in context prevent repeated mistakes

### For Developers
```csharp
ToolResponse response = await toolSet.ExecuteToolAsync(toolCall);
if (!response.success)
{
    Debug.LogWarning($"[Eat Item Tool] Failed: {response.errorCode} - {response.message}");

    // Graceful degradation: NPC continues turn without healing
    // Could trigger "search for food" behavior in future AI logic
}
```

## LLM Prompt Integration

**System Prompt Addition**:
```
CONSUMABLE ITEMS:
- Apple: Restores 20 HP. Consumable, Edible. Stack limit: 3.
- Use eat_item tool when health is low and you have consumables.
- Health caps at maximum (100 HP) - eating at full health wastes the item.
- Eating consumes 1 action point and removes item from inventory permanently.

RESOURCE MANAGEMENT STRATEGY:
- Eat when health < 50% and consumable available
- Avoid eating when health > 80% (save for emergencies)
- Track consumable count in context to avoid running out
```

## Version History

- **1.0.0** (2025-10-27): Initial implementation
  - Health restoration only (Apple: +20 HP)
  - 1 action point cost
  - Health capping at maximum
  - ItemType enum parameter with validation

## Future Enhancements (Out of Scope Phase 1)

- **Multiple Consumable Types**: Bread (+30 HP), Potion (+50 HP), Berry (+10 HP)
- **Mana Restoration**: RestoreMana effect for mage characters
- **Buffs/Debuffs**: ApplyBuff (temporary stat boost), RemoveDebuff (cure poison)
- **Drinking Mechanics**: Separate `drink_item` tool for beverages (water, potions)
- **Consumption Animation**: 0.5s eating animation before health applies
- **Quantity Parameter**: Allow eating multiple items at once (e.g., eat 2 Apples)
- **Over-Eating Penalty**: Eating too much causes temporary slow debuff (balance mechanic)
