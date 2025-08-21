
이 프로젝트는 MCP-Memorizer로  LLM Agent가 필요한 지식을 제공하는 프로젝트입니다.

posggres DB에 저장된 메모리를 Neo4j 그래프 데이터베이스로 동기화하고, LLM을 활용해 관계를 생성하는 기능을 추가하는 프로젝트입니다.

다음 지침을 준수해 프로젝트 업데이트를 진행합니다.

# 지침
- ui/graph 엔드포인트는 graph DB를 조회검색하는 페이지입니다. Sync기능은 별도로 분리되어있음으로 UI에서 제거하고 대신에.. cyper 쿼리로 GraphDB를 검색하는 기능을 추가합니다.
- ui/graph 에 추가로 자연어로 검색을 하고, LLM이 cypher 쿼리로 변환해 GraphDB를 검색하는 기능을 만듭니다. API 엔드포인트는 있음으로 조사해 연결해
- Node Detail간 연결관계가 Postgre 연결과 동일하게 작동하고 있으며, 추가로 컨텐츠의 내용중 핵심 키워드를 추출한후 NodeWord 를 만들어 NodeDetail->NodeWord 관계를 생성합니다. NodeWord는 단어별로 생성되며 중복단어 생성을 방지합니다.
- PostgreSQL에 데이터가 추가될때~ GraphDB에도 함께 데이터가 추가되며 확장된 NodeWord의 Edge도 함께 업데이트됩니다.   ui/tools/graph-sync 은 전체 싱크기능으로 graph db 데이터를 초기화후 전체 업데이트를 재수행합니다.
- 여기서 추가된되거나 누락된 graph 관련 기능을 유닛테스트도, 기존 유닛테스트를 참고해 작성합니다.
- 코드작성이 모두 완료되면 유닛테스트를 수행하면서, 오류및 잘못작동되는 부분을 개선해갑니다. 자연어를 cypher 쿼리로 변환하는 부분은 LLM이 활용됨으로 올바른 cypher 쿼리가 생성되는지 확인을하면서 프롬프트도 개선해갑니다.
- 유닛테스트가 완료되면 local port 5012로 구동해... 로컬 통합 api test를 수행합니다. postgres,ollama,neo4j 등은 이미 구동되어 있으며 docker ps 를 통해 구동여부및 포트를 확인할수 있습니다.

# 소스위치
- "src/Memorizer/ 에 프로젝트 파일일 있습니다.
- "src/Memorizer.IntegrationTests/" 에 유닛테스트 파일이 있습니다.
- postgres 스키마는 src/Memorizer/migrations에 있습니다.


## fix
- Word node 속성중 word를 name으로 변경합니다. neo4j 에서 name을 시각화해 표현하기때문입니다.
- Neo4j 툴에서는 Graph의 Node가 잘 표시됩니다. ui/graph 페이지가 graph db를 조회하는지 체크..postgres의 view를 표현하는것같습니다. view를 개선하고 검색기능도 다시 체크해 연결
- NodeWord는 단어별로 생성되며 중복단어 생성을 방지는... "Hello" 란 Node를 중복해서 생성하지 않아야함을 의미하는것이며... 연결은 N개 가능해야합니다. 언급된 단어가 많은 도큐먼트를 찾을때 유용하기때문입니다. 로직을 파악후 개선
-  GRAPH.md 에서 이 프로젝트가 만드는 graph 종류를 파악해 cyper query를 학습할수 있는 다양한 샘플 쿼리를 작성해~
 