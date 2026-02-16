# PCTama - Desktop Pet with AI Integration

[![Build and Test](https://github.com/yourusername/PCTama/workflows/PCTama%20Build%20and%20Test/badge.svg)](https://github.com/yourusername/PCTama/actions)
[![CMake Build](https://github.com/yourusername/PCTama/workflows/CMake%20Build/badge.svg)](https://github.com/yourusername/PCTama/actions)

A sophisticated microservices-based desktop pet application that integrates AI to enable intelligent interactions. Built with ASP.NET Core, .NET Aspire framework, and Model Context Protocol (MCP) integration, PCTama uses local LLMs to process streaming text input and provides interactive visual output via Avalonia UI.

## 🎯 Project Goals

- ✅ **Aspire-based microservices** - Cloud-native architecture with service discovery
- ✅ **MCP integration ready** - Framework for Model Context Protocol implementation
- ✅ **Streaming text input** - Real-time text processing from multiple sources
- ✅ **Desktop pet display** - Avalonia UI-based output with actions and animations
- ✅ **Extensible design** - Easy to add new input/output MCPs
- ✅ **Cross-platform builds** - CMake support for Windows, macOS, and Linux

## Quick Start

```bash
# Clone and enter directory
git clone https://github.com/yourusername/PCTama.git
cd PCTama

# Build and run (macOS/Linux)
./build.sh run

# Or on Windows
build.bat run
```

Then open **http://localhost:15000** to access the Aspire Dashboard (copy the token from terminal output).

## 🏗️ Architecture

PCTama is built as a collection of ASP.NET microservices orchestrated through .NET Aspire with built-in observability:

```
┌─────────────────────────────────────────────────────────────┐
│                    PCTama.AppHost                           │
│                  (Aspire Orchestrator)                      │
│           Dashboard: http://localhost:15000                 │
└────────┬────────────────────┬───────────────────┬──────────┘
         │                    │                   │
         ▼                    ▼                   ▼
┌────────────────┐   ┌────────────────┐   ┌────────────────┐
│   Text MCP     │   │   Controller   │   │   Actor MCP    │
│   (Port 5001)  │──>│   (Port 5003)  │──>│   (Port 5000)  │
│                │   │                │   │                │
│ • OBS LocalVoice   │ • MCP SDK      │   │ • Avalonia UI  │
│ • Text Stream      │ • Local LLM    │   │ • Actions      │
│ • Buffering        │ • Orchestration│   │ • Display      │
└────────────────┘   └────────────────┘   └────────────────┘
```

**Microservices**:

- **PCTama.Controller** (port 5000) - Central orchestration
  - MCP SDK client integration
  - Local LLM communication
  - Service routing and health management
  
- **PCTama.TextMCP** (port 5001) - Streaming text input
  - OBS LocalVoice integration
  - Thread-safe text buffering
  - Multiple input source support
  
- **PCTama.ActorMCP** (port 5002) - Desktop pet display
  - Avalonia UI-based window management
  - Action queue processing
  - Animation support
  
- **PCTama.AppHost** - Aspire orchestrator
  - Service discovery and registration
  - OpenTelemetry metrics & dashboard
  - Configuration management

## 🚀 Features

### ✅ Implemented
- **Aspire Framework** - Complete microservices orchestration with service discovery
- **MCP SDK Ready** - Framework for Model Context Protocol with configuration support
- **Streaming Text** - OBS LocalVoice integration with thread-safe buffering
- **Action Queue** - Background processing with multiple action types
- **OpenTelemetry** - Built-in observability dashboard
- **Health Checks** - Service monitoring and status endpoints
- **Extensible Config** - Add new MCPs and input sources easily
- **Avalonia UI Display** - Cross-platform UI framework
- **REST APIs** - Comprehensive endpoints for all services
- **Full Testing** - xUnit suite with unit & integration tests
- **CI/CD** - GitHub Actions multi-platform builds
- **CMake** - Cross-platform build system (Windows, macOS, Linux)

## 📋 Prerequisites

- **.NET 8.0 SDK** or later ([download](https://dotnet.microsoft.com/download))
- **CMake** 3.20 or later
- **Git** for version control
- **Windows 10 Build 19041+** (for ActorMCP/Avalonia UI only)

### Optional
- **Ollama** or local LLM for AI responses
- **OBS Studio** with LocalVoice plugin for voice input
- **Visual Studio 2022** or **VS Code**

### Install Dependencies

**Windows:**
```bash
winget install Microsoft.DotNet.SDK.8
winget install Kitware.CMake
```

**macOS:**
```bash
brew install dotnet-sdk cmake
brew install ollama              # Optional: for AI
```

**Linux (Ubuntu/Debian):**
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 8.0

sudo apt-get install cmake
```

## 🛠️ Building

### Using Build Scripts (Recommended)

```bash
# macOS/Linux
./build.sh build          # Build
./build.sh run            # Run
./build.sh test           # Test
./build.sh clean          # Clean

# Windows
build.bat build
build.bat run
build.bat test
build.bat clean
```

### Using .NET CLI

```bash
# Restore and build
dotnet restore PCTama.sln
dotnet build PCTama.sln --configuration Release

# Run tests
dotnet test tests/PCTama.Tests/PCTama.Tests.csproj

# Run specific service
cd src/PCTama.AppHost
dotnet run
```

### Using CMake

```bash
cmake -B build
cmake --build build
cmake --build build --target test_all
```

### Using Visual Studio

1. Open `PCTama.sln` in Visual Studio 2022
2. Set `PCTama.AppHost` as startup project
3. Press F5 to debug (Aspire Dashboard opens automatically)

## 🏃 Running

### Quick Start

```bash
# All services with Aspire Dashboard
./build.sh run              # macOS/Linux
build.bat run               # Windows

# Or manually
cd src/PCTama.AppHost
dotnet run
```

You'll see output with a dashboard token:
```
Login to the dashboard at http://localhost:15000/login?t=YOUR_TOKEN
```

**Copy and paste the full URL into your browser.**

### Services

Once running, you have:
- **Dashboard**: http://localhost:15000 (monitoring & observability)
- **Controller**: http://localhost:5000 (orchestration API)
- **Text MCP**: http://localhost:5001 (text input service)
- **Actor MCP**: http://localhost:5002 (desktop output service)

### Running Individual Services

```bash
# Controller
cd src/PCTama.Controller && dotnet run

# Text MCP
cd src/PCTama.TextMCP && dotnet run

# Actor MCP (Windows only)
cd src/PCTama.ActorMCP && dotnet run
```

### Stop Application

Press `Ctrl+C` in the terminal.

## ⚙️ Configuration

### Controller Setup

Edit `src/PCTama.Controller/appsettings.json`:

```json
{
  "McpConfiguration": {
    "LocalLlmEndpoint": "http://localhost:11434",
    "ModelName": "llama2",
    "McpServers": [
      {
        "Name": "text",
        "Endpoint": "http://localhost:5001",
        "Type": "Input",
        "Enabled": true
      }
    ],
    "AdditionalInputMcps": []
  }
}
```

### Text MCP Setup

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

### Actor MCP Setup

Edit `src/PCTama.ActorMCP/appsettings.json`:

```json
{
  "ActorMcpConfiguration": {
    "DisplayType": "Avalonia UI",
    "WindowWidth": 400,
    "WindowHeight": 300,
    "WindowTitle": "PCTama Actor",
    "AlwaysOnTop": true,
    "EnableAnimations": true
  }
}
```

## Optional: Set up Local LLM (Ollama)

PCTama defaults to `http://localhost:11434` (Ollama):

```bash
# Install Ollama
brew install ollama              # macOS
# Or download from https://ollama.ai for other OS

# Start Ollama server
ollama serve

# In another terminal, pull a model
ollama pull qwen2.5:3b

# Verify it's running
curl http://localhost:11434/api/tags
```

## Optional: Set up OBS LocalVoice

To use voice-to-text input:

1. Install [OBS Studio](https://obsproject.com/)
2. Install [LocalVoice plugin](https://github.com/ClusterM/aitranscription)
3. Configure WebSocket at `ws://localhost:4455` in OBS
4. Update `src/PCTama.TextMCP/appsettings.json` endpoint if different

## 🧪 Testing

```bash
# Run all tests
dotnet test tests/PCTama.Tests/PCTama.Tests.csproj

# With code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific tests
dotnet test --filter "FullyQualifiedName~ControllerTests"

# Using build script
./build.sh test              # macOS/Linux
build.bat test               # Windows
```

## 📊 API Endpoints

### Controller API

- `GET /api/controller/status` - MCP connection status
- `GET /api/controller/health` - Health check
- `GET /health` - Aspire health endpoint
- `GET /alive` - Liveness check

### Text MCP API

- `GET /api/text/stream` - Latest text from stream
- `GET /api/text/buffer` - All buffered text
- `GET /api/text/status` - Service status

### Actor MCP API

- `POST /api/actor/say` - Display text with speech
- `POST /api/actor/display` - Display text only
- `POST /api/actor/perform` - Perform action
- `GET /api/actor/status` - Service status

### Example API Calls

```bash
# Get controller status
curl http://localhost:5000/api/controller/status

# Get latest text
curl http://localhost:5001/api/text/stream

# Make actor speak
curl -X POST http://localhost:5002/api/actor/say \
  -H "Content-Type: application/json" \
  -d '{"text":"Hello, world!"}'

# Get actor status
curl http://localhost:5002/api/actor/status
```

## 🔌 Adding New MCP Sources

To add a new input MCP:

1. Update `appsettings.json`:
```json
{
  "McpConfiguration": {
    "AdditionalInputMcps": [
      {
        "Name": "custom-source",
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

2. The controller automatically discovers and connects to the new service.

## 🏗️ Project Structure

```
PCTama/
├── .github/workflows/          # GitHub Actions CI/CD
├── src/
│   ├── PCTama.AppHost/         # Aspire orchestrator
│   ├── PCTama.ServiceDefaults/ # Shared configuration
│   ├── PCTama.Controller/      # Main controller service
│   ├── PCTama.TextMCP/         # Text input service
│   └── PCTama.ActorMCP/        # Avalonia UI output service
├── tests/
│   └── PCTama.Tests/           # Unit & integration tests
├── build.sh                    # macOS/Linux build script
├── build.bat                   # Windows build script
├── CMakeLists.txt              # Root CMake config
├── PCTama.sln                  # Visual Studio solution
├── ARCHITECTURE.md             # Detailed architecture
├── QUICKSTART.md               # Quick start guide
├── RUNNING.md                  # Running guide
└── README.md                   # This file
```

## 🤝 Contributing

Contributions welcome! Please:

1. Ensure all tests pass: `dotnet test`
2. Follow .editorconfig guidelines
3. Include tests for new features
4. Update documentation as needed

See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## 📚 Documentation

- [QUICKSTART.md](QUICKSTART.md) - Get started quickly
- [ARCHITECTURE.md](ARCHITECTURE.md) - Deep dive into design
- [RUNNING.md](RUNNING.md) - Running and troubleshooting guide
- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines

## 📝 License

[Your License Here]

## 🙏 Acknowledgments

- .NET Aspire team for the excellent cloud-native framework
- Model Context Protocol (MCP) for standardized AI integration
- OBS Studio and LocalVoice plugin for voice-to-text capabilities
- The open-source .NET community

## 🎉 Status

PCTama is **complete** and ready for development and deployment!

All core features are implemented:
- ✅ Aspire microservices framework
- ✅ MCP SDK integration foundation
- ✅ Streaming text service
- ✅ Actor output service
- ✅ OpenTelemetry observability
- ✅ Health monitoring
- ✅ Comprehensive testing
- ✅ CI/CD pipeline
- ✅ Cross-platform build support

---

**PCTama - Your AI-Powered Desktop Pet** 🎮✨
