# Implementation Tasks: Context System Refactoring and Gameplay Enhancements

**Feature Branch**: `001-context-refactor-features`
**Date**: 2025-10-27
**Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md)

## Overview

This document provides a dependency-ordered, independently testable task breakdown for implementing the context system refactoring and 7 gameplay enhancements. Tasks are organized by user story (US1-US7) to enable parallel development and incremental delivery.

**Total Tasks**: 78 tasks
**Parallelizable Tasks**: 42 tasks (54%)
**MVP Scope**: User Story 1 only (Context Foundation) - 15 tasks

---

## Task Summary by User Story

| Phase | User Story | Priority | Tasks | Parallelizable | Independent Test |
|-------|------------|----------|-------|----------------|------------------|
| 1 | Setup | N/A | 6 | 3 | N/A |
| 2 | Foundational | N/A | 4 | 0 | N/A |
| 3 | US1: Context-Aware NPCs | P1 | 15 | 8 | Multi-turn memory test |
| 4 | US2: Flip Action | P2 | 8 | 5 | Flip without movement test |
| 5 | US3: End Turn Early | P2 | 7 | 4 | Early termination test |
| 6 | US4: Consumable Items | P2 | 12 | 7 | Apple consumption test |
| 7 | US5: Animated Movement | P3 | 10 | 5 | Smooth animation test |
| 8 | US6: Logging System | P3 | 11 | 6 | 60-event log test |
| 9 | US7: Editor Auto-Position | P3 | 5 | 4 | Inspector update test |

---

## Phase 1: Setup & Configuration

**Goal**: Initialize project configuration and shared infrastructure

### Configuration & Enums

- [X] T001 [P] Create GameplayConfiguration.cs ScriptableObject in Assets/Scripts/Configuration/
- [X] T002 [P] Create LogCategory enum in Assets/Scripts/Enums/LogCategory.cs
- [X] T003 [P] Create MessageType enum in Assets/Scripts/Enums/MessageType.cs
- [X] T004 [P] Create ConsumableEffect enum in Assets/Scripts/Enums/ConsumableEffect.cs
- [X] T005 Create ItemProperty enum in Assets/Scripts/Enums/ItemProperty.cs (Flags attribute for Consumable, Edible, etc.)
- [X] T006 Create GameplayConfiguration ScriptableObject asset at Assets/Configuration/GameplayConfiguration.asset with default values (maxContextSlidingMessages=50, movementAnimationSpeed=5.0, maxLogEntries=50)

---

## Phase 2: Foundational Components

**Goal**: Implement blocking prerequisites needed by all user stories

### Message System Extensions

- [X] T007 Modify MessageRole enum in Assets/Scripts/Enums/MessageRole.cs to add Tool role (System, User, Assistant, Tool)
- [X] T008 Modify Message.cs in Assets/Scripts/Models/Context/ to add isPinned (bool), toolCallId (string), timestamp (long), messageType (MessageType) fields
- [X] T009 Create ConversationContext.cs in Assets/Scripts/Models/Context/ with pinnedMessages (List), slidingMessages (Queue), AddMessage(), GetAllMessages() methods
- [X] T010 Modify ContextManager.cs in Assets/Scripts/Services/Context/ to use ConversationContext with AddSystemContextAsync(), AddToolResult(), GetMessagesForLLM() methods

---

## Phase 3: User Story 1 - NPCs Make Context-Aware Decisions (P1)

**Priority**: P1 (Foundation for all other features)

**Independent Test**: Create multi-turn scenario: Turn 1 attack NPC, Turn 2 give item, Turn 3+ verify NPC references both events in decision-making

**Acceptance Criteria**:
- NPC references attack from 3+ turns prior
- Context includes combat, inventory, dialogue events in chronological order
- Sliding window retains 50-100 messages maximum
- Pinned system prompts never removed

### Context Core Implementation

- [X] T011 [P] [US1] Implement ConversationContext sliding window logic in AddMessage() method (dequeue oldest when slidingMessages.Count >= maxSlidingMessages)
- [X] T012 [P] [US1] Implement ConversationContext.GetAllMessages() with chronological ordering (pinnedMessages + slidingMessages sorted by timestamp)
- [ ] T013 [US1] Modify CharacterAgent.ExecuteTurnAsync() in Assets/Scripts/Services/Agents/ to use ContextManager.GetMessagesForLLM() instead of building prompts manually
- [X] T014 [P] [US1] Add ContextManager.AddSystemContextAsync() calls in CharacterAgent.Initialize() for game rules and character personality (isPinned=true)
- [ ] T015 [US1] Refactor CharacterAgent turn loop to append tool results via ContextManager.AddToolResult() with MessageRole.Tool

