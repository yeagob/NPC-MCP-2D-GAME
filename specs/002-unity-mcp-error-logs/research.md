# Research: Unity MCP Error Log Server

**Date**: 2025-10-30
**Feature**: Unity MCP Error Log Server
**Branch**: `002-unity-mcp-error-logs`

## Overview

This document consolidates technical research for implementing a Model Context Protocol (MCP) server within Unity Editor that exposes error logs to external AI agents. Research focused on resolving five key technical unknowns: MCP transport protocol, JSON serialization, threading model, server lifecycle management, and log capture patterns.

---

## Decision 1: MCP Transport Protocol

**Decision**: Streamable HTTP (JSON-RPC 2.0 over HTTP)

**Rationale**:
- **Unity Editor Integration**: HTTP server runs directly within Unity Editor using `System.Net.HttpListener` (available in .NET Framework 4.x), requiring no external process management
- **Debugging & Development**: HTTP endpoints can be tested independently using tools like Postman/curl
- **Multi-Client Support**: Streamable HTTP naturally supports multiple concurrent AI agent connections
- **Unity Thread Safety**: HTTP requests can be queued and processed on Unity's main thread or background threads as needed
- **Session Management**: Built-in session ID support enables stateful tool execution tracking

**Alternatives Considered**:
- **stdio transport** (rejected): Requires launching Unity as a subprocess or creating a separate stdio-to-Unity bridge process. Unity Editor doesn't naturally fit the "launched subprocess" model that stdio transport expects.
- **Hybrid approach** (not necessary initially): Can add stdio bridges later if needed without refactoring core implementation

**Implementation Approach**:
1. Create `ScriptableSingleton<MCPServerManager>` hosting `HttpListener` on `http://localhost:[configurable_port]/mcp`
2. Listen for POST requests containing JSON-RPC messages
3. Parse incoming JSON-RPC 2.0 requests using Newtonsoft.Json
4. Route to tool handlers (log retrieval, error filtering)
5. Return JSON-RPC responses or SSE stream for server-initiated messages
6. Implement MCP session management with unique session IDs
7. Add Origin header validation for security (localhost-only binding)

**Protocol Details**:
- **Message Format**: JSON-RPC 2.0 with `jsonrpc`, `id`, `method`, `params` fields
- **Request/Response Flow**: Initialize → Capability negotiation → Tool discovery → Tool execution → Results
- **Error Handling**: Use standard JSON-RPC error codes (-32700 to -32603) for protocol errors, application errors use `isError: true` flag in content
- **Headers**: `Content-Type: application/json`, `Mcp-Session-Id: <uuid>`, Origin validation required

---

## Decision 2: JSON Serialization

**Decision**: Newtonsoft.Json (com.unity.nuget.newtonsoft-json)

**Rationale**:
- **Official Unity Support**: Available directly through Unity Package Manager as `com.unity.nuget.newtonsoft-json` (version 3.2.2 = Newtonsoft 13.0.2)
- **Zero Configuration**: Many Unity internal packages depend on it, often pre-installed in Unity 2019.1+ projects
- **Full Feature Set**: Supports Dictionary serialization, polymorphic types, and complex nested structures needed for MCP tool definitions and JSON Schema
- **Project Constitution Compliance**: Unity's officially blessed JSON library (aligns with "no external dependencies" principle)
- **Proven Compatibility**: Tested extensively with Unity's .NET Standard 2.1 and Framework 4.x profiles
- **MCP JSON Schema Support**: Can serialize/deserialize complex MCP tool definitions with nested properties, required fields, and type constraints

**Alternatives Considered**:
- **Unity's JsonUtility** (rejected): Cannot serialize Dictionary types (needed for tool parameters), doesn't support properties (only public fields), lacks polymorphic support
- **System.Text.Json** (rejected): Requires manual installation of version 6.0.9 + three dependency DLLs, has Unity compatibility issues

**Integration Notes**:
- Installation: Package Manager → Add package by name → `com.unity.nuget.newtonsoft-json`
- Namespace: `using Newtonsoft.Json;`
- Usage: `JsonConvert.SerializeObject()` / `JsonConvert.DeserializeObject<T>()`
- JSON-RPC message handling: Use `JObject.Parse()` for dynamic inspection before typed deserialization

---

## Decision 3: Threading Model

**Decision**: Standard .NET Tasks with async/await (NOT Unity Job System)

**Rationale**:
- **Job System Mismatch**: Unity Job System is designed for short, performance-critical computations within a single frame. It has no callbacks and is designed to complete within a tick.
- **MCP Server Needs**: Long-running I/O operations (HTTP requests/responses, waiting for client connections) that span multiple frames - exactly what Unity's async/await support is designed for
- **Unity 6 Support**: Improved async/await support with the new `Awaitable` class, but for Editor scripts dealing with network I/O, standard .NET `Task` is appropriate
- **Existing Patterns**: Codebase already uses `async/await` extensively (ContextManager, CharacterAgent, etc.) - maintains consistency

