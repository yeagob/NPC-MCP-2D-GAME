# MCP Tool Contract: flip

**Feature**: Context System Refactoring and Gameplay Enhancements
**Tool ID**: `flip`
**Version**: 1.0.0
**Date**: 2025-10-27

## Overview

The `flip` tool allows NPCs to rotate their character orientation 180 degrees without changing grid position. This enables tactical positioning for combat (facing direction affects attack validity) and defensive maneuvers without consuming movement resources.

## Function Definition (MCP Format)

```json
{
  "name": "flip",
  "description": "Flip character orientation 180 degrees (left ↔ right) without changing grid position. Consumes 1 action point. Useful for preparing attacks or defensive positioning.",
  "parameters": {
    "type": "object",
    "properties": {},
    "required": []
  }
}
```

## Tool Annotations

```json
{
  "readOnly": false,
  "destructive": false,
  "actionPointCost": 1,
  "changesGameState": true,
  "blockedDuringAnimation": true
}
```

**Annotation Meanings**:
- `readOnly: false` - Tool modifies game state (character orientation)
- `destructive: false` - Does not remove entities or cause irreversible changes
- `actionPointCost: 1` - Consumes exactly 1 action point per execution
- `changesGameState: true` - Alters character facing direction (persists across turns)
- `blockedDuringAnimation: true` - Cannot execute during movement animation

## Parameters

**None**. The tool operates on the calling character's current state without external inputs.

## Preconditions

1. **Action Points Available**: Character must have ≥1 action point remaining
2. **Movement Enabled**: Character's movement capability must not be disabled (e.g., stunned, rooted)
3. **Not Animating**: No movement animation in progress for the character
4. **Character's Turn**: Must be called during the character's active turn

## Execution Logic

```csharp
// In CharacterToolSet.cs
public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall)
{
    if (toolCall.name != "flip")
    {
        return new ToolResponse(false, "INVALID_TOOL", "Tool not supported");
    }

    // Precondition: Check action points
    if (_characterAgent.CurrentActionPoints < 1)
    {
        return new ToolResponse(false, "INSUFFICIENT_ACTION_POINTS",
            "Cannot flip: no action points remaining");
    }

    // Precondition: Check movement enabled
    CharacterElement element = _characterAgent.GetCharacterElement();
    if (!element.CanMove())
    {
        return new ToolResponse(false, "MOVEMENT_DISABLED",
            "Cannot flip: movement disabled");
    }

    // Execute: Flip orientation
    ViewDirection currentDirection = element.CurrentViewDirection;
    ViewDirection newDirection = (currentDirection == ViewDirection.Left)
        ? ViewDirection.Right
        : ViewDirection.Left;

    element.SetViewDirection(newDirection);

    // Consume action point
    _characterAgent.ConsumeActionPoint();

    // Log to context
    string message = $"{element.name} flipped to face {newDirection}";
    _contextManager.AddUserMessage(element.Id, message, MessageType.Movement);

    // Log to UI
    UniversalLogUI.Instance.LogMessage(message, LogCategory.Action);

    // Return success
    return new ToolResponse(true, "SUCCESS", message, new
    {
        newDirection = newDirection.ToString(),
        position = new { row = element.CurrentGridCell.row, col = element.CurrentGridCell.col }
    });
}
```

## Response Format

### Success Response

```json
{
  "success": true,
  "errorCode": "SUCCESS",
  "message": "Pablo flipped to face Right",
  "data": {
    "newDirection": "Right",
    "position": {
      "row": 5,
      "col": 3
    }
  }
}
```

### Error Responses

#### Insufficient Action Points
```json
{
  "success": false,
  "errorCode": "INSUFFICIENT_ACTION_POINTS",
  "message": "Cannot flip: no action points remaining",
  "data": null
}
```

#### Movement Disabled
```json
{
  "success": false,
  "errorCode": "MOVEMENT_DISABLED",
  "message": "Cannot flip: movement disabled",
  "data": null
}
```

#### Not Character's Turn
```json
{
  "success": false,
  "errorCode": "NOT_YOUR_TURN",
  "message": "Cannot flip: not your turn",
  "data": null
}
```

## Side Effects

1. **Character Orientation Changed**: `CharacterElement.CurrentViewDirection` updated
2. **Sprite Flipped**: Character sprite visual orientation updated (SpriteRenderer.flipX)
3. **Action Point Consumed**: `CharacterAgent.CurrentActionPoints` decremented by 1
4. **Context Updated**: Message added to ConversationContext (role: User, type: Movement)
5. **Log Entry Created**: LogPanel displays flip action with LogCategory.Action (yellow)

## Context Integration

