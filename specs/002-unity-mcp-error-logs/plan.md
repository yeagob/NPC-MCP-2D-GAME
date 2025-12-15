# Implementation Plan: Unity MCP Error Log Server

**Branch**: `002-unity-mcp-error-logs` | **Date**: 2025-10-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-unity-mcp-error-logs/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a Model Context Protocol (MCP) server within Unity that exposes runtime error logs to external AI agents (Claude Code and other MCP clients). The server will capture errors from Unity's Debug.Log system, maintain them in an in-memory buffer, and expose them via MCP tools for querying with optional filters (severity, count limit, time range). The implementation will follow a three-phase approach: (1) Core query functionality (P1 - MVP), (2) Severity filtering (P2), and (3) Real-time push notifications (P3).

## Technical Context

**Language/Version**: C# (Unity-compatible .NET Standard 2.1 / .NET Framework 4.x) - Unity 6000.0.45
**Primary Dependencies**: NEEDS CLARIFICATION - MCP protocol library selection, JSON serialization approach (Unity JsonUtility vs third-party)
**Storage**: In-memory only (no persistence) - circular buffer data structure
**Testing**: Unity Test Framework (NUnit-based PlayMode and EditMode tests)
**Target Platform**: Unity Editor (Windows/Mac/Linux) - Development tool only, not production builds
**Project Type**: Single Unity project with new MCP server subsystem
**Performance Goals**: <500ms query response for 1000 errors, <1 second notification latency, no measurable frame rate impact during moderate logging (10 errors/min)
**Constraints**: Non-blocking main thread operation, <50MB memory for error buffer, thread-safe concurrent access for up to 10 clients
**Scale/Scope**: Support 1000-error buffer capacity, handle 10 concurrent MCP clients, capture all Unity Debug.Log* methods

