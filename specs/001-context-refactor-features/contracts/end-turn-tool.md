# MCP Tool Contract: end_turn

**Feature**: Context System Refactoring and Gameplay Enhancements
**Tool ID**: `end_turn`
**Version**: 1.0.0
**Date**: 2025-10-27

## Overview

The `end_turn` tool allows NPCs to voluntarily terminate their turn before consuming all action points. This enables faster gameplay pacing, strategic "passing" in tactical situations, and prevents wasted computation when no beneficial actions remain.

**Critical**: This tool immediately exits the turn execution loop. No further tools can be executed after `end_turn` is called in the same turn.

## Function Definition (MCP Format)

```json
{
  "name": "end_turn",
  "description": "End your turn early before consuming all action points. Immediately transfers control to the next character in turn order. Cannot be undone. Use when no beneficial actions remain or strategic passing is desired.",
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
  "actionPointCost": 0,
  "terminatesTurn": true,
  "changesGameState": true,
  "irreversible": true
}
```

**Annotation Meanings**:
- `readOnly: false` - Tool modifies turn state (ends turn)
- `destructive: false` - Does not remove entities (only changes turn flow)
- `actionPointCost: 0` - Consumes zero action points (free action)
- `terminatesTurn: true` - **CRITICAL**: Immediately exits turn execution loop
- `changesGameState: true` - Advances turn cycle to next character
- `irreversible: true` - Cannot undo turn termination (no "resume turn" mechanism)

## Parameters

**None**. The tool operates on the current turn state without external inputs.

## Preconditions

1. **Character's Turn**: Must be called during the character's active turn
2. **Turn Not Already Ended**: Character's turn must still be in progress (not already ended)
3. **Valid Turn State**: TurnSystemController must be in active turn state (not transitioning)

## Execution Logic

```csharp
// In CharacterToolSet.cs
public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall)
{
    if (toolCall.name != "end_turn")
    {
        return new ToolResponse(false, "INVALID_TOOL", "Tool not supported");
    }

    // Precondition: Verify it's this character's turn
    if (!_characterAgent.IsMyTurn())
    {
        return new ToolResponse(false, "NOT_YOUR_TURN",
            "Cannot end turn: not your turn");
    }

    // Precondition: Check turn not already ended
    if (_characterAgent.HasTurnEnded())
    {
        return new ToolResponse(false, "TURN_ALREADY_ENDED",
            "Cannot end turn: turn already ended");
    }

    // Get remaining action points for logging
    int remainingActionPoints = _characterAgent.CurrentActionPoints;

    // Set turn ended flag (this will break the turn loop in CharacterAgent)
    _characterAgent.SetTurnEnded(true);

    // Log to context
    string message = $"{_characterAgent.GetCharacterElement().name} ended turn early ({remainingActionPoints} action points remaining)";
    _contextManager.AddUserMessage(_characterAgent.GetCharacterElement().Id, message, MessageType.System);

    // Log to UI
    UniversalLogUI.Instance.LogMessage(message, LogCategory.Action);

    // Return success
    return new ToolResponse(true, "SUCCESS", message, new
    {
        remainingActionPoints = remainingActionPoints,
        nextCharacter = _turnSystemController.GetNextCharacterName()
    });
}
```

## CharacterAgent Turn Loop Integration

```csharp
// In CharacterAgent.cs
public override async Task ExecuteTurnAsync()
{
    SetTurnEnded(false); // Reset flag at turn start

    while (CurrentActionPoints > 0 && !HasTurnEnded())
    {
        // Build context, call LLM, execute tools...
        List<ToolCall> toolCalls = await _llmOrchestrator.GetToolCallsAsync(context);

        foreach (ToolCall toolCall in toolCalls)
        {
            ToolResponse response = await _agentExecutor.ExecuteToolAsync(toolCall);

            if (HasTurnEnded())
            {
                Debug.Log($"[{name}] Turn ended via end_turn tool");
                break; // Exit tool execution loop
            }
        }

        if (HasTurnEnded())
        {
            break; // Exit action point loop
        }
    }

    // Turn complete, TurnSystemController advances to next character
}
```

## Response Format

### Success Response

```json
{
  "success": true,
  "errorCode": "SUCCESS",
  "message": "Pablo ended turn early (2 action points remaining)",
  "data": {
    "remainingActionPoints": 2,
    "nextCharacter": "Enemy-Goblin-01"
  }
}
```