### Context Integration

- [X] T016 [P] [US1] Modify CombatComponent.Attack() in Assets/Scripts/Components/ to call ContextManager.AddUserMessage() with combat event details (damage, death, attacker name) and MessageType.Combat
- [X] T017 [P] [US1] Modify InventoryComponent pickup/drop/give methods in Assets/Scripts/Components/ to call ContextManager.AddUserMessage() with inventory events and MessageType.Inventory
- [X] T018 [P] [US1] Modify CharacterToolSet.ExecuteToolAsync() in Assets/Scripts/Services/Tools/ to log movement actions (flip, teleport) via ContextManager with MessageType.Movement
- [X] T019 [US1] Modify TurnSystemController in Assets/Scripts/Controllers/ to log turn start/end events via ContextManager with MessageType.System

### Context Validation & Testing

- [X] T020 [P] [US1] Implement ContextManager.DebugPrintContext() helper method for console logging (timestamp, role, messageType, content preview)
- [X] T021 [P] [US1] Implement ContextManager.ExportContextToJson() for debugging (exports to file path for LLM inspection)
- [ ] T022 [US1] Add OnValidate() to ConversationContext to warn if slidingMessages exceeds configured maximum
- [ ] T023 [US1] Create multi-turn playtesting scenario in Unity Editor: attack NPC turn 1, give item turn 2, verify NPC decision references both in turn 3+
- [ ] T024 [US1] Profile ContextManager.GetMessagesForLLM() with Unity Profiler (verify <100ms per turn)
- [ ] T025 [US1] Test 100-turn game session and verify memory growth <20% (Unity Profiler Memory section)

**Story Dependencies**: None (foundational)
**Parallel Execution**: T011, T012, T014, T016, T017, T018, T020, T021 can run in parallel (different files/systems)

---

## Phase 4: User Story 2 - Players and NPCs Can Change Orientation Without Moving (P2)

**Priority**: P2 (Tactical enhancement)

**Independent Test**: Place character facing left, execute flip, verify faces right, same position, 1 AP consumed

**Acceptance Criteria**:
- Flip inverts orientation (Left ↔ Right) without grid movement
- Consumes exactly 1 action point
- Blocked when no action points or movement disabled
- Logged to context for NPC awareness

### Flip Tool Setup

- [ ] T026 [P] [US2] Create FlipToolConfig.asset ScriptableObject at Assets/Agents/ToolSets/-2DGameTools-/charactertools/ with FunctionDefinition (name: flip, description, parameters: none, required: [])
- [ ] T027 [P] [US2] Add flip tool annotations to FlipToolConfig (readOnly: false, destructive: false, actionPointCost: 1, blockedDuringAnimation: true)

### Flip Implementation

- [ ] T028 [US2] Implement ExecuteFlipAsync() in CharacterToolSet.cs in Assets/Scripts/Services/Tools/ with preconditions (AP check, movement enabled), flip logic (invert ViewDirection), AP consumption, context logging
- [ ] T029 [US2] Register flip tool in CharacterToolSet constructor (RegisterTool("flip"))
- [ ] T030 [P] [US2] Add flip button to ActionMenuView.cs in Assets/Scripts/Views/ (OnFlipButtonClicked handler)
- [ ] T031 [P] [US2] Implement OnFlipButtonClicked() in PlayerController.cs in Assets/Scripts/Controllers/ with CanExecuteAction() check, CharacterElement.SetViewDirection() call, ConsumeActionPoint()

### Flip Integration & Testing

- [ ] T032 [P] [US2] Add FlipToolConfig to all NPC AgentConfig assets in Assets/Agents/-2DGameAgents-/ (expand ToolConfigs array, drag FlipToolConfig)
- [ ] T033 [US2] Test flip action: place character facing left, flip, verify ViewDirection=Right, position unchanged, 1 AP consumed, context message logged

**Story Dependencies**: Requires US1 (context logging)
**Parallel Execution**: T026, T027, T030, T031, T032 can run in parallel

