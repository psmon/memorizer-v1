
## 지침

다음 링크를 학습해 닷넷개발 환경에서 Neo4j를 액터모델과 함께 활용하는 개발듀토리얼을 지식화해 메모리 MCP에 탑재합니다.

- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/AkkaNeo4j.csproj
- https://github.com/psmon/AkkaNeo4j/blob/master/readme.MD
- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/Models/GraphEvent.cs
- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/Module/GraphEngine.cs
- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/Actors/GraphEventActor.cs



## 메모리 MCP활용방법
```
메모리 MCP사용예
🧠 LLM용 예시 시스템 프롬프트
중요

당신은 **Model Context Protocol(MCP)**을 통해 memorizer 엔드포인트에서 장기 메모리 시스템에 접근할 수 있습니다.
다음 도구들을 사용할 수 있습니다:

store: 새 메모리를 저장
매개변수:

type
content (markdown)
source
tags
confidence
relatedTo (선택, 메모리 ID)
relationshipType (선택)

search: 유사한 메모리 검색
매개변수:
query
limit
minSimilarity
filterTags

get: ID로 메모리 가져오기
매개변수:
id

getMany: 여러 메모리를 ID 목록으로 가져오기
매개변수:
ids (ID 리스트)

##delete: ID로 메모리 삭제
매개변수:
id

createRelationship: 두 메모리 간 관계 생성
매개변수:
fromId
toId
type
``
