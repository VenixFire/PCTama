#!/bin/bash
# Test script for TextMCP file monitoring

set -e

cd "$(dirname "$0")"

echo "🧪 Testing TextMCP File Monitoring"
echo "=================================="
echo ""

# Use the absolute path configured in appsettings.json
FILE_PATH="/Users/venix/Programming/DesktopPet/FromLocalVocalText"

# Clean up any existing file
rm -f "$FILE_PATH"
echo "Initial test content" > "$FILE_PATH"

echo "📝 Starting TextMCP service..."
cd src/PCTama.TextMCP
dotnet run --no-build > /tmp/textmcp_test_output.log 2>&1 &
SERVICE_PID=$!
echo "   Service started with PID: $SERVICE_PID"

# Wait for service to start
sleep 3

echo ""
echo "✍️  Writing test data to $FILE_PATH..."
echo "Test message 1" >> "$FILE_PATH"
echo "Test message 2 - Hello World!" >> "$FILE_PATH"
echo '{"text": "JSON formatted message", "confidence": 0.98}' >> "$FILE_PATH"

# Wait for processing
sleep 2

echo ""
echo "📊 Service log output:"
echo "---"
cat /tmp/textmcp_test_output.log | grep -E "(info:|📁|📝|💡|Working|File exists)" | tail -20 || echo "(No matching output yet)"
echo "---"

echo ""
echo "🛑 Stopping service..."
kill $SERVICE_PID 2>/dev/null || true
sleep 1

echo ""
echo "✅ Test complete!"
echo ""
echo "💡 To view full logs: cat /tmp/textmcp_test_output.log"
echo "📄 To view file content: cat $FILE_PATH"
