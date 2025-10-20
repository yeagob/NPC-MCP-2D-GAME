# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Information

**Repository**: `yeagob/NPC-MCP-2D-GAME` (private)
**Base Branch**: Always work from `develop`
**Project Type**: 2D turn-based game with LLM-powered NPCs using Model Context Protocol (MCP)

---

## Development Commands

### Unity Editor
- Open project in Unity Hub with Unity 6000.0.45
- Main scene: Located in `Assets/Scenes/`

### Git Workflow
```bash
# Ensure you're on develop branch
git checkout develop

# Create feature branch from develop
git branch feature/your-feature-name
git checkout feature/your-feature-name

# Standard commit workflow
git add .
git commit -m "feat: your message"
git push origin feature/your-feature-name
```

**Branch Strategy**: All development happens from `develop`. Feature branches merge back to `develop`.

---

## Code Standards

### Critical Rules

1. **Language**: All code (variables, methods, classes, comments) in English
2. **Type Safety**: Never use `var` - everything must be explicitly typed
3. **No Hardcoding**: Extract all magic numbers/strings to configuration files
4. **No Comments**: Code must be self-explanatory through naming
5. **SOLID Principles**: Apply rigorously
6. **POCO Philosophy**: Data structures follow Plain Old CLR Object pattern
7. **No External Dependencies**: Do not use System.Text.Json or similar libraries
8. **Brace Style**: Always place braces on new line:
```csharp
if (condition)
{
    // code
}
```

### File Organization

- **One Class Per File**: Each class, struct, enum, or interface in separate file
- **Folder Structure**: Organize by type (Enums/, Models/, Services/, etc.)
- **Configuration Files**: Create dedicated config files for all constants

### Example - Bad vs Good

❌ **Bad**:
```csharp
var damage = 10; // hardcoded value, var usage
```

✅ **Good**:
```csharp
int damage = CombatConfiguration.BaseAttackDamage;
```

---

## High-Level Architecture

### Core System Interaction

```
TurnSystemController (Turn Management)
    ├─→ PlayerController (Human Input)
    │   └─→ Action Menu → Game State Update
    │
    └─→ CharacterAgent (LLM-Driven NPCs)
        ├─→ ChatOrchestrator (Conversation Lifecycle)
        ├─→ LLMOrchestrator (Multi-Agent Coordination)
        └─→ AgentExecutor (Tool Execution)
            ├─→ CharacterToolSet (move, talk, flip)
            ├─→ InventoryToolSet (pickup, drop, give)
            └─→ CombatToolSet (attack)
```

### Turn-Based Game Loop

**Flow**: Player → NPC1 → NPC2 → ... → Player (infinite cycle)

Each character receives action points per turn. Actions consume points until depleted.

**Key Classes**:
- `TurnSystemController`: Orchestrates turn sequence
- `ITurnCharacter`: Interface implemented by PlayerController and CharacterAgent
- `TurnCharacter`: Base class for turn participants

### LLM Integration (CharacterAgent System)

NPCs make decisions through Large Language Models with MCP tool calling:

**NPC Turn Execution**:
```
1. Build Context Prompts
   ├─→ Map vision (JSON of visible cells)
   ├─→ Inventory status
   └─→ Combat status (health, damage taken)

2. Send to LLM
   ├─→ System prompt (character personality)
   ├─→ Context prompts (game state)
   ├─→ Available tools (move, attack, etc.)
   └─→ Conversation history

3. Process LLM Response
   ├─→ Parse tool calls
   ├─→ Execute tools via IToolSet implementations
   ├─→ Update game state
   └─→ Store in conversation context

4. Clean Up
   └─→ Remove turn-specific prompts
```

**Orchestrator Layers** (responsibility separation):
- **ChatOrchestrator**: Manages conversation lifecycle
- **LLMOrchestrator**: Coordinates multiple agents
- **AgentExecutor**: Executes single agent with tools

