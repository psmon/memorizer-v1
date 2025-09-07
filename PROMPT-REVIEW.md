# Memorizer Prompt Management - Review Document

## Executive Summary
This document outlines the improvements made to centralize and enhance LLM prompt management in the Memorizer application. All LLM prompts have been consolidated into a single location (`PromptTemplates.cs`) for better maintainability and consistency.

## Improvements Completed

### 1. Centralized Prompt Management
**Location**: `src/Memorizer/Prompts/PromptTemplates.cs`

All LLM prompts are now managed in a single file with the following structure:
- System messages as constants
- Prompt generation methods with clear parameters
- Consistent formatting and structure

### 2. Prompts Consolidated

#### Title Generation
- **System Message**: `TitleGenerationSystemMessage`
- **Method**: `CreateTitleGenerationPrompt()`
- **Used By**: All LLM services (OpenAI, Ollama, Custom)
- **Purpose**: Generate descriptive titles for memories

#### Keyword Extraction
- **System Message**: `KeywordExtractionSystemMessage`
- **Method**: `CreateKeywordExtractionPrompt()`
- **Used By**: All LLM services
- **Purpose**: Extract relevant keywords from content

#### Graph Query Generation
- **Method**: `CreateGraphQueryPrompt()`
- **Used By**: GraphSearchService
- **Purpose**: Convert natural language queries to Neo4j Cypher queries
- **Enhancement**: Comprehensive prompt with extensive examples and detailed schema documentation

#### Relationship Analysis
- **Method**: `CreateRelationshipAnalysisPrompt()`
- **Used By**: GraphSyncService
- **Purpose**: Analyze memories and suggest relationships
- **Enhancement**: Structured JSON response format with confidence scoring

#### Health Check
- **System Message**: `HealthCheckSystemMessage`
- **Used By**: OpenAILlmService (and potentially others)
- **Purpose**: Simple health check for LLM connectivity

## Files Modified

1. **src/Memorizer/Prompts/PromptTemplates.cs**
   - Replaced unused `CreateGraphQueryPrompt` with comprehensive version
   - Replaced unused `CreateRelationshipSuggestionPrompt` with `CreateRelationshipAnalysisPrompt`
   - Added `HealthCheckSystemMessage` constant
   - Removed duplicate `GraphQuerySystemMessage`

2. **src/Memorizer/Services/GraphSearchService.cs**
   - Removed 90+ lines of hardcoded prompt
   - Now uses `PromptTemplates.CreateGraphQueryPrompt()`

3. **src/Memorizer/Services/GraphSyncService.cs**
   - Removed hardcoded relationship analysis prompt
   - Now uses `PromptTemplates.CreateRelationshipAnalysisPrompt()`

4. **src/Memorizer/Services/OpenAILlmService.cs**
   - Now uses `PromptTemplates.HealthCheckSystemMessage` for health checks

## Benefits Achieved

### Maintainability
- Single source of truth for all prompts
- Easier to update and improve prompts
- Consistent prompt structure across services

### Code Quality
- Reduced code duplication
- Better separation of concerns
- Cleaner service implementations

### Prompt Quality
- Comprehensive Cypher query generation with 30+ examples
- Structured relationship analysis with confidence scoring
- Consistent JSON response formats

## Prompt Inventory

| Prompt | Purpose | Services Using | Status |
|--------|---------|---------------|---------|
| Title Generation | Generate titles for memories | All LLM Services | ✅ Active |
| Keyword Extraction | Extract keywords from content | All LLM Services | ✅ Active |
| Graph Query | Convert natural language to Cypher | GraphSearchService | ✅ Active |
| Relationship Analysis | Analyze memory relationships | GraphSyncService | ✅ Active |
| Health Check | Test LLM connectivity | OpenAILlmService | ✅ Active |

## Testing Results
- Application builds successfully with Docker
- Graph search functionality tested and working
- Cypher query generation producing valid queries
- All services starting without errors

## Future Recommendations

1. **Prompt Versioning**: Consider adding version tracking for prompts to manage changes over time
2. **Prompt Configuration**: Move prompts to configuration files for easier updates without recompilation
3. **Prompt Testing**: Add unit tests for prompt generation methods
4. **Prompt Analytics**: Track prompt performance and effectiveness
5. **Multi-language Support**: Consider internationalization for prompts if needed