---

## Phase 5: User Story 3 - Characters Can End Their Turn Early (P2)

**Priority**: P2 (Pacing improvement)

**Independent Test**: Start turn with 3 AP, execute 1 action, end turn early, verify control transfers to next character with 2 AP forfeited

**Acceptance Criteria**:
- Immediately exits turn execution loop
- Transfers control to next character in turn order
- Forfeits remaining action points (not carried over)
- Logged to context with remaining AP count

### End Turn Tool Setup

- [ ] T034 [P] [US3] Create EndTurnToolConfig.asset ScriptableObject at Assets/Agents/ToolSets/-2DGameTools-/charactertools/ with FunctionDefinition (name: end_turn, parameters: none)
- [ ] T035 [P] [US3] Add end_turn tool annotations to EndTurnToolConfig (actionPointCost: 0, terminatesTurn: true, irreversible: true)

### End Turn Implementation

- [ ] T036 [US3] Add HasTurnEnded() flag (bool) to CharacterAgent.cs and SetTurnEnded() method
- [ ] T037 [US3] Implement ExecuteEndTurnAsync() in CharacterToolSet.cs with turn ended flag setting, remaining AP logging, context message
- [ ] T038 [US3] Modify CharacterAgent.ExecuteTurnAsync() turn loop to check HasTurnEnded() condition (while CurrentActionPoints > 0 && !HasTurnEnded())
- [ ] T039 [P] [US3] Add "End Turn" button to ActionMenuView.cs with OnEndTurnButtonClicked handler
- [ ] T040 [P] [US3] Implement OnEndTurnButtonClicked() in PlayerController.cs to call EndTurn(), log remaining AP to context

### End Turn Integration & Testing

- [ ] T041 [US3] Add EndTurnToolConfig to all NPC AgentConfig assets in Assets/Agents/-2DGameAgents-/
- [ ] T042 [US3] Test end turn: start with 3 AP, move (consume 1 AP), end turn, verify turn transfers to next character with 2 AP forfeited and context log message

**Story Dependencies**: Requires US1 (context logging)
**Parallel Execution**: T034, T035, T039, T040 can run in parallel

---

## Phase 6: User Story 4 - Characters Can Consume Items for Effects (P2)

**Priority**: P2 (Resource management)

**Independent Test**: Place Apple in inventory, reduce health to 50/100, eat Apple, verify health increases by 20, Apple removed, context logged

**Acceptance Criteria**:
- Health restoration capped at maximum HP
- Item removed from inventory on consumption
- Non-consumable items rejected with error
- Logged to context with HP restored amount

### Consumable Data Structures

- [ ] T043 [P] [US4] Create ConsumableData.cs model in Assets/Scripts/Models/Inventory/ with fields (itemType, effect, value, consumeMessage, consumeDelay)
- [ ] T044 [P] [US4] Create ItemTypeConsumableDataPair.cs wrapper class for Unity serialization (itemType, ConsumableData data)
- [ ] T045 [P] [US4] Create ConsumableConfiguration.cs ScriptableObject in Assets/Scripts/Configuration/ with List<ItemTypeConsumableDataPair> consumables, GetConsumableData(itemType) method, OnValidate duplicate check
- [ ] T046 [US4] Create ConsumableConfiguration.asset at Assets/Configuration/ with Apple entry (itemType: Apple, effect: RestoreHealth, value: 20, message: "You ate an apple and restored 20 HP")

### Consumable Item Properties

- [ ] T047 [P] [US4] Modify ItemElement.cs in Assets/Scripts/Map/Elements/ to add properties field (ItemProperty flags), IsConsumable() helper method, IsEdible() helper method
- [ ] T048 [P] [US4] Update Apple ItemElement prefab in Assets/Prefabs/Items/ to set properties flags (Consumable | Edible | Stackable)

### Eat Item Tool

- [ ] T049 [US4] Implement ExecuteEatItemAsync() in InventoryToolSet.cs in Assets/Scripts/Services/Tools/ with parameter parsing (itemType), preconditions (in inventory, is consumable, has AP), RemoveItem call, health restoration with capping (Mathf.Min), context logging
- [ ] T050 [US4] Create EatItemToolConfig.asset at Assets/Agents/ToolSets/-2DGameTools-/inventory/ with FunctionDefinition (parameters: itemType enum ["Apple"], required: ["itemType"])
- [ ] T051 [US4] Register eat_item tool in InventoryToolSet constructor
- [ ] T052 [P] [US4] Add EatItemToolConfig to all NPC AgentConfig assets

