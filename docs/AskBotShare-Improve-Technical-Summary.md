# AskBot 공유 대화 연관 문서 표시 기능 구현

## 기술적 가치

### 1. **데이터 무결성과 일관성**
- 대화 공유 시 참조된 문서 정보를 함께 저장하여 컨텍스트 보존
- JSONB 타입을 활용한 유연한 메타데이터 저장 구조
- 기존 데이터 호환성 보장 (NULL 허용 및 처리)

### 2. **Actor Model 패턴 활용**
- `ConversationEntry` 불변 레코드에 `ReferencedMemoryIds` 추가
- 액터 간 메시지 전달로 연관 문서 정보 전파
- 세션별 대화 상태 관리

### 3. **UX 개선**
- 공유된 대화에서도 원본과 동일한 참조 문서 표시
- 모달을 통한 비침투적 문서 상세 보기
- 마크다운/코드 하이라이팅 지원

## 핵심 구현 코드

### 1. 데이터베이스 스키마 (Migration 013)

```sql
-- Migration 013: Add referenced_memories column to askbot_share_links
ALTER TABLE askbot_share_links
ADD COLUMN IF NOT EXISTS referenced_memories JSONB;

-- Index for referenced_memories queries
CREATE INDEX IF NOT EXISTS idx_askbot_share_links_referenced_memories
    ON askbot_share_links USING gin(referenced_memories);
```

**기술적 의미:**
- JSONB 타입으로 구조화된 배열 저장 가능
- GIN 인덱스로 JSONB 쿼리 성능 최적화
- 유연한 스키마로 향후 확장 용이

### 2. ConversationEntry 불변 레코드 확장

```csharp
// ChatBotMessages.cs
public sealed record ConversationEntry
{
    public required string UserMessage { get; init; }
    public required string BotResponse { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool UsedMemorySearch { get; init; }

    /// <summary>
    /// List of referenced memory IDs used in this response
    /// </summary>
    public List<Guid>? ReferencedMemoryIds { get; init; }
}
```

**기술적 의미:**
- C# 9.0 record 타입으로 불변성 보장
- 액터 간 안전한 메시지 전달
- NULL 허용으로 기존 데이터 호환성 유지

### 3. ChatBotActor에서 연관 문서 전달

```csharp
// ChatBotActor.cs
private async Task UpdateConversationEntries(
    string userMessage,
    string botResponse,
    bool usedMemorySearch,
    List<Guid>? referencedMemoryIds = null)
{
    var newEntry = new ConversationEntry
    {
        UserMessage = userMessage,
        BotResponse = botResponse,
        UsedMemorySearch = usedMemorySearch,
        ReferencedMemoryIds = referencedMemoryIds
    };

    _conversationEntries.Add(newEntry);
}

// 메모리 기반 응답 생성 시 호출
await UpdateConversationEntries(
    request.Message,
    llmResponse,
    true,
    usedMemoryIds  // 참조된 메모리 ID 전달
);
```

**기술적 의미:**
- 선택적 매개변수로 하위 호환성 유지
- 액터 내부 상태에 연관 문서 정보 저장
- 세션별 대화 히스토리 관리

### 4. API - 공유 링크 생성 시 연관 문서 저장

```csharp
// AskBotController.cs - CreateShareLink
// Convert conversation entries to messages with memory IDs
foreach (var entry in historyResponse.ConversationEntries)
{
    messages.Add(new
    {
        role = "assistant",
        content = entry.BotResponse,
        timestamp = entry.Timestamp,
        usedMemorySearch = entry.UsedMemorySearch,
        referencedMemoryIds = entry.ReferencedMemoryIds ?? new List<Guid>()
    });
}

// Extract referenced memories from messages
var referencedMemories = new List<object>();
int messageIndex = 0;
foreach (dynamic msg in messages)
{
    if (msg.GetType().GetProperty("referencedMemoryIds") != null)
    {
        var memoryIds = msg.referencedMemoryIds as List<Guid>;
        if (memoryIds != null && memoryIds.Count > 0)
        {
            referencedMemories.Add(new
            {
                messageIndex = messageIndex,
                memoryIds = memoryIds
            });
        }
    }
    messageIndex++;
}

var referencedMemoriesJson = referencedMemories.Count > 0
    ? JsonSerializer.Serialize(referencedMemories)
    : null;

// Store with referenced memories
var insertQuery = @"
    INSERT INTO askbot_share_links
    (short_code, session_id, content, referenced_memories, created_at)
    VALUES (@shortCode, @sessionId, @content::jsonb,
            @referencedMemories::jsonb, @createdAt)";
```

