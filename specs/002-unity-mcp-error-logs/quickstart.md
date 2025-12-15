# Quickstart: Unity MCP Error Log Server

**Date**: 2025-10-30
**Feature**: Unity MCP Error Log Server
**Branch**: `002-unity-mcp-error-logs`

## Overview

This quickstart guide helps developers understand, test, and use the Unity MCP Error Log Server. It covers installation, configuration, testing workflows, and integration with AI agents like Claude Code.

---

## Prerequisites

- Unity 6000.0.45 (or compatible Unity 6 version)
- Newtonsoft.Json package installed (`com.unity.nuget.newtonsoft-json`)
- Basic understanding of JSON-RPC 2.0 protocol
- HTTP client for testing (curl, Postman, or Claude Code)

---

## Installation

### 1. Install Newtonsoft.Json Package

Open Unity Package Manager:
```
Window → Package Manager → [+] → Add package by name
```

Enter package name:
```
com.unity.nuget.newtonsoft-json
```

Wait for installation to complete (version 3.2.2 or later).

### 2. Verify Project Structure

After implementation, verify these directories exist:
```
Assets/Scripts/
├── Configuration/
│   └── MCPServerConfiguration.cs
├── Models/MCP/
│   └── [ErrorLogEntry, MCPRequest, MCPResponse, etc.]
├── Services/MCP/
│   └── [MCPServer, ErrorLogBuffer, etc.]
└── Enums/
    └── [ErrorSeverity, MCPServerStatus]
```

### 3. Create MCP Server Configuration

In Unity Editor:
```
Assets → Create → Configuration → MCP Server
```

Configure settings:
- **Server Port**: 5678 (default) or custom port
- **Max Buffer Size**: 10000 (adjust based on memory constraints)
- **Session Timeout**: 3600 seconds (1 hour)

Save as `Assets/Configuration/MCPServerConfiguration.asset`

---

## Starting the Server

### Automatic Startup (Recommended)

The MCP server starts automatically when Unity Editor launches, managed by `MCPServerManager` ScriptableSingleton with `InitializeOnLoad`.

**Verify Server Started**:
1. Open Unity Console
2. Look for log message: `[MCP Server] Started on http://127.0.0.1:5678/mcp`
3. If no message appears, check for initialization errors

### Manual Control (Optional)

If you implemented the optional Editor Window:
```
Window → MCP → Server Control Panel
```

Controls:
- **Start Server**: Manually start if auto-start disabled
- **Stop Server**: Gracefully shutdown server
- **View Status**: Check active sessions, buffer size, error count

---

## Testing the Server

### Test 1: Server Health Check

**Using curl**:
```bash
curl -X POST http://127.0.0.1:5678/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "get_server_status",
    "params": {}
  }'
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"status\":\"running\",\"port\":5678,\"activeSessions\":0,\"bufferSize\":0,\"totalErrorsCaptured\":0,\"lastErrorTimestamp\":null,\"isCapturing\":true}"
      }
    ]
  }
}
```

### Test 2: Tool Discovery

**Request**:
```bash
curl -X POST http://127.0.0.1:5678/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "get_unity_errors",
        "description": "Retrieves Unity console error logs with optional filtering...",
        "inputSchema": { ... }
      },
      {
        "name": "clear_error_log",
        "description": "Clears all buffered error logs from memory...",
        "inputSchema": { ... }
      },
      {
        "name": "get_server_status",
        "description": "Retrieves the current MCP server operational status...",
        "inputSchema": { ... }
      }
    ]
  }
}
```

### Test 3: Triggering and Retrieving Errors

**Step 1: Generate Test Errors in Unity**

Create a temporary test script:
```csharp
// Assets/Scripts/Test/MCPTestErrors.cs
using UnityEngine;

public class MCPTestErrors : MonoBehaviour
{
    void Start()
    {
        Debug.LogError("Test error 1: Null reference in CharacterAgent");
        Debug.LogWarning("Test warning: Missing audio clip");
        Debug.LogException(new System.Exception("Test exception: Failed to load scene"));
    }
}
```

Attach to any GameObject and enter Play Mode.

**Step 2: Query Errors**

```bash
curl -X POST http://127.0.0.1:5678/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "get_unity_errors",
      "arguments": {
        "severity": "error",
        "limit": 10
      }
    }
  }'
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "[{\"EntryId\":\"abc-123\",\"Message\":\"Test error 1: Null reference in CharacterAgent\",\"StackTrace\":\"...\",\"Severity\":\"Error\",\"Timestamp\":\"2025-10-30T15:45:30Z\"}]"
      }
    ]
  }
}
```

### Test 4: Clearing Error Log

```bash
curl -X POST http://127.0.0.1:5678/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
      "name": "clear_error_log",
      "arguments": {
        "confirm": true
      }
    }
  }'
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Error log cleared. 3 entries removed."
      }
    ]
  }
}
```

---

## Integration with Claude Code

### Configuration

Add MCP server to Claude Code configuration:

