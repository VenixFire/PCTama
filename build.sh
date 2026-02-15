#!/bin/bash

# PCTama Build Script
# This script provides a simple interface to build and run PCTama

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

DOTNET_CMD=""

function resolve_dotnet() {
    if [ -n "$DOTNET_CMD" ]; then
        return 0
    fi

    if [ -n "$DOTNET_ROOT" ] && [ -x "$DOTNET_ROOT/dotnet" ]; then
        DOTNET_CMD="$DOTNET_ROOT/dotnet"
        return 0
    fi

    if command -v dotnet >/dev/null 2>&1; then
        DOTNET_CMD="$(command -v dotnet)"
        return 0
    fi

    local candidates=(
        "/opt/homebrew/opt/dotnet@8/bin/dotnet"
        "/opt/homebrew/opt/dotnet/bin/dotnet"
        "/usr/local/share/dotnet/dotnet"
        "/usr/local/bin/dotnet"
        "/usr/share/dotnet/dotnet"
        "$HOME/.dotnet/dotnet"
    )

    for candidate in "${candidates[@]}"; do
        if [ -x "$candidate" ]; then
            DOTNET_CMD="$candidate"
            break
        fi
    done

    if [ -z "$DOTNET_CMD" ]; then
        echo "❌ dotnet not found. Please install .NET SDK 8+ or set DOTNET_ROOT." >&2
        exit 1
    fi
}

# Ensure PATH includes dotnet directory when needed
if [[ "$OSTYPE" == "darwin"* ]]; then
    resolve_dotnet
    export PATH="$(dirname "$DOTNET_CMD"):$PATH"
fi

function show_help() {
    echo "PCTama Build Script"
    echo ""
    echo "Usage: ./build.sh [command]"
    echo ""
    echo "Commands:"
    echo "  restore     - Restore NuGet packages"
    echo "  build       - Build the solution (Debug)"
    echo "  release     - Build the solution (Release)"
    echo "  test        - Run tests"
    echo "  clean       - Clean build artifacts"
    echo "  run         - Run the Aspire AppHost"
    echo "  shutdown    - Stop all services and free ports"
    echo "  cmake       - Build using CMake"
    echo "  help        - Show this help message"
    echo ""
}

function restore() {
    echo "Restoring packages..."
    resolve_dotnet
    dotnet restore PCTama.sln
}

function build() {
    echo "Building solution (Debug)..."
    resolve_dotnet
    dotnet build PCTama.sln --configuration Debug
}

function build_release() {
    echo "Building solution (Release)..."
    resolve_dotnet
    dotnet build PCTama.sln --configuration Release
}

function run_tests() {
    echo "Running tests..."
    resolve_dotnet
    dotnet test tests/PCTama.Tests/PCTama.Tests.csproj --verbosity normal
}

function clean() {
    echo "Cleaning build artifacts..."
    resolve_dotnet
    dotnet clean PCTama.sln
    rm -rf bin obj build
    find . -type d -name "bin" -o -name "obj" | xargs rm -rf
}

function run_app() {
    echo ""
    echo "╔════════════════════════════════════════════════════════════╗"
    echo "║          PCTama Aspire Framework Starting                  ║"
    echo "╠════════════════════════════════════════════════════════════╣"
    echo "║  📊 Dashboard: http://localhost:15000                      ║"
    echo "║  🔐 LOGIN TOKEN: Look for this line in output below:       ║"
    echo "║                                                            ║"
    echo "║  'Login to the dashboard at http://localhost:15000/...'   ║"
    echo "║                                                            ║"
    echo "║  📱 Services:                                              ║"
    echo "║     Controller: http://localhost:5000                      ║"
    echo "║     Text MCP:   http://localhost:5001                      ║"
    echo "║     Actor MCP:  http://localhost:5002                      ║"
    echo "╚════════════════════════════════════════════════════════════╝"
    echo ""
    
    resolve_dotnet
    cd src/PCTama.AppHost
    export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
    export ASPNETCORE_URLS="http://localhost:15000"
    export DOTNET_DASHBOARD_OTLP_ENDPOINT_URL="http://localhost:18889"

    "$DOTNET_CMD" run --no-build
}

function build_cmake() {
    echo "Building with CMake..."
    cmake -B build
    cmake --build build
}

function shutdown_app() {
    echo ""
    echo "╔════════════════════════════════════════════════════════════╗"
    echo "║              PCTama Shutdown Sequence                      ║"
    echo "╚════════════════════════════════════════════════════════════╝"
    echo ""
    echo "🛑 Shutting down all services..."
    
    # Kill dotnet processes
    pkill -9 dotnet 2>/dev/null || true
    
    # Kill DCP processes
    pkill -9 dcp 2>/dev/null || true
    
    # Wait for processes to terminate
    sleep 2
    
    # Verify no processes are running
    if pgrep -f "dotnet|dcp" > /dev/null 2>&1; then
        echo "⚠️  Warning: Some processes still running, forcing termination..."
        pgrep -f "dotnet|dcp" | xargs -r kill -9 2>/dev/null || true
        sleep 1
    fi
    
    echo "✅ All dotnet/dcp services terminated"
    echo ""
    echo "🔌 Verifying ports are free..."
    echo ""
    
    # Check each port
    local all_free=true
    
    for port in 5000 5001 5002 15000; do
        if lsof -i :$port 2>/dev/null | grep -q LISTEN; then
            # Get the process name (skip header line)
            local proc=$(lsof -i :$port 2>/dev/null | tail -1 | awk '{print $1}')
            
            # If it's ControlCenter or system service, just note it
            if [[ "$proc" == "ControlCe" ]] || [[ "$proc" == "kernel" ]]; then
                echo "   Port $port: ⚠️  System service (will clear when needed)"
            else
                echo "   Port $port: ❌ Process '$proc' (run 'kill -9 $(lsof -ti :$port)' to force)"
                all_free=false
            fi
        else
            echo "   Port $port: ✅ Free"
        fi
    done
    
    echo ""
    if [ "$all_free" = true ]; then
        echo "✅ All ports are available for PCTama services"
        echo ""
        echo "💡 Tip: Run './build.sh run' to start PCTama again"
    else
        echo "⚠️  Note: macOS may hold port 5000 via ControlCenter."
        echo "    This doesn't prevent PCTama from running."
        echo ""
        echo "💡 Tip: Run './build.sh run' to start PCTama again"
    fi
    echo ""
}

case "${1:-help}" in
    restore)
        restore
        ;;
    build)
        restore
        build
        ;;
    release)
        restore
        build_release
        ;;
    test)
        run_tests
        ;;
    clean)
        clean
        ;;
    run)
        run_app
        ;;
    shutdown)
        shutdown_app
        ;;
    cmake)
        build_cmake
        ;;
    help|*)
        show_help
        ;;
esac