## Conclusion
The prompt management system has been successfully centralized and improved. All scattered prompts have been consolidated into `PromptTemplates.cs`, making the system more maintainable and consistent. The application has been tested and verified to work correctly with the new centralized prompts.

## Appendix: Actual Prompts and Concepts

### 1. Title Generation Prompt

#### English Version
**System Message:**
```
You are an expert at creating concise, descriptive titles for various types of content.
```

**Prompt Structure:**
```
TASK: Generate a clear, descriptive title for the provided content that captures its main topic and purpose.

GUIDELINES:
- Maximum title length: {maxTitleLength} characters
- Make it descriptive and searchable
- Capture the main topic or purpose
- Use natural language, avoid generic phrases
- Consider the content type and existing tags for context

CONTENT DETAILS:
- Type: {contentType}
- Tags: {tags}
- Length: {content.Length} characters

CONTENT TO ANALYZE:
```{content}```

RESPOND WITH VALID JSON in this exact format:
{
  "title": "Generated title here",
  "reasoning": "Brief explanation of why this title was chosen"
}
```

#### Korean Translation (한글 번역)
**시스템 메시지:**
```
당신은 다양한 유형의 콘텐츠에 대해 간결하고 설명적인 제목을 만드는 전문가입니다.
```

**프롬프트 구조:**
```
작업: 제공된 콘텐츠의 주요 주제와 목적을 포착하는 명확하고 설명적인 제목을 생성하세요.

가이드라인:
- 최대 제목 길이: {maxTitleLength} 문자
- 설명적이고 검색 가능하게 만들기
- 주요 주제나 목적 포착
- 자연스러운 언어 사용, 일반적인 문구 피하기
- 콘텐츠 유형과 기존 태그를 컨텍스트로 고려

콘텐츠 세부사항:
- 유형: {contentType}
- 태그: {tags}
- 길이: {content.Length} 문자

분석할 콘텐츠:
```{content}```

다음 형식의 유효한 JSON으로 응답:
{
  "title": "생성된 제목",
  "reasoning": "이 제목을 선택한 이유에 대한 간단한 설명"
}
```

### 2. Keyword Extraction Prompt

#### English Version
**System Message:**
```
You are an expert at extracting meaningful keywords from text content.
```

**Prompt Structure:**
```
TASK: Extract the most important and relevant keywords from the provided content.

GUIDELINES:
- Extract up to {maxKeywords} keywords
- Focus on technical terms, concepts, and significant entities
- Include both single words and meaningful multi-word phrases
- Prioritize domain-specific terminology
- Normalize keywords to lowercase
- Avoid common stop words unless they are part of technical terms
- Consider the content type for appropriate keyword selection

CONTENT DETAILS:
- Type: {contentType}
- Length: {content.Length} characters

CONTENT TO ANALYZE:
```{content}```

RESPOND WITH VALID JSON in this exact format:
{
  "keywords": ["keyword1", "keyword2", "keyword3"],
  "reasoning": "Brief explanation of keyword selection"
}
```

#### Korean Translation (한글 번역)
**시스템 메시지:**
```
당신은 텍스트 콘텐츠에서 의미 있는 키워드를 추출하는 전문가입니다.
```

**프롬프트 구조:**
```
작업: 제공된 콘텐츠에서 가장 중요하고 관련성 있는 키워드를 추출하세요.

가이드라인:
- 최대 {maxKeywords}개의 키워드 추출
- 기술 용어, 개념, 중요한 엔티티에 집중
- 단일 단어와 의미 있는 여러 단어 구문 모두 포함
- 도메인별 용어 우선순위 지정
- 키워드를 소문자로 정규화
- 기술 용어의 일부가 아닌 한 일반적인 불용어 피하기
- 적절한 키워드 선택을 위해 콘텐츠 유형 고려

콘텐츠 세부사항:
- 유형: {contentType}
- 길이: {content.Length} 문자

분석할 콘텐츠:
```{content}```

다음 형식의 유효한 JSON으로 응답:
{
  "keywords": ["키워드1", "키워드2", "키워드3"],
  "reasoning": "키워드 선택에 대한 간단한 설명"
}
```

