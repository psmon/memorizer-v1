namespace Memorizer.Services;

/// <summary>
/// LLM prompts for PRD Maker feature
/// Analyzes PRD documents using Event Storming and Example Mapping methodologies
/// </summary>
public static class PrdMakerPrompts
{
    /// <summary>
    /// Extract a search keyword from Event Storming result for memory search
    /// </summary>
    public static string GetSearchKeywordExtractionPrompt(string eventStormingResult)
    {
        return $@"당신은 키워드 추출 전문가입니다.
아래 이벤트 스토밍 결과를 분석하여 관련 기술이나 도메인 지식을 검색하기 위한 핵심 키워드를 추출해주세요.

## 이벤트 스토밍 결과
{eventStormingResult}

## 규칙
- 반드시 20자 이내의 핵심 검색어 1개만 출력
- 기술 용어, 도메인 개념, 패턴명 등 검색에 유용한 키워드 선택
- 한국어 또는 영어 모두 가능
- 키워드만 출력 (설명, 따옴표, 접두사 없이)

키워드:";
    }

    /// <summary>
    /// Evaluate if memory is useful for discussion
    /// </summary>
    public static string GetMemoryUsefulnessPrompt(string eventStormingResult, string memoryTitle, string memoryContent)
    {
        return $@"당신은 기술 문서 평가 전문가입니다.
아래 이벤트 스토밍 결과를 기반으로 한 예제 맵핑 토론에서 참고 자료가 유용한지 판단해주세요.

## 이벤트 스토밍 결과 (요약)
{eventStormingResult.Substring(0, Math.Min(eventStormingResult.Length, 1000))}

## 검색된 메모리
제목: {memoryTitle}
내용: {memoryContent.Substring(0, Math.Min(memoryContent.Length, 500))}

## 판단 기준
- 이벤트 스토밍에서 도출된 도메인/기술과 관련이 있는가?
- 예제 맵핑 토론에서 구체적인 인사이트를 제공할 수 있는가?
- 비즈니스 규칙, 기술 패턴, 구현 사례 등 실질적인 정보가 있는가?

## 응답 형식
유용함 또는 유용하지않음 중 하나만 출력하세요.

판단:";
    }

    /// <summary>
    /// Generate virtual collaborator discussion for Example Mapping with memory reference
    /// </summary>
    public static string GetExampleMappingDiscussionWithMemoryPrompt(string prdContent, string eventStormingResult, string memoryReferences)
    {
        return $@"당신은 소프트웨어 개발팀의 가상 협업 시뮬레이터입니다.
이벤트 스토밍 결과를 바탕으로 예제 맵핑을 위한 팀 토론을 시뮬레이션해주세요.
이번 토론에는 메모리즈(Memoriz)라는 AI 참고자료 제공자가 참여합니다.

## 원본 PRD
{prdContent}

## 이벤트 스토밍 결과
{eventStormingResult}

## 참고 자료 (메모리즈 제공)
{memoryReferences}

## 가상 협업자 역할
다음 역할의 팀원들이 토론에 참여합니다:
- **PM (Product Manager)**: 비즈니스 요구사항과 사용자 관점 대변
- **Dev (개발자)**: 기술적 실현 가능성과 구현 관점
- **QA (품질 담당자)**: 테스트 시나리오와 엣지 케이스 발견
- **UX (UX 디자이너)**: 사용자 경험과 인터랙션 관점
- **메모리즈 (Memoriz)**: 관련 기술 문서, 도메인 지식, 사례를 참고하여 인사이트 제공

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

**메모리즈**: [참고 자료를 바탕으로 한 기술적 인사이트, 유사 사례, 주의사항 등. 반드시 제공된 참고 자료의 내용을 활용하여 구체적인 조언 제공]

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

### 📚 참고 자료 활용 요약
메모리즈가 제공한 참고 자료가 어떻게 토론에 기여했는지 간략히 정리.

**중요: 반드시 한국어로 응답하세요. 자연스러운 대화체로 토론을 표현해주세요. 메모리즈는 반드시 제공된 참고 자료 내용을 구체적으로 언급해야 합니다.**";
    }

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

