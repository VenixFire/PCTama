# LLM Response Logging Improvements

## Overview
Enhanced logging has been implemented across the LLM response pipeline to provide comprehensive visibility into LLM interactions, performance metrics, and system behavior.

## Files Modified

### 1. **McpSdkClient.cs** - Core LLM Communication Logging
Location: `src/PCTama.Controller/Services/McpSdkClient.cs`

#### GenerateAsync() Improvements
- **Request Logging**: Model, prompt length, system prompt presence
- **Performance Metrics**: 
  - Total response time (seconds)
  - Response tokens (eval count)
  - Prompt tokens (prompt eval count)
  - Tokens per second calculation
  - Response length (characters)
  - Model load time (seconds)
- **Error Logging**: HTTP status codes with detailed error context
- **Content Preview**: First 150 characters of response (trace level)
- **Duration Tracking**: Stopwatch measurements for all operations

#### ChatAsync() Improvements
- **Message Statistics**:
  - Total message count breakdown (system, user, assistant)
  - Total character count across all messages
  - Message role distribution
- **Performance Metrics**: Same as Generate + truncation detection
- **Last User Message Preview**: Preview of the most recent user input
- **Response Truncation Detection**: Flag when responses exceed 2000 characters
- **Request/Response Cycle Tracking**: Full request-response timing

### 2. **McpClientService.cs** - High-Level LLM Processing Logging
Location: `src/PCTama.Controller/Services/McpClientService.cs`

#### ExecuteAsync() Lifecycle Logging
- Service startup/shutdown with timestamps
- Processing interval configuration logging
- Initialization complete notification

#### ProcessMcpCycleAsync() Input Logging
- Input preview (first 80 characters)
- Input length in characters
- Structured input tracking

#### ProcessWithLlmAsync() Response Processing
- **Mode Selection**: Chat vs. Generate mode logging
- **Chat Mode Specifics**:
  - System prompt presence
  - History size tracking
  - Total message count
  - History updates after each response
- **Generate Mode Specifics**:
  - Template usage detection
  - Final prompt length
- **Response Analysis**:
  - Response length in characters
  - Token calculations (response + prompt)
  - Total tokens
  - Duration in seconds
  - Response preview (first 120 characters)
- **Error Handling**:
  - Connection error detection and reporting
  - Ollama startup hints
  - Retry schedule notifications
  - Exception type classification

#### CheckLlmAvailabilityAsync() Health Monitoring
- Health check OK/FAIL status
- Endpoint and model information
- Detailed error messages
- Exception tracking

#### SendToActorAsync() Action Dispatching
- Response length tracking
- Action type and endpoint logging
- HTTP status code reporting
- Client availability status
- Response truncation on empty responses
- Detailed error context

#### ApplyBehaviorRules() Behavior Rule Application
- Rule trigger patterns and names
- Input preview for matching
- Response override comparisons (original vs. override length)
- Exception handling per rule

#### DetermineAction() Action Mapping
- Pattern matching diagnostics
- Matched action details
- Response preview for pattern context
- Fallback to default action notification

#### InitializeMcpClientsAsync() Initialization
- LLM SDK client configuration (endpoint, model, timeout, chat mode, max history)
- MCP server initialization per server
- Additional input source registration
- Exception handling with server identification

## Logging Format Features

### Structured Logging
All logs use consistent patterns with:
- **[Component]** prefix: `[LLM]`, `[Input]`, `[Actor]`, `[Behavior]`, `[Action]`, `[MCP]`
- **Named Parameters**: Properties for structured search/filtering
- **Context Information**: Timestamps, durations, metrics
- **Hierarchical Details**: Debug and Trace levels for detailed diagnostics

### Log Levels
- **Information**: Major operations, successful responses, state changes
- **Warning**: Connection issues, retries, unavailable services
- **Error**: Processing failures, exceptions, catastrophic errors
- **Debug**: Operational details, pattern matching, response previews
- **Trace**: Content previews, detailed message structures, configuration details

### Performance Metrics Tracked
| Metric | Purpose | Unit |
|--------|---------|------|
| Total Duration | End-to-end LLM call time | Seconds |
| Response Tokens | Output generation size | Count |
| Prompt Tokens | Input processing size | Count |
| Tokens Per Second | Generation speed | Tokens/sec |
| Load Time | Model loading overhead | Seconds |
| Response Length | Output size | Characters |
| Message Count | Conversation history | Count |
| Input Length | Input size | Characters |

## Example Log Output

```
[LLM] MCP SDK client initialized | Endpoint=http://localhost:11434 | Model=mistral | Timeout=120s | ChatMode=True | MaxHistory=10
[LLM] Health check OK | Endpoint=http://localhost:11434 | Model=mistral | Status=available
[Input] Received text | Content=Hello, how are you?... | Length=23c
[LLM] Sending chat request | Model=mistral | Messages=3 (System=1, User=2, Assistant=0) | TotalChars=156
[LLM] Chat response received | Model=mistral | Duration=2.34s | ResponseTokens=45 | PromptTokens=28 | TokensPerSec=19.2 | ResponseLength=187 | LoadTime=0.12s | Truncated=False
[LLM] Processing complete | ResponseLength=187c | ResponseTokens=45 | PromptTokens=28 | TotalTokens=73 | Duration=2.34s | Mode=chat
[Actor] Action sent | Action=say | Endpoint=/api/actor/say | ResponseLength=187c
```

## Benefits

1. **Debugging**: Detailed logs enable quick identification of issues in the LLM pipeline
2. **Performance Monitoring**: Track response times, token counts, and throughput
3. **Diagnostics**: Identify connection issues, model problems, and configuration errors
4. **Audit Trail**: Complete record of all LLM interactions for troubleshooting
5. **Trend Analysis**: Collect metrics for performance optimization
6. **User Experience**: Understand behavior rule application and action mapping
7. **Health Monitoring**: Track LLM availability and service health

## Build Status
✅ All code compiles successfully with no errors (4 warnings related to OpenTelemetry package vulnerabilities)

## Configuration Recommendations

For optimal logging visibility, configure your logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PCTama.Controller.Services": "Debug",
      "PCTama.Controller.Services.McpSdkClient": "Trace"
    }
  }
}
```

This configuration ensures:
- **Information**: Normal operations and important events
- **Debug**: MCP Controller operational details and pattern matching
- **Trace**: Detailed content and configuration for deep diagnostics