### Model Context Protocol (MCP) Tools

Tools enable NPCs to interact with game world:

**Tool Registration Pattern**:
```csharp
CharacterAgent.Initialize()
└─→ CreateToolSets()
    ├─→ characterToolSet = new CharacterToolSet(this)
    ├─→ inventoryToolSet = new InventoryToolSet(this, mapSystem)
    ├─→ combatToolSet = new CombatToolSet(this, mapSystem)
    └─→ agentExecutor.RegisterToolSet(toolSet)
```

**Available Tool Sets**:
- `CharacterToolSet`: Movement, communication, orientation
- `InventoryToolSet`: Item pickup, drop, transfer
- `CombatToolSet`: Attack nearby characters

**Tool Execution Flow**:
```
LLM returns tool call → AgentExecutor routes to IToolSet →
Tool executes game logic → Returns ToolResponse →
Response added to context → Follow-up LLM synthesis
```

### Map/Grid System

**Two-Layer Architecture**:
- **GridSystem**: Abstract coordinate layer (row/col positions)
- **MapSystem**: Game element management (characters, items, obstacles)

**Map Elements** (all inherit from `MapElement`):
- `CharacterElement`: NPCs and player (with movement, stats)
- `ItemElement`: Collectible objects
- `ObstacleElement`: Impassable terrain
- `MapElementType`: Character, Item, Obstacle, DeadBody

**Spatial Queries**:
```csharp
mapSystem.GetElementById(id)
mapSystem.GetDistanceBetweenElements(a, b)
mapSystem.GetElementsInVisionRange(character, range)
mapSystem.IsNavigationViable(targetCell)
```

### Combat System (Phase 1)

Component-based combat integrated with MCP:

**Key Components**:
- `CombatComponent`: Health, attack logic, death handling
- `CombatToolSet`: MCP tool for LLM-driven attacks
- `TurnManager`: Removes dead characters from turn order
- `CombatConfiguration`: Attack range, damage, death rotation

**Combat Flow**:
```
NPC/Player initiates attack
└─→ CombatComponent.Attack(target)
    ├─→ Validate range (≤1 cell)
    ├─→ Validate facing direction
    ├─→ Play attack animation (left/right animator)
    ├─→ Apply damage to target.HealthPoints
    ├─→ If target dies:
    │   ├─→ Rotate sprite 90°
    │   └─→ SetElementType(MapElementType.DeadBody)
    └─→ Return AttackResult (success/damage/death)
```

**Combat Context for LLM**:
Each turn, NPCs receive combat prompt with:
- Current/max health
- Damage taken this turn
- Attacker name (if attacked)
- Combat rules (range, facing requirements)

### Inventory System

Per-character item management:

**InventoryComponent**:
- 3 slots (one per ItemType: Key, Money, Apple)
- Stack limits vary by item type
- Range validation for pickup/transfer

**Inventory Tools**:
- `pickup_item(itemId)`: Collect from map
- `drop_item(itemType, quantity)`: Place on map
- `give_item(targetId, itemType, quantity)`: Transfer to character

### Configuration System (ScriptableObjects)

All game configuration is data-driven:

**Agent Configuration**:
```
AgentConfig (per NPC)
├─→ ProviderConfiguration (API credentials, endpoint)
├─→ ModelConfig (model name, temperature, tokens)
├─→ PromptConfig (system prompt - personality)
├─→ PromptConfig[] (context prompts - dynamic)
└─→ ToolConfig[] (available tools)
```

**Tool Configuration**:
```
ToolConfig
├─→ toolId, toolName
├─→ FunctionDefinition (MCP format)
│   ├─→ name
│   ├─→ description
│   └─→ parameters (JSON schema)
└─→ annotations (hints: readOnly, destructive, etc.)
```

**Configuration Locations**:
- Agents: `Assets/Agents/-2DGameAgents-/`
- Tools: `Assets/Agents/ToolSets/-2DGameTools-/`
- Models: `Assets/Agents/Models/`
- Providers: `Assets/Agents/Providers/`

