#!/bin/bash

# Test conversation context with ASKBot

SESSION_ID=""
API_URL="http://localhost:5012/api/askbot"

echo "=== Testing ASKBot Conversation Context ==="
echo ""

# Function to send message and get session
send_message() {
    local message="$1"
    local session="$2"

    echo "User: $message"

    if [ -z "$session" ]; then
        # First message, no session yet
        response=$(curl -s -X POST "$API_URL/message" \
            -H "Content-Type: application/json" \
            -d "{\"message\": \"$message\"}")
    else
        # Subsequent messages with session
        response=$(curl -s -X POST "$API_URL/message" \
            -H "Content-Type: application/json" \
            -d "{\"message\": \"$message\", \"sessionId\": \"$session\"}")
    fi

    # Extract session ID if not set
    if [ -z "$SESSION_ID" ]; then
        SESSION_ID=$(echo "$response" | jq -r '.sessionId')
        echo "Session created: $SESSION_ID"
    fi

    echo "Response status: $(echo "$response" | jq -r '.status')"
    echo ""

    # Wait for processing
    sleep 3
}

# Test 1: Greeting and introduction
echo "=== Test 1: Greeting and Context ==="
send_message "안녕, 너는 누구야?" ""
send_message "너는 무엇을 할 수 있니?" "$SESSION_ID"
send_message "아 그렇구나, 고마워" "$SESSION_ID"

echo ""
echo "=== Test 2: Topic with Context ==="
send_message "궁금한게 있어" "$SESSION_ID"
send_message "AI 개발방법론이 궁금해" "$SESSION_ID"

echo ""
echo "=== Test 3: Multiple Topics ==="
send_message "Reactive Streams에 대해 알려줘" "$SESSION_ID"
send_message "그것의 주요 특징은 뭐야?" "$SESSION_ID"

echo ""
echo "Session ID used: $SESSION_ID"
echo "Test completed!"