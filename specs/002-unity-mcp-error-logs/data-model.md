# Data Model: Unity MCP Error Log Server

**Date**: 2025-10-30
**Feature**: Unity MCP Error Log Server
**Branch**: `002-unity-mcp-error-logs`

## Overview

This document defines the core data structures (POCOs and enums) for the Unity MCP Error Log Server implementation. All models follow the Plain Old CLR Object (POCO) philosophy with explicit typing, serializable fields, and no implementation logic.

---

## Error Log Domain

### ErrorLogEntry

**Purpose**: Represents a single error/warning/exception captured from Unity's Debug.Log system.

**File**: `Assets/Scripts/Models/MCP/ErrorLogEntry.cs`

**Fields**:
```csharp
public class ErrorLogEntry
{
    public string EntryId;           // Unique identifier (GUID)
    public string Message;           // Error message text
    public string StackTrace;        // Full stack trace
    public ErrorSeverity Severity;   // Error, Warning, Exception, Assert
    public LogType UnityLogType;     // Unity's LogType enum (for compatibility)
    public DateTime Timestamp;       // UTC timestamp when error occurred
    public string SourceContext;     // Optional: scene name, game mode (Edit/Play)
}
```

**Validation Rules** (from FR-003):
- `EntryId`: MUST be unique per entry, generated using `System.Guid.NewGuid().ToString()`
- `Message`: MUST NOT be null or empty
- `StackTrace`: Can be empty for warnings, MUST be present for errors/exceptions
- `Timestamp`: MUST be UTC (use `DateTime.UtcNow`)
- `Severity`: MUST map correctly from Unity's `LogType`

**Relationships**:
- Stored in `ErrorLogBuffer` (1:N relationship - buffer contains many entries)
- Serialized to JSON for MCP client responses

---

### ErrorSeverity

**Purpose**: Categorize log entries by severity level for filtering.

**File**: `Assets/Scripts/Enums/ErrorSeverity.cs`

**Values**:
```csharp
public enum ErrorSeverity
{
    Error,      // Debug.LogError calls
    Warning,    // Debug.LogWarning calls
    Exception,  // Debug.LogException calls and uncaught exceptions
    Assert      // Debug.LogAssertion calls
}
```

**Mapping from Unity LogType**:
```
LogType.Error       → ErrorSeverity.Error
LogType.Assert      → ErrorSeverity.Assert
LogType.Warning     → ErrorSeverity.Warning
LogType.Log         → (Filtered out - not captured)
LogType.Exception   → ErrorSeverity.Exception
```

---

## MCP Protocol Domain

### MCPRequest

**Purpose**: Represents an incoming JSON-RPC 2.0 request from an MCP client.

**File**: `Assets/Scripts/Models/MCP/MCPRequest.cs`

**Fields**:
```csharp
public class MCPRequest
{
    public string JsonRpc;           // Always "2.0"
    public object Id;                // string or number (NOT null)
    public string Method;            // e.g., "tools/call", "tools/list", "initialize"
    public MCPParams Params;         // Request parameters (varies by method)
}

public class MCPParams
{
    public string Name;              // Tool name (for tools/call)
    public Dictionary<string, object> Arguments; // Tool arguments
    public MCPClientInfo ClientInfo; // Client capabilities (for initialize)
}

public class MCPClientInfo
{
    public string Name;              // Client application name
    public string Version;           // Client version
    public MCPCapabilities Capabilities; // Supported features
}

public class MCPCapabilities
{
    public bool Experimental;        // Supports experimental features
    public SamplingCapability Sampling; // LLM sampling support (optional)
}
```

**Validation Rules** (from FR-008):
- `JsonRpc`: MUST be "2.0"
- `Id`: MUST NOT be null for requests (can be omitted for notifications)
- `Method`: MUST be recognized method name
- Thread-safe access required for concurrent client requests

---

### MCPResponse

**Purpose**: Represents an outgoing JSON-RPC 2.0 response to an MCP client.

**File**: `Assets/Scripts/Models/MCP/MCPResponse.cs`

