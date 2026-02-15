# PCTama - Project Architecture

## Overview

PCTama is a microservices-based desktop pet application that uses the Model Context Protocol (MCP) to enable AI-driven interactions. The system is built on .NET 8.0 Aspire framework and uses CMake for cross-platform build management.

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    PCTama.AppHost                           │
│                  (Aspire Orchestrator)                      │
└────────┬────────────────────┬───────────────────┬──────────┘
         │                    │                   │
         ▼                    ▼                   ▼
┌────────────────┐   ┌────────────────┐   ┌────────────────┐
│   TextMCP      │   │   Controller   │   │   ActorMCP     │
│   (Input)      │──▶│   (Orchestr.)  │──▶│   (Output)     │
│                │   │                │   │                │
│ - OBS Local    │   │ - MCP SDK      │   │ - WinUI3       │
│   Voice        │   │ - Local LLM    │   │ - Actions      │
│ - Streaming    │   │ - Routing      │   │ - Display      │
│ - Buffering    │   │                │   │                │
└────────────────┘   └────────────────┘   └────────────────┘
         │                    │                   │
         └────────────────────┴───────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ ServiceDefaults  │
                    │ - Telemetry      │
                    │ - Health Checks  │
                    │ - Service Disc.  │
                    └──────────────────┘
```

## Components

### 1. PCTama.AppHost
**Purpose**: Aspire orchestration host

**Responsibilities**:
- Service discovery and registration
- Configuration management
- Health monitoring
- Endpoint routing

**Key Files**:
- `Program.cs` - Main orchestration logic
- `appsettings.json` - Configuration

### 2. PCTama.Controller
**Purpose**: Central orchestrator and MCP client

**Responsibilities**:
- MCP SDK integration
- Local LLM communication
- Input/output routing
- State management

**Key Components**:
- `Controllers/ControllerController.cs` - API endpoints
- `Services/McpClientService.cs` - MCP integration
- `Models/McpConfiguration.cs` - Configuration models

**Endpoints**:
- `GET /api/controller/status` - System status
- `GET /api/controller/health` - Health check

### 3. PCTama.TextMCP
**Purpose**: Input streaming service

**Responsibilities**:
- OBS LocalVoice integration
- Text stream buffering
- Multiple input source support
- Read-only text provision

**Key Components**:
- `Controllers/TextController.cs` - API endpoints
- `Services/TextStreamService.cs` - Streaming logic
- `Models/TextStreamConfiguration.cs` - Configuration

**Endpoints**:
- `GET /api/text/stream` - Latest text
- `GET /api/text/buffer` - All buffered text
- `GET /api/text/status` - Service status

**Input Sources**:
- OBS LocalVoice (WebSocket)
- Extensible for additional sources

### 4. PCTama.ActorMCP
**Purpose**: Output display service

**Responsibilities**:
- WinUI3 desktop window
- Action execution
- Visual output
- Animation support

**Key Components**:
- `Controllers/ActorController.cs` - API endpoints
- `Services/ActorService.cs` - Action processing
- `Models/ActorConfiguration.cs` - Configuration

**Endpoints**:
- `POST /api/actor/perform` - Execute action
- `POST /api/actor/say` - Display with speech
- `POST /api/actor/display` - Simple display
- `GET /api/actor/status` - Service status

**Actions**:
- `say` - Display text with optional speech
- `display` - Show text in window
- `animate` - Perform animations

### 5. PCTama.ServiceDefaults
**Purpose**: Shared service infrastructure

**Responsibilities**:
- OpenTelemetry configuration
- Health check defaults
- Service discovery setup
- Resilient HTTP clients

**Features**:
- Distributed tracing
- Metrics collection
- Log aggregation
- Standard health endpoints

## Data Flow

### Input Processing Flow
```
OBS LocalVoice → WebSocket → TextMCP → Buffer → Controller → LLM
```

### Output Processing Flow
```
LLM → Controller → ActorMCP → Action Queue → WinUI3 Display
```

### Complete Cycle
```
1. TextMCP receives streaming text from OBS LocalVoice
2. Text is buffered and made available via API
3. Controller polls TextMCP for new text
4. Controller sends text to local LLM via MCP SDK
5. LLM processes and generates response
6. Controller routes response to ActorMCP
7. ActorMCP queues and executes actions
8. WinUI3 displays output to user
```

## Configuration

### Controller Configuration
```json
{
  "McpConfiguration": {
    "LocalLlmEndpoint": "http://localhost:11434",
    "ModelName": "llama2",
    "McpServers": [...],
    "AdditionalInputMcps": [...]
  }
}
```

### Text MCP Configuration
```json
{
  "TextMcpConfiguration": {
    "Source": "OBSLocalVoice",
    "OBSLocalVoiceEndpoint": "ws://localhost:4455",
    "StreamingEnabled": true,
    "BufferSize": 4096,
    "AdditionalSources": [...]
  }
}
```

### Actor MCP Configuration
```json
{
  "ActorMcpConfiguration": {
    "DisplayType": "WinUI3",
    "WindowWidth": 400,
    "WindowHeight": 300,
    "AlwaysOnTop": true,
    "EnableAnimations": true
  }
}
```

## Extensibility

### Adding New Input Sources

1. Create configuration entry in `AdditionalInputMcps`
2. Implement source-specific handler in TextMCP
3. Controller automatically discovers and connects

### Adding New Action Types

1. Add action case in `ActorService.ProcessActionAsync()`
2. Implement action handler method
3. Update WinUI3 interface as needed

### Adding New MCP Services

1. Create new project following MCP pattern
2. Add to `PCTama.AppHost/Program.cs`
3. Update CMakeLists.txt
4. Controller will discover via service discovery

## Technology Stack

- **.NET 8.0**: Runtime and framework
- **ASP.NET Core**: Web API framework
- **.NET Aspire**: Microservices orchestration
- **WinUI3**: Windows desktop UI
- **MCP SDK**: Model Context Protocol integration
- **CMake**: Cross-platform build system
- **xUnit**: Testing framework
- **OpenTelemetry**: Observability

## Build System

### CMake Integration
- Root CMakeLists.txt orchestrates all projects
- Delegates to dotnet CLI for actual builds
- Supports parallel builds
- Custom targets for restore, build, test

### .NET Build
- Standard .NET solution structure
- NuGet package management
- Multi-targeting support
- Project references for dependencies

## Testing Strategy

### Unit Tests
- Service logic testing
- Configuration validation
- Model validation

### Integration Tests
- API endpoint testing
- Inter-service communication
- Health check validation

### Test Organization
```
tests/PCTama.Tests/
├── Controller/
│   ├── ControllerTests.cs
│   └── McpClientServiceTests.cs
├── TextMCP/
│   └── TextStreamServiceTests.cs
└── ActorMCP/
    └── ActorServiceTests.cs
