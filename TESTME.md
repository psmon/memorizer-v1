
# Memorizer Graph Local Test

```bash
docker-compose -f docker-compose.local.yml up -d --build
```

## Claude Cli Setting

```
claude mcp add memorizer http://localhost:5000/sse --transport sse --header "X-API-Key: your-api-key-here"

claude mcp list

claude mcp remove memorizer
```

## Test Prompt for MCPTOOL - KR

### Memorizer Store

- reactive stream의 역사와 akka진영의 기여도를 조사해 메모리화
- 닷넷진영의 액터모델 조사해 메모리화
- 자바진영에서 스칼라/자바/코틀린 typed actor이용할때 차이점과 특징

### Search Similarity Vector

- akka 관련 자료 메모리에서 검색
- stream,reactive 메모리검색

### Search Graph

- 메모리간 연결이 많은 문서를 높은순으로 메모리 그래프검색
- 단어(태그)가 가장 많이 언급된 문서 높은순 메모리 그래프검색


## Search Vector With Graph

- stream 으로 연관성 높은 메모리 검색을 먼저한후, 이 문서가 가진 단어(태깅)로 연관 메모리탐색해, 스트림 프로그래밍에대해 요약


## Test Prompt for MCPTOOL - EN

### Memorizer Store

- Investigate the history of Reactive Streams and Akka’s contributions; persist the findings to memory.
- Investigate the actor model in the .NET ecosystem; persist the findings to memory.

Within the JVM ecosystem, compare typed actors in Scala, Java, and Kotlin—highlight differences, strengths, and common use cases; persist the findings to memory.

### Search Similarity Vector

- Search memory for Akka-related materials.
- Search memory for the terms “stream” and “reactive.”

### Search Graph

- In the memory graph, return documents ordered by the number of inter-memory connections (highest first).
- In the memory graph, return documents ordered by tag/keyword frequency (highest first).

### Search Vector With Graph
- First, use vector similarity to find memories most related to “stream.” Then expand via the resulting documents’ tags/keywords to discover related memories in the graph, and produce a concise summary.