**Fields**:
```csharp
public class MCPResponse
{
    public string JsonRpc;           // Always "2.0"
    public object Id;                // Matches request Id
    public MCPResult Result;         // Success result (mutually exclusive with Error)
    public MCPError Error;           // Error result (mutually exclusive with Result)
}

public class MCPResult
{
    public List<MCPContent> Content; // Array of content items
    public bool IsError;             // Application-level error flag
    public Dictionary<string, object> Metadata; // Optional metadata
}

public class MCPContent
{
    public string Type;              // "text", "image", "resource"
    public string Text;              // Text content (for type="text")
    public string MimeType;          // MIME type (for non-text content)
    public byte[] Data;              // Binary data (for images, etc.)
}

public class MCPError
{
    public int Code;                 // JSON-RPC error code (-32700 to -32603)
    public string Message;           // Human-readable error message
    public object Data;              // Optional additional error data
}
```

**Validation Rules**:
- `Id`: MUST match the request Id exactly
- `Result` XOR `Error`: Exactly one must be present, never both
- `IsError`: Application errors use `IsError=true` in Result, NOT Error object

---

### MCPToolDefinition

**Purpose**: Describes an MCP tool (function) available for clients to call.

**File**: `Assets/Scripts/Models/MCP/MCPToolDefinition.cs`

**Fields**:
```csharp
public class MCPToolDefinition
{
    public string Name;              // Tool identifier (e.g., "get_unity_errors")
    public string Description;       // Human-readable tool description
    public MCPInputSchema InputSchema; // JSON Schema for tool parameters
}

public class MCPInputSchema
{
    public string Type;              // Always "object"
    public Dictionary<string, MCPProperty> Properties; // Parameter definitions
    public List<string> Required;    // Required parameter names
}

public class MCPProperty
{
    public string Type;              // "string", "integer", "boolean", etc.
    public string Description;       // Parameter description
    public List<string> Enum;        // Allowed values (for enums)
    public int? Minimum;             // Min value (for integers)
    public int? Maximum;             // Max value (for integers)
    public string Format;            // Format hint (e.g., "date-time")
}
```

**Tools Defined** (from FR-006):
1. **get_unity_errors**: Query error log with filters
2. **clear_error_log**: Clear all buffered errors
3. **get_server_status**: Check MCP server status

---

### MCPSession

**Purpose**: Tracks an active MCP client session.

**File**: `Assets/Scripts/Models/MCP/MCPSession.cs`

**Fields**:
```csharp
public class MCPSession
{
    public string SessionId;         // Unique session identifier (GUID)
    public string ClientName;        // Client application name
    public DateTime CreatedAt;       // Session creation timestamp
    public DateTime LastActivityAt;  // Last request timestamp
    public MCPCapabilities Capabilities; // Client capabilities
    public bool IsActive;            // Session active flag
}
```

**Validation Rules** (from FR-008):
- `SessionId`: MUST be cryptographically secure (use `Guid.NewGuid()`)
- `LastActivityAt`: Updated on every request
- Thread-safe access required (multiple threads may query sessions)

**State Transitions**:
```
Created (initialize request) → Active → Expired (timeout) → Closed
                                  ↓
                              Active (on activity)
```

---

## Configuration Domain

### MCPServerConfiguration

**Purpose**: ScriptableObject defining MCP server settings.

**File**: `Assets/Scripts/Configuration/MCPServerConfiguration.cs`

**Fields**:
```csharp
[CreateAssetMenu(fileName = "MCPServerConfiguration", menuName = "Configuration/MCP Server")]
public class MCPServerConfiguration : ScriptableObject
{
    [Header("Network Settings")]
    public int ServerPort = 5678;              // HTTP listener port
    public string BindAddress = "127.0.0.1";   // Localhost only
    public List<string> AllowedOrigins;        // CORS origins (security)

    [Header("Buffer Settings")]
    public int MaxBufferSize = 10000;          // Max error log entries
    public int DefaultQueryLimit = 100;        // Default errors returned per query

    [Header("Session Settings")]
    public int SessionTimeoutSeconds = 3600;   // 1 hour timeout
    public int MaxConcurrentSessions = 10;     // Max simultaneous clients

    [Header("Performance Settings")]
    public bool EnableHighVolumeMode = false;  // Rate limiting for 1000s of errors
    public int HighVolumeThreshold = 100;      // Errors/second before rate limiting
}
```

