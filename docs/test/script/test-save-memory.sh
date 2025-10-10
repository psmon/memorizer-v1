#!/bin/bash

# Test script for SaveConversationAsMemory feature
# This tests the improved multi-conversation-pair memory saving

BASE_URL="http://localhost:5012"

echo "=== Testing SaveConversationAsMemory Feature ==="
echo ""

# Step 1: Login to get authentication
echo "Step 1: Authenticating..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' \
  -c cookies.txt)

echo "Login response: $LOGIN_RESPONSE"
echo ""

# Step 2: Create a new AskBot session
echo "Step 2: Creating new AskBot session..."
SESSION_RESPONSE=$(curl -s -X POST "$BASE_URL/api/askbot/session/new" \
  -b cookies.txt)

SESSION_ID=$(echo $SESSION_RESPONSE | grep -o '"sessionId":"[^"]*' | cut -d'"' -f4)
echo "Session ID: $SESSION_ID"
echo ""

# Step 3: Send multiple messages to create conversation pairs
echo "Step 3: Creating conversation with multiple pairs..."

# First message
echo "Sending message 1..."
curl -s -X POST "$BASE_URL/api/askbot/message" \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d "{\"sessionId\":\"$SESSION_ID\",\"message\":\"What is Docker?\"}" > /dev/null

sleep 5

# Second message
echo "Sending message 2..."
curl -s -X POST "$BASE_URL/api/askbot/message" \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d "{\"sessionId\":\"$SESSION_ID\",\"message\":\"How do I use docker-compose?\"}" > /dev/null

sleep 5

# Third message
echo "Sending message 3..."
curl -s -X POST "$BASE_URL/api/askbot/message" \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d "{\"sessionId\":\"$SESSION_ID\",\"message\":\"What are the benefits of containerization?\"}" > /dev/null

sleep 5

echo "Conversation created with 3 pairs"
echo ""

# Step 4: Create share link
echo "Step 4: Creating share link..."
SHARE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/askbot/share" \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d "{\"sessionId\":\"$SESSION_ID\"}")

SHORT_CODE=$(echo $SHARE_RESPONSE | grep -o '"shortCode":"[^"]*' | cut -d'"' -f4)
echo "Share link created: $SHORT_CODE"
echo "Full response: $SHARE_RESPONSE"
echo ""

# Step 5: Save conversation as memory (this will create multiple memories)
echo "Step 5: Saving conversation as memory (multiple pairs)..."
SAVE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/askbot/share/$SHORT_CODE/save-memory" \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d '{"createRelationships":true}')

echo "Save response:"
echo "$SAVE_RESPONSE" | jq '.' 2>/dev/null || echo "$SAVE_RESPONSE"
echo ""

# Extract memory IDs
MEMORY_IDS=$(echo $SAVE_RESPONSE | grep -o '"memoryIds":\[[^]]*\]' | sed 's/"memoryIds"://')
echo "Memory IDs created: $MEMORY_IDS"
echo ""

# Step 6: Verify memories were created
FIRST_MEMORY_ID=$(echo $SAVE_RESPONSE | grep -o '"memoryId":"[^"]*' | head -1 | cut -d'"' -f4)

if [ ! -z "$FIRST_MEMORY_ID" ]; then
    echo "Step 6: Verifying first memory..."
    MEMORY_RESPONSE=$(curl -s "$BASE_URL/api/memory/$FIRST_MEMORY_ID" -b cookies.txt)
    echo "First memory details:"
    echo "$MEMORY_RESPONSE" | jq '.' 2>/dev/null || echo "$MEMORY_RESPONSE"
    echo ""
fi

# Cleanup
rm -f cookies.txt

echo "=== Test Complete ==="
echo ""
echo "Summary:"
echo "- Session ID: $SESSION_ID"
echo "- Share Code: $SHORT_CODE"
echo "- Memory IDs: $MEMORY_IDS"
echo ""
echo "Visit: http://localhost:5012/ui/askbot/share/$SHORT_CODE"