**기술적 의미:**
- 메시지 인덱스 기반 매핑으로 정확한 참조 관계 유지
- NULL safe 처리로 기존 데이터 호환
- JSONB로 직접 삽입하여 타입 안정성 확보

### 5. 공유 페이지 - 연관 문서 표시 (JavaScript)

```javascript
// Share.cshtml
let referencedMemoriesMap = {}; // messageIndex -> memoryIds 매핑

// API에서 데이터 로드
const data = await response.json();
if (data.referencedMemories) {
    data.referencedMemories.forEach(ref => {
        referencedMemoriesMap[ref.messageIndex] = ref.memoryIds;
    });
}

// 메시지 렌더링 시 연관 문서 버튼 추가
function renderConversation(messages) {
    messages.forEach((msg, index) => {
        let memoryReferencesHtml = '';

        if (msg.role === 'assistant' && referencedMemoriesMap[index]) {
            const memoryIds = referencedMemoriesMap[index];
            if (memoryIds && memoryIds.length > 0) {
                const buttons = memoryIds.map((id, idx) =>
                    `<button class="btn btn-sm btn-outline-info"
                             onclick="viewMemory('${id}')">
                        <i class="fas fa-eye me-1"></i>참고 문서 ${idx + 1} 보기
                    </button>`
                ).join('');

                memoryReferencesHtml = `
                    <div class="memory-references">
                        <small class="text-muted">
                            <i class="fas fa-book me-1"></i>
                            <strong>참고 문서:</strong>
                        </small>
                        <div class="mt-2">${buttons}</div>
                    </div>
                `;
            }
        }
        // 메시지 렌더링에 포함
    });
}
```

**기술적 의미:**
- 클라이언트 측에서 메시지 인덱스 기반 매핑
- Fallback 메커니즘으로 다양한 데이터 소스 지원
- 이벤트 핸들러로 모달 표시

### 6. 모달을 통한 문서 상세 보기

```javascript
// Modal 표시 함수
async function viewMemory(id) {
    const modal = new bootstrap.Modal(document.getElementById('memoryModal'));

    // 로딩 표시
    document.getElementById('modalLoadingContent').style.display = 'block';
    document.getElementById('modalMemoryContent').style.display = 'none';
    modal.show();

    try {
        const response = await fetch(`/api/memory/${id}`);
        const memory = await response.json();

        // 메타데이터 표시
        document.getElementById('modalMemoryTitle').textContent =
            memory.title || 'Memory';
        document.getElementById('modalMemoryType').textContent = memory.type;
        document.getElementById('modalMemoryConfidence').textContent =
            `${(memory.confidence * 100).toFixed(0)}%`;

        // 마크다운 렌더링
        const contentContainer = document.getElementById('modalRenderedContent');
        contentContainer.innerHTML = renderMarkdown(memory.text || memory.content);

        // Mermaid 다이어그램 렌더링
        mermaid.run();

        // 코드 하이라이팅
        contentContainer.querySelectorAll('pre code').forEach((block) => {
            hljs.highlightElement(block);
        });

        // 전체 보기 링크 설정
        document.getElementById('modalOpenFullView').href = `/ui/view/${id}`;

        // 컨텐츠 표시
        document.getElementById('modalLoadingContent').style.display = 'none';
        document.getElementById('modalMemoryContent').style.display = 'block';

    } catch (error) {
        console.error('Error loading memory:', error);
        // 에러 표시
    }
}
```

**기술적 의미:**
- Bootstrap Modal API 활용
- 비동기 데이터 로딩과 상태 관리
- 마크다운/Mermaid/코드 하이라이팅 통합
- 에러 처리로 사용자 경험 개선

## 아키텍처 패턴

### 1. **Actor Model Pattern**
```
User Request → ChatBotActor → SearchMemoryActor → DecisionActor
                    ↓
              ConversationEntry (with ReferencedMemoryIds)
                    ↓
              GetConversationHistoryResponse
                    ↓
              ShareLink Creation (with referenced_memories JSONB)
```

### 2. **Data Flow**
```
[Actor State] → [API Response] → [Database JSONB] → [Client Render] → [Modal Display]
```

### 3. **Null Safety Pattern**
```csharp
// Backend
referencedMemoryIds ?? new List<Guid>()
reader.IsDBNull(3) ? null : reader.GetString(3)

// Frontend
if (data.referencedMemories) { ... }
if (memoryIds && memoryIds.length > 0) { ... }
```

## 성능 최적화

### 1. **Database Indexing**
- GIN 인덱스로 JSONB 쿼리 최적화
- 메시지 인덱스 기반 직접 접근

