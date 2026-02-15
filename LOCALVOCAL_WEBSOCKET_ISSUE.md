# LocalVocal WebSocket Connection Issue - DIAGNOSIS

## Problem
TextMCP is NOT receiving transcription data from OBS LocalVocal, even though a WebSocket connection is established.

## What We Found
The service successfully connects to `ws://localhost:4455`, but receives **OBS WebSocket protocol messages** instead of LocalVocal transcription data:

```json
{
  "op": 0,
  "d": {
    "authentication": {
      "challenge": "...",
      "salt": "..."
    },
    "obsStudioVersion": "32.0.4",
    "obsWebSocketVersion": "5.6.3",
    "rpcVersion": 1
  }
}
```

This is the **OBS WebSocket v5 Hello message**, NOT LocalVocal transcription output.

## Root Cause
Port 4455 is the **main OBS WebSocket server** (obs-websocket plugin), not LocalVocal's output endpoint.

## Possible Solutions

### Option 1: LocalVocal Uses a Different Port
Check in OBS:
1. `Tools → LocalVocal Settings`
2. Look for "WebSocket Port" or "Output Port"
3. LocalVocal might use a different port (e.g., 4456, 8080, etc.)

### Option 2: LocalVocal Sends Through OBS WebSocket
LocalVocal might send transcription as **custom events** through the main OBS WebSocket. This would require:
1. Authenticating with OBS WebSocket first
2. Subscribing to LocalVocal-specific events
3. Parsing event messages instead of direct messages

### Option 3: LocalVocal HTTP/REST Endpoint
Some plugins provide HTTP endpoints instead of WebSocket:
- Check for `http://localhost:PORT/transcription`
- Or a REST API endpoint

## Next Steps

**URGENT: Check LocalVocal settings in OBS**
1. Open OBS
2. Go to `Tools → LocalVocal Settings`
3. Find the "WebSocket Server" or "Output" section
4. **Report back the actual port number and connection details**

**Alternative Check:**
```bash
# List all listening ports to find LocalVocal
lsof -iTCP -sTCP:LISTEN | grep -E "obs|local"
```

## Expected LocalVocal Message Format
When correctly connected, we should see messages like:
```json
{
  "text": "hello world",
  "confidence": 0.95,
  "is_final": true,
  "language": "en-US"
}
```

**NOT** OBS WebSocket protocol messages with `"op"` fields.

## Current Log Evidence
From `/tmp/textmcp_debug.log`:
```
dbug: 📬 Complete message received (219 bytes)
dbug: 📥 Raw message from OBS LocalVocal: {"d":{"authentication":...},"op":0}
```

This confirms we're connected to the **wrong WebSocket endpoint**.