**Thread-Safe Collection**: `ConcurrentQueue<T>`
- Lock-free implementation using `Interlocked` operations
- Excellent performance for single producer/single consumer scenarios (MCP server thread queuing, Unity main thread dequeueing)
- Scales well with modest processing time (>500 FLOPS per item)

**Main Thread Communication Pattern**:
```
Background Server Thread              Unity Main Thread
        |                                     |
   Enqueue Request     ───────────>    ConcurrentQueue<MCPRequest>
        |                                     |
   Continue Listening                  EditorApplication.update dequeues
        |                                     |
   Wait for Response   <───────────    Process & Enqueue Response
        |                                     |
   Dequeue & Send                      Continue frame
```

**Implementation Considerations**:
- Use `async Task` methods for all network operations
- Unity's `UnitySynchronizationContext` ensures Task continuations run on main thread by default when called from main thread
- For operations requiring Unity API access, use dispatcher pattern to marshal back to main thread
- Store thread-safe references: `ConcurrentQueue<MCPRequest>`, `ConcurrentQueue<MCPResponse>`, `ConcurrentQueue<ErrorLogEntry>`

---

## Decision 4: Server Lifecycle Management

**Decision**: ScriptableSingleton<T> with InitializeOnLoad (hybrid approach)

**Rationale**:
- **ScriptableSingleton<T> Benefits**:
  - Automatic persistence across domain reloads
  - File-based state persistence with `[FilePathAttribute]`
  - Editor-only, doesn't pollute runtime
  - Survives assembly reloading in Editor
- **InitializeOnLoad Issues When Used Alone**:
  - Fires on EVERY assembly reload (script changes, entering play mode, asset imports)
  - Can be called from background AssetImporterWorker threads (not just main thread)
  - Creates multiple instances that persist without cleanup, causing port conflicts
  - Resources (sockets, HTTP listeners) don't auto-cleanup between domain reloads

**Startup Hook Pattern**:
```csharp
[FilePath("MCPServer/ServerState.asset", FilePathAttribute.Location.PreferencesFolder)]
public class MCPServerManager : ScriptableSingleton<MCPServerManager>
{
    [SerializeField] private bool isServerRunning;
    [SerializeField] private int serverPort;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update += InitializeOnce;
    }

    private static void InitializeOnce()
    {
        EditorApplication.update -= InitializeOnce; // Unregister immediately

        if (!instance.isServerRunning)
        {
            instance.StartServer();
        }
    }
}
```

**Shutdown Hooks (Multi-Layered)**:
1. **Assembly Reload**: `AssemblyReloadEvents.beforeAssemblyReload` - fires on script recompilation, entering play mode
2. **Editor Quit**: `EditorApplication.wantsToQuit` - fires when Unity Editor closes
3. **Play Mode Changes**: `EditorApplication.playModeStateChanged` - optionally stop server when entering play mode

**Domain Reload Handling**:
- `ScriptableSingleton` automatically persists serialized fields across reloads
- Store server state (`isServerRunning`, `port`, last known client connections) in serializable fields
- On reload, check state and restart if needed
- Use `[NonSerialized]` for runtime-only objects (HttpListener, background tasks)

**Why NOT EditorWindow**:
- Requires window to be open for server to run
- Window can be closed by user, stopping server unexpectedly
- Doesn't survive domain reloads as cleanly

---

## Decision 5: Log Capture Best Practices

**Decision**: `Application.logMessageReceivedThreaded`

**Rationale**:
- MCP server runs on background thread and needs thread-safe log access
- `logMessageReceivedThreaded` is called from **any thread** (including worker threads from burst Jobs, background threads)
- `logMessageReceived` (non-threaded) only fires on **main thread** - would miss logs from background operations
- Captures everything - don't need to subscribe to both

**Thread Safety**:
- Callback can be invoked from ANY thread (main thread, worker threads, Job system threads)
- Handler MUST be thread-safe:
  - Don't access Unity APIs from callback (most are main-thread only)
  - Don't modify shared state without synchronization
  - Use thread-safe collections for buffering (`ConcurrentQueue<ErrorLogEntry>`)

**Implementation Pattern**:
```csharp
private readonly ConcurrentQueue<ErrorLogEntry> logBuffer = new ConcurrentQueue<ErrorLogEntry>();
private const int MAX_BUFFER_SIZE = 10000; // Prevent memory overflow

void OnEnable()
{
    Application.logMessageReceivedThreaded += OnLogReceived;
}

void OnDisable()
{
    Application.logMessageReceivedThreaded -= OnLogReceived;
}

private void OnLogReceived(string condition, string stackTrace, LogType type)
{
    // Filter early to reduce overhead
    if (type != LogType.Error && type != LogType.Exception && type != LogType.Warning)
        return;

    ErrorLogEntry entry = new ErrorLogEntry
    {
        Message = condition,
        StackTrace = stackTrace,
        LogType = type,
        Timestamp = System.DateTime.UtcNow
    };

    logBuffer.Enqueue(entry);

    // Prevent unbounded growth - circular buffer pattern
    while (logBuffer.Count > MAX_BUFFER_SIZE)
    {
        logBuffer.TryDequeue(out _); // Discard oldest
    }
}
```

