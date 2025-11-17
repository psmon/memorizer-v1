# LLM API 사용 가이드

이 문서는 Memorizer 프로젝트에서 제공하는 OpenAI 호환 LLM API 사용 방법을 설명합니다.

## 개요

Memorizer는 `/api/llm` 엔드포인트를 통해 OpenAI API와 호환되는 LLM 기능을 제공합니다. 이 API는 CORS가 활성화되어 있어 프론트엔드 애플리케이션에서 직접 호출할 수 있습니다.

**기본 URL**: `http://your-server:port/api/llm`

**인증**: 필요 없음 (내부 네트워크 사용)

**CORS**: 모든 도메인 허용

## 사용 가능한 엔드포인트

### 1. Chat Completions (대화형 완성)

OpenAI의 `/v1/chat/completions`와 호환되는 엔드포인트입니다.

**엔드포인트**: `POST /api/llm/chat/completions`

**Content-Type**: `application/json`

#### 비-스트리밍 요청 예제

```bash
curl -X POST http://localhost:5000/api/llm/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-oss-20b",
    "messages": [
      {"role": "system", "content": "You are a helpful assistant."},
      {"role": "user", "content": "Hello, how are you?"}
    ],
    "max_tokens": 5000,
    "temperature": 0.7,
    "stream": false
  }'
```

**응답 예제**:
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
        "content": "Hello! I'm doing great—thanks for asking. How can I help you today?"
      },
      "finishReason": "stop"
    }
  ],
  "usage": {
    "promptTokens": 73,
    "completionTokens": 33,
    "totalTokens": 106
  }
}
```

#### 스트리밍 요청 예제

스트리밍을 사용하면 응답이 생성되는 대로 실시간으로 받을 수 있습니다.

```bash
curl -X POST http://localhost:5000/api/llm/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-oss-20b",
    "messages": [
      {"role": "user", "content": "Tell me a short story"}
    ],
    "max_tokens": 5000,
    "temperature": 0.7,
    "stream": true
  }'
```

**스트리밍 응답 형식** (Server-Sent Events):
```
data: {"id":"chatcmpl-abc","object":"chat.completion.chunk","created":1234567890,"model":"openai/gpt-oss-20b","choices":[{"delta":{"role":"assistant"},"index":0}]}

data: {"id":"chatcmpl-abc","object":"chat.completion.chunk","created":1234567890,"model":"openai/gpt-oss-20b","choices":[{"delta":{"content":"Once"},"index":0}]}

data: {"id":"chatcmpl-abc","object":"chat.completion.chunk","created":1234567890,"model":"openai/gpt-oss-20b","choices":[{"delta":{"content":" upon"},"index":0}]}

data: [DONE]
```

### 2. Text Completions (텍스트 완성)

텍스트 프롬프트에 대한 완성을 생성합니다.

**엔드포인트**: `POST /api/llm/completions`

**Content-Type**: `application/json`

#### 요청 예제

```bash
curl -X POST http://localhost:5000/api/llm/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-oss-20b",
    "prompt": "The weather today is",
    "max_tokens": 5000,
    "temperature": 0.7
  }'
```

**응답 예제**:
```json
{
  "id": "cmpl-abc123",
  "object": "text_completion",
  "created": 1757085637,
  "model": "openai/gpt-oss-20b",
  "choices": [
    {
      "index": 0,
      "text": "beautiful and sunny with clear skies and a gentle breeze.",
      "finishReason": "stop"
    }
  ],
  "usage": {
    "promptTokens": 74,
    "completionTokens": 59,
    "totalTokens": 133
  }
}
```

### 3. Health Check (상태 확인)

LLM 서비스의 상태를 확인합니다.

**엔드포인트**: `GET /api/llm/health`

#### 요청 예제

```bash
curl -X GET http://localhost:5000/api/llm/health
```

**응답 예제** (정상):
```json
{
  "healthy": true,
  "model": "openai/gpt-oss-20b",
  "responseTimeMs": 234,
  "timestamp": "2025-01-17T10:30:00Z"
}
```

**응답 예제** (오류):
```json
{
  "healthy": false,
  "error": "Connection timeout",
  "responseTimeMs": 5000,
  "timestamp": "2025-01-17T10:30:00Z"
}
```

## 프론트엔드 통합 예제

### JavaScript/TypeScript (Fetch API)

#### 비-스트리밍 요청

```javascript
async function chatCompletion(messages) {
  const response = await fetch('http://localhost:5000/api/llm/chat/completions', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      model: 'openai/gpt-oss-20b',
      messages: messages,
      max_tokens: 5000,
      temperature: 0.7,
      stream: false
    })
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const data = await response.json();
  return data.choices[0].message.content;
}

