namespace Memorizer.Services;

/// <summary>
/// LLM prompts for PRD Maker feature
/// Analyzes PRD documents using Event Storming and Example Mapping methodologies
/// </summary>
public static class PrdMakerPrompts
{
    /// <summary>
    /// Generate Event Storming analysis from PRD content
    /// </summary>
    public static string GetEventStormingPrompt(string prdContent)
    {
        return $@"당신은 DDD(Domain-Driven Design) 전문가이자 이벤트 스토밍 퍼실리테이터입니다.
주어진 PRD(Product Requirements Document)를 분석하여 이벤트 스토밍 결과를 도출해주세요.

## PRD 내용
{prdContent}

## 분석 과제
위 PRD를 기반으로 DDD 원칙에 따른 이벤트 스토밍을 수행해주세요.
비즈니스 도메인의 흐름 순서에 따라 결과를 정리해주세요.

## 응답 형식 (Markdown)

### 📋 도메인 개요
PRD에서 파악된 핵심 비즈니스 도메인과 목표를 2-3문장으로 요약.

### 🎯 이벤트 (Events)
비즈니스 도메인에서 발생하는 주요 이벤트들을 시간 순서대로 나열.
| 순서 | 이벤트명 | 설명 |
|------|----------|------|
| 1 | [이벤트명] | [이벤트 발생 조건 및 결과] |

### ⚡ 명령 (Commands)
이벤트를 발생시키는 명령들.
| 명령명 | 트리거 이벤트 | 설명 |
|--------|--------------|------|
| [명령명] | [결과 이벤트] | [명령 설명] |

### 👤 액터 (Actors)
시스템과 상호작용하는 주체들.
- **[액터명]**: [역할 설명]

### 📜 정책/조건/제약 사항 (Policies)
비즈니스 규칙과 제약 조건들.
| 정책명 | 조건 | 결과 |
|--------|------|------|
| [정책명] | [조건] | [결과/행동] |

### 🏗️ 애그리거트/바운디드 컨텍스트
도메인 모델의 경계와 애그리거트.

```mermaid
graph TB
    subgraph BC1[Bounded Context 1]
        AG1[Aggregate 1]
        AG2[Aggregate 2]
    end
    subgraph BC2[Bounded Context 2]
        AG3[Aggregate 3]
    end
    BC1 --> BC2
```

### 📊 이벤트 플로우 다이어그램
```mermaid
sequenceDiagram
    participant Actor
    participant System
    Actor->>System: Command
    System-->>Actor: Event
```

**중요: Mermaid 다이어그램 작성 규칙**
- 다이어그램 내부의 노드명, 레이블, subgraph 이름은 반드시 **영문**으로 작성
- 괄호 () 대신 대괄호 [] 사용
- 특수문자 사용 금지
- 한글 설명은 다이어그램 외부에 별도로 작성

**중요: 반드시 한국어로 응답하세요. 모든 분석 결과는 한국어로 작성해주세요.**";
    }

    /// <summary>
    /// Generate virtual collaborator discussion for Example Mapping
    /// </summary>
    public static string GetExampleMappingDiscussionPrompt(string prdContent, string eventStormingResult)
    {
        return $@"당신은 소프트웨어 개발팀의 가상 협업 시뮬레이터입니다.
이벤트 스토밍 결과를 바탕으로 예제 맵핑을 위한 팀 토론을 시뮬레이션해주세요.

## 원본 PRD
{prdContent}

## 이벤트 스토밍 결과
{eventStormingResult}

## 가상 협업자 역할
다음 역할의 팀원들이 토론에 참여합니다:
- **PM (Product Manager)**: 비즈니스 요구사항과 사용자 관점 대변
- **Dev (개발자)**: 기술적 실현 가능성과 구현 관점
- **QA (품질 담당자)**: 테스트 시나리오와 엣지 케이스 발견
- **UX (UX 디자이너)**: 사용자 경험과 인터랙션 관점

## 토론 형식 (Markdown)

### 🗣️ 예제 맵핑 토론

각 주요 사용자 스토리에 대해 팀 토론을 진행합니다.

---
#### 스토리 1: [스토리 제목]
> [사용자 스토리 설명]

**💬 토론 진행**

**PM**: [비즈니스 관점에서의 의견이나 질문]

**Dev**: [기술적 관점에서의 의견이나 우려사항]

**QA**: [테스트 관점에서 발견한 엣지 케이스나 질문]

**UX**: [사용자 경험 관점에서의 의견]

**📝 도출된 예제**
1. [구체적인 사용 예제 시나리오]
2. [또 다른 예제 시나리오]

**📐 발견된 규칙**
- [비즈니스 규칙 1]
- [비즈니스 규칙 2]

**❓ 미해결 질문**
- [추가 확인이 필요한 사항]

---
#### 스토리 2: [다음 스토리 제목]
...

(각 주요 스토리에 대해 동일한 형식으로 토론 진행)

### 💡 토론 요약
토론을 통해 도출된 핵심 인사이트와 다음 단계에서 고려해야 할 사항들.

**중요: 반드시 한국어로 응답하세요. 자연스러운 대화체로 토론을 표현해주세요.**";
    }

