# OBS LocalVocal WebSocket Verification Guide

## Current Status

✅ **OBS is running** (PID: 56145)  
❌ **WebSocket connection to localhost:4455 is NOT available**

## Diagnosis

The LocalVocal plugin is either:
- Not installed in OBS
- Installed but WebSocket server is not enabled
- Configured on a different port

## Setup Instructions

### 1. Install OBS LocalVocal Plugin

**Option A: Via GitHub**
```bash
# Download from: https://github.com/occ-ai/obs-localvocal
# Install the appropriate version for macOS
```

**Option B: Via OBS Plugin Manager** (if available)
- Open OBS
- Go to Tools → Browse Plugins
- Search for "LocalVocal"
- Install and restart OBS

### 2. Configure LocalVocal Plugin

1. **Open LocalVocal Settings**
   - In OBS, go to: `Tools → LocalVocal Settings`

2. **Enable WebSocket Server**
   - Check: ✅ Enable WebSocket Server
   - Port: `4455` (must match `appsettings.json`)
   - Check: ✅ Send Transcription to WebSocket
   - Format: JSON

3. **Configure Transcription**
   - Model: Choose your preferred speech recognition model
   - Language: Select your language (e.g., "en-US")
   - Confidence Threshold: 0.7 (adjust as needed)

### 3. Apply LocalVocal to Audio Source

1. **Right-click your microphone source** in OBS
2. Select: **Filters**
3. Click: **+ (Add Filter)**
4. Choose: **LocalVocal**
5. Configure the filter:
   - Enable real-time transcription
   - Set confidence threshold
   - Enable WebSocket output

### 4. Verify Connection

Run the test script:
```bash
./test-localvocal.sh
```

Expected output when working:
```
✅ OBS is running (PID: xxxxx)
✅ Port 4455 is open and accepting connections
✅ WebSocket endpoint is accessible
```

### 5. Test with PCTama

Once the WebSocket is available:

```bash
# Build and run the services
./build.sh build
./build.sh run
```

Watch the TextMCP service logs for:
```
info: PCTama.TextMCP.Services.TextStreamService[0]
      ✅ Connected to OBS LocalVocal WebSocket

info: PCTama.TextMCP.Services.TextStreamService[0]
      📝 Received from OBS LocalVocal: "Your transcribed text here" (confidence: 95%)
```

## Expected Message Format

LocalVocal sends JSON messages like:
```json
{
  "text": "Transcribed speech content",
  "confidence": 0.95,
  "is_final": true,
  "language": "en-US",
  "timestamp": "2026-02-14T20:30:00Z"
}
```

## Troubleshooting

### Port Already in Use
If port 4455 is in use by another application:
1. Change the port in LocalVocal settings
2. Update `src/PCTama.TextMCP/appsettings.json`:
   ```json
   "OBSLocalVoiceEndpoint": "ws://localhost:YOUR_NEW_PORT"
   ```

### No Transcription Appearing
- Verify your microphone is working in OBS
- Check audio levels are above threshold
- Ensure LocalVocal filter is enabled
- Check OBS logs for any errors

### Connection Drops
- The PCTama service auto-reconnects every 10 seconds
- Check OBS logs for WebSocket errors
- Verify firewall isn't blocking localhost connections

## Manual WebSocket Test

Test the WebSocket endpoint manually:
```bash
# Using websocat (install: brew install websocat)
websocat ws://localhost:4455

# Or using curl
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Sec-WebSocket-Version: 13" \
  -H "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==" \
  http://localhost:4455/
```

## Next Steps

1. ✅ Install LocalVocal plugin in OBS
2. ✅ Enable WebSocket server in plugin settings
3. ✅ Configure port 4455
4. ✅ Apply filter to microphone source
5. ✅ Run `./test-localvocal.sh` to verify
6. ✅ Start PCTama with `./build.sh run`
7. ✅ Speak into microphone and watch logs

## Related Files

- Test Script: `test-localvocal.sh`
- Configuration: `src/PCTama.TextMCP/appsettings.json`
- Service Implementation: `src/PCTama.TextMCP/Services/TextStreamService.cs`
- Running Guide: `RUNNING.md`
