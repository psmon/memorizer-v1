#!/bin/bash

# Memorizer MCP Proxy Server Startup Script for WSL
# This script starts the Python MCP proxy server and exposes it via ngrok

set -e

# Default values
PORT=5000
SKIP_NGROK=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--port)
            PORT="$2"
            shift 2
            ;;
        --skip-ngrok)
            SKIP_NGROK=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./start-proxy.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -p, --port PORT      Specify port (default: 5000)"
            echo "  --skip-ngrok         Skip ngrok tunnel (local only)"
            echo "  -h, --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo "================================"
echo "Memorizer MCP Proxy Server"
echo "================================"
echo ""

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo "Error: .env file not found!"
    echo "Please create .env file with the following content:"
    echo "ngrok_key=YOUR_NGROK_AUTH_TOKEN"
    echo "mcp_api_key=YOUR_MCP_API_KEY"
    exit 1
fi

# Load environment variables from .env file
echo "Loading environment variables from .env..."
export $(grep -v '^#' .env | xargs)

# Validate environment variables
if [ -z "$mcp_api_key" ]; then
    echo "Error: mcp_api_key not found in .env file!"
    exit 1
fi

if [ "$SKIP_NGROK" = false ] && [ -z "$ngrok_key" ]; then
    echo "Error: ngrok_key not found in .env file!"
    exit 1
fi

# Check if Python is installed
echo "Checking Python installation..."
if ! command -v python3 &> /dev/null; then
    echo "Error: Python3 is not installed or not in PATH!"
    exit 1
fi

PYTHON_VERSION=$(python3 --version)
echo "Found: $PYTHON_VERSION"

# Check if required Python packages are installed
echo "Checking Python dependencies..."

declare -A packages=(
    ["flask"]="flask"
    ["requests"]="requests"
    ["python-dotenv"]="dotenv"
)

MISSING_PACKAGES=()

for pkg_name in "${!packages[@]}"; do
    import_name="${packages[$pkg_name]}"
    if python3 -c "import $import_name" 2>/dev/null; then
        echo "  ✓ Found: $pkg_name"
    else
        echo "  ✗ Missing: $pkg_name"
        MISSING_PACKAGES+=("$pkg_name")
    fi
done

if [ ${#MISSING_PACKAGES[@]} -gt 0 ]; then
    echo ""
    echo "Installing missing Python packages: ${MISSING_PACKAGES[*]}..."
    python3 -m pip install --break-system-packages "${MISSING_PACKAGES[@]}" 2>/dev/null
    if [ $? -ne 0 ]; then
        # Try without --break-system-packages flag
        python3 -m pip install "${MISSING_PACKAGES[@]}" 2>/dev/null
        if [ $? -ne 0 ]; then
            echo "Error: Failed to install Python packages!"
            echo ""
            echo "Please install manually using one of these methods:"
            echo "  1. pip3 install --break-system-packages ${MISSING_PACKAGES[*]}"
            echo "  2. Create virtual environment:"
            echo "     python3 -m venv venv"
            echo "     source venv/bin/activate"
            echo "     pip install ${MISSING_PACKAGES[*]}"
            exit 1
        fi
    fi
    echo "Packages installed successfully!"
else
    echo "All required packages are installed!"
fi

# Check if ngrok is installed (if not skipping)
if [ "$SKIP_NGROK" = false ]; then
    echo "Checking ngrok installation..."
    if ! command -v ngrok &> /dev/null; then
        echo "Error: ngrok is not installed or not in PATH!"
        echo ""
        echo "To install ngrok on WSL:"
        echo "  curl -s https://ngrok-agent.s3.amazonaws.com/ngrok.asc | sudo tee /etc/apt/trusted.gpg.d/ngrok.asc >/dev/null"
        echo "  echo 'deb https://ngrok-agent.s3.amazonaws.com buster main' | sudo tee /etc/apt/sources.list.d/ngrok.list"
        echo "  sudo apt update && sudo apt install ngrok"
        echo ""
        echo "Or run with --skip-ngrok flag for local only"
        exit 1
    fi

    NGROK_VERSION=$(ngrok version)
    echo "Found: $NGROK_VERSION"

    # Configure ngrok with auth token
    echo "Configuring ngrok..."
    ngrok config add-authtoken "$ngrok_key"
fi

# Set PORT environment variable
export PORT=$PORT

# Kill any existing processes on the port
echo "Checking for existing processes on port $PORT..."
if lsof -Pi :$PORT -sTCP:LISTEN -t >/dev/null 2>&1; then
    PID=$(lsof -Pi :$PORT -sTCP:LISTEN -t)
    echo "Killing existing process on port $PORT (PID: $PID)..."
    kill -9 $PID
    sleep 2
fi

# Start the Python server in background
echo ""
echo "Starting MCP Proxy Server on port $PORT..."
python3 mcp_server.py &
SERVER_PID=$!

# Wait for server to start
echo "Waiting for server to start..."
sleep 3

# Check if server is running
if ! kill -0 $SERVER_PID 2>/dev/null; then
    echo "Error: Server failed to start!"
    exit 1
fi

# Test server health
if curl -s http://localhost:$PORT/health >/dev/null 2>&1; then
    echo "Server started successfully!"
else
    echo "Error: Server health check failed!"
    kill -9 $SERVER_PID
    exit 1
fi

# Cleanup function
cleanup() {
    echo ""
    echo "Shutting down..."
    if [ ! -z "$SERVER_PID" ]; then
        kill -9 $SERVER_PID 2>/dev/null || true
    fi
    if [ ! -z "$NGROK_PID" ]; then
        kill -9 $NGROK_PID 2>/dev/null || true
    fi
    exit 0
}

trap cleanup SIGINT SIGTERM

# Start ngrok if not skipped
if [ "$SKIP_NGROK" = false ]; then
    echo ""
    echo "Starting ngrok tunnel..."
    ngrok http $PORT > /dev/null &
    NGROK_PID=$!

    # Wait for ngrok to start
    sleep 3

    # Get ngrok public URL
    if command -v curl &> /dev/null; then
        PUBLIC_URL=$(curl -s http://localhost:4040/api/tunnels | python3 -c "import sys, json; print(json.load(sys.stdin)['tunnels'][0]['public_url'])" 2>/dev/null || echo "")

        echo ""
        echo "================================"
        echo "Server is running!"
        echo "================================"
        echo "Local URL:  http://localhost:$PORT"
        if [ ! -z "$PUBLIC_URL" ]; then
            echo "Public URL: $PUBLIC_URL"
            echo ""
            echo "SSE Endpoint: $PUBLIC_URL/sse"
            echo "Health Check: $PUBLIC_URL/health"
        else
            echo ""
            echo "Warning: Could not get ngrok public URL"
            echo "Check http://localhost:4040 for the tunnel URL"
        fi
        echo ""
        echo "Press Ctrl+C to stop the server and ngrok tunnel"
        echo "================================"
    fi
else
    echo ""
    echo "================================"
    echo "Server is running!"
    echo "================================"
    echo "Local URL: http://localhost:$PORT"
    echo ""
    echo "SSE Endpoint: http://localhost:$PORT/sse"
    echo "Health Check: http://localhost:$PORT/health"
    echo ""
    echo "Press Ctrl+C to stop the server"
    echo "================================"
fi

# Keep script running
while true; do
    sleep 1
    # Check if server is still running
    if ! kill -0 $SERVER_PID 2>/dev/null; then
        echo "Server process died unexpectedly!"
        cleanup
    fi
done
