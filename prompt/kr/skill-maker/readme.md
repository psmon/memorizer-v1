# Memorizer 프로젝트 Claude Code 스킬 가이드

## 개요

Memorizer 프로젝트 전용으로 개발된 Claude Code 스킬 모음입니다. 이 스킬들은 프로젝트의 고급 패턴과 기술 스택을 효율적으로 활용할 수 있도록 설계되었습니다.

## 스킬 목록

### 핵심 스킬

| 스킬명 | 위치 | 용도 |
|--------|------|------|
| **llm-api** | `.claude/skills/llm-api/` | LLM API 활용 (Ollama, OpenAI, Custom) |
| **actor-model** | `.claude/skills/actor-model/` | Akka.NET 액터 모델 구현 |
| **graph-db** | `.claude/skills/graph-db/` | Neo4j 그래프 DB 활용 |
| **project-convention** | `.claude/skills/project-convention/` | 프로젝트 컨벤션 및 구조 |
| **fullstack-ui** | `.claude/skills/fullstack-ui/` | 풀스택 UI (MVC, SSE, Markdown) |

### 확장 스킬

| 스킬명 | 위치 | 용도 |
|--------|------|------|
| **db-migration** | `.claude/skills/db-migration/` | PostgreSQL DB 마이그레이션 |
| **mcp-tools** | `.claude/skills/mcp-tools/` | MCP(Model Context Protocol) 도구 구현 |
| **embedding-service** | `.claude/skills/embedding-service/` | 텍스트 임베딩/벡터 검색 |
| **multimodal-service** | `.claude/skills/multimodal-service/` | 멀티모달(이미지+텍스트) 처리 |

## 스킬 활용 목적

### 1. 고급 패턴 구현 카테고리별 스킬화
- LLM 스트리밍 응답 처리
- 액터 모델 기반 비동기 워크플로우
- 그래프 DB 탐색 및 관계 생성
- SSE 기반 실시간 UI 업데이트

### 2. 코드 개발 일관성
- 디렉토리 구조 및 네이밍 규칙
- 서비스/컨트롤러 패턴
- DI 등록 방식
- 테스트 패턴

### 3. 토큰 절약
- 반복적인 프롬프트 대신 스킬 호출
- 필요한 컨텍스트만 로드
- 일관된 코드 생성

## 스킬별 상세 설명

### llm-api (LLM API 활용)

**언제 사용하나요?**
- ILlmService 인터페이스 활용
- 스트리밍 응답 처리
- 프롬프트 템플릿 설계
- 제목/키워드 생성

**핵심 패턴:**
```csharp
// 기본 완료
var response = await _llmService.CompleteAsync(prompt);

// 스트리밍
await foreach (var chunk in _llmService.CompleteStreamingAsync(prompt))
{
    // SSE로 전송
}
```

---

### actor-model (Akka.NET 액터 모델)

**언제 사용하나요?**
- 세션 기반 상태 관리
- 타이머 기반 자동 종료
- 메시지 기반 비동기 통신
- 복잡한 워크플로우 처리

**핵심 패턴:**
```csharp
public class MyActor : ReceiveActor, IWithTimers
{
    public ITimerScheduler Timers { get; set; } = null!;

    public MyActor()
    {
        Receive<MyRequest>(HandleRequest);
        Receive<SessionTimeout>(HandleTimeout);
    }
}
```

---

### graph-db (Neo4j 그래프 DB)

**언제 사용하나요?**
- Cypher 쿼리 작성
- 메모리 간 관계 생성
- 그래프 탐색 (경로, 허브 노드)
- 자연어 → Cypher 변환

**핵심 패턴:**
```csharp
var cypher = @"
    MATCH (m:Memory)-[:RELATES_TO]->(related:Memory)
    WHERE m.id = $id
    RETURN related
    LIMIT 10
";
var records = await _graphRepository.RunQueryAsync(cypher, new { id });
```

---

### project-convention (프로젝트 컨벤션)

**언제 사용하나요?**
- 새 기능 추가 시
- 디렉토리/파일 생성
- 네이밍 규칙 확인
- 테스트 작성