### Player Eat Action

- [ ] T053 [P] [US4] Add "Eat" action option to InventoryUI panel (Assets/Scripts/Views/) with item type selection dropdown
- [ ] T054 [P] [US4] Implement OnEatItemSelected() in PlayerController.cs with ConsumableConfiguration lookup, health modification, inventory removal, context logging

### Consumable Testing & Validation

- [ ] T055 [US4] Test Apple consumption: inventory has Apple, health 50/100, eat Apple, verify health 70/100, Apple removed, context "Ate Apple, restored 20 HP"
- [ ] T056 [US4] Test health capping: health 95/100, eat Apple (+20 potential), verify health capped at 100/100, only +5 HP restored logged
- [ ] T057 [US4] Test non-consumable rejection: attempt eat Key, verify error "Item is not consumable", Key remains in inventory

**Story Dependencies**: Requires US1 (context logging)
**Parallel Execution**: T043, T044, T045, T047, T048, T052, T053, T054 can run in parallel

---

## Phase 7: User Story 5 - Characters Move Visibly Across the Grid (P3)

**Priority**: P3 (Visual polish)

**Independent Test**: Command character to move 3 cells, observe smooth interpolation through intermediate cells, verify 60 FPS maintained

**Acceptance Criteria**:
- Smooth cell-by-cell interpolation (no teleportation)
- Facing direction updates during movement
- Input blocked during animation
- 60 FPS with 10 simultaneous animations

### Movement Animator Service

- [ ] T058 [P] [US5] Create MovementAnimator.cs service class in Assets/Scripts/Services/Animation/ with fields (GridSystem gridSystem, GameplayConfiguration config, bool isAnimating)
- [ ] T059 [P] [US5] Implement AnimateMovementAsync() in MovementAnimator.cs with async Task return, Vector3.Lerp interpolation, Task.Yield() frame yielding, duration calculation (distance / movementAnimationSpeed)
- [ ] T060 [US5] Add isAnimating property getter to MovementAnimator for input blocking checks

### Movement Integration

- [ ] T061 [US5] Modify CharacterElement.TryMoveTo() in Assets/Scripts/Map/Elements/ to call MovementAnimator.AnimateMovementAsync() if GameplayConfiguration.enableMovementAnimation is true, otherwise instant teleport
- [ ] T062 [P] [US5] Add input blocking check in PlayerController.Update() to return early if MovementAnimator.isAnimating (prevents player input during animation)
- [ ] T063 [P] [US5] Add input blocking check in CharacterAgent.ExecuteTurnAsync() to await animation completion before processing next action

### Movement Facing & Cleanup

- [ ] T064 [P] [US5] Implement facing direction update in MovementAnimator to calculate direction vector (target - start), determine ViewDirection (Left/Right), call CharacterElement.SetViewDirection()
- [ ] T065 [US5] Add safety timeout to MovementAnimator.AnimateMovementAsync() (maxDuration: 10s) to prevent infinite loops if duration calculation error

### Movement Testing

- [ ] T066 [US5] Test single character movement: move 3 cells, verify smooth interpolation through intermediate cells, precise final positioning
- [ ] T067 [US5] Performance test: spawn 10 NPCs, command all to move 5 cells simultaneously, verify Unity Profiler FPS ≥ 60

**Story Dependencies**: Requires US1 (context for movement decisions)
**Parallel Execution**: T058, T059, T062, T063, T064 can run in parallel

---

## Phase 8: User Story 6 - Improved Logging and Event History (P3)

**Priority**: P3 (UX improvement)

**Independent Test**: Execute 60 diverse actions, verify all appear in log with correct colors, only 50 most recent retained, smooth auto-scroll

**Acceptance Criteria**:
- Log panel displays events with category color coding
- Auto-scroll to most recent entry
- Maximum 50 entries retained (oldest removed)
- Clear Logs button functional

### Log Data Structures

- [ ] T068 [P] [US6] Create LogEntry.cs model in Assets/Scripts/Models/UI/ with fields (LogCategory category, string message, long timestamp, Color color, GameObject gameObject)
- [ ] T069 [P] [US6] Add GetFormattedMessage() method to LogEntry for timestamp formatting ("[HH:mm:ss] [category] message")

