
이 프로젝트는 dotnet 9.0을 사용하며, 다음과 같은 임베딩과 LLM이 구현되어 있으며
설정에따른 선택가능한 구조로 되어 있습니다.

src/Memorizer/Extensions/ServiceCollectionExtensions.cs

OepnAI:
src/Memorizer/Services/OpenAILlmService.cs
src/Memorizer/Services/OpenAIEmbeddingService.cs

Ollama:
src/Memorizer/Services/OllamaLlmService.cs
src/Memorizer/Services/OllamaEmbeddingService.cs


AI API Guide에 내부망에서 작동하는 CUSTMOM 임베딩/LLM API에 대한 가이드 문서가 있습니다.
이를 참고해  다음 지침을 수행해 주세요

- Custom LLM/Embedding 서비스를 구현하고 선택해 작동하도록 업그레이드 해주세요
- LLM에 사용된 프롬프트는 공통으로 분리해 관리되도록 업그레이드 해주세요


# AI API Guide - Internal LLM Service

## Overview

The internal network provides an OpenAI-compatible LLM API service that can be used for AI feature development. This API closely follows the OpenAI specification, making it easy to integrate with existing OpenAI-compatible libraries and tools.

- **Base URL**: `http://192.168.0.50:1234/v1`
- **Authentication**: None required (internal network only)
- **Default max_tokens**: 5000 (always use this value as default)

## Available Models

### 1. Chat/Completion Model
- **Model ID**: `openai/gpt-oss-20b`
- **Type**: Large Language Model
- **Supported Endpoints**:
    - `/v1/chat/completions`
    - `/v1/completions`

### 2. Embedding Model
- **Model ID**: `text-embedding-nomic-embed-text-v1.5`
- **Type**: Text Embedding Model
- **Embedding Dimensions**: 768
- **Supported Endpoints**: `/v1/embeddings`

## API Endpoints

### 1. List Available Models

```bash
curl -X GET http://192.168.0.50:1234/v1/models
```

**Response Example:**
```json
{
  "data": [
    {
      "id": "openai/gpt-oss-20b",
      "object": "model",
      "owned_by": "organization_owner"
    },
    {
      "id": "text-embedding-nomic-embed-text-v1.5",
      "object": "model",
      "owned_by": "organization_owner"
    }
  ],
  "object": "list"
}
```

### 2. Chat Completions

#### Standard Request

```bash
curl -X POST http://192.168.0.50:1234/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-oss-20b",
    "messages": [
      {"role": "system", "content": "You are a helpful assistant."},
      {"role": "user", "content": "Hello, how are you?"}
    ],
    "max_tokens": 5000,
    "temperature": 0.7
  }'
```

**Response Example:**
```json
{
  "id": "chatcmpl-xyz123",
  "object": "chat.completion",
  "created": 1757085619,
  "model": "openai/gpt-oss-20b",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! I'm doing great—thanks for asking. How can I help you today?",
        "reasoning": "We should respond politely.",
        "tool_calls": []
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 73,
    "completion_tokens": 33,
    "total_tokens": 106
  }
}
```

#### Streaming Request

```bash
curl -X POST http://192.168.0.50:1234/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-oss-20b",
    "messages": [
      {"role": "system", "content": "You are a helpful assistant."},
      {"role": "user", "content": "What is 2+2?"}
    ],
    "max_tokens": 5000,
    "temperature": 0.5,
    "stream": true
  }'
```

**Streaming Response Format:**
```
data: {"id":"chatcmpl-abc","choices":[{"delta":{"role":"assistant"},"index":0}]}
data: {"id":"chatcmpl-abc","choices":[{"delta":{"content":"4"},"index":0}]}
data: {"id":"chatcmpl-abc","choices":[{"delta":{},"finish_reason":"stop","index":0}]}
data: [DONE]
```

### 3. Text Completions

```bash
curl -X POST http://192.168.0.50:1234/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-oss-20b",
    "prompt": "The weather today is",
    "max_tokens": 5000,
    "temperature": 0.7
  }'
```

**Response Example:**
```json
{
  "id": "cmpl-abc123",
  "object": "text_completion",
  "created": 1757085637,
  "model": "openai/gpt-oss-20b",
  "choices": [
    {
      "index": 0,
      "text": "beautiful and sunny with clear skies...",
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 74,
    "completion_tokens": 59,
    "total_tokens": 133
  }
}
```

### 4. Embeddings

#### Single Text Embedding

```bash
curl -X POST http://192.168.0.50:1234/v1/embeddings \
  -H "Content-Type: application/json" \
  -d '{
    "model": "text-embedding-nomic-embed-text-v1.5",
    "input": "Hello world"
  }'
```

#### Batch Embeddings

```bash
curl -X POST http://192.168.0.50:1234/v1/embeddings \
  -H "Content-Type: application/json" \
  -d '{
    "model": "text-embedding-nomic-embed-text-v1.5",
    "input": ["Text 1", "Text 2", "Text 3"]
  }'
```

**Response Example:**
```json
{
  "object": "list",
  "model": "text-embedding-nomic-embed-text-v1.5",
  "data": [
    {
      "object": "embedding",
      "embedding": [0.0234, -0.0098, ...],  // 768 dimensions
      "index": 0
    }
  ],
  "usage": {
    "prompt_tokens": 0,
    "total_tokens": 0
  }
}
```

## Important Notes

1. **Max Tokens**: Always use `max_tokens: 5000` as the default value for optimal performance.

2. **Streaming**: When using streaming (`stream: true`), responses are sent as Server-Sent Events (SSE) format, each line prefixed with `data: ` and ending with `data: [DONE]`.

3. **OpenAI Compatibility**: This API is designed to be compatible with OpenAI's API specification, allowing easy migration of existing OpenAI-based applications to use the internal LLM service.

4. **Model Selection**:
    - Use `openai/gpt-oss-20b` for chat and text generation tasks
    - Use `text-embedding-nomic-embed-text-v1.5` for embedding generation

5. **Response Fields**: The API includes some additional fields like `reasoning` in chat completions that are not in the standard OpenAI API but can be safely ignored if not needed.

## Example Integration with Python

```python
import requests
import json

# Chat completion example
def chat_with_llm(messages):
    response = requests.post(
        "http://192.168.0.50:1234/v1/chat/completions",
        headers={"Content-Type": "application/json"},
        json={
            "model": "openai/gpt-oss-20b",
            "messages": messages,
            "max_tokens": 5000,
            "temperature": 0.7
        }
    )
    return response.json()

# Embedding example
def get_embedding(text):
    response = requests.post(
        "http://192.168.0.50:1234/v1/embeddings",
        headers={"Content-Type": "application/json"},
        json={
            "model": "text-embedding-nomic-embed-text-v1.5",
            "input": text
        }
    )
    return response.json()["data"][0]["embedding"]
```

## Troubleshooting

- **Connection Issues**: Ensure you are on the internal network and can reach `192.168.0.50:1234`
- **Model Not Found**: Use the exact model IDs as listed in the `/v1/models` endpoint
- **Timeout Issues**: For longer responses, consider using streaming to receive partial results
