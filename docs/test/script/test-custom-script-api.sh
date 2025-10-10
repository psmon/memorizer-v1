#!/bin/bash

BASE_URL="http://localhost:5012"

echo "=== Custom Script API Test ==="
echo ""

echo "1. Login and get session cookie..."
LOGIN_RESPONSE=$(curl -s -c cookies.txt -L -X POST "$BASE_URL/login" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=admin&password=admin123" -w "\n%{http_code}")

HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -1)
echo "Login HTTP Status: $HTTP_CODE"

if [ "$HTTP_CODE" != "200" ]; then
  echo "Login failed. Exiting."
  exit 1
fi

echo ""
echo "2. Create a test script (Google Analytics example)..."
CREATE_RESPONSE=$(curl -s -b cookies.txt -X POST "$BASE_URL/api/customscript" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "google-analytics",
    "scriptContent": "<script async src=\"https://www.googletagmanager.com/gtag/js?id=GA_MEASUREMENT_ID\"></script>\n<script>\n  window.dataLayer = window.dataLayer || [];\n  function gtag(){dataLayer.push(arguments);}\n  gtag(\"js\", new Date());\n  gtag(\"config\", \"GA_MEASUREMENT_ID\");\n</script>",
    "isActive": true
  }' -w "\n%{http_code}")

echo "$CREATE_RESPONSE"
echo ""

echo "3. Get all scripts..."
curl -s -b cookies.txt "$BASE_URL/api/customscript" | jq '.' || curl -s -b cookies.txt "$BASE_URL/api/customscript"
echo ""

echo "4. Get specific script..."
curl -s -b cookies.txt "$BASE_URL/api/customscript/google-analytics" | jq '.' || curl -s -b cookies.txt "$BASE_URL/api/customscript/google-analytics"
echo ""

echo "5. Test UI page to see if script is injected..."
curl -s "$BASE_URL/ui/blog" | grep -A 5 "<!-- Custom script -->"
echo ""

echo "6. Delete the test script..."
curl -s -b cookies.txt -X DELETE "$BASE_URL/api/customscript/google-analytics" | jq '.' || curl -s -b cookies.txt -X DELETE "$BASE_URL/api/customscript/google-analytics"
echo ""

rm -f cookies.txt
echo "Test completed!"