### Log Panel UI Component

- [ ] T070 [P] [US6] Create LogPanel.cs MonoBehaviour in Assets/Scripts/Views/ with fields (Queue<LogEntry> entries, Queue<GameObject> pool, ScrollRect scrollRect, Transform contentTransform, int maxEntries)
- [ ] T071 [US6] Implement LogPanel.AddLogEntry() with object pooling (GetPooledObject(), ReturnToPool()), max entries enforcement (dequeue oldest), color assignment (GameplayConfiguration.GetLogColor()), auto-scroll logic
- [ ] T072 [P] [US6] Implement GetPooledObject() in LogPanel to return from pool or Instantiate(logEntryPrefab) if pool empty
- [ ] T073 [P] [US6] Implement ReturnToPool() in LogPanel to SetActive(false) and enqueue to pool
- [ ] T074 [US6] Add ClearLogs() method to LogPanel to dequeue all entries and return to pool

### Log Color Configuration

- [ ] T075 [P] [US6] Add log color fields to GameplayConfiguration.cs (logColorSystem, logColorAction, logColorCombat, logColorInventory, logColorDialogue, logColorAI)
- [ ] T076 [US6] Implement GetLogColor(LogCategory) method in GameplayConfiguration to return appropriate color via switch statement

### Log Integration

- [ ] T077 [US6] Integrate LogPanel calls across codebase: CombatComponent (LogCategory.Combat), InventoryComponent (LogCategory.Inventory), CharacterToolSet (LogCategory.Action), TurnSystemController (LogCategory.System)
- [ ] T078 [US6] Test log panel: execute 60 actions (10 combat, 10 inventory, 10 movement, 10 dialogue, 10 system, 10 AI), verify all logged with correct colors, oldest removed after 50, auto-scroll functional

**Story Dependencies**: Requires US1 (context provides event sources)
**Parallel Execution**: T068, T069, T070, T072, T073, T075 can run in parallel

---

## Phase 9: User Story 7 - Editor Auto-Positioning for Map Elements (P3)

**Priority**: P3 (Editor tooling)

**Independent Test**: Open Unity Editor, select CharacterElement, change currentGridIndex to 15, verify transform.position updates to grid cell 15 world position without entering play mode

**Acceptance Criteria**:
- Grid index change in Inspector updates world position instantly
- Invalid indices clamped or ignored (no errors)
- Only functions in Edit mode (disabled in Play mode)
- Works for all MapElement types

### Editor Auto-Positioning

- [ ] T079 [P] [US7] Add OnValidate() method to MapElement.cs in Assets/Scripts/Components/ with Application.isPlaying guard (return if true), null GridSystem check, GridSystem.GetWorldPosition() call, transform.position assignment
- [ ] T080 [P] [US7] Add grid bounds validation to MapElement.OnValidate() to clamp currentGridIndex to valid range (0 to GridSystem.TotalCells - 1) or log warning if out of bounds
- [ ] T081 [P] [US7] Add safe GridSystem reference lookup in MapElement.OnValidate() with FindObjectOfType<GridSystem>() fallback if _gridSystem field is null

### Editor Testing

- [ ] T082 [US7] Test auto-positioning: Unity Editor (not play mode), select CharacterElement, change currentGridIndex from 5 to 15, verify transform.position updates to grid cell 15 center
- [ ] T083 [US7] Test invalid index: set currentGridIndex to -1, verify position unchanged or clamped to 0, no errors thrown

**Story Dependencies**: None (editor-only feature)
**Parallel Execution**: T079, T080, T081 can run in parallel

---

## Implementation Strategy

### MVP First Approach

**MVP = User Story 1 Only** (Context Foundation)

Rationale:
- US1 is P1 and foundational for all other features
- Provides immediate value: NPCs make context-aware decisions
- Independently testable: Multi-turn memory verification
- 15 tasks (~2-3 days of focused work)

**MVP Task Range**: T001-T025 (Setup + Foundational + US1)

### Incremental Delivery

After MVP, deliver in priority order:

1. **Sprint 2** (P2 Features): US2 + US3 + US4 (T026-T057) - Tactical mechanics
2. **Sprint 3** (P3 Features): US5 + US6 + US7 (T058-T083) - Visual polish

