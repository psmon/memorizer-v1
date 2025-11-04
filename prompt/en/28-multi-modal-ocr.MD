# Multi-modal Model Integration

Please refer to the following code to create a module that calls a multi-modal model.

- src/Memorizer/Services/CustomLlmService.cs
  - Reference the SendChatRequest method.

## Guidelines
- Create it as a separate module from ILlmService.
- Write code that receives image + text input, passes it to a multi-modal model, and receives a response.
- The default model name is "qwen2/qwen3-vl-8b" and can be configured via environment variables.

## Python Code Example (for reference when implementing .NET code)
```python
from openai import OpenAI
import base64

client = OpenAI(base_url="http://localhost:1234/v1", api_key="lm-studio")  # Token can be any string

# Image -> base64
with open("test.jpg", "rb") as f:
    b64 = base64.b64encode(f.read()).decode("utf-8")
data_uri = f"data:image/jpeg;base64,{b64}"

resp = client.chat.completions.create(
    model="qwen2-vl-2b-instruct",  # Model ID loaded in LM Studio
    messages=[{
        "role": "user",
        "content": [
            {"type": "text", "text": "What do you see in this image?"},
            {"type": "image_url", "image_url": {"url": data_uri}}
        ]
    }],
    temperature=0.2,
)
print(resp.choices[0].message.content)
```

## Reference Information
- Project files are located in "src/Memorizer/"
- Unit test files are in "src/Memorizer.IntegrationTests/"
- Memory storage service code for querying and managing is in "src/Memorizer/Services/Memory.cs"
- Postgres schema is in "src/Memorizer/migrations" and manages schema migration according to numbering order. Reading them all in order will show you the complete schema.

## Testing Guidelines
- Write unit tests using a triangle image.
  - For unit tests, use the test profile from src/Memorizer.IntegrationTests/appsettings.json
    - http://192.168.0.50:1234 can be used in unit tests.
    - This is the first introduction of multi-modal, so add model settings for multi-modal.
  - Use a triangle image and verify that when asking "What do you see in this image?", the response mentions "triangle".
