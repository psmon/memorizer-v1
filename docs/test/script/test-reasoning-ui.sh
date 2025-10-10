#!/bin/bash

# Test reasoning UI display logic

API_URL="http://localhost:5012/api/askbot"

echo "=== Testing Reasoning Display Logic ==="
echo ""

# Create a new session
echo "Creating new session..."
SESSION_ID=$(curl -s -X POST "$API_URL/session/new" | python3 -c "import sys, json; print(json.load(sys.stdin)['sessionId'])")
echo "Session ID: $SESSION_ID"
echo ""

# Function to send message
send_message() {
    local message="$1"
    local session="$2"

    echo ">>> Sending: $message"

    response=$(curl -s -X POST "$API_URL/message" \
        -H "Content-Type: application/json" \
        -d "{\"message\": \"$message\", \"sessionId\": \"$session\"}")

    echo "Response: $(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin)['status'])")"
    echo ""

    # Wait for processing
    sleep 5
}

echo "=== Test 1: Simple greeting (No memory search - reasoning should be hidden) ==="
send_message "안녕하세요!" "$SESSION_ID"

echo "=== Test 2: Question with memory search (reasoning should be shown) ==="
send_message "SOLID 원칙에 대해 설명해주세요" "$SESSION_ID"

echo "=== Test 3: Follow-up without search (reasoning should be hidden) ==="
send_message "감사합니다!" "$SESSION_ID"

echo "=== Test 4: Another memory search query (reasoning should be shown) ==="
send_message "Reactive Streams의 백프레셔에 대해 알려주세요" "$SESSION_ID"

echo ""
echo "Test completed!"
echo ""
echo "Please check the UI at http://localhost:5012/ui/askbot"
echo "Session ID: $SESSION_ID"
echo ""
echo "Expected behavior:"
echo "1. Simple greeting - NO reasoning steps shown"
echo "2. SOLID question - Reasoning steps SHOWN (memory search)"
echo "3. Thank you - NO reasoning steps shown"
echo "4. Reactive Streams - Reasoning steps SHOWN (memory search)"