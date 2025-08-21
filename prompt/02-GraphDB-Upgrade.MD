
이 프로젝트는 MCP-Memorizer로  LLM Agent가 필요한 지식을 제공하는 프로젝트입니다.

다음지침을 준수해 프로젝트 업데이트를 진행합니다.

# 지침

- 기존 DB는 postgres기반 pgvector를 활용해 데이터를 저장합니다. 추가로 neo4j를 활용해 그래프DB에 메모리를 데이터를 저장합니다.
- Neo4j에는 모든컨텐츠를 저장할필요는 없습니다. 기존 postgre로 담긴 메모리를, Graph탐색을 찾는 보조적 역할을 수행하면됩니다.
- /ui/tools/metadata-embedding 의 엔드포인트 페이지에서 posgres db에 재 업데이트하는 기능이 있습니다. 이를 참고해 postgre데이터로부터 Graph 탐색을 전체갱신하는 기능을 만듭니다.
  - 검색된 문서는 id를 가지고 있으며 다음과 같은 패턴으로 볼수있습니다. 노드요소중 문서정보가 속성으로 있으며 클릭시 페이지뷰가 전환되도록, 샘플 엔드포인트는 다음과같습니다. /ui/view/fab3af81-eeb0-46d8-9373-3706c6019a71
- Graph 연관성을 통해 기존 문서를 더 잘찾기위해 엣지데이터를 만들때 LLM을 이용합니다. 목적수행을 위한 프롬프트도 작성합니다.
- /ui 엔드포인트 UI에서 postgres db를 조회/검색하는 페이지가 있습니다. 이를 참고해 ui/graph 라는 엔드포인트를 추가해(메뉴도추가) GraphDB 를 조회/검색하는 페이지를 만듭니다. Graph가 시각화
- 기능구현이 다되면 mcp에 search-graph라는 이름으로 GraphDB를 검색하는 기능을 추가합니다. 자연어로 검색을 하고, llm이 cypher 쿼리로 변환해 GraphDB를 검색하는 기능을 만듭니다.
- 기존이용중인 Nuget Package 버전을 가급적 그대로 이용하며, 새롭게 적용하는 버전은 기존 Package 버전과 호환되는 버전을 사용합니다.
- 코드를 모두 작성후.. 빌드가 되는지 빌드수행합니다. 유닛테스트는 안해도됩니다.

# 소스위치
- "src/Memorizer/ 하위디렉토리에 있습니다.
- postgres 스키마는 src/Memorizer/migrations에 있습니다.


## 레퍼런스 활용 지침

- Neo4j개발듀토리얼을 메모리에 검색한후 참고지식으로 활용합니다. 검색이 안될시 유사도를 조금씩 낮춰 자료를 찾습니다.


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
