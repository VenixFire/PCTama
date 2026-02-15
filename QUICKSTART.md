# PCTama Quick Start Guide

This guide will help you get PCTama up and running quickly.

## ⚡ Quick Start

### 1. Clone and Navigate

```bash
git clone https://github.com/yourusername/PCTama.git
cd PCTama
```

### 2. Build the Project

**Option A: Using the build scripts (easiest)**

```bash
# Linux/macOS
./build.sh build

# Windows
build.bat build
```

**Option B: Using .NET CLI**

```bash
dotnet restore
dotnet build
```

**Option C: Using CMake**

```bash
cmake -B build
cmake --build build
```

### 3. Run the Application

```bash
# Linux/macOS
./build.sh run

# Windows
build.bat run

# Or directly
cd src/PCTama.AppHost
dotnet run
```

## 🎯 First Time Setup

### Configure Local LLM (Optional)

PCTama is configured to connect to a local LLM (e.g., Ollama) running on port 11434.

1. **Install Ollama** (or your preferred local LLM):
   ```bash
   # macOS/Linux
   curl https://ollama.ai/install.sh | sh
   
   # Pull a model
   ollama pull llama2
   ```

2. **Update configuration** in `src/PCTama.Controller/appsettings.json`:
   ```json
   {
     "McpConfiguration": {
       "LocalLlmEndpoint": "http://localhost:11434",
       "ModelName": "llama2"
     }
   }
   ```

### Configure OBS LocalVoice (Optional)

If you want to use OBS LocalVoice for text input:

1. Install OBS Studio
2. Install the LocalVoice plugin
3. Configure the WebSocket endpoint (default: ws://localhost:4455)
4. Update `src/PCTama.TextMCP/appsettings.json`

## 🧪 Verify Installation

### Run Tests

```bash
# Linux/macOS
./build.sh test

# Windows
build.bat test

# Or directly
dotnet test
```

### Check Services

Once running, verify the services are accessible:

- Controller: http://localhost:5000/api/controller/health
- Text MCP: http://localhost:5001/api/text/status
- Actor MCP: http://localhost:5002/api/actor/status

## 🔧 Development Mode

### Enable Development Features

1. Set environment variable:
   ```bash
   export ASPNETCORE_ENVIRONMENT=Development
   ```

2. Access Swagger UI:
   - Controller: http://localhost:5000/swagger
   - Text MCP: http://localhost:5001/swagger
   - Actor MCP: http://localhost:5002/swagger

### Enable Hot Reload

```bash
dotnet watch --project src/PCTama.Controller/PCTama.Controller.csproj
```

## 📊 Monitoring and Observability

PCTama uses .NET Aspire for built-in observability:

1. Start the application
2. Open the Aspire dashboard (URL shown in console output)
3. View:
   - Service health
   - Metrics
   - Distributed traces
   - Logs

## 🐛 Troubleshooting

### Build Errors

**Problem**: "SDK not found" error

**Solution**:
```bash
dotnet --version  # Should show 8.0.x
```
Install .NET 8.0 SDK if not present.

**Problem**: CMake not found

**Solution**:
```bash
# macOS
brew install cmake

# Ubuntu/Debian
sudo apt-get install cmake

# Windows
# Download from https://cmake.org/download/
```

### Runtime Errors

**Problem**: Port already in use

**Solution**: Change ports in `src/PCTama.AppHost/Program.cs`:
```csharp
.WithHttpEndpoint(port: 5000, name: "http")  // Change to available port
```

**Problem**: WinUI3 not working (Windows only)

**Solution**: Ensure you have:
- Windows 10 Build 19041 or later
- Windows App SDK installed
- Run on Windows (WinUI3 is Windows-only)

### Test Failures

**Problem**: Tests fail with connection errors

**Solution**: Ensure no other services are using the test ports (5000-5002)

## 📚 Next Steps

- Read the [full README](README.md) for detailed documentation
- Check [CONTRIBUTING.md](CONTRIBUTING.md) to contribute
- Explore the API documentation at `/swagger` endpoints
- Configure additional MCP input sources

## 💬 Getting Help

- Open an issue on GitHub
- Check existing issues for solutions
- Review the documentation

---

Happy coding! 🎉