    /// <summary>
    /// Generate final Example Mapping result
    /// </summary>
    public static string GetExampleMappingResultPrompt(string prdContent, string eventStormingResult, string discussionResult)
    {
        return $@"당신은 예제 맵핑 전문가입니다.
이전 분석 결과와 토론 내용을 바탕으로 최종 예제 맵핑을 완성해주세요.

## 원본 PRD
{prdContent}

## 이벤트 스토밍 결과
{eventStormingResult}

## 팀 토론 내용
{discussionResult}

## 예제 맵핑 완성 과제
토론 결과를 정리하여 체계적인 예제 맵핑 문서를 작성해주세요.

## 응답 형식 (Markdown)

### 📑 예제 맵핑 결과

---
#### 📌 스토리: [스토리 제목]
> **사용자 스토리**: [As a... I want... So that... 형식 또는 한국어 서술]

**✅ 규칙 (Rules)**
| 규칙 ID | 규칙 설명 | 관련 예제 |
|---------|----------|----------|
| R1 | [비즈니스 규칙] | E1, E2 |

**📋 예제 (Examples)**
| 예제 ID | 시나리오 | Given | When | Then |
|---------|---------|-------|------|------|
| E1 | [시나리오명] | [초기 상태] | [행동] | [예상 결과] |
| E2 | [시나리오명] | [초기 상태] | [행동] | [예상 결과] |

**❓ 의문점 (Questions)**
- [ ] [해결이 필요한 질문 1]
- [ ] [해결이 필요한 질문 2]

---
(각 스토리에 대해 동일한 형식으로 작성)

### 🔗 스토리 간 의존성
```mermaid
graph LR
    S1[Story 1] --> S2[Story 2]
    S1 --> S3[Story 3]
```

**중요: Mermaid 다이어그램 작성 규칙**
- 다이어그램 내부의 노드명, 레이블은 반드시 **영문**으로 작성
- 괄호 () 대신 대괄호 [] 사용
- 특수문자 사용 금지

### 📊 전체 요약

| 항목 | 수량 |
|------|------|
| 총 스토리 수 | N개 |
| 총 규칙 수 | N개 |
| 총 예제 수 | N개 |
| 미해결 질문 | N개 |

### 🎯 다음 단계 권장사항
1. [권장 사항 1]
2. [권장 사항 2]

**중요: 반드시 한국어로 응답하세요. 실무에서 바로 활용할 수 있는 형태로 작성해주세요.**";
    }

    /// <summary>
    /// Generate refined PRD based on all analysis results
    /// </summary>
    public static string GetRefinedPrdPrompt(string prdContent, string eventStormingResult, string discussionResult, string exampleMappingResult)
    {
        return $@"당신은 PRD(Product Requirements Document) 전문가입니다.
이벤트 스토밍과 예제 맵핑을 통해 도출된 인사이트를 바탕으로, 원본 PRD를 보완하여 더 완성도 높은 PRD를 작성해주세요.

## 원본 PRD
{prdContent}

## 이벤트 스토밍 결과
{eventStormingResult}

## 팀 토론 내용
{discussionResult}

## 예제 맵핑 결과
{exampleMappingResult}

## 보완된 PRD 작성 과제
위의 분석 결과들을 종합하여, 원본 PRD의 부족한 부분을 보완하고 더 구체적이고 실행 가능한 PRD를 작성해주세요.

## 응답 형식 (Markdown)

# [프로젝트명] PRD (보완본)

## 1. 개요
### 1.1 프로젝트 목표
[이벤트 스토밍에서 파악한 핵심 비즈니스 목표를 반영]

### 1.2 주요 이해관계자
[예제 맵핑 토론에서 도출된 액터들]

## 2. 범위
### 2.1 포함 범위 (In-Scope)
[바운디드 컨텍스트를 기반으로 명확한 범위 정의]

### 2.2 제외 범위 (Out-of-Scope)
[분석 과정에서 식별된 제외 사항]

## 3. 기능 요구사항
### 3.1 [기능 영역 1]
#### 3.1.1 [상세 기능]
- **설명**: [기능 설명]
- **사용자 스토리**: As a [액터], I want [목표], so that [이유]
- **수용 조건**:
  - [ ] [예제 맵핑에서 도출된 구체적인 조건]
  - [ ] [Given-When-Then 형태의 조건]

### 3.2 [기능 영역 2]
...

## 4. 비기능 요구사항
### 4.1 성능 요구사항
[분석에서 도출된 성능 관련 제약사항]

### 4.2 보안 요구사항
[정책/규칙에서 도출된 보안 사항]

## 5. 비즈니스 규칙
| 규칙 ID | 규칙명 | 설명 | 관련 기능 |
|---------|--------|------|----------|
| BR-001 | [규칙명] | [예제 맵핑에서 도출된 규칙] | [관련 기능] |

## 6. 제약 사항
[이벤트 스토밍의 정책/제약사항 반영]

## 7. 용어 정의
| 용어 | 정의 |
|------|------|
| [도메인 용어] | [정의] |

## 8. 미해결 사항
- [ ] [예제 맵핑에서 도출된 미해결 질문]
- [ ] [추가 확인이 필요한 사항]

## 9. 시스템 구조 다이어그램

```mermaid
graph TB
    subgraph System[System Overview]
        subgraph BC1[Bounded Context 1]
            A1[Aggregate 1]
        end
        subgraph BC2[Bounded Context 2]
            A2[Aggregate 2]
        end
    end
    BC1 --> BC2
```

**중요: Mermaid 다이어그램 작성 규칙**
- 다이어그램 내부의 노드명, 레이블, subgraph 이름은 반드시 **영문**으로 작성
- 괄호 () 대신 대괄호 [] 사용
- 특수문자 사용 금지

---
*이 PRD는 이벤트 스토밍과 예제 맵핑 분석을 통해 보완되었습니다.*

**중요: 반드시 한국어로 응답하세요. 원본 PRD의 내용을 유지하면서 분석 결과를 반영하여 보완해주세요.**";
    }
}
