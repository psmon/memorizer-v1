이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능합니다.

ui/askbot 에는 대화형 챗봇이 이미 구현되어 있으며, 대화내용 공유하기 기능도 있습니다.
다음 지침에 의해  기능을 강화 하려고합니다.

# 지침
- askbot에서는 현재 text 만 요청가능하며, 이미지가 포함된 멀티모달형태의 요청은 할수 없습니다.
- 다음과 같은 방식으로 멀티모달 요청을 할수 있도록 개선합니다.
  - src/Memorizer.IntegrationTests/Services/MultiModalServiceTests.cs 에 멀티모달에 사용할 유닛테스트를 완료했습니다. 이것을 참고해 활용합니다.
  - 현재 최초 요청을 하게되면~ 메모리검색 및 판단을 거쳐 LLM에 요청을 하게됩니다.
    - Only Text 요청시에는 기존과 동일하게 작동합니다.
    - Multi-modal 요청시에는 이미지 + 텍스트를 LLM에 전달해 응답을 받도록 개선합니다.
  - 멀티모달은 서비스코드에서 처음사용함으로 설정및 DI를 주입하는것을 추가합니다.
  - 멀티모달 모델은 "qwen2/qwen3-vl-8b" 모델을 사용하며, 환경변수로 모델명을 지정할수 있도록 개선합니다.
    - 환경변수가 지정되지 않을시 디폴트 모델명으로 작동합니다.
  - 대화입력 창에 이미지 업로드기능이 있으며 이미지 업로드후 텍스트를 입력하게되면 멀티모달로 작동됩니다. 
    - 이미지 업로드는 최대 1개만 가능하며, 여러개 업로드시 가장 마지막 업로드한 이미지만 사용됩니다. -설정으로 제약 
    - 이미지 업로드는 용량제한이 있으며, 3MB를 초과할수 없습니다. 초과시 오류메시지를 표시합니다. - 설정으로 제약
    - 이미지 업로드는 jpg, png 포맷만 허용하며, 그외 포맷은 오류메시지를 표시합니다.
    - 이미지는 버튼으로도 업로드가능하지만 ,드래그앤드랍또는 클립보드 붙여넣기로도 가능해야합니다. - 웹기술사용

## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Controllers - API컨트롤러가 있습니다.
- src/Memorizer/Services - 검색로직을 포함 서비스로직이 있습니다.
- src/Memorizer/Services/ILlmService.cs - LLM및 임베딩을 이용할수 있습니다.  
- src/Memorizer/Views - UI관련 뷰파일이 있습니다.
- src/Memorizer/Actors - 액터모델이 있으며, 액터모델이 필요시 참고합니다. 
  - ChatBotActor.cs - SSE를 엣지로 사용자와 연결해 대화요청을 처리하는 액터모델입니다. 검색,판단 액터를 이용해 응답을 생성합니다.
  - SearchMemoryActor.cs - 메모리 검색을 담당하는 액터모델입니다.
  - DecisionActor.cs - 검색된 메모리와 요청내용이 관련성이 있는지 판단하는 액터모델입니다.
  - MetadataEmbeddingActor.cs - 메타데이터 임베딩을 담당하는 액터모델입니다. 메모리 등록시 메타데이터 임베딩을 비동기로 처리합니다. 이 과정에서 임베딩을 이용 벡터화합니다.
  - GraphsyncActor.cs - Neo4j를 이용해 ,Postgres데이터를 그래프로 모델로 싱크하는 액터모델입니다.
- src/Memorizer.IntegrationTests - 통합테스트 프로젝트가 있습니다.
- PageUrl : 다음과 같은 페이지 url을 가지고 있습니다.
    - ui/blog
        - ViewMore : 팝업버튼을 통해 컨텐츠보기
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : id별로 컨텐츠보기
    - ui/askbot - 챗봇 페이지
    - ui/askbot/share/{숏링크} - 공유된 챗봇 대화내용 보기 페이지
- src/Memorizer.IntegrationTests/Actors : 기존 유닛테스트가 있습니다. 유닛테스트 방법을 참고
  
## 로컬 테스트방법
- 빌드오류만 해결합니다.
- 빌드오류 잡고나서 테스트는 직접예정으로 피드백에따라 수정합니다.

## 테스트시 API인증방법
- src/Memorizer/Controllers/AuthController.cs 코드확인후.. api인증을 통과한후 등록 API를 통해 진행..
- 참고로 뷰는 미인증, 메모리등록은 인증된 API로만 가능 로긴시  admin/admin123 로 성공한후 반환된  쿠키값을 사용
- src/Memorizer/Controllers/MemoryController.cs 메모리 등록은 CreateMemory 사용 ... db를 조작하지 말고 제공되는 API를 파악해 활용할것

