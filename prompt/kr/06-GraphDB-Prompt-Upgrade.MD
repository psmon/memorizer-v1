
이 프로젝트는 MCP-Memorizer로  LLM Agent가 필요한 지식을 제공하는 프로젝트입니다.
posggres DB에 저장된 메모리를 Neo4j 그래프 데이터베이스로 동기화하고, LLM을 활용해 관계를 생성하는 기능을 추가하는 프로젝트입니다.
다음 지침을 준수해 프로젝트 업데이트를 진행합니다.


# 지침
- ui/graph 엔드포인트는 graph DB를 조회검색하는 페이지입니다. 이 페이지에서 검색기능을 개선하려고 합니다.
 - 검색기능과 관련 API는 미인증으로도 검색조회가 가능해야합니다. 검색 API에서 인증제약이 있다면 제거해주세요
 - Cypher Query에는 다소 긴문장의 쿼리가 입력될수 있음으로 한줄이아닌 멀티라인으로 입력가능하도록 개선합니다.
 - Cypher Query는 다소 복잡할수 있음으로 쿼리작성시 도움을 받을수 있도록, 자주사용되는 쿼리 예제를 드롭다운으로 선택해 입력가능하도록 개선합니다. 
 - Natural 은 자연어로 검색을 수행하는 기능입니다. LLM이 자연어를 cypher 쿼리로 변환해 graph db 검색해야하지만 , LLM 프롬프트가 적절하지 않아 올바른 쿼리가 생성되지 않습니다. 프롬프트를 개선해 자연어로 검색이 가능하도록 합니다.
 - Natural 로 검색시도되는 쿼리샘플도 도움을 받을수 있게 예제를 드롭다운으로 선택가능하게합니다 , GrapgDB의 Relation예제로 예제는 영문샘플로 작성합니다. 
 - Natural 검색수행후 이용된 Cypher Query도 함께 UI 표시합니다. 
 - Cypher Query UX로 시도되는 쿼리결과와 GraphView가 따로 별개 작동합니다. GraphView가 CyperQuery 시도된 결과모드와 호환되도록 수정, Limit 옵션과도 호환되도록 개선합니다. 쿼리 샘플에서는 Limit이 없어도됩니다.
- MCP에 이미 탑재된 search-graph 기능도 개선된 버전으로 적용합니다.


# 소스위치
- "src/Memorizer/ 에 프로젝트 파일일 있습니다.
- "src/Memorizer.IntegrationTests/" 에 유닛테스트 파일이 있습니다.
- postgres 스키마는 src/Memorizer/migrations에 있습니다.


# 코드완성후 테스트 지침
- postgres 에 원본 메모리데이터가 있습니다. 이것은 유지하고~ neo4j graph db는 초기화후 다시 동기화 테스트를 수행합니다. api/graph/sync와 관련된 구현된 코드를 참고
- 코드작성이 모두 완료되면 유닛테스트를 수행하면서, 오류및 잘못작동되는 부분을 개선해갑니다. 자연어를 cypher 쿼리로 변환하는 부분은 LLM이 활용됨으로 올바른 cypher 쿼리가 생성되는지 확인을하면서 프롬프트도 개선해갑니다.
- 유닛테스트가 완료되면 local port 5012로 구동해(이미구동중인경우 재실행)... 여기선 개선된 내용으로만 api test를 수행합니다. postgres,ollama,neo4j 등은 이미 구동되어 있으며 docker ps 를 통해 구동여부및 포트를 확인해 이용할수 있습니다. 
- 로컬에서 프로그램업데이트후 netcore 5012 재시작시 이전에 작동된 port가 해제안되 재대로 구동안될수 있습니다. 재시작시 어플리케이션이 잘작동되는지 먼저 확인합니다. 필요하면 5012에 한해서 강력한 포트kill을 사용 
- 최종 테스트가 완성되면... GRAPH.md에  이 프로젝트에서 유용한  CypherQuery 예제를 정리합니다. 작동가능한 유용한 샘플이여야합니다.


# 이 프로젝트 GraphDB Data for Memory 구조
neo4j 그래프 데이터베이스를 이용하며  Memory, Word 노드와 관계에 대한 설명입니다.


## Node : Memory Properties

핵심 데이터로 Memory Content를 다룹니다. 이 내용을 먼저 파악하면 개선시 도움이 됩니다.

```
<elementId>: 4:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:90
<id>: 90
confidence: 1.0
createdAt: 2025-08-22T01:31:57.6292047Z
id: 8f02c0f0-bb7e-42e5-aa99-9904f4d9bcb8
source: LLM
summary: # SSE-PUSH-SYSTEM - Server-Sent Events 실시간 이벤트 푸시 시스템

SSE(Server-Sent Events)를 활용한 실시간 이벤트 푸시 시스템으로, 토픽 기반의 pub/sub 패턴을 구현하여 클라이언트가 특정 토픽을 구독하고 실시간으로 이벤트를 수신할 수 있습니다.

## 기술 스택
- **언어**: Kotlin 2.0.0...
tags: sse,server-sent-events,real-time,pub-sub,spring-webflux,pekko,actor-model,kotlin,reactive
title: SSE-PUSH-SYSTEM - Server-Sent Events 실시간 이벤트 푸시 시스템
type: reference
```

## Node : Word Properties

Memory Content 로 부터 추출된 Tag를 다루며 Word Node로 관리합니다.

```
<elementId>: 4:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:15
<id>: 15
createdAt: 2025-08-21T17:16:37.6302691Z
frequency: 3
language: en
name: reactive-streams
updatedAt: 2025-08-22T01:31:56.8380793Z
```

## Relationship : HAS_KEYWORD

Memory -> HAS_KEYWORD -> Word 관계 연결정보가 있습니다.
Memory 가 가진 Word 만큼 Tag정보를 가진것과 동일합니다.


```
<elementId>: 5:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:13
<id>: 13
createdAt: 2025-08-21T17:16:37.6378220Z
relevance: 1.0
```


## Relationship : RLELATES_TO

Memory-> RLELATES_TO -> Memory 관계 연결정보가 있습니다.
다양한 type 유형으로 연관문서를 관리할때 사용합니다.

```
<elementId>: 5:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:189
<id>: 189
createdAt: 2025-08-22T01:54:37.9541646Z
type: enhanced-version
weight: 1.0
```