---

## Key Design Patterns

### 1. Orchestrator Pattern (Multi-Layer)
Three-layer orchestration for LLM integration:
- ChatOrchestrator → LLMOrchestrator → AgentExecutor
- Each layer has single responsibility
- Message flow: User → Chat → LLM → Agent → Tools

### 2. Tool Provider Pattern (Strategy + Factory)
- `IToolSet` interface with multiple implementations
- Tools registered dynamically per agent
- AgentExecutor routes by tool name

### 3. Component-Based Architecture
- CharacterElement + InventoryComponent + CombatComponent
- Loose coupling via GetComponent<T>()
- Extensible through additional components

### 4. ScriptableObject Configuration
- All configs are ScriptableObjects (AgentConfig, ToolConfig, etc.)
- No hardcoded values in game logic
- Runtime reconfiguration possible

### 5. Prompt-as-Config Pattern
- Dynamic context prompts added/removed per turn
- Game state injected as JSON prompts
- Allows flexible LLM context without code changes

---

## Critical Data Flows

### NPC Decision Making

```
CharacterAgent.ExecuteTurn() called by TurnSystemController
    ↓
Build context: map vision + inventory + combat status
    ↓
ChatOrchestrator.ProcessUserMessageAsync()
    ↓
LLMOrchestrator.ProcessMessageAsync()
    ↓
AgentExecutor.ExecuteAgentAsync()
    ├─→ Build LLMRequest (system prompt + context + tools)
    ├─→ Call LLM API (OpenAI/QWEN)
    ├─→ Parse tool calls from response
    ├─→ Execute tools via registered IToolSets
    ├─→ Collect ToolResponses
    └─→ Follow-up LLM call with tool results
    ↓
Update game state (MapSystem, InventoryComponent, etc.)
    ↓
Store in ConversationContext (memory for next turn)
    ↓
Clean up turn-specific prompts
    ↓
Next character's turn
```

### Tool Execution Chain

```
LLM returns: {"toolCalls": [{"name": "move", "arguments": {"row": 5, "col": 3}}]}
    ↓
AgentExecutor.ExecuteToolCallsAsync()
    ↓
Iterate registered IToolSets
    ├─→ CharacterToolSet.IsToolSupported("move") → true
    └─→ CharacterToolSet.ExecuteToolAsync(ToolCall)
        ├─→ Parse arguments (row: 5, col: 3)
        ├─→ CharacterAgent.Teleport(5, 3)
        │   └─→ CharacterElement.TryMoveTo(5, 3)
        │       └─→ MapSystem.MoveElement(this, targetCell)
        ├─→ Log action via UniversalLogUI
        └─→ Return ToolResponse("success", "Position: 5,3")
    ↓
Add ToolResponse to conversation context
    ↓
Follow-up LLM synthesis with tool results
```

### Combat Execution

```
Tool call: attack(targetCharacterId: "5")
    ↓
CombatToolSet.ExecuteToolAsync()
    ├─→ MapSystem.GetElementById("5") → CharacterElement
    ├─→ Validate target is Character type
    └─→ CombatComponent.Attack(targetCharacter)
        ├─→ IsTargetInRange() check (≤1 cell)
        ├─→ IsFacingTarget() check (orientation match)
        ├─→ PlayAttackAnimation() (left/right animator)
        ├─→ targetCharacter.ModifyHealth(-damage)
        ├─→ targetCombat.RegisterAttackReceived(attackerName, damage)
        ├─→ Check if targetCharacter.HealthPoints <= 0
        └─→ If died:
            ├─→ RotateDeadSprite() (90° rotation)
            └─→ ChangeToDeadBodyType() (MapElementType.DeadBody)
    ↓
Return AttackResult (success, damageDealt, targetDied)
    ↓
Next turn: TurnManager.RemoveDeadCharacters()
    └─→ Unregister dead from turn order
```