**Key Technical Unknowns**:
- NEEDS CLARIFICATION: MCP protocol implementation approach (stdio vs HTTP transport)
- NEEDS CLARIFICATION: JSON serialization library compatible with Unity and MCP
- NEEDS CLARIFICATION: Threading model (Unity's Job System vs standard .NET threads)
- NEEDS CLARIFICATION: MCP server lifecycle management (Unity Editor window vs InitializeOnLoad attribute)
- NEEDS CLARIFICATION: MCP tool registration and discovery mechanism within Unity

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Based on CLAUDE.md project principles**:

✅ **Language**: All code in English - PASS
✅ **Type Safety**: No `var` usage, explicit types required - PASS
✅ **No Hardcoding**: Configuration-driven via ScriptableObjects - PASS (follows existing pattern)
✅ **No Comments**: Self-documenting code through naming - PASS
✅ **SOLID Principles**: Apply rigorously - PASS (interface-based design planned)
✅ **POCO Philosophy**: Data structures follow Plain Old CLR Object pattern - PASS
✅ **No External Dependencies**: Limited to Unity-compatible libraries only - CAUTION (MCP library needs review)
✅ **Brace Style**: New line for braces - PASS
✅ **One Class Per File**: Each class in separate file - PASS
✅ **Configuration Files**: ScriptableObjects for all constants - PASS

**Potential Violations**:
- **External Dependency Risk**: MCP protocol implementation may require third-party library. Will evaluate Unity-compatible options in Phase 0 research.

**Decision**: PROCEED to Phase 0 with condition that MCP library research confirms Unity compatibility or guides custom implementation.

## Project Structure

### Documentation (this feature)

```text
specs/002-unity-mcp-error-logs/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── mcp-tools.json   # MCP tool definitions (get_unity_errors, clear_errors, etc.)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Assets/
├── Scripts/
│   ├── Configuration/
│   │   └── MCPServerConfiguration.cs    # ScriptableObject: port, buffer size, retention
│   ├── Models/
│   │   └── MCP/
│   │       ├── ErrorLogEntry.cs         # POCO: message, stackTrace, timestamp, severity
│   │       ├── MCPRequest.cs            # POCO: MCP protocol request structure
│   │       ├── MCPResponse.cs           # POCO: MCP protocol response structure
│   │       └── MCPToolCall.cs           # POCO: Tool invocation data
│   ├── Services/
│   │   └── MCP/
│   │       ├── Interfaces/
│   │       │   ├── IMCPServer.cs        # Interface: Start/Stop, handle requests
│   │       │   └── IErrorLogBuffer.cs   # Interface: Add/Query/Clear error logs
│   │       ├── MCPServer.cs             # Service: MCP protocol server implementation
│   │       ├── ErrorLogBuffer.cs        # Service: Circular buffer for error storage
│   │       ├── ErrorLogCapture.cs       # Service: Hook Application.logMessageReceived
│   │       └── MCPToolExecutor.cs       # Service: Execute MCP tool calls
│   ├── Enums/
│   │   ├── ErrorSeverity.cs             # Enum: Error, Warning, Exception, Assert
│   │   └── MCPServerStatus.cs           # Enum: Running, Stopped, Error
│   └── Editor/
│       └── MCPServerWindow.cs           # EditorWindow: Control panel for MCP server
└── Tests/
    ├── EditMode/
    │   ├── ErrorLogBufferTests.cs       # Unit tests: buffer operations
    │   └── MCPToolExecutorTests.cs      # Unit tests: tool execution logic
    └── PlayMode/
        └── MCPServerIntegrationTests.cs # Integration tests: end-to-end MCP flows
```

**Structure Decision**: Unity single-project structure following existing codebase conventions. New MCP subsystem organized under `Assets/Scripts/Services/MCP/` to mirror existing service patterns (Orchestrators, Agents, Tools). Configuration follows ScriptableObject pattern consistent with `AgentConfig`, `ToolConfig`, etc. Tests use Unity Test Framework with EditMode for unit tests and PlayMode for integration tests.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations requiring justification at this stage. Potential external MCP library dependency will be evaluated in Phase 0 research with preference for minimal/custom implementation if third-party options conflict with Unity compatibility requirements.

---

## Post-Phase 1 Constitution Re-Evaluation

**Date**: 2025-10-30 (After research and design completion)

✅ **Language**: All code in English - PASS
✅ **Type Safety**: No `var` usage, explicit types required - PASS (POCOs use explicit types)
✅ **No Hardcoding**: Configuration-driven via ScriptableObjects - PASS (MCPServerConfiguration created)
✅ **No Comments**: Self-documenting code through naming - PASS (model fields clearly named)
✅ **SOLID Principles**: Apply rigorously - PASS (interfaces IMCPServer, IErrorLogBuffer defined)
✅ **POCO Philosophy**: Data structures follow Plain Old CLR Object pattern - PASS (all models are POCOs)
✅ **No External Dependencies**: Limited to Unity-compatible libraries only - **RESOLVED: PASS**
  - **Decision**: Use Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`)
  - **Justification**: Official Unity package, pre-installed in Unity 2019.1+, aligns with constitution
  - **Custom MCP Implementation**: No third-party MCP SDK required, building custom JSON-RPC layer
✅ **Brace Style**: New line for braces - PASS
✅ **One Class Per File**: Each class in separate file - PASS (file structure defined)
✅ **Configuration Files**: ScriptableObjects for all constants - PASS

**Technical Unknowns - All Resolved**:
- ✅ MCP protocol implementation: Streamable HTTP with custom JSON-RPC layer
- ✅ JSON serialization: Newtonsoft.Json (official Unity package)
- ✅ Threading model: Standard .NET Tasks with async/await, ConcurrentQueue for thread safety
- ✅ Server lifecycle: ScriptableSingleton<T> with InitializeOnLoad, multi-layered shutdown hooks
- ✅ MCP tool registration: Custom MCPToolRegistry converting existing ToolConfig patterns

**Final Verdict**: ✅ **PASS** - All constitution principles satisfied. No violations. Ready to proceed to Phase 2 (task generation with `/speckit.tasks`).