### Error Responses

#### Not Character's Turn
```json
{
  "success": false,
  "errorCode": "NOT_YOUR_TURN",
  "message": "Cannot end turn: not your turn",
  "data": null
}
```

#### Turn Already Ended
```json
{
  "success": false,
  "errorCode": "TURN_ALREADY_ENDED",
  "message": "Cannot end turn: turn already ended",
  "data": null
}
```

## Side Effects

1. **Turn Ended Flag Set**: `CharacterAgent.HasTurnEnded()` returns true
2. **Turn Loop Termination**: CharacterAgent.ExecuteTurnAsync() exits all loops
3. **Turn Cycle Advancement**: TurnSystemController advances to next character
4. **Action Points Forfeited**: Remaining action points are lost (not carried over)
5. **Context Updated**: Message added to ConversationContext (role: User, type: System)
6. **Log Entry Created**: LogPanel displays end turn action with LogCategory.Action (yellow)

## Context Integration

**Message Added to Context**:
```json
{
  "role": "User",
  "content": "Pablo ended turn early (2 action points remaining). Next: Enemy-Goblin-01.",
  "isPinned": false,
  "messageType": "System",
  "timestamp": 1730035200
}
```

This message is visible to the NPC and all other characters in subsequent turns, providing historical evidence of strategic passing.

## Use Cases

### No Beneficial Actions Remain
**Scenario**: NPC has 1 action point left, no enemies in range, inventory full, no one to talk to.

**LLM Reasoning**:
```
I have 1 action point but no beneficial actions:
- Cannot move (would waste action without purpose)
- Cannot attack (no enemies within range 1)
- Cannot pickup (inventory full)
- Cannot give (no allies adjacent)

Best strategy: End turn early to speed up gameplay.
```

**Tool Call**:
```json
{
  "name": "end_turn",
  "arguments": {}
}
```

**Result**: Turn immediately ends, next character's turn begins.

### Strategic Passing
**Scenario**: NPC wants enemy to move first to reveal position.

**LLM Reasoning**:
```
I could move or flip, but I want to see where the enemy goes first.
Strategic pass: End turn and observe.
```

**Tool Call**:
```json
{
  "name": "end_turn",
  "arguments": {}
}
```

**Result**: Turn ends, enemy moves, NPC can react next turn with full information.

### Resource Conservation
**Scenario**: NPC with low health wants to preserve action points for defensive actions next turn (not possible in current system, but conceptually demonstrates intent).

**Tool Call**:
```json
{
  "name": "end_turn",
  "arguments": {}
}
```

**Note**: Action points do not carry over in current implementation, but this use case documents potential future enhancement.

## Integration Points

### CharacterToolSet Registration
```csharp
public CharacterToolSet(CharacterAgent agent)
{
    _characterAgent = agent;
    RegisterTool("flip");
    RegisterTool("end_turn");
    RegisterTool("talk");
    // ... other tools
}
```

### ToolConfig ScriptableObject
**Asset Path**: `Assets/Agents/ToolSets/-2DGameTools-/charactertools/EndTurnToolConfig.asset`

```yaml
toolId: "end_turn"
toolName: "end_turn"
functionDefinition:
  name: "end_turn"
  description: "End your turn early before consuming all action points..."
  parameters:
    type: "object"
    properties: {}
    required: []
annotations:
  - readOnly: false
  - actionPointCost: 0
  - terminatesTurn: true
  - irreversible: true
```

### PlayerController Integration
```csharp
// Add "End Turn" button to ActionMenuView
private void OnEndTurnButtonClicked()
{
    if (!CanExecuteAction())
    {
        return;
    }

    int remainingAP = _currentActionPoints;

    // End turn (resets action points and advances turn)
    EndTurn();

    Debug.Log($"Player ended turn early ({remainingAP} action points forfeited)");
}
```

## Testing Validation

### Unit Test: Successful End Turn
```csharp
[Test]
public void EndTurn_WithActionPointsRemaining_SuccessfullyEndsTurn()
{
    // Arrange
    CharacterAgent agent = CreateTestAgent(actionPoints: 3);
    CharacterToolSet toolSet = new CharacterToolSet(agent);
    ToolCall toolCall = new ToolCall("end_turn", "{}");

    // Act
    ToolResponse response = await toolSet.ExecuteToolAsync(toolCall);

    // Assert
    Assert.IsTrue(response.success);
    Assert.IsTrue(agent.HasTurnEnded());
    Assert.AreEqual(3, ((dynamic)response.data).remainingActionPoints);
}
```