// 사용 예제
const messages = [
  { role: 'system', content: 'You are a helpful assistant.' },
  { role: 'user', content: 'What is the capital of France?' }
];

chatCompletion(messages)
  .then(answer => console.log('Answer:', answer))
  .catch(error => console.error('Error:', error));
```

#### 스트리밍 요청

```javascript
async function chatCompletionStream(messages, onChunk) {
  const response = await fetch('http://localhost:5000/api/llm/chat/completions', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      model: 'openai/gpt-oss-20b',
      messages: messages,
      max_tokens: 5000,
      temperature: 0.7,
      stream: true
    })
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();

    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = line.slice(6);

        if (data === '[DONE]') {
          return;
        }

        try {
          const json = JSON.parse(data);
          const content = json.choices[0]?.delta?.content;

          if (content) {
            onChunk(content);
          }
        } catch (e) {
          console.error('Error parsing SSE data:', e);
        }
      }
    }
  }
}

// 사용 예제
const messages = [
  { role: 'user', content: 'Tell me a short story about a cat.' }
];

let fullResponse = '';

chatCompletionStream(messages, (chunk) => {
  fullResponse += chunk;
  console.log('Received chunk:', chunk);
  // UI 업데이트 로직
  document.getElementById('response').textContent = fullResponse;
})
.then(() => console.log('Stream completed'))
.catch(error => console.error('Error:', error));
```

### React 예제

```tsx
import React, { useState } from 'react';

interface Message {
  role: 'system' | 'user' | 'assistant';
  content: string;
}

function ChatComponent() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const sendMessage = async () => {
    if (!input.trim()) return;

    const userMessage: Message = { role: 'user', content: input };
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    try {
      const response = await fetch('http://localhost:5000/api/llm/chat/completions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          model: 'openai/gpt-oss-20b',
          messages: [...messages, userMessage],
          max_tokens: 5000,
          temperature: 0.7,
          stream: false
        })
      });

      const data = await response.json();
      const assistantMessage: Message = {
        role: 'assistant',
        content: data.choices[0].message.content
      };

      setMessages(prev => [...prev, assistantMessage]);
    } catch (error) {
      console.error('Error:', error);
      alert('Failed to get response from LLM');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="chat-container">
      <div className="messages">
        {messages.map((msg, idx) => (
          <div key={idx} className={`message ${msg.role}`}>
            <strong>{msg.role}:</strong> {msg.content}
          </div>
        ))}
      </div>

      <div className="input-area">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
          disabled={isLoading}
          placeholder="Type your message..."
        />
        <button onClick={sendMessage} disabled={isLoading}>
          {isLoading ? 'Sending...' : 'Send'}
        </button>
      </div>
    </div>
  );
}

export default ChatComponent;
```

### Vue.js 예제

```vue
<template>
  <div class="chat-component">
    <div class="messages">
      <div v-for="(msg, idx) in messages" :key="idx" :class="['message', msg.role]">
        <strong>{{ msg.role }}:</strong> {{ msg.content }}
      </div>
    </div>

    <div class="input-area">
      <input
        v-model="input"
        @keyup.enter="sendMessage"
        :disabled="isLoading"
        placeholder="Type your message..."
      />
      <button @click="sendMessage" :disabled="isLoading">
        {{ isLoading ? 'Sending...' : 'Send' }}
      </button>
    </div>
  </div>
</template>