```

## Deployment

### Local Development
```bash
dotnet run --project src/PCTama.AppHost/PCTama.AppHost.csproj
```

### Production (Future)
- Container deployment via Docker
- Kubernetes orchestration
- Azure Container Apps (Aspire native)

## Monitoring

### Health Endpoints
- `/health` - Overall service health
- `/alive` - Liveness probe
- Service-specific health checks

### Metrics
- Request rates
- Response times
- Error rates
- Queue depths

### Tracing
- Distributed traces across services
- LLM call tracking
- Input/output correlation

## Security Considerations

- Local-only LLM (no external API calls)
- Service-to-service authentication (future)
- Input validation and sanitization
- Rate limiting (future enhancement)

## Performance

### Optimizations
- Buffered input streams
- Asynchronous processing
- Queue-based action execution
- Efficient service discovery

### Scalability
- Horizontal scaling via Aspire
- Independent service deployment
- Stateless design (except buffers)

## Future Enhancements

1. **Additional Input Sources**
   - Keyboard input
   - File monitoring
   - Web hooks

2. **Enhanced Output**
   - Speech synthesis
   - Custom animations
   - Multiple display modes

3. **Advanced MCP Features**
   - Tool calling
   - Context management
   - Multi-model support

4. **Production Features**
   - Authentication/Authorization
   - Rate limiting
   - Persistent storage
   - Container deployment