---

## Important Implementation Notes

### When Adding New Features

1. **Configuration First**: Create configuration file for all constants
2. **Interface Design**: Define interfaces before implementations
3. **POCO Models**: Create data structures as simple classes/structs
4. **Component Pattern**: Consider if feature should be a component
5. **MCP Integration**: If NPCs need access, create IToolSet implementation
6. **Context Prompts**: Add relevant info to LLM context for AI awareness

### When Modifying Existing Code

1. **Check Config Files**: Look for hardcoded values to extract
2. **Maintain SOLID**: Don't violate single responsibility
3. **Update Documentation**: Keep technical docs in `Assets/Docs/` updated
4. **Preserve Patterns**: Follow existing architectural patterns
5. **No Breaking Changes**: Ensure backward compatibility with ScriptableObjects

### Common Pitfalls to Avoid

❌ Using `var` instead of explicit types
❌ Hardcoding values instead of using configuration
❌ Adding external dependencies (System.Text.Json, etc.)
❌ Writing comments instead of self-documenting code
❌ Creating multiple classes in one file
❌ Working directly on `main` branch instead of `develop`
❌ Placing braces on same line as conditionals

### LLM Provider Configuration

**Currently Supported**:
- OpenAI (fully functional): GPT-4, GPT-3.5-turbo, GPT-4-turbo
- QWEN (architecture ready): qwen-max, qwen-plus, qwen-turbo

**Adding New Provider**:
1. Create service class implementing LLM calls
2. Add ServiceProvider enum value
3. Create ProviderConfiguration ScriptableObject
4. Implement response parsing to LLMResponse format
5. Handle tool calling format conversion

---

## Project File Structure (Key Paths)

```
Assets/
├── Scripts/
│   ├── Configuration/
│   │   ├── ScriptableObjects/     # Config definitions
│   │   ├── CombatConfiguration.cs # Combat constants
│   │   ├── InventoryConfiguration.cs
│   │   └── PlayerActionConfiguration.cs
│   ├── Services/
│   │   ├── Orchestrators/         # ChatOrchestrator, LLMOrchestrator
│   │   ├── Agents/                # AgentExecutor, CharacterAgent
│   │   ├── Tools/                 # IToolSet implementations
│   │   ├── Context/               # ContextManager
│   │   └── LLM/                   # OpenAIService, QWENService
│   ├── Models/
│   │   ├── Context/               # Message, ConversationContext
│   │   ├── Tools/MCP/             # FunctionDefinition, ToolCall
│   │   ├── Agents/                # Agent, AgentResponse
│   │   ├── Combat/                # AttackResult, CombatContext (POCO)
│   │   └── Inventory/             # InventoryItem
│   ├── Components/
│   │   ├── InventoryComponent.cs
│   │   └── CombatComponent.cs
│   ├── Controllers/
│   │   ├── PlayerController.cs
│   │   └── TurnSystemController.cs
│   ├── Map/
│   │   ├── Systems/               # GridSystem, MapSystem
│   │   └── Elements/              # CharacterElement, ItemElement
│   └── Enums/
│       ├── ItemType.cs
│       ├── PlayerActionState.cs
│       ├── MapElementType.cs
│       └── AttackDirection.cs
├── Agents/
│   ├── -2DGameAgents-/            # Agent configs
│   ├── ToolSets/                  # Tool configs
│   ├── Models/                    # Model configs
│   └── Providers/                 # Provider configs
└── Docs/
    └── Combat System/             # Technical documentation
```

---

## Philosophy: Simplificar es Ganar

This project prioritizes:
- **Clarity over cleverness**: Self-documenting code
- **Configuration over hardcoding**: Data-driven design
- **Interfaces over implementations**: Dependency inversion
- **Components over monoliths**: Separation of concerns
- **Analysis before implementation**: Think first, code second

When in doubt, choose the simpler solution that maintains architectural consistency.