**핵심 구조:**
```
src/Memorizer/
├── Actors/          # 액터 모델
├── Controllers/     # API 컨트롤러
├── Services/        # 비즈니스 로직
├── Models/          # 도메인 모델
├── Settings/        # 설정 클래스
└── Views/           # Razor 뷰
```

---

### fullstack-ui (풀스택 UI)

**언제 사용하나요?**
- SSE 스트리밍 UI
- Markdown 렌더링
- Mermaid 다이어그램
- 반응형 테이블
- 단계별 진행 UI

**핵심 패턴:**
```javascript
const eventSource = new EventSource('/api/stream?prompt=...');
eventSource.onmessage = (event) => {
    if (event.data === '[DONE]') {
        eventSource.close();
        return;
    }
    const data = JSON.parse(event.data);
    updateUI(data.content);
};
```

---

### db-migration (DB 마이그레이션)

**언제 사용하나요?**
- 새 테이블 추가
- 컬럼 추가/수정
- 인덱스 생성
- 벡터 컬럼 추가

**핵심 패턴:**
```sql
-- Migration 0XX: Add {feature} table
CREATE TABLE IF NOT EXISTS {table_name} (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_{table_name}_{column}
    ON {table_name}({column});
```

**파일 네이밍:** `{버전번호}_{설명}.sql` (예: `020_add_feature.sql`)

---

### mcp-tools (MCP 도구 구현)

**언제 사용하나요?**
- 새 MCP 도구 추가
- Description 작성
- 인증(ApiKey/OAuth) 처리
- 그래프 검색 도구 구현

**핵심 패턴:**
```csharp
[McpServerTool]
[Description(@"도구의 목적과 사용 시나리오를 상세히 설명.
Use this when... Filtering by... Returns...")]
public async Task<string> ToolName(
    [Description("파라미터 설명")] string param,
    CancellationToken cancellationToken = default
)
{
    // 비즈니스 로직
    return FormatResult(result);
}
```

---

### embedding-service (임베딩/벡터 검색)

**언제 사용하나요?**
- 텍스트 벡터화
- 유사도 검색 구현
- 메타데이터 임베딩
- 폴백 검색 패턴

**핵심 패턴:**
```csharp
// 임베딩 생성
var embedding = await _embeddingService.Generate(text);

// 벡터 유사도 검색 (PostgreSQL)
var sql = @"
    SELECT *, 1 - (embedding <=> @embedding::vector) as similarity
    FROM memories
    WHERE 1 - (embedding <=> @embedding::vector) >= @minSimilarity
    ORDER BY embedding <=> @embedding::vector
    LIMIT @limit
";
```

---

### multimodal-service (멀티모달 처리)

**언제 사용하나요?**
- 이미지+텍스트 분석
- 비전 LLM 통합
- 이미지 업로드 UI
- 멀티모달 챗봇

**핵심 패턴:**
```csharp
// 이미지 분석
var response = await _multiModalService.AnalyzeImageAsync(
    imageData,
    "이 이미지를 분석해주세요",
    "jpeg"
);

// 프론트엔드 - 클립보드/드래그앤드롭
document.addEventListener('paste', (e) => {
    const items = e.clipboardData.items;
    for (const item of items) {
        if (item.type.startsWith('image/')) {
            processFile(item.getAsFile());
        }
    }
});
```

---

## 베스트 샘플 프롬프트

### 1. 새로운 액터 구현

```
액터 모델 스킬을 참고해 NotificationActor를 구현해줘.
- 사용자별 알림 큐 관리
- 24시간 비활성 시 자동 종료
- WebSocket으로 실시간 알림 전송
```

### 2. LLM 기반 기능 추가

```
llm-api 스킬을 참고해 요약 생성 기능을 추가해줘.
- 긴 텍스트를 3문장으로 요약
- 스트리밍 응답으로 실시간 표시
- 컨트롤러와 뷰 모두 구현
```

### 3. 그래프 검색 기능

```
graph-db 스킬을 참고해 연관 메모리 추천 기능을 구현해줘.
- 현재 메모리와 2-hop 이내의 관련 메모리 찾기
- 공통 키워드 기반 가중치 계산
- 결과를 카드 UI로 표시
```