**Performance Optimizations**:
- **Filter at Capture**: Only capture Error/Exception/Warning (ignore Log/Info) unless user enables verbose mode
- **Limit Buffer Size**: Use circular buffer pattern or discard oldest when limit reached
- **Avoid Allocations**: Store log entries as-is, don't manipulate strings in callback
- **Batch Transmission**: Don't send individual log to MCP client - buffer and send batches

**High-Volume Handling** (1000s of errors):
- **Rate Limiting**: If >100 logs/second, sample (capture every Nth log)
- **Aggregation**: Group duplicate error messages (same condition text) with count
- **Async Flush**: Use background Task to periodically process buffer
- **Circuit Breaker**: If buffer constantly full, disable capture temporarily with warning

---

## MCP Protocol Implementation Strategy

**Approach**: Custom implementation (inspired by UnityNaturalMCP patterns)

**Rationale**:
- **Project Constitution Alignment**: Existing libraries introduce dependencies (System.Text.Json 9.0.x, Microsoft.Extensions.DependencyInjection, Python/Node.js runtimes) that conflict with "no external dependencies" principle and simplicity goals
- **Learning & Control**: Building custom implementation ensures deep understanding of MCP protocol for future extensions, full control over error handling and Unity Editor lifecycle
- **Scope Appropriateness**: Use case (exposing error logs) is focused and doesn't require full complexity of multi-language server frameworks
- **Pattern Reuse**: Codebase already has MCP-compatible tool architecture (`ToolConfig`, `IToolSet`, `FunctionDefinition`)

**Core Components** (following existing patterns):
- `MCPServerManager` (ScriptableSingleton) - hosts HttpListener
- `IMCPTransport` interface - abstraction for HTTP/stdio future-proofing
- `MCPServerConfiguration` (ScriptableObject) - port, allowed origins, session timeout
- `JsonRpcMessage`, `JsonRpcRequest`, `JsonRpcResponse` (POCO models)
- `MCPToolRegistry` - converts existing `ToolConfig` to MCP tool definitions

**Reuse Existing Architecture**:
- Leverage `IToolSet` pattern for actual log retrieval
- Use `ContextManager` patterns for session state
- Apply `CombatConfiguration`-style pattern for MCP constants

**Estimated Implementation Size**:
- ~500 lines for HTTP server + JSON-RPC routing
- ~200 lines for MCP protocol messages (initialize, tools/list, tools/call)
- ~100 lines for session management
- Total: <1000 lines vs. integrating multi-thousand-line external frameworks

**Future-Proofing**:
- Clean interfaces allow swapping HTTP for stdio later
- Can adopt official C# SDK when it supports Unity's .NET profile
- Attribute-based tool registration can be added incrementally

---

## Existing Unity MCP Resources (Reference)

**Libraries Evaluated**:
1. **notargs/UnityNaturalMCP** - Streamable HTTP + stdio bridge, System.Text.Json, attribute-based tools
2. **IvanMurzak/Unity-MCP** - Flexible transport, plugin architecture, reflection-based discovery
3. **CoplayDev/unity-mcp** - Python MCP server + socket bridge to Unity
4. **CoderGamester/mcp-unity** - Node.js MCP server + Unity bridge
5. **nurture-tech/unity-mcp-server** - Official MCP C# SDK integration

**When to Reconsider**: If scope expands to require MCP resources, prompts, sampling, or complex multi-agent orchestration, revisit **nurture-tech/unity-mcp-server** as it tracks latest spec (2025-06-18).

---

## Summary Table

| Decision Point | Choice | Key Reason |
|---------------|--------|------------|
| **Transport** | Streamable HTTP | Native Unity Editor integration, no subprocess management |
| **JSON Library** | Newtonsoft.Json | Official Unity support, full feature set, zero config |
| **Threading** | .NET Tasks/async/await | Long-running I/O, existing pattern consistency |
| **Lifecycle** | ScriptableSingleton + InitializeOnLoad | Domain reload persistence, clean shutdown hooks |
| **Log Capture** | logMessageReceivedThreaded | Thread-safe capture from any thread |
| **Base Approach** | Custom Implementation | Constitution alignment, focused scope, architectural consistency |

---

## Next Steps (Phase 1: Design)

1. ✅ Install `com.unity.nuget.newtonsoft-json` via Package Manager
2. Create data model (ErrorLogEntry, MCPRequest, MCPResponse POCOs)
3. Define API contracts (MCP tool definitions for get_unity_errors, clear_errors)
4. Design MCPServerManager with ScriptableSingleton pattern
5. Design ErrorLogBuffer with ConcurrentQueue backing
6. Create MCPServerConfiguration ScriptableObject
7. Update agent context with MCP technology stack
