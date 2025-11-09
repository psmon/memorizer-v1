"""
Memorizer MCP Proxy Server
This server acts as a proxy to the Memorizer MCP service
SSE Streamable HTTP Protocol:
- GET /sse: Opens SSE stream for server->client messages
- POST /message: Sends client->server messages
"""

import asyncio
import os
import uuid
from dotenv import load_dotenv
from flask import Flask, request, Response, stream_with_context
import requests
import json
import threading
import queue

# Load environment variables
load_dotenv()

app = Flask(__name__)

# Configuration
MCP_SSE_URL = "https://mcp.webnori.com/sse"
MCP_MESSAGE_URL = "https://mcp.webnori.com/message"
MCP_API_KEY = os.getenv("mcp_api_key", "")

# Store active SSE connections
sse_connections = {}

@app.route('/sse', methods=['GET', 'OPTIONS'])
def proxy_sse():
    """Proxy SSE stream (GET only) - Opens connection to receive server messages"""

    # Handle CORS preflight
    if request.method == 'OPTIONS':
        response = Response()
        response.headers['Access-Control-Allow-Origin'] = '*'
        response.headers['Access-Control-Allow-Methods'] = 'GET, OPTIONS'
        response.headers['Access-Control-Allow-Headers'] = 'Content-Type, X-API-Key'
        return response

    # Prepare headers for SSE connection
    headers = {
        'X-API-Key': MCP_API_KEY,
        'Accept': 'text/event-stream'
    }

    print(f"[SSE] Opening SSE stream to {MCP_SSE_URL}")
    print(f"[SSE] API Key: {MCP_API_KEY}")

    # Forward GET request to open SSE stream
    try:
        resp = requests.get(MCP_SSE_URL, headers=headers, stream=True, timeout=None)

        print(f"[SSE] Response status: {resp.status_code}")
        print(f"[SSE] Response headers: {dict(resp.headers)}")

        def generate():
            try:
                # Stream SSE events line by line
                for line in resp.iter_lines(decode_unicode=True):
                    if line:
                        print(f"[SSE] << {line[:150]}")
                        yield f"{line}\n"
                    else:
                        # Keep empty lines for SSE format
                        yield "\n"
            except Exception as e:
                print(f"[SSE] Error in streaming: {e}")
                yield f"event: error\ndata: {json.dumps({'error': str(e)})}\n\n"

        response = Response(
            stream_with_context(generate()),
            content_type='text/event-stream',
            headers={
                'Cache-Control': 'no-cache',
                'X-Accel-Buffering': 'no',
                'Connection': 'keep-alive',
                'Access-Control-Allow-Origin': '*'
            }
        )
        return response

    except Exception as e:
        print(f"[SSE] Exception: {e}")
        return {'error': str(e)}, 500


@app.route('/message', methods=['POST', 'OPTIONS'])
def proxy_message():
    """Proxy client messages (POST) - Sends client->server messages"""

    # Handle CORS preflight
    if request.method == 'OPTIONS':
        response = Response()
        response.headers['Access-Control-Allow-Origin'] = '*'
        response.headers['Access-Control-Allow-Methods'] = 'POST, OPTIONS'
        response.headers['Access-Control-Allow-Headers'] = 'Content-Type, X-API-Key'
        return response

    # Prepare headers
    headers = {
        'X-API-Key': MCP_API_KEY,
        'Content-Type': 'application/json'
    }

    data = request.get_data()

    # Get query parameters (especially sessionId)
    query_params = request.args.to_dict()
    query_string = '&'.join([f"{k}={v}" for k, v in query_params.items()])

    # Build full URL with query parameters
    url = MCP_MESSAGE_URL
    if query_string:
        url = f"{MCP_MESSAGE_URL}?{query_string}"

    print(f"[MESSAGE] >> POST to {url}")
    print(f"[MESSAGE] >> Data: {data[:200]}")

    # Forward POST request to message endpoint with query params
    try:
        resp = requests.post(
            url,
            headers=headers,
            data=data,
            timeout=30
        )

        print(f"[MESSAGE] << Status: {resp.status_code}")
        print(f"[MESSAGE] << Response: {resp.text[:200]}")

        return Response(
            resp.content,
            status=resp.status_code,
            headers={
                'Content-Type': resp.headers.get('Content-Type', 'application/json'),
                'Access-Control-Allow-Origin': '*'
            }
        )

    except Exception as e:
        print(f"[MESSAGE] Exception: {e}")
        return {'error': str(e)}, 500

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return {'status': 'healthy', 'service': 'memorizer-proxy'}, 200

@app.route('/', methods=['GET'])
def root():
    """Root endpoint with service information"""
    return {
        'service': 'Memorizer MCP Proxy Server',
        'version': '1.0.0',
        'endpoints': {
            '/sse': 'MCP SSE endpoint',
            '/health': 'Health check endpoint'
        }
    }, 200

def main():
    """Main entry point"""
    port = int(os.getenv('PORT', 5000))
    print(f"Starting Memorizer MCP Proxy Server on port {port}")
    print(f"MCP API Key: {MCP_API_KEY[:10]}...")
    print(f"MCP SSE URL: {MCP_SSE_URL}")
    print(f"MCP Message URL: {MCP_MESSAGE_URL}")
    print("")
    print("Endpoints:")
    print(f"  GET  /sse     - SSE stream (server->client)")
    print(f"  POST /message - Send message (client->server)")
    print(f"  GET  /health  - Health check")
    app.run(host='0.0.0.0', port=port, debug=False, threaded=True)

if __name__ == '__main__':
    main()