**File**: `.claude/mcp-servers.json` (create if doesn't exist)
```json
{
  "mcpServers": {
    "unity-errors": {
      "url": "http://127.0.0.1:5678/mcp",
      "transport": "http"
    }
  }
}
```

### Usage Examples

**Example 1: Check Unity for Errors**

In Claude Code chat:
```
Check Unity for any errors in the last 5 minutes
```

Claude will:
1. Call `get_unity_errors` with `since` timestamp
2. Parse error messages and stack traces
3. Provide debugging suggestions

**Example 2: Clear Errors Before Testing**

```
Clear all Unity errors and start fresh debugging session
```

Claude will:
1. Call `clear_error_log` with `confirm: true`
2. Confirm successful clearing
3. Optionally monitor for new errors

**Example 3: Monitor Specific Error Types**

```
Show me only exceptions from the last hour
```

Claude will:
1. Calculate timestamp from 1 hour ago
2. Call `get_unity_errors` with `severity: "exception"` and `since: [timestamp]`
3. Format results for readability

---

## Common Workflows

### Workflow 1: Debugging Session

1. **Start Unity and MCP Server**:
   - Unity Editor launches → MCP server auto-starts
   - Verify server running: check Console for startup message

2. **Clear Previous Errors**:
   - Use Claude Code: "Clear Unity error log"
   - Or manually call `clear_error_log`

3. **Reproduce Bug**:
   - Perform actions in Unity that trigger the bug
   - Errors automatically captured in buffer

4. **Analyze Errors with AI Agent**:
   - Ask Claude: "What errors occurred in Unity?"
   - Claude retrieves and analyzes error patterns
   - Provides root cause analysis and fix suggestions

5. **Iterate**:
   - Apply fixes in code
   - Clear errors
   - Test again

### Workflow 2: Continuous Monitoring (Priority 3 Feature)

**When implemented**:
1. **Subscribe to Notifications**:
   ```
   Use Claude Code: "Monitor Unity for errors"
   ```
   Claude calls `subscribe_error_notifications`

2. **Work in Unity**:
   - Errors push to Claude Code in real-time
   - No need to manually query

3. **Automatic Alerts**:
   - Claude notifies when critical errors occur
   - Proactive debugging assistance

4. **Unsubscribe When Done**:
   ```
   Stop monitoring Unity errors
   ```
   Claude calls `unsubscribe_error_notifications`

### Workflow 3: Pre-Commit Error Check

1. **Before Committing Code**:
   - Run Unity tests
   - Ask Claude: "Check Unity for any errors in the last 10 minutes"

2. **Review Errors**:
   - Claude lists all errors with timestamps
   - Identify errors introduced by recent changes

3. **Fix Before Commit**:
   - Resolve all errors
   - Clear log
   - Run tests again to confirm clean state

4. **Commit**:
   - Only commit when error log is clear

---

## Troubleshooting

### Issue: Server Won't Start

**Symptoms**: No startup message in Console, curl requests fail

**Solutions**:
1. **Port Conflict**: Another application using port 5678
   - Change port in `MCPServerConfiguration`
   - Restart Unity Editor

2. **Initialization Error**: Check Console for exception messages
   - Look for `[MCP Server]` tagged errors
   - Common issues: missing Newtonsoft.Json, permission errors

3. **Domain Reload Issues**: Server stuck from previous session
   - Close Unity Editor completely
   - Delete `Library/ScriptAssemblies/` folder
   - Reopen Unity to force clean domain reload

### Issue: Errors Not Captured

**Symptoms**: `get_unity_errors` returns empty array despite errors in Console

**Solutions**:
1. **Check Log Type**: Server only captures Error/Warning/Exception
   - Debug.Log() calls are NOT captured (by design)
   - Use Debug.LogError() for testing

2. **Buffer Full**: Old errors discarded (circular buffer)
   - Check `get_server_status` for `bufferSize`
   - Increase `MaxBufferSize` in configuration

3. **Timing Issue**: Errors occurred before server started
   - Server only captures errors AFTER initialization (by design)
   - Restart Unity to capture from fresh start

### Issue: Claude Code Can't Connect

**Symptoms**: "Failed to connect to MCP server" in Claude Code

**Solutions**:
1. **Verify Server Running**: Check Unity Console for startup message

2. **Check Configuration**: Ensure `.claude/mcp-servers.json` URL matches Unity port

3. **Firewall/Network**: localhost connections should work by default
   - On Windows: Check Windows Firewall isn't blocking Unity
   - Try pinging: `curl http://127.0.0.1:5678/mcp`

4. **CORS Issues**: Server should allow localhost origin
   - Check `MCPServerConfiguration.AllowedOrigins` includes Claude Code origin

### Issue: High Memory Usage

**Symptoms**: Unity Editor using excessive RAM

**Solutions**:
1. **Reduce Buffer Size**: Lower `MaxBufferSize` in configuration
   - Default 10000 entries ≈ 50-100MB depending on stack traces
   - Reduce to 1000 for lower memory footprint

2. **Clear Periodically**: Set up auto-clear on interval
   - Call `clear_error_log` every N minutes
   - Or after each test run

3. **Enable High Volume Mode**: If seeing 1000s of errors
   - Set `EnableHighVolumeMode = true` in configuration
   - Server will rate-limit capture to prevent overflow

---

## Performance Benchmarks

Based on Success Criteria (SC-002, SC-004):

| Operation | Target Performance | Typical Performance |
|-----------|-------------------|---------------------|
| Query 100 errors | <500ms | ~50ms |
| Query 1000 errors | <500ms | ~200ms |
| Clear error log | <100ms | ~10ms |
| Capture error | <10ms (non-blocking) | ~1ms |
| Frame rate impact (10 errors/min) | 0% | <0.1% |

---

## Next Steps

1. **Explore Tool Definitions**: Review `contracts/mcp-tools.json` for full API documentation
2. **Read Data Model**: Understand data structures in `data-model.md`
3. **Review Architecture**: See implementation plan in `plan.md`
4. **Contribute**: Implement Priority 3 features (real-time notifications)

---

## Support

- **Issues**: Check Unity Console for `[MCP Server]` tagged messages
- **Documentation**: See `specs/002-unity-mcp-error-logs/` directory
- **Code**: Located in `Assets/Scripts/Services/MCP/`

---

## Version History

- **v1.0 (Priority 1)**: Core query functionality
- **v1.1 (Priority 2)**: Severity filtering (planned)
- **v2.0 (Priority 3)**: Real-time notifications (planned)
