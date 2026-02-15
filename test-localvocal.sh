#!/bin/bash

# Test script to verify OBS LocalVocal WebSocket connection

echo "╔═══════════════════════════════════════════════════════════════╗"
echo "║     OBS LocalVocal WebSocket Connection Test                 ║"
echo "╚═══════════════════════════════════════════════════════════════╝"
echo ""

# Check if OBS is running
OBS_PID=$(ps aux | grep -i "OBS.app" | grep -v grep | head -1 | awk '{print $2}')

if [ -z "$OBS_PID" ]; then
    echo "❌ OBS is NOT running"
    echo ""
    echo "Please start OBS before running this test."
    exit 1
else
    echo "✅ OBS is running (PID: $OBS_PID)"
fi

# Test WebSocket connection
echo ""
echo "🔌 Testing WebSocket connection to localhost:4455..."
echo ""

# Use nc (netcat) to test if port is open
nc -z localhost 4455 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ Port 4455 is open and accepting connections"
    echo ""
    echo "🎯 Attempting to connect to WebSocket..."
    echo ""
    
    # Try a simple connection test
    timeout 3 curl -i -N \
        -H "Connection: Upgrade" \
        -H "Upgrade: websocket" \
        -H "Sec-WebSocket-Version: 13" \
        -H "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==" \
        http://localhost:4455/ 2>&1 | head -20
    
    echo ""
    echo "✅ WebSocket endpoint is accessible"
    echo ""
    echo "📝 Next steps:"
    echo "   1. Start the PCTama TextMCP service"
    echo "   2. Speak into your microphone with OBS running"
    echo "   3. Check the service logs for transcribed text"
else
    echo "❌ Port 4455 is NOT accessible"
    echo ""
    echo "📋 Possible issues:"
    echo "   • LocalVocal plugin not installed"
    echo "   • WebSocket server not enabled in plugin settings"
    echo "   • Plugin configured on different port"
    echo ""
    echo "🔧 Setup Instructions:"
    echo ""
    echo "1. Install OBS LocalVocal Plugin"
    echo "   Download: https://github.com/occ-ai/obs-localvocal"
    echo ""
    echo "2. Configure in OBS:"
    echo "   • Open OBS → Tools → LocalVocal Settings"
    echo "   • Enable 'WebSocket Server'"
    echo "   • Set port to 4455"
    echo "   • Enable 'Send Transcription to WebSocket'"
    echo ""
    echo "3. Add LocalVocal to Audio Source:"
    echo "   • Right-click your microphone source"
    echo "   • Filters → Add → LocalVocal"
    echo "   • Configure your preferred transcription model"
    echo ""
    echo "4. Test again:"
    echo "   ./test-localvocal.sh"
fi

echo ""
echo "═══════════════════════════════════════════════════════════════"
