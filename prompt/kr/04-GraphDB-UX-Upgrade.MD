
이 프로젝트는 MCP-Memorizer로  LLM Agent가 필요한 지식을 제공하는 프로젝트입니다.

posggres DB에 저장된 메모리를 Neo4j 그래프 데이터베이스로 동기화하고, LLM을 활용해 관계를 생성하는 기능을 추가하는 프로젝트입니다.

다음 지침을 준수해 프로젝트 업데이트를 진행합니다.

# 지침
- Word node 속성중 word를 name으로 최근 변경하였습니다. 속성중 word는 사용안하니 name하나만 유지합니다.
- Memory 노드와 Word 관계는, LLM을 이용하지 않고 postgres 에 이미 추출한 tag를 활용합니다. 즉 word의 name은 tag가 되며  mcp에서 sore될때와 , ui/tools/graph-sync 전체업데이트 두가지를 고려해 업데이트 반영합니다.
- PostgreSQL에 데이터가 추가될때~ GraphDB에도 함께 데이터가 추가되며 확장된 NodeWord의 Edge도 함께 업데이트됩니다.   ui/tools/graph-sync 은 전체 싱크기능으로 graph db 데이터를 초기화후 전체 업데이트를 재수행합니다.
- ui/tools/graph-sync Start Sync를 수행하면 기존 GraphDB를 "MATCH (n) DETACH DELETE n;" 쿼리로 초기화후, postgres DB에 있는 데이터를 모두 가져와서 GraphDB에 Memory 노드를 추가합니다. 이때 Word 노드도 함께 생성됩니다.
- Memory와 Memory의 연결은 postgres DB에 있는 관계를 그대로 GraphDB에 추가합니다. 
- 여기서 추가된되거나 누락된 graph 관련 기능을 유닛테스트도, 기존 유닛테스트를 참고해 작성합니다.
- 코드작성이 모두 완료되면 유닛테스트를 수행하면서, 오류및 잘못작동되는 부분을 개선해갑니다. 자연어를 cypher 쿼리로 변환하는 부분은 LLM이 활용됨으로 올바른 cypher 쿼리가 생성되는지 확인을하면서 프롬프트도 개선해갑니다.
- 유닛테스트가 완료되면 local port 5012로 구동해... 로컬 통합 api test를 수행합니다. postgres,ollama,neo4j 등은 이미 구동되어 있으며 docker ps 를 통해 구동여부및 포트를 확인할수 있습니다.

# 소스위치
- "src/Memorizer/ 에 프로젝트 파일일 있습니다.
- "src/Memorizer.IntegrationTests/" 에 유닛테스트 파일이 있습니다.
- postgres 스키마는 src/Memorizer/migrations에 있습니다.
