#!/bin/bash

# Test last response feature with ASKBot

API_URL="http://localhost:5012/api/askbot"

echo "=== Testing Last Response Feature ==="
echo ""

# Create a new session
echo "Creating new session..."
NEW_SESSION=$(curl -s -X POST "$API_URL/session/new" | python3 -c "import sys, json; print(json.load(sys.stdin)['sessionId'])")
echo "New session ID: $NEW_SESSION"
echo ""

# Function to send message and wait
send_and_wait() {
    local message="$1"
    local session="$2"

    echo "User: $message"

    response=$(curl -s -X POST "$API_URL/message" \
        -H "Content-Type: application/json" \
        -d "{\"message\": \"$message\", \"sessionId\": \"$session\"}")

    echo "Status: $(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin)['status'])")"

    # Wait for processing
    sleep 4
    echo "---"
}

# Test scenario: Provide detailed information and then reference it
echo "=== Test 1: Detailed Response Storage ==="
send_and_wait "Tell me about the key principles of Domain-Driven Design (DDD)" "$NEW_SESSION"
send_and_wait "What are the main building blocks mentioned?" "$NEW_SESSION"
echo ""

echo "=== Test 2: Technical Information Retention ==="
send_and_wait "Explain the concept of backpressure in Reactive Streams" "$NEW_SESSION"
send_and_wait "How does that relate to flow control?" "$NEW_SESSION"
echo ""

echo "=== Test 3: Context Switch with Important Response ==="
send_and_wait "What are the SOLID principles in software engineering?" "$NEW_SESSION"
send_and_wait "Now let's talk about something else. What's the weather like?" "$NEW_SESSION"
send_and_wait "Going back to the principles, which one deals with dependency?" "$NEW_SESSION"
echo ""

# Check session info
echo "=== Session Information ==="
curl -s -X GET "$API_URL/session/$NEW_SESSION" | python3 -m json.tool

echo ""
echo "Test completed for session: $NEW_SESSION"