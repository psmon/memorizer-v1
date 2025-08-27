
이 프로젝트는 MCP-Memorizer로  LLM Agent가 필요한 지식을 제공하는 프로젝트입니다.
posggres DB에 저장된 메모리를 Neo4j 그래프 데이터베이스로 동기화하고, LLM을 활용해 관계를 생성하는 기능을 추가하는 프로젝트입니다.

다음 지침을 준수해 프로젝트 업데이트를 진행합니다.

# 지침
- ui/graph 엔드포인트는 graph DB를 조회검색하는 페이지입니다. 이 페이지에서 검색기능과 표현기능을 개선하려고 합니다.
 -Node(Memory,Word) Relationship ( HAS_KEYWORD , RLELATES_TO) RLELATES_TO 일때는 연결유형(type) 이 잘 표시되게 Node 검색시 Node(Memory)->RLELATES_TO->NODE(Memory) , Node(Memory)->HAS_KEYWORD->NODE(WORD) 간 연결성도 풍부하게 표현되도록 llm을 통한 graph 탐색기능 응답을 개선합니다.
 - 조회 페이지에서도 연결성을 풍부하게 표현할수 있도록 개선합니다.  
 - MCP에 이미 탑재된 search-graph 기능도 개선된 버전으로 적용합니다.
 - 메모리 노드의 summary 는 컨텐트의 내용으로, 메모리노드 클릭후 작동하는 뷰에서 마크다운 style을 볼수 있는 뷰로 컨텐츠를 볼수 있는 뷰기능을 추가합니다.
 - 코드완성후 테스트 지침을 추가로 수행해주세요

# 소스위치
- "src/Memorizer/ 에 프로젝트 파일일 있습니다.
- "src/Memorizer.IntegrationTests/" 에 유닛테스트 파일이 있습니다.
- postgres 스키마는 src/Memorizer/migrations에 있습니다.

# 테스트 지침
- postgres 에 원본 메모리데이터가 있습니다. 이것은 유지하고~ neo4j graph db는 초기화후 다시 동기화 테스트를 수행합니다. api/graph/sync와 관련된 구현된 코드를 참고
- 코드작성이 모두 완료되면 유닛테스트를 수행하면서, 오류및 잘못작동되는 부분을 개선해갑니다. 자연어를 cypher 쿼리로 변환하는 부분은 LLM이 활용됨으로 올바른 cypher 쿼리가 생성되는지 확인을하면서 프롬프트도 개선해갑니다.
- 유닛테스트가 완료되면 local port 5012로 로컬로 구동해(이미구동중인경우 재실행) 여기선 개선된 내용을 중심으로 api test를 수행합니다. 
- postgres,ollama,neo4j 등은 이미 구동되어 있으며 docker ps 를 통해 구동여부및 포트를 확인해 이용할수 있습니다. 접속정보는 docker-compose.local.yml 를 참고합니다.
- 어플리케이션 재구동은 반복테스트시 빌드및 재수행 오버헤드가 있을수 있습니다. 코드변경시 HotReload 기능을 최대한 활용해 재시작없이 수행확인합니다. 변경기능이 갱신 되지 않았다 판단될때만 kill을 이용 재수행합니다.
- 로컬 구동은 dotnet run 으로 5012 port에서 수행..HotReload 기능을 netcore9 에서 지원하는 기능을 활용해 재시작을 최소화할것..
- Hotreload 는 net9 에서 지원하는지 조사후 진행  
- 최종 테스트가 완성되면... GRAPH.md에  이 프로젝트에서 유용한  CypherQuery 예제를 정리합니다. 기존 가이드가 있음으로, 추가된기능으로 업데이트된 사항이있다고하면  작동가능한 유용한 샘플로 업데이트합니다.

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

## 최종완료후 후속 개선지침 - Phase1

GetGraphVisualizationAsync 함수에서 limit이 걸려있어서  Word만 조회된 한경우 Memory가 없어서 연결관계가 이후 UX로 되지않으며 Memory가 없게됩니다.
Memory 만 100개 Limit을 걸고 , Memory가 가진 Word를 표현한후... Relation 처리를 후속으로 진행 해주세요  추가로 Word표현되는 크기가 작은데 약간더 크게해~ Memory보다는 크지않게 표현해주세요
Relation기능은 잘작동해 개선필요없으며 또한~ Memory개수 Limit 기본값을 20개로 조정해주세요