### 3. Graph Query Generation Prompt

#### English Version (Excerpt)
```
You are a Neo4j Cypher query expert. Convert the following natural language query to a precise Cypher query.

## Graph Database Schema

### Nodes
1. **Memory Node**
   - id: string (UUID) - unique identifier
   - type: string - values: 'reference', 'how-to', 'system', 'conversation', 'document'
   - source: string - origin of memory (e.g., 'LLM', 'user', 'system')
   - title: string - descriptive title
   - summary: string - detailed content/description
   - tags: string[] - array of keyword tags
   - confidence: double (0.0-1.0) - confidence score
   - createdAt: string (ISO datetime) - creation timestamp

2. **Word Node**
   - name: string - the keyword/word
   - language: string - language code (e.g., 'en', 'ko')
   - frequency: int - usage frequency count
   - createdAt: string (ISO datetime)
   - updatedAt: string (ISO datetime)

### Relationships
1. **RELATES_TO** (Memory -> Memory)
   - type: string - relationship subtype
   - Common types: 'extends', 'enhanced-version', 'supports', 'contradicts', 'implements', 'references', 'related-to', 'example-of', 'explains'
   - weight: double (0.0-1.0) - relationship strength
   - createdAt: string (ISO datetime)

2. **HAS_KEYWORD** (Memory -> Word)
   - relevance: double (0.0-1.0) - keyword relevance to memory
   - createdAt: string (ISO datetime)

## Query Guidelines
1. Use case-insensitive matching with CONTAINS for text searches
2. Always include LIMIT clause (default 50 unless specified)
3. Return nodes and relationships when traversing paths
4. Use DISTINCT when necessary to avoid duplicates
5. For keyword searches, utilize the Word nodes and HAS_KEYWORD relationships
6. Consider multiple search patterns (title, summary, tags) for comprehensive results
7. Use WITH clauses for complex aggregations
8. Order results by relevance when applicable

## Natural Language Query: {naturalLanguageQuery}

[30+ detailed examples follow...]

## IMPORTANT INSTRUCTIONS
1. Return ONLY a valid Neo4j Cypher query
2. Do NOT include any explanations, comments, or markdown
3. Do NOT include backticks or code blocks
4. Do NOT include any text before or after the query
5. The response must be directly executable in Neo4j

Generate the Cypher query now:
```

#### Korean Translation (한글 번역 - 발췌)
```
당신은 Neo4j Cypher 쿼리 전문가입니다. 다음 자연어 쿼리를 정확한 Cypher 쿼리로 변환하세요.

## 그래프 데이터베이스 스키마

### 노드
1. **Memory 노드**
   - id: 문자열 (UUID) - 고유 식별자
   - type: 문자열 - 값: 'reference', 'how-to', 'system', 'conversation', 'document'
   - source: 문자열 - 메모리 출처 (예: 'LLM', 'user', 'system')
   - title: 문자열 - 설명적 제목
   - summary: 문자열 - 상세 콘텐츠/설명
   - tags: 문자열[] - 키워드 태그 배열
   - confidence: 실수 (0.0-1.0) - 신뢰도 점수
   - createdAt: 문자열 (ISO 날짜시간) - 생성 타임스탬프

2. **Word 노드**
   - name: 문자열 - 키워드/단어
   - language: 문자열 - 언어 코드 (예: 'en', 'ko')
   - frequency: 정수 - 사용 빈도 수
   - createdAt: 문자열 (ISO 날짜시간)
   - updatedAt: 문자열 (ISO 날짜시간)

### 관계
1. **RELATES_TO** (Memory -> Memory)
   - type: 문자열 - 관계 하위 유형
   - 일반적인 유형: 'extends', 'enhanced-version', 'supports', 'contradicts', 'implements', 'references', 'related-to', 'example-of', 'explains'
   - weight: 실수 (0.0-1.0) - 관계 강도
   - createdAt: 문자열 (ISO 날짜시간)

2. **HAS_KEYWORD** (Memory -> Word)
   - relevance: 실수 (0.0-1.0) - 메모리에 대한 키워드 관련성
   - createdAt: 문자열 (ISO 날짜시간)

## 쿼리 가이드라인
1. 텍스트 검색에는 대소문자 구분 없는 CONTAINS 사용
2. 항상 LIMIT 절 포함 (지정되지 않으면 기본값 50)
3. 경로 탐색 시 노드와 관계 반환
4. 중복 방지를 위해 필요시 DISTINCT 사용
5. 키워드 검색에는 Word 노드와 HAS_KEYWORD 관계 활용
6. 포괄적인 결과를 위해 다중 검색 패턴(제목, 요약, 태그) 고려
7. 복잡한 집계에는 WITH 절 사용
8. 해당되는 경우 관련성에 따라 결과 정렬

## 자연어 쿼리: {naturalLanguageQuery}

[30개 이상의 상세한 예제...]

## 중요 지침
1. 유효한 Neo4j Cypher 쿼리만 반환
2. 설명, 주석, 마크다운 포함 금지
3. 백틱이나 코드 블록 포함 금지
4. 쿼리 전후에 텍스트 포함 금지
5. 응답은 Neo4j에서 직접 실행 가능해야 함

지금 Cypher 쿼리를 생성하세요:
```

