# PCTama - Desktop Pet with AI Integration

[![Build and Test](https://github.com/yourusername/PCTama/workflows/PCTama%20Build%20and%20Test/badge.svg)](https://github.com/yourusername/PCTama/actions)
[![CMake Build](https://github.com/yourusername/PCTama/workflows/CMake%20Build/badge.svg)](https://github.com/yourusername/PCTama/actions)

A sophisticated desktop pet application that integrates AI to allow the pet to talk to the user. Built with ASP.NET Core, .NET Aspire framework, and Model Context Protocol (MCP) integration, PCTama uses a local LLM to process streaming text input and provides interactive output via WinUI3.

## 🎯 Project Goals

- **Modeless app with transparency** - Desktop pet overlay using WinUI3
- **Controller architecture** - Associate various inputs with different models and MCPs
- **Extensible input sources** - Support for OBS LocalVoice and additional text sources
- **Interactive output** - Display actions and responses through WinUI3 interface

## 🏗️ Architecture

PCTama is built as a collection of ASP.NET microservices orchestrated through .NET Aspire:

- **PCTama.Controller**: Main orchestration service that integrates the MCP SDK and coordinates between input and output MCPs
- **PCTama.TextMCP**: Streaming text input service (supports OBS LocalVoice and extensible for other sources)
- **PCTama.ActorMCP**: WinUI3-based output service for displaying actions and responses
- **PCTama.AppHost**: Aspire orchestrator for service discovery and management
- **PCTama.ServiceDefaults**: Shared service defaults with OpenTelemetry and health checks

## 🚀 Features

- **MCP Integration**: Uses the official .NET MCP SDK for local LLM connectivity
- **Streaming Text Input**: Real-time text processing from OBS LocalVoice with support for additional sources
- **WinUI3 Display**: Modern Windows UI for desktop pet interactions
- **Extensible Architecture**: Easy to add new MCP services for additional input sources
- **Aspire Framework**: Built-in service discovery, observability, and health monitoring
- **CMake Build System**: Cross-platform build support with .NET integration
- **Comprehensive Testing**: Full test suite using xUnit with integration tests

## 📋 Prerequisites

- .NET 8.0 SDK or later
- CMake 3.20 or later
- Visual Studio 2022 (for WinUI3 development on Windows) or VS Code
- Windows 10 Build 19041 or later (for ActorMCP/WinUI3)
- Optional: OBS Studio with LocalVoice plugin for text input

## 🔧 Building the Project

### Using CMake

```bash
# Configure the build
cmake -B build

# Build all projects
cmake --build build

# Run tests
cmake --build build --target test_all
```

### Using .NET CLI

```bash
# Restore dependencies
dotnet restore PCTama.sln

# Build all projects
dotnet build PCTama.sln --configuration Release

# Run tests
dotnet test tests/PCTama.Tests/PCTama.Tests.csproj
```

### Using Visual Studio

1. Open `PCTama.sln` in Visual Studio 2022
2. Build the solution (Ctrl+Shift+B)
3. Run tests from Test Explorer

## 🏃 Running the Application

### Using .NET Aspire AppHost

```bash
cd src/PCTama.AppHost
dotnet run
```

This will start:
- Controller service on http://localhost:5000
- TextMCP service on http://localhost:5001
- ActorMCP service on http://localhost:5002

### Running Individual Services

```bash
# Controller
cd src/PCTama.Controller
dotnet run

# Text MCP
cd src/PCTama.TextMCP
dotnet run

# Actor MCP (Windows only)
cd src/PCTama.ActorMCP
dotnet run
```

## ⚙️ Configuration

### Controller Configuration

Edit `src/PCTama.Controller/appsettings.json`:

```json
{
  "McpConfiguration": {
    "LocalLlmEndpoint": "http://localhost:11434",
    "ModelName": "llama2",
    "McpServers": [
      {
        "Name": "text",
        "Endpoint": "http://textmcp",
        "Type": "Input",
        "Enabled": true
      }
    ],
    "AdditionalInputMcps": []
  }
}
```

### Text MCP Configuration

Edit `src/PCTama.TextMCP/appsettings.json`:

```json
{
  "TextMcpConfiguration": {
    "Source": "OBSLocalVoice",
    "OBSLocalVoiceEndpoint": "ws://localhost:4455",
    "StreamingEnabled": true,
    "BufferSize": 4096,
    "AdditionalSources": []
  }
}
```

### Actor MCP Configuration

Edit `src/PCTama.ActorMCP/appsettings.json`:

```json
{
  "ActorMcpConfiguration": {
    "DisplayType": "WinUI3",
    "WindowWidth": 400,
    "WindowHeight": 300,
    "WindowTitle": "PCTama Actor",
    "AlwaysOnTop": true,
    "EnableAnimations": true
  }
}
```

## 🧪 Testing

The test suite includes unit tests and integration tests for all services:

```bash
# Run all tests
dotnet test tests/PCTama.Tests/PCTama.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ControllerTests"
```

## 📊 API Endpoints

### Controller API

- `GET /api/controller/status` - Get MCP connection status
- `GET /api/controller/health` - Health check
- `GET /health` - Aspire health endpoint
- `GET /alive` - Liveness check

### Text MCP API

- `GET /api/text/stream` - Get latest text from stream
- `GET /api/text/buffer` - Get all buffered text
- `GET /api/text/status` - Get service status

### Actor MCP API

- `POST /api/actor/perform` - Perform an action
- `POST /api/actor/say` - Display text with speech
- `POST /api/actor/display` - Display text
- `GET /api/actor/status` - Get service status

## 🔌 Adding Additional MCP Sources

To add a new input MCP source:

1. Add configuration in `appsettings.json`:

```json
{
  "McpConfiguration": {
    "AdditionalInputMcps": [
      {
        "Name": "customsource",
        "Endpoint": "http://localhost:5003",
        "Type": "Input",
        "Enabled": true,
        "Configuration": {
          "customProperty": "value"
        }
      }
    ]
  }
}
```

2. The controller will automatically discover and connect to the new MCP service.

## 🏗️ Project Structure

```
PCTama/
├── .github/workflows/      # GitHub Actions workflows
├── src/
│   ├── PCTama.AppHost/     # Aspire orchestration
│   ├── PCTama.ServiceDefaults/  # Shared service configuration
│   ├── PCTama.Controller/  # Main controller service
│   ├── PCTama.TextMCP/     # Text input MCP service
│   └── PCTama.ActorMCP/    # WinUI3 actor service
├── tests/
│   └── PCTama.Tests/       # Test suite
├── CMakeLists.txt          # Root CMake configuration
├── PCTama.sln              # Visual Studio solution
└── README.md
```

## 🤝 Contributing

Contributions are welcome! Please ensure:

1. All tests pass: `dotnet test`
2. Code follows .editorconfig guidelines
3. New features include tests
4. Update documentation as needed

## 📝 License

[Your License Here]

## 🙏 Acknowledgments

- .NET Aspire team for the excellent framework
- Model Context Protocol (MCP) for standardized AI integration
- OBS Studio and LocalVoice plugin for text input capabilities