### 4. 새 UI 페이지 추가

```
project-convention과 fullstack-ui 스킬을 참고해
대시보드 페이지를 추가해줘.
- /ui/dashboard URL
- 최근 메모리 통계 차트
- 그래프 관계 시각화
```

### 5. 복합 기능 구현

```
다음 스킬들을 활용해 "스마트 태그 추천" 기능을 구현해줘:
- llm-api: 내용 분석하여 태그 추천
- actor-model: 백그라운드에서 비동기 처리
- project-convention: 기존 패턴에 맞게 구현

요구사항:
- 메모리 저장 시 자동으로 태그 추천
- 사용자가 추천 태그 수락/거부 가능
```

### 6. DB 마이그레이션

```
db-migration 스킬을 참고해 북마크 기능을 위한 마이그레이션을 작성해줘.
- bookmarks 테이블 생성
- memory_id, user_id 외래키
- created_at 타임스탬프
- 복합 인덱스 추가
```

### 7. MCP 도구 추가

```
mcp-tools 스킬을 참고해 "태그 검색" MCP 도구를 추가해줘.
- SearchByTags 메서드
- 여러 태그 AND/OR 검색 지원
- LLM이 사용하기 좋은 Description 작성
- 결과 포맷팅 포함
```

### 8. 벡터 검색 기능

```
embedding-service 스킬을 참고해 "의미 기반 중복 검사" 기능을 구현해줘.
- 저장 전 유사 메모리 검색
- 유사도 90% 이상이면 경고
- 임계값 폴백 패턴 적용
```

### 9. 이미지 분석 기능

```
multimodal-service 스킬을 참고해 "스크린샷 분석" 기능을 추가해줘.
- 화면 캡처 붙여넣기 지원
- 이미지에서 텍스트/UI 요소 추출
- 결과를 메모리로 저장
```

### 10. ShapeUp 화이트보드 기능

```
fullstack-ui 스킬의 ShapeUp 패턴을 참고해 다이어그램 생성 기능을 추가해줘.
- Fabric.js 캔버스에 도형(박스, 원, 다이아몬드) 그리기
- 화살표 자석 연결 (연결점 자동 계산)
- AI 보드 생성 (Free Board 템플릿)
- 드래그 시 연결된 화살표 자동 업데이트
```

### 11. SVG 아이콘/BOX 추가

```
fullstack-ui 스킬을 참고해 커스텀 SVG 도형을 추가해줘.
- SVG path로 벡터 아이콘 그리기
- 투명 배경 + 테두리 색상/두께 조절
- SVG Content Editor 모달로 실시간 미리보기
- 아이콘 라이브러리 (화살표, 클라우드, 서버 등)
```

### 12. AI 생성 + 메모리 검색 연동

```
fullstack-ui 스킬을 참고해 AI 콘텐츠 생성에 메모리 검색을 연동해줘.
- 프롬프트에서 3개 키워드 추출
- 각 키워드별 유사 메모리 3개씩 검색 (총 최대 9개)
- LLM 배치 평가로 최적 3개 선택
- SSE phase 이벤트로 진행 상황 표시
```

### 13. 공유 페이지 캔버스 뷰어

```
fullstack-ui 스킬을 참고해 공유 페이지에 캔버스 뷰어를 구현해줘.
- 읽기 전용 Fabric.js 캔버스
- 마우스 휠 줌 (Container 레벨 이벤트, passive: false)
- Pan 모드 (손바닥 아이콘, 드래그로 이동)
- Edit 버튼 → /ui/shapeup으로 편집 모드 전환
- PNG/SVG 내보내기
```

### 14. 프롬프트 요약 기능

```
fullstack-ui 스킬을 참고해 긴 프롬프트 요약 기능을 추가해줘.
- 2000자 초과 시 LLM으로 요약
- 핵심 기능, 화면 구성, 사용자 흐름 유지
- 불필요한 설명, 중복 내용 제거
- 토스트 팝업으로 요약 완료 알림
```