### 4. Relationship Analysis Prompt

#### English Version
```
Analyze the following memory and suggest relationships to other memories.
Source Memory:
Title: {sourceTitle}
Type: {sourceType}
Content: {sourceContent}

Candidate Memories:
1. Title: {title1}, Type: {type1}, Similarity: {similarity1}
2. Title: {title2}, Type: {type2}, Similarity: {similarity2}
...

For each relevant relationship, suggest a type from:
- extends (extends concepts)
- supports (provides supporting evidence)
- contradicts (presents opposing view)
- implements (practical implementation)
- references (direct reference)
- related-to (general relation)

Return as JSON array with format:
[{"targetIndex": 1, "type": "extends", "confidence": 0.8}]
```

#### Korean Translation (한글 번역)
```
다음 메모리를 분석하고 다른 메모리와의 관계를 제안하세요.
소스 메모리:
제목: {sourceTitle}
유형: {sourceType}
콘텐츠: {sourceContent}

후보 메모리:
1. 제목: {title1}, 유형: {type1}, 유사도: {similarity1}
2. 제목: {title2}, 유형: {type2}, 유사도: {similarity2}
...

각 관련 관계에 대해 다음 중에서 유형을 제안하세요:
- extends (개념 확장)
- supports (지원 증거 제공)
- contradicts (반대 견해 제시)
- implements (실용적 구현)
- references (직접 참조)
- related-to (일반 관계)

다음 형식의 JSON 배열로 반환:
[{"targetIndex": 1, "type": "extends", "confidence": 0.8}]
```

### 5. Health Check Prompt

#### English Version
```
You are a helpful assistant.
```

#### Korean Translation (한글 번역)
```
당신은 도움이 되는 어시스턴트입니다.
```

## Prompt Design Concepts

### Core Principles
1. **Structured Format**: All prompts follow a consistent structure with clear sections (TASK, GUIDELINES, CONTENT DETAILS, etc.)
2. **JSON Response Format**: Enforces structured output for reliable parsing
3. **Context Awareness**: Prompts include relevant context like content type, tags, and metadata
4. **Example-Driven**: Extensive examples for complex tasks (e.g., 30+ Cypher query examples)
5. **Constraint Definition**: Clear constraints like character limits and output formats
6. **Reasoning Capture**: Prompts request reasoning to understand LLM decision-making

### 핵심 원칙 (한글)
1. **구조화된 형식**: 모든 프롬프트는 명확한 섹션(작업, 가이드라인, 콘텐츠 세부사항 등)을 가진 일관된 구조를 따름
2. **JSON 응답 형식**: 안정적인 파싱을 위한 구조화된 출력 강제
3. **컨텍스트 인식**: 프롬프트에 콘텐츠 유형, 태그, 메타데이터 등 관련 컨텍스트 포함
4. **예제 중심**: 복잡한 작업을 위한 광범위한 예제 (예: 30개 이상의 Cypher 쿼리 예제)
5. **제약 조건 정의**: 문자 제한 및 출력 형식과 같은 명확한 제약 조건
6. **추론 캡처**: LLM 의사결정을 이해하기 위해 추론 요청