# Running PCTama - Development Guide

## 🚀 Quick Start

To start PCTama with all services:

```bash
./build.sh run
```

You will see output like:

```
╔════════════════════════════════════════════════════════════╗
║          PCTama Aspire Framework Starting                  ║
╠════════════════════════════════════════════════════════════╣
║  📊 Dashboard: http://localhost:15000                      ║
║  🔐 LOGIN TOKEN: Look for this line in output below:       ║
║                                                            ║
║  'Login to the dashboard at http://localhost:15000/...'   ║
...
info: Aspire.Hosting.DistributedApplication[0]
      Login to the dashboard at http://localhost:15000/login?t=YOUR_TOKEN_HERE
```

## 🔑 Dashboard Access

Once running, you'll see the login token in the console output:

```
Login to the dashboard at http://localhost:15000/login?t=1f2789a1695912eb51b9d1cfcedc928f
```

**Copy this complete URL and paste it into your browser** - it includes the authentication token needed to access the dashboard.

## ✅ Currently Running Services

Your PCTama application runs with:
- **Aspire Dashboard**: http://localhost:15000 (requires token to login)
- **Controller API**: http://localhost:5000
- **Text MCP Service**: http://localhost:5001
- **Actor MCP Service**: http://localhost:5002

### Quick Commands

**Check service status:**
```bash
# Dashboard
curl http://localhost:15000

# Controller health
curl http://localhost:5000/api/controller/health

# Text MCP status
curl http://localhost:5001/api/text/status

# Actor MCP status
curl http://localhost:5002/api/actor/status
```

### Stopping the Application

Press `Ctrl+C` in the terminal where the application is running.

### Running Again

```bash
# Using the build script
./build.sh run

# Or manually
cd src/PCTama.AppHost
export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
export ASPNETCORE_URLS="http://localhost:15000"
export DOTNET_DASHBOARD_OTLP_ENDPOINT_URL="http://localhost:18889"
dotnet run
```

### Environment Variables Explained

- `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` - Allows HTTP for local development
- `ASPNETCORE_URLS` - URL where the Aspire dashboard listens
- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` - OpenTelemetry endpoint for metrics

### Testing the APIs

**Controller API:**
```bash
# Get status
curl http://localhost:5000/api/controller/status

# Get health
curl http://localhost:5000/api/controller/health
```

**Text MCP:**
```bash
# Get latest text
curl http://localhost:5001/api/text/stream

# Get buffer status
curl http://localhost:5001/api/text/status
```

**Actor MCP:**
```bash
# Send an action
curl -X POST http://localhost:5002/api/actor/say \
  -H "Content-Type: application/json" \
  -d '"Hello, world!"'

# Get status
curl http://localhost:5002/api/actor/status
```

### Understanding the Warnings

**OpenTelemetry Package Warnings:**
These are advisory warnings about package vulnerabilities. They don't prevent the application from running. You can update to newer versions later:
- OpenTelemetry.Instrumentation.AspNetCore
- OpenTelemetry.Instrumentation.Http  

**Data Protection Warning:**
This is normal for development. In production, configure proper data protection.

### Next Steps

1. **Configure OBS LocalVoice** (optional):
   - Install OBS Studio
   - Install LocalVoice plugin
   - Configure WebSocket at ws://localhost:4455
   - Update `src/PCTama.TextMCP/appsettings.json`

2. **Set up Local LLM** (optional):
   ```bash
   # Install Ollama
   brew install ollama
   
   # Start Ollama
   ollama serve
   
   # Pull a model
   ollama pull llama2
   ```

3. **Run Tests**:
   ```bash
   ./build.sh test
   ```

4. **View Logs**:
   - Check the terminal for application logs
   - Visit the Aspire Dashboard for visual monitoring

### Troubleshooting

**Port already in use:**
```bash
# Check what's using the port
lsof -i :5000
lsof -i :15000

# Kill the process if needed
kill -9 <PID>
```

**Services not starting:**
- Check logs in the terminal
- Ensure .NET SDK is in PATH
- Verify all dependencies are installed

**Dashboard not loading:**
- Wait 10-15 seconds for all services to start
- Check that all environment variables are set
- Try accessing individual service endpoints directly

---

## 🎉 Success!

PCTama is running! You now have a complete Aspire-based microservices architecture with:
- ✅ MCP SDK integration framework
- ✅ Streaming text input service
- ✅ Actor output service
- ✅ OpenTelemetry observability
- ✅ Service discovery
- ✅ Health monitoring

Happy coding!