<script>
export default {
  name: 'ChatComponent',
  data() {
    return {
      messages: [],
      input: '',
      isLoading: false
    };
  },
  methods: {
    async sendMessage() {
      if (!this.input.trim()) return;

      const userMessage = { role: 'user', content: this.input };
      this.messages.push(userMessage);
      this.input = '';
      this.isLoading = true;

      try {
        const response = await fetch('http://localhost:5000/api/llm/chat/completions', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            model: 'openai/gpt-oss-20b',
            messages: this.messages,
            max_tokens: 5000,
            temperature: 0.7,
            stream: false
          })
        });

        const data = await response.json();
        this.messages.push({
          role: 'assistant',
          content: data.choices[0].message.content
        });
      } catch (error) {
        console.error('Error:', error);
        alert('Failed to get response from LLM');
      } finally {
        this.isLoading = false;
      }
    }
  }
};
</script>
```

## API 파라미터 상세 설명

### Chat Completions 요청 파라미터

| 파라미터 | 타입 | 필수 | 기본값 | 설명 |
|---------|------|-----|--------|------|
| model | string | No | "openai/gpt-oss-20b" | 사용할 LLM 모델 |
| messages | array | Yes | - | 대화 메시지 배열 |
| messages[].role | string | Yes | - | 메시지 역할 (system, user, assistant) |
| messages[].content | string | Yes | - | 메시지 내용 |
| max_tokens | integer | No | 5000 | 생성할 최대 토큰 수 |
| temperature | number | No | 0.7 | 응답의 창의성 (0.0 ~ 2.0) |
| stream | boolean | No | false | SSE 스트리밍 활성화 여부 |

### Text Completions 요청 파라미터

| 파라미터 | 타입 | 필수 | 기본값 | 설명 |
|---------|------|-----|--------|------|
| model | string | No | "openai/gpt-oss-20b" | 사용할 LLM 모델 |
| prompt | string | Yes | - | 완성할 텍스트 프롬프트 |
| max_tokens | integer | No | 5000 | 생성할 최대 토큰 수 |
| temperature | number | No | 0.7 | 응답의 창의성 (0.0 ~ 2.0) |

## 오류 처리

API는 다음과 같은 HTTP 상태 코드를 반환합니다:

- **200 OK**: 요청 성공
- **400 Bad Request**: 잘못된 요청 (예: messages가 비어있음)
- **500 Internal Server Error**: 서버 내부 오류
- **503 Service Unavailable**: LLM 서비스 연결 실패

### 오류 응답 예제

```json
{
  "error": "Messages array cannot be empty"
}
```

```json
{
  "error": "LLM service unavailable",
  "details": "Connection timeout"
}
```

## Swagger 문서

Swagger UI를 통해 API를 직접 테스트할 수 있습니다:

**URL**: `http://localhost:5000/swagger`

Swagger UI에서 `/api/llm` 엔드포인트를 찾아 "Try it out" 버튼을 클릭하여 요청을 테스트할 수 있습니다.

## 주의사항

1. **CORS 설정**: 프로덕션 환경에서는 허용된 도메인만 접근할 수 있도록 CORS 설정을 제한하는 것이 좋습니다.

2. **Rate Limiting**: 현재 API에는 속도 제한이 없습니다. 프로덕션 환경에서는 Rate Limiting 미들웨어를 추가하는 것을 권장합니다.

3. **인증**: 내부 네트워크 전용으로 설계되어 인증이 필요 없지만, 외부 접근이 필요한 경우 인증 메커니즘을 추가해야 합니다.

4. **타임아웃**: 긴 응답의 경우 클라이언트 타임아웃 설정을 적절히 조정하거나 스트리밍 모드를 사용하세요.

5. **최대 토큰**: 기본값은 5000이며, 서버 설정에 따라 제한될 수 있습니다.

## 설정

LLM API는 `appsettings.json`의 `LLM` 섹션에서 설정됩니다:

```json
{
  "LLM": {
    "Type": "Custom",
    "ApiUrl": "http://192.168.0.50:1234",
    "Model": "openai/gpt-oss-20b",
    "Timeout": "00:05:00"
  }
}
```

설정을 변경한 후에는 애플리케이션을 재시작해야 합니다.