**Message Added to Context**:
```json
{
  "role": "User",
  "content": "Pablo flipped to face Right. New position: row 5, col 3.",
  "isPinned": false,
  "messageType": "Movement",
  "timestamp": 1730035200
}
```

This message is visible to the NPC in subsequent turns, enabling tactical decision-making based on orientation history.

## Use Cases

### Tactical Combat Positioning
**Scenario**: NPC detects enemy to the right but is facing left. Attack tool requires facing target.

**Tool Call Sequence**:
1. `flip` (face right)
2. `attack(targetCharacterId: "enemy-001")`

**Result**: NPC successfully attacks without wasting action points on movement.

### Defensive Positioning
**Scenario**: NPC wants to face a doorway to watch for incoming threats.

**Tool Call**:
```json
{
  "name": "flip",
  "arguments": {}
}
```

**Result**: NPC faces desired direction, ready to react in next turn.

## Integration Points

### CharacterToolSet Registration
```csharp
public CharacterToolSet(CharacterAgent agent)
{
    _characterAgent = agent;
    RegisterTool("flip");
    RegisterTool("end_turn");
    // ... other tools
}
```

### ToolConfig ScriptableObject
**Asset Path**: `Assets/Agents/ToolSets/-2DGameTools-/charactertools/FlipToolConfig.asset`

```yaml
toolId: "flip"
toolName: "flip"
functionDefinition:
  name: "flip"
  description: "Flip character orientation 180 degrees..."
  parameters:
    type: "object"
    properties: {}
    required: []
annotations:
  - readOnly: false
  - actionPointCost: 1
```

## Testing Validation

### Unit Test: Successful Flip
```csharp
[Test]
public void Flip_WithActionPoints_SuccessfullyFlipsOrientation()
{
    // Arrange
    CharacterAgent agent = CreateTestAgent(actionPoints: 2, direction: ViewDirection.Left);
    CharacterToolSet toolSet = new CharacterToolSet(agent);
    ToolCall toolCall = new ToolCall("flip", "{}");

    // Act
    ToolResponse response = await toolSet.ExecuteToolAsync(toolCall);

    // Assert
    Assert.IsTrue(response.success);
    Assert.AreEqual(ViewDirection.Right, agent.GetCharacterElement().CurrentViewDirection);
    Assert.AreEqual(1, agent.CurrentActionPoints); // 1 consumed
}
```

### Integration Test: Flip Then Attack
```csharp
[Test]
public void FlipThenAttack_EnablesCombatWithOrientationRequirement()
{
    // Arrange: NPC facing left, enemy to the right
    CharacterAgent npc = CreateTestAgent(position: (5, 3), direction: ViewDirection.Left);
    CharacterElement enemy = CreateTestEnemy(position: (5, 4)); // Right of NPC

    // Act: Flip to face enemy
    ToolResponse flipResponse = await npc.ExecuteTool("flip", "{}");
    ToolResponse attackResponse = await npc.ExecuteTool("attack", $"{{\"targetCharacterId\":\"{enemy.Id}\"}}");

    // Assert: Both succeed
    Assert.IsTrue(flipResponse.success);
    Assert.IsTrue(attackResponse.success);
    Assert.IsTrue(enemy.CombatComponent.HealthPoints < enemy.CombatComponent.MaxHealthPoints);
}
```

## Performance Considerations

- **Execution Time**: <1ms (direct property assignment)
- **Memory Impact**: Negligible (no allocations, only state changes)
- **Context Growth**: +1 message per flip (~500 bytes)

## Error Handling Best Practices

### For LLMs
When the flip tool fails, the LLM should:
1. **Parse error code** from `ToolResponse.errorCode`
2. **Adjust strategy**:
   - `INSUFFICIENT_ACTION_POINTS` → End turn or use no-cost action
   - `MOVEMENT_DISABLED` → Attempt other actions (talk, attack if facing)
3. **Communicate limitation** in dialogue (if applicable)

### For Developers
```csharp
ToolResponse response = await toolSet.ExecuteToolAsync(toolCall);
if (!response.success)
{
    Debug.LogWarning($"[Flip Tool] Failed: {response.errorCode} - {response.message}");
    // Handle error (e.g., skip tool, try alternative)
}
```

## Version History

- **1.0.0** (2025-10-27): Initial implementation
  - No parameters
  - 1 action point cost
  - Left ↔ Right orientation only (no Up/Down for 2D top-down)

## Future Enhancements (Out of Scope Phase 1)

- **8-directional facing**: Support diagonal orientations (NE, SE, SW, NW)
- **Flip animation**: 0.2s rotation animation instead of instant flip
- **Flip combo moves**: Reduced cost if combined with other actions