### 2. **Client-Side Caching**
- referencedMemoriesMap으로 중복 조회 방지
- 모달 재사용으로 DOM 조작 최소화

### 3. **Lazy Loading**
- 모달 표시 시점에 문서 로드
- 필요한 경우에만 API 호출

## 확장 가능성

### 1. **추가 메타데이터**
JSONB 구조로 손쉬운 확장:
```json
{
  "messageIndex": 1,
  "memoryIds": ["uuid1", "uuid2"],
  "relevanceScores": [0.95, 0.87],
  "searchQuery": "original query"
}
```

### 2. **다양한 참조 타입**
```csharp
public List<DocumentReference>? References { get; init; }

public record DocumentReference
{
    public Guid Id { get; init; }
    public string Type { get; init; }  // "memory", "external", "graph"
    public double Relevance { get; init; }
}
```

### 3. **실시간 업데이트**
- SSE를 통한 참조 문서 실시간 푸시
- WebSocket으로 협업 기능 확장

## 테스트 전략

### 1. **통합 테스트**
```csharp
[Fact]
public async Task CreateShareLink_WithReferencedMemories_StoresCorrectly()
{
    // Arrange: 메모리 참조가 있는 세션 생성
    // Act: 공유 링크 생성
    // Assert: referenced_memories JSONB 검증
}
```

### 2. **API 테스트**
```csharp
[Fact]
public async Task GetSharedConversation_WithReferences_ReturnsAllData()
{
    // Arrange: 참조가 포함된 공유 링크
    // Act: API 호출
    // Assert: content + referencedMemories 반환 확인
}
```

### 3. **프론트엔드 테스트**
```javascript
describe('Memory References Display', () => {
    it('should render memory reference buttons', () => {
        // Given: 참조가 있는 메시지
        // When: 렌더링
        // Then: 버튼 표시 확인
    });
});
```

## 마이그레이션 전략

### 1. **Zero Downtime Migration**
```sql
-- Step 1: 컬럼 추가 (NULL 허용)
ALTER TABLE askbot_share_links
ADD COLUMN IF NOT EXISTS referenced_memories JSONB;

-- Step 2: 인덱스 생성 (CONCURRENTLY)
CREATE INDEX CONCURRENTLY idx_askbot_share_links_referenced_memories
    ON askbot_share_links USING gin(referenced_memories);

-- Step 3: 기존 데이터는 NULL로 유지
-- Step 4: 새로운 공유부터 데이터 저장
```

### 2. **Backward Compatibility**
- NULL 체크로 기존 공유 링크 정상 작동
- Fallback UI로 참조 없는 경우 처리
- 점진적 데이터 마이그레이션 가능

## 구현 파일 위치

### Backend
- `src/Memorizer/migrations/013_add_referenced_memories_to_share_links.sql` - 데이터베이스 마이그레이션
- `src/Memorizer/Actors/ChatBotMessages.cs:329` - ConversationEntry 레코드 정의
- `src/Memorizer/Actors/ChatBotActor.cs:718` - UpdateConversationEntries 메서드
- `src/Memorizer/Actors/ChatBotActor.cs:388` - 메모리 기반 응답 시 ID 전달
- `src/Memorizer/Controllers/AskBotController.cs:330` - 공유 링크 생성 API
- `src/Memorizer/Controllers/AskBotController.cs:481-485` - 공유 링크 조회 API

### Frontend
- `src/Memorizer/Views/AskBot/Share.cshtml:257` - referencedMemoriesMap 초기화
- `src/Memorizer/Views/AskBot/Share.cshtml:319-358` - 연관 문서 렌더링
- `src/Memorizer/Views/AskBot/Share.cshtml:424-486` - viewMemory 모달 함수
- `src/Memorizer/Views/AskBot/Share.cshtml:181-230` - Memory View Modal HTML

## 결론

이 구현은 다음과 같은 기술적 가치를 제공합니다:

1. **데이터 무결성**: 대화와 참조 문서의 관계 보존
2. **확장성**: JSONB 기반 유연한 스키마
3. **성능**: GIN 인덱스와 클라이언트 캐싱
4. **UX**: 비침투적 모달 UI
5. **유지보수성**: Actor Model과 명확한 책임 분리
6. **호환성**: 기존 데이터와 완벽한 호환

Actor Model, JSONB, 그리고 현대적인 프론트엔드 패턴의 조합으로 견고하고 확장 가능한 기능을 구현했습니다.

## Tags
#askbot #actor-model #jsonb #dotnet #postgresql #shared-conversation #referenced-documents #modal-ui #migration #csharp #javascript #akka.net