### 15. 요소 복사/붙여넣기 기능

```
fullstack-ui 스킬을 참고해 캔버스 요소 복사/붙여넣기를 구현해줘.
- Ctrl+C/V로 복사/붙여넣기
- 그룹 요소도 그룹 유지한 채로 복사
- 복제 시 우측으로 오프셋 (겹침 방지)
- 커스텀 속성 유지 (customType, connectedArrows 등)
```

### 16. 비AI 생성 시 제목/설명 자동 생성

```
fullstack-ui 스킬을 참고해 비AI 생성 보드 공유 시 제목/설명 자동 생성해줘.
- 캔버스 텍스트 요소에서 내용 추출 (최대 500자)
- LLM으로 Title(30자 이하)과 Description 생성
- Share 버튼 클릭 시 프로그레스 표시
- "No description available" 방지
```

### 17. 전체 스킬 활용 (대규모 기능)

```
다음 스킬들을 모두 활용해 "지식 그래프 시각화" 기능을 구현해줘:
- project-convention: 디렉토리 구조, 네이밍
- graph-db: Cypher로 관계 조회
- embedding-service: 클러스터링을 위한 벡터 유사도
- fullstack-ui: Mermaid 다이어그램 렌더링
- actor-model: 대량 데이터 비동기 처리

요구사항:
- /ui/knowledge-map 페이지
- 메모리 간 관계를 시각화
- 클러스터별 색상 구분
```

---

## 스킬 활용 팁

### 스킬 조합 가이드

| 작업 유형 | 권장 스킬 조합 |
|----------|---------------|
| 새 API 엔드포인트 | project-convention + (llm-api 또는 embedding-service) |
| 새 UI 페이지 | project-convention + fullstack-ui |
| 데이터 모델 변경 | db-migration + project-convention |
| AI 기능 추가 | llm-api + actor-model |
| 검색 기능 개선 | embedding-service + graph-db + llm-api (배치 평가) |
| 외부 도구 연동 | mcp-tools + project-convention |
| 이미지 처리 | multimodal-service + actor-model |
| 화이트보드/다이어그램 | fullstack-ui (ShapeUp 패턴) + llm-api |
| AI 생성 + 메모리 연동 | fullstack-ui + embedding-service + llm-api |
| 공유 페이지 뷰어 | fullstack-ui (캔버스 뷰어 패턴) |
| 긴 프롬프트 처리 | fullstack-ui (요약 기능) + llm-api |

### 스킬 선택 플로우차트

```
새 기능 요청 받음
    ↓
DB 스키마 변경 필요? → Yes → db-migration
    ↓ No
MCP 도구 추가? → Yes → mcp-tools
    ↓ No
이미지 처리 필요? → Yes → multimodal-service
    ↓ No
벡터 검색 필요? → Yes → embedding-service
    ↓ No                    + 배치 평가 → llm-api
LLM 호출 필요? → Yes → llm-api
    ↓ No
비동기/상태 관리? → Yes → actor-model
    ↓ No
그래프 관계 조회? → Yes → graph-db
    ↓ No
화이트보드/다이어그램? → Yes → fullstack-ui (ShapeUp 패턴)
    ↓ No
공유 페이지 뷰어? → Yes → fullstack-ui (캔버스 뷰어 패턴)
    ↓ No
긴 프롬프트 처리? → Yes → fullstack-ui (요약 기능)
    ↓ No
UI 페이지 추가? → Yes → fullstack-ui
    ↓
항상 → project-convention
```

---

## 스킬 확장 가이드

새로운 스킬을 추가하려면:

1. `.claude/skills/{skill-name}/` 디렉토리 생성
2. `SKILL.md` 파일 작성:
   ```markdown
   ---
   name: skill-name
   description: 스킬 설명. 언제 사용하는지 명확히 기술.
   ---

   # 스킬 제목

   ## Instructions
   ...
   ```
3. 필요시 `scripts/`, `references/` 폴더 추가

---

## 문의 및 개선

스킬 개선이 필요하거나 새로운 패턴이 추가되면 해당 스킬의 SKILL.md를 업데이트하세요.