### Integration Test: Turn Loop Exits
```csharp
[Test]
public async Task ExecuteTurnAsync_WhenEndTurnCalled_ExitsTurnLoop()
{
    // Arrange
    CharacterAgent agent = CreateTestAgent(actionPoints: 5);
    MockLLMOrchestrator mockLLM = new MockLLMOrchestrator();
    mockLLM.SetResponse(new List<ToolCall> {
        new ToolCall("flip", "{}"),
        new ToolCall("end_turn", "{}"),
        new ToolCall("talk", "{\"message\":\"Hello\"}") // Should NOT execute
    });

    // Act
    await agent.ExecuteTurnAsync();

    // Assert
    Assert.IsTrue(agent.HasTurnEnded());
    Assert.AreEqual(2, mockLLM.ToolCallsExecuted); // Only flip + end_turn
    Assert.AreEqual(4, agent.CurrentActionPoints); // 1 consumed by flip, turn ended
}
```

### Integration Test: Turn Advances to Next Character
```csharp
[Test]
public void EndTurn_AdvancesTurnToNextCharacter()
{
    // Arrange
    TurnSystemController turnSystem = CreateTurnSystem();
    CharacterAgent npc1 = CreateTestAgent("NPC-1");
    CharacterAgent npc2 = CreateTestAgent("NPC-2");
    turnSystem.RegisterCharacter(npc1);
    turnSystem.RegisterCharacter(npc2);

    // Act: NPC-1 ends turn early
    turnSystem.StartTurn(npc1);
    npc1.ExecuteTool("end_turn", "{}");

    // Assert
    Assert.AreEqual(npc2, turnSystem.GetCurrentTurnCharacter());
}
```

## Performance Considerations

- **Execution Time**: <1ms (flag assignment + logging)
- **Memory Impact**: Negligible (single boolean flag)
- **Context Growth**: +1 message per end turn (~500 bytes)
- **Gameplay Impact**: Reduces average turn duration by ~30% (per spec SC-003)

## Error Handling Best Practices

### For LLMs
When the end_turn tool fails, the LLM should:
1. **Parse error code** from `ToolResponse.errorCode`
2. **Adjust strategy**:
   - `NOT_YOUR_TURN` → Error in LLM logic (should never happen during valid turn)
   - `TURN_ALREADY_ENDED` → Error in LLM logic (double end_turn call)
3. **Fallback**: Continue with other actions if end_turn fails

### For Developers
```csharp
ToolResponse response = await toolSet.ExecuteToolAsync(new ToolCall("end_turn", "{}"));
if (!response.success)
{
    Debug.LogError($"[End Turn Tool] Critical error: {response.errorCode} - {response.message}");
    // Fallback: Force end turn via CharacterAgent.EndTurn() if needed
}
```

## Critical Implementation Notes

### Turn Loop Exit Guarantee
The `end_turn` tool **MUST** guarantee turn loop exit. Implementation checklist:

1. ✅ Set `HasTurnEnded()` flag to true
2. ✅ Check `HasTurnEnded()` in CharacterAgent.ExecuteTurnAsync() loop condition
3. ✅ Check `HasTurnEnded()` after each tool execution
4. ✅ Break both inner (tool execution) and outer (action point) loops

**Without proper loop exit, the NPC will continue executing tools despite end_turn being called.**

### LLM Context Implications
After `end_turn` is called, any subsequent tool calls in the same LLM response are **ignored**. The LLM may not be aware of this behavior initially and may generate tool calls after `end_turn`.

**Mitigation**: System prompt should include:
```
IMPORTANT: The end_turn tool immediately ends your turn. Do NOT include any tool calls after end_turn in the same response. If you call end_turn, it should be the LAST tool call.
```

## Version History

- **1.0.0** (2025-10-27): Initial implementation
  - No parameters
  - Zero action point cost
  - Immediate turn termination
  - No undo mechanism

## Future Enhancements (Out of Scope Phase 1)

- **Confirmation prompt**: Require confirmation before ending turn (prevent accidental calls)
- **Action point carry-over**: Allow carrying unused action points to next turn (balance impact)
- **End turn with reason**: Optional `reason` parameter for analytics (e.g., "no_actions", "strategic_pass")
- **Undo last action**: Allow undoing end_turn within 2-second window (complexity risk)