**Validation Rules** (from FR-005, FR-008):
- `ServerPort`: MUST be between 1024-65535 (non-privileged ports)
- `BindAddress`: MUST be localhost (127.0.0.1 or ::1) for security
- `MaxBufferSize`: MUST be positive, recommended 1000-10000
- `MaxConcurrentSessions`: MUST be positive, ≤10 per spec

---

## Server Status Domain

### MCPServerStatus

**Purpose**: Enum representing MCP server operational state.

**File**: `Assets/Scripts/Enums/MCPServerStatus.cs`

**Values**:
```csharp
public enum MCPServerStatus
{
    Stopped,     // Server not running
    Starting,    // Server initialization in progress
    Running,     // Server active and accepting connections
    Error,       // Server encountered fatal error
    Stopping     // Server shutdown in progress
}
```

**State Transitions** (from FR-010):
```
Stopped → Starting → Running ⇄ Running (normal operation)
                        ↓
                      Error (fatal error)
                        ↓
                     Stopping → Stopped

Running → Stopping → Stopped (graceful shutdown)
```

---

## Error Log Buffer Domain

### ErrorLogBufferState

**Purpose**: Serializable state for error log buffer (domain reload persistence).

**File**: `Assets/Scripts/Models/MCP/ErrorLogBufferState.cs`

**Fields**:
```csharp
[Serializable]
public class ErrorLogBufferState
{
    public int CurrentSize;              // Number of entries in buffer
    public int TotalErrorsCaptured;      // Lifetime error count
    public DateTime LastErrorTimestamp;  // Most recent error timestamp
    public bool IsCapturing;             // Capture enabled flag
}
```

**Validation Rules**:
- Not serializing actual log entries (memory-only per FR-007)
- Used for diagnostics and server status reporting

---

## Relationships Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                   MCP Client (External)                     │
└────────────────┬────────────────────────────────────────────┘
                 │ HTTP POST
                 ↓
         ┌───────────────┐
         │  MCPRequest   │
         └───────┬───────┘
                 │
         ┌───────↓────────┐
         │ MCPServerManager│ ←──── MCPServerConfiguration
         │ (Singleton)     │
         └───────┬─────────┘
                 │
         ┌───────↓─────────┐
         │  MCPSession     │ (1:N sessions)
         └─────────────────┘
                 │
         ┌───────↓──────────┐
         │ MCPToolExecutor  │
         └───────┬──────────┘
                 │
         ┌───────↓──────────┐
         │ ErrorLogBuffer   │ ←──── ErrorLogBufferState
         │                  │
         │ Contains         │
         │ ErrorLogEntry[]  │
         └──────────────────┘
                 │
         ┌───────↓─────────┐
         │  MCPResponse    │
         │  (with Content) │
         └───────┬─────────┘
                 │
                 ↓ HTTP Response
┌────────────────────────────────────────────────────────────┐
│                   MCP Client (External)                    │
└────────────────────────────────────────────────────────────┘
```

---

## File Location Summary

```
Assets/Scripts/
├── Models/MCP/
│   ├── ErrorLogEntry.cs           # Core error log data
│   ├── MCPRequest.cs              # Incoming JSON-RPC request
│   ├── MCPResponse.cs             # Outgoing JSON-RPC response
│   ├── MCPToolDefinition.cs       # Tool schema definition
│   ├── MCPSession.cs              # Client session tracking
│   └── ErrorLogBufferState.cs     # Buffer diagnostics state
├── Enums/
│   ├── ErrorSeverity.cs           # Error categorization
│   └── MCPServerStatus.cs         # Server operational state
└── Configuration/
    └── MCPServerConfiguration.cs  # ScriptableObject settings
```

---

## Notes

- All models follow POCO pattern with public fields (not properties) for Unity serialization compatibility
- All DateTime fields use UTC to avoid timezone issues
- All collections use concrete types (`List<T>`, `Dictionary<K,V>`) rather than interfaces for serialization
- Thread-safe access patterns documented in individual class implementations (not in POCO definitions)
- No implementation logic in models - pure data structures only
