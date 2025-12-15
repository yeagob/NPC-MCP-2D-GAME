# Feature Specification: Unity MCP Error Log Server

**Feature Branch**: `002-unity-mcp-error-logs`
**Created**: 2025-10-30
**Status**: Draft
**Input**: User description: "quiero crear un Model Context Protocol que te permita solicitar a Unity el log de errores actuales del proyecto, de manera que Unity haría de server y podemos pedirle el estado de los logs de error que hay actualmente, analzia como podríamos hacer esto."

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Query Current Error Logs (Priority: P1)

An AI agent (Claude Code or other MCP client) needs to check if Unity has any runtime errors during development or gameplay sessions to assist developers in debugging.

**Why this priority**: This is the core functionality - enabling external tools to access Unity's error state. Without this, the feature has no value. It provides immediate debugging assistance capability.

**Independent Test**: Can be fully tested by starting Unity with the MCP server, triggering an error in Unity (e.g., null reference), then using an MCP client to query the error log and verify the error appears in the response.

**Acceptance Scenarios**:

1. **Given** Unity is running with MCP server active and has captured 3 errors, **When** an MCP client requests the current error logs, **Then** the client receives all 3 error messages with timestamps and stack traces
2. **Given** Unity is running with no errors logged, **When** an MCP client requests the current error logs, **Then** the client receives an empty error list with a success status
3. **Given** Unity has logged 5 errors and the client requests only the last 2, **When** the query includes a limit parameter, **Then** the client receives only the 2 most recent errors

---

### User Story 2 - Filter Errors by Severity (Priority: P2)

A developer wants to focus only on critical errors while ignoring warnings during a debugging session.

**Why this priority**: Filtering improves usability but isn't essential for MVP. Developers can manually filter results from the full log initially.

**Independent Test**: Can be tested by generating errors of different severity levels (Error, Warning, Exception) in Unity, then querying with severity filters and verifying only matching entries are returned.

**Acceptance Scenarios**:

1. **Given** Unity has logged 2 errors and 3 warnings, **When** an MCP client requests logs filtered by severity "Error", **Then** the client receives only the 2 error entries
2. **Given** Unity has logged multiple log types, **When** an MCP client requests logs with severity "Exception", **Then** only exception entries are returned
3. **Given** Unity has mixed log entries, **When** an MCP client requests logs without a severity filter, **Then** all log entries are returned regardless of type

---

### User Story 3 - Real-Time Error Monitoring (Priority: P3)

An AI agent monitors Unity for errors continuously during a development session, receiving notifications when new errors occur.

**Why this priority**: Real-time monitoring is valuable but requires more complex implementation (pub/sub pattern). The core query functionality (P1) already enables periodic polling as a workaround.

**Independent Test**: Can be tested by subscribing to error notifications, triggering an error in Unity, and verifying the client receives a push notification within 1 second.

**Acceptance Scenarios**:

1. **Given** an MCP client has subscribed to error notifications, **When** a new error occurs in Unity, **Then** the client receives a notification with the error details within 1 second
2. **Given** multiple clients are subscribed, **When** an error occurs, **Then** all subscribed clients receive the notification
3. **Given** a client unsubscribes from notifications, **When** new errors occur, **Then** that client no longer receives notifications

---

### Edge Cases

- What happens when Unity logs thousands of errors in rapid succession? (System must handle high-volume logging without blocking Unity's main thread or causing performance degradation)
- How does the system handle errors that occur before the MCP server starts? (System only captures errors after MCP server initialization - no historical Unity console buffer retrieval)
- What happens when an MCP client disconnects while Unity is running? (Server must clean up resources and handle reconnection gracefully)
- How are errors handled when Unity is in Play Mode vs Edit Mode? (System must capture errors from both modes)
- What happens when the error log buffer reaches maximum capacity? (System must implement a circular buffer or truncation strategy to prevent memory exhaustion)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose Unity error logs via Model Context Protocol (MCP) server interface
- **FR-002**: System MUST capture errors logged through Unity's Debug.LogError, Debug.LogException, and Debug.LogWarning methods
- **FR-003**: System MUST provide error details including message text, stack trace, timestamp, and severity level
- **FR-004**: System MUST allow clients to query the current error log with optional filters (severity level, count limit, time range)
- **FR-005**: System MUST run as a background server within Unity without blocking the main thread or editor operations
- **FR-006**: System MUST provide a tool definition for MCP clients to discover and invoke error log queries
- **FR-007**: System MUST maintain error logs in memory only (no persistence between Unity sessions - logs cleared when Unity closes)
- **FR-008**: System MUST support concurrent requests from multiple MCP clients without data corruption
- **FR-009**: System MUST provide a mechanism to clear the error log buffer on demand
- **FR-010**: System MUST expose server connection status (running, stopped, error) for monitoring

### Key Entities

- **Error Log Entry**: Represents a single logged error/warning with attributes: message (string), stack trace (string), timestamp (datetime), severity level (enum: Error, Warning, Exception, Assert), log type (enum: Error, Warning, Exception)
- **MCP Tool Definition**: Defines the "get_unity_errors" tool callable by MCP clients with parameters: severity_filter (optional), limit (optional), since_timestamp (optional)
- **Error Log Buffer**: In-memory collection of Error Log Entries with configurable maximum capacity and retention policy
- **MCP Server Configuration**: Defines server settings including port number, enabled/disabled state, buffer size, and retention duration

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: MCP clients can successfully retrieve Unity error logs with 100% accuracy (all logged errors are returned with correct details)
- **SC-002**: Error log queries complete within 500ms for buffers containing up to 1000 errors
- **SC-003**: Server handles at least 10 concurrent client connections without dropping requests or corrupting data
- **SC-004**: Unity performance remains unaffected (no measurable frame rate impact) when the MCP server is running with moderate error logging (up to 10 errors per minute)
- **SC-005**: 90% of error log queries return results without client-side parsing errors (well-formed JSON responses)
- **SC-006**: Developers can identify and resolve errors 30% faster by having AI agents with access to Unity error logs compared to manual console checking

## Assumptions

- Unity version supports background networking and threading for the MCP server
- MCP clients will use standard MCP protocol for communication (JSON-RPC over stdio or HTTP)
- Error logs will be primarily text-based (no binary attachments or large file references)
- The system will use Unity's existing Debug.Log infrastructure rather than implementing custom logging
- Network communication will occur on localhost (no remote access required initially)
- The MCP server will be a development/debugging tool, not intended for production builds

## Dependencies

- Unity's Application.logMessageReceived or Application.logMessageReceivedThreaded callback API for capturing logs
- MCP protocol implementation (either custom or third-party library compatible with Unity's .NET version)
- JSON serialization capability for MCP message formatting
- Threading/async support for non-blocking server operations

## Out of Scope

- Modifying or clearing Unity's native Console window (feature only exposes logs via MCP, doesn't alter Unity's UI)
- Log persistence to disk or database (memory-only for MVP)
- Advanced log analytics or aggregation (clients handle analysis)
- Authentication or encryption for MCP connections (assumed trusted local development environment)
- Support for non-error logs (Info, Debug level logs excluded in MVP)
- Historical log playback from previous Unity sessions
- Integration with external logging services (Sentry, LogRocket, etc.)
