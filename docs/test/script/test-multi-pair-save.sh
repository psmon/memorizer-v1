#!/bin/bash

# Test SaveConversationAsMemory with multiple conversation pairs
BASE_URL="http://localhost:5012"

echo "=== Testing Multi-Pair SaveConversationAsMemory ==="
echo ""

# Login
echo "Step 1: Login..."
LOGIN_RESP=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' \
  -c /tmp/cookies.txt -w "\n%{http_code}")

HTTP_CODE=$(echo "$LOGIN_RESP" | tail -n1)
echo "Login HTTP code: $HTTP_CODE"
echo ""

# Manually create a share link with conversation data (simulating multi-pair conversation)
# Since we can't easily create real multi-pair via API, we'll test with the bf6WUQ link first
SHORT_CODE="bf6WUQ"

echo "Step 2: Using existing share code: $SHORT_CODE"
SHARE_DATA=$(curl -s "$BASE_URL/api/askbot/share/$SHORT_CODE")
echo "Share data preview:"
echo "$SHARE_DATA" | head -c 500
echo "..."
echo ""

# Save as memory
echo "Step 3: Saving conversation as memory..."
SAVE_RESP=$(curl -s -X POST "$BASE_URL/api/askbot/share/$SHORT_CODE/save-memory" \
  -H "Content-Type: application/json" \
  -b /tmp/cookies.txt \
  -d '{"createRelationships":true}')

echo "Save response:"
echo "$SAVE_RESP"
echo ""

# Check if it contains memoryIds array
if echo "$SAVE_RESP" | grep -q "memoryIds"; then
    echo "✓ Response contains memoryIds field"
    MEMORY_COUNT=$(echo "$SAVE_RESP" | grep -o '"count":[0-9]*' | grep -o '[0-9]*')
    echo "✓ Number of memories created: $MEMORY_COUNT"
else
    echo "✗ Response does not contain memoryIds field"
fi

# Extract first memory ID
MEMORY_ID=$(echo "$SAVE_RESP" | grep -o '"memoryId":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ ! -z "$MEMORY_ID" ]; then
    echo ""
    echo "Step 4: Verifying memory..."
    MEMORY_DATA=$(curl -s "$BASE_URL/api/memory/$MEMORY_ID" -b /tmp/cookies.txt)
    TITLE=$(echo "$MEMORY_DATA" | grep -o '"title":"[^"]*"' | cut -d'"' -f4)
    echo "✓ Memory ID: $MEMORY_ID"
    echo "✓ Memory Title: $TITLE"
fi

echo ""
echo "=== Test Complete ===\"