    /// <summary>
    /// Generate Bounded Context definition based on all analysis results
    /// </summary>
    public static string GetBoundedContextPrompt(string prdContent, string eventStormingResult, string exampleMappingResult, string refinedPrdResult)
    {
        return $@"당신은 DDD(Domain-Driven Design) 전문가이자 시스템 아키텍트입니다.
이전 분석 결과들을 종합하여 바운디드 컨텍스트(Bounded Context)를 정의해주세요.

## 원본 PRD
{prdContent}

## 이벤트 스토밍 결과
{eventStormingResult}

## 예제 맵핑 결과
{exampleMappingResult}

## 보완된 PRD
{refinedPrdResult}

## 바운디드 컨텍스트 정의 과제
위의 분석 결과들을 종합하여 DDD 원칙에 따른 바운디드 컨텍스트를 정의해주세요.

## 분석 기준 (반드시 모든 기준을 적용하여 분석)

### 1. 서브도메인 분해 (Strategic DDD)
- **Core Domain**: 경쟁우위/차별화 영역 → 모델을 깊게, 독립적으로 설계
- **Supporting Domain**: 핵심을 돕는 업무 → 비교적 단순/유연하게 설계
- **Generic Domain**: 범용 기능(로그인, 알림 등) → 패키지/외부 서비스 고려

### 2. 유비쿼터스 언어(용어) 충돌
- 같은 단어가 팀/업무에 따라 의미가 다르면 BC 분리 신호
- 예: ""정산""이 결제팀, 판매팀, 회계팀에서 다른 의미로 사용되는 경우

### 3. 변경 이유 기준
- 규칙/정책이 바뀌는 축이 다르면 BC 분리 후보
- 할인 정책이 자주 바뀌는 영역 vs 주문 규칙이 안정적인 영역

### 4. 트랜잭션/일관성 경계
- 한 번에 같이 저장/롤백되어야 하는 것은 같은 BC
- 도메인 이벤트 + 최종적 일관성으로 분리 가능한 영역

### 5. 데이터 소유권 (Write Model)
- ""누가 이 데이터를 쓴다(write)?""를 기준으로 소유 BC 결정
- 다른 BC는 조회(read model) 또는 이벤트/ACL로 접근

### 6. 조직/팀 경계 (Conway's Law)
- 팀이 다르면 BC도 분리되는 것이 자연스러울 수 있음
- 단, 결합도(변경/트랜잭션/용어)가 함께 맞을 때만 분리 확정

## 응답 형식 (Markdown)

# 바운디드 컨텍스트 정의

## 1. 서브도메인 분석

### 1.1 Core Domain
| 서브도메인 | 설명 | 경쟁우위 요소 |
|-----------|------|--------------|
| [서브도메인명] | [설명] | [차별화 포인트] |

### 1.2 Supporting Domain
| 서브도메인 | 설명 | 지원 대상 |
|-----------|------|----------|
| [서브도메인명] | [설명] | [Core 중 지원 대상] |

### 1.3 Generic Domain
| 서브도메인 | 설명 | 구현 전략 |
|-----------|------|----------|
| [서브도메인명] | [설명] | [자체 구현/외부 서비스/패키지] |

## 2. 바운디드 컨텍스트 도출

### BC 1: [컨텍스트명]
- **서브도메인 유형**: [Core/Supporting/Generic]
- **핵심 책임**: [이 BC가 담당하는 주요 업무]
- **주요 애그리거트**: [애그리거트 목록]
- **데이터 소유권**: [Write하는 주요 엔티티]
- **유비쿼터스 언어**: [이 컨텍스트 내 핵심 용어와 정의]

### BC 2: [컨텍스트명]
...

(각 BC에 대해 동일한 형식으로 작성)

## 3. 컨텍스트 맵 (Context Map)

### 3.1 컨텍스트 관계 다이어그램
```mermaid
graph TB
    subgraph Core[Core Domain]
        BC1[Bounded Context 1]
        BC2[Bounded Context 2]
    end
    subgraph Supporting[Supporting Domain]
        BC3[Bounded Context 3]
    end
    subgraph Generic[Generic Domain]
        BC4[Bounded Context 4]
    end

    BC1 -->|Upstream/Downstream| BC2
    BC1 -->|ACL| BC3
    BC3 -->|Event| BC4
```

### 3.2 컨텍스트 관계 상세
| 상류 BC | 하류 BC | 관계 패턴 | 설명 |
|---------|---------|----------|------|
| [상류 BC] | [하류 BC] | [U/D, ACL, OHS, PL, CF 등] | [관계 설명] |

**관계 패턴 설명**:
- **U/D (Upstream/Downstream)**: 상류가 하류에 영향을 줌
- **ACL (Anti-Corruption Layer)**: 하류가 상류 모델 변환층 사용
- **OHS (Open Host Service)**: 상류가 공개 API 제공
- **PL (Published Language)**: 공유 언어/스키마 사용
- **CF (Conformist)**: 하류가 상류 모델을 그대로 수용

## 4. 이벤트 흐름 다이어그램
```mermaid
sequenceDiagram
    participant BC1 as Context 1
    participant BC2 as Context 2
    participant BC3 as Context 3

    BC1->>BC2: DomainEvent1
    BC2->>BC3: DomainEvent2
```

## 5. BC 분리 근거 체크리스트

| BC명 | 언어 충돌 | 변경 이유 분리 | 팀 분리 | 트랜잭션 독립 | 외부 연동 | 데이터 소유권 | 총점 |
|------|----------|--------------|--------|--------------|----------|-------------|------|
| [BC명] | ✓/✗ | ✓/✗ | ✓/✗ | ✓/✗ | ✓/✗ | ✓/✗ | N/6 |

*2-3개 이상 해당 시 분리 후보로 강력 권장*

## 6. 주의사항 및 권장사항

### 6.1 흔한 실수 회피
- [ ] DB 테이블 기준으로 자르지 않았는지 확인
- [ ] 너무 미세하게 쪼개지 않았는지 확인
- [ ] 공유 모델을 강요하지 않았는지 확인

### 6.2 구현 권장사항
- [구체적인 권장 사항들]

### 6.3 다음 단계
- [향후 개발 진행 시 고려사항]

**중요: Mermaid 다이어그램 작성 규칙**
- 다이어그램 내부의 노드명, 레이블, subgraph 이름은 반드시 **영문**으로 작성
- 괄호 () 대신 대괄호 [] 사용
- 특수문자 사용 금지
- 한글 설명은 다이어그램 외부에 별도로 작성

**중요: 반드시 한국어로 응답하세요. 실무에서 바로 활용할 수 있는 구체적인 바운디드 컨텍스트 정의를 작성해주세요.**";
    }
}