Each sprint delivers independently testable value.

### Parallel Execution Examples

**Phase 3 (US1) Parallelization**:
```
Parallel Group 1 (Context Core):
- T011: ConversationContext sliding window
- T012: GetAllMessages() chronological sorting
- T014: Add system context calls

Parallel Group 2 (Integration):
- T016: Combat event logging
- T017: Inventory event logging
- T018: Movement event logging

Parallel Group 3 (Tooling):
- T020: DebugPrintContext()
- T021: ExportContextToJson()
```

**Phase 6 (US4) Parallelization**:
```
Parallel Group 1 (Data Structures):
- T043: ConsumableData model
- T044: ItemTypeConsumableDataPair wrapper
- T045: ConsumableConfiguration ScriptableObject

Parallel Group 2 (Item Properties):
- T047: ItemElement properties field
- T048: Update Apple prefab

Parallel Group 3 (UI):
- T053: Eat action UI option
- T054: PlayerController OnEatItemSelected()
```

---

## Dependency Graph

```
Setup (T001-T006)
    ↓
Foundational (T007-T010)
    ↓
    ├─→ US1 (T011-T025) [P1 - Foundation]
    │      ↓
    │      ├─→ US2 (T026-T033) [P2 - Flip]
    │      ├─→ US3 (T034-T042) [P2 - End Turn]
    │      ├─→ US4 (T043-T057) [P2 - Consumables]
    │      ├─→ US5 (T058-T067) [P3 - Animation]
    │      ├─→ US6 (T068-T078) [P3 - Logging]
    │      └─→ US7 (T079-T083) [P3 - Editor] (independent)
    │
    └─→ US7 can start immediately (no dependencies)
```

**Critical Path**: Setup → Foundational → US1 → US2/US3/US4/US5/US6
**Parallel Path**: US7 (Editor) can run independently

---

## Task Validation Checklist

✅ **Format Compliance**: All 78 tasks follow `- [ ] [TID] [P?] [Story?] Description with file path` format
✅ **User Story Mapping**: Each US2-US7 task includes [US#] label
✅ **Parallelization**: 42 tasks (54%) marked with [P] for parallel execution
✅ **Independent Tests**: Each user story phase includes test criteria and validation task
✅ **File Paths**: All tasks specify exact file paths (e.g., Assets/Scripts/Services/Context/ContextManager.cs)
✅ **Dependencies**: Dependency graph clearly shows US1 as foundation, US7 as independent
✅ **MVP Defined**: T001-T025 (Setup + Foundational + US1) = 25 tasks

---

## Success Metrics Validation

Each user story maps to spec success criteria:

- **US1** → SC-001 (NPCs reference 5+ turns prior), SC-008 (<20% memory growth), SC-013 (zero context exceptions)
- **US2** → SC-009 (flip executes <100ms)
- **US3** → SC-003 (30% turn duration decrease), SC-009 (end turn <100ms)
- **US4** → SC-004 (health restoration accurate), SC-014 (consumable effects 100% accurate)
- **US5** → SC-005 (60 FPS with 10 animations), SC-009 (animation <100ms per frame)
- **US6** → SC-012 (log panel responsive at 10 events/sec)
- **US7** → No performance metric (editor-only feature)

**Measurement**: Unity Profiler (FPS, memory), manual playtesting (NPC behavior), integration tests (edge cases)

---

## Next Steps

1. **Review task breakdown** with team (verify file paths, dependencies, parallelization opportunities)
2. **Begin MVP implementation**: T001-T025 (Setup + Foundational + US1)
3. **Validate MVP**: Multi-turn test scenario (attack turn 1, give item turn 2, verify NPC references both turn 3+)
4. **Iterate on P2 features**: US2, US3, US4 (tactical mechanics)
5. **Polish with P3 features**: US5, US6, US7 (visual enhancements)

**Estimated Timeline**:
- MVP (US1): 2-3 days
- Sprint 2 (US2-US4): 3-4 days
- Sprint 3 (US5-US7): 3-4 days
- **Total**: 8-11 days (single developer, full-time)

**Parallel Team**: With 3 developers, total timeline: 4-6 days (MVP → Sprint 2 → Sprint 3 with parallelization)

---

**Document Status**: ✅ Ready for Implementation
**Last Updated**: 2025-10-27
**Generated by**: `/speckit.tasks` command
