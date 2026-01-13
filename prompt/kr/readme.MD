# Claude Code 이용 프로그램업데이트

Fork Origin Version : https://github.com/Aaronontheweb/memorizer-v1

원본 오픈소스에서 출발해 Claude코드를 활용해 기능 업그레이드 시도하는 프로젝트입니다.

## Version 0.0.9

노트 : 기본버전이 임베디드 Ollama를 활용하고 있으며, 임베딩/LLM성능을 높이기위해 OpenAI API를 선택할수 있습니다.
LLM은 비용이 드는 단점이 있어서 개인연구용은 역시 라이트웨이한 온디바이스 Ollama를 활용하는것이 추천합니다.

- [OpenAI-Upgrade](01-OpenAI-Upgrade.MD)


## Version 1.0.0

노트 : 이 버전부터 이 프로젝트가 제공하는 메모리를 함께활용해 필요한 컨텍스트를 할습시킨후 프로그램업데이트 
개선시도되며  GraphDB를 활용해 메모리를 찾을수 있는 고급기능을 추가합니다. 
자연어를 Cypher 쿼리로 LLM으로 변환해 유용한검색이 되게까지 하기위해서는 프롬프트 엔지니어링이 필요할수 있습니다. GraphView를 통해 탐색하는 기본기능을 우선제공합니다.

- [GraphDB-Memory](02-GraphDB-Memory.MD)
- [GraphDB-Upgrade](02-GraphDB-Upgrade.MD)
- [GraphDB-Upgrade-Fix](02-GraphDB-Upgrade-FIX.MD)
- [GraphDB-UX-Upgrade3](03-GraphDB-UX-Upgrade.MD)
- [GraphDB-UX-Upgrade4](04-GraphDB-UX-Upgrade.MD)


## Version 1.0.1

관리기능과 MCP에 인증기능 간소화된 인증기능추가, 메모리 웹탐색은 비인증뷰로 개선 
Public모드로 지식을 공유하고 특정 사용자와 메모리를 함께 관리하고자할때 활용

- [Auth-Upgrade](05-Auth-Upgrade.MD)

이 업데이트 이후  MCP-Memorizer를 구축하고, 닷넷/자바 진영의 AKKA를 메모리에 학습시켜 액터모델도 바이브코딩에 이용하고 있습니다.
컨텐츠는 한글로 작성되어, 영어권에서는 번역이 필요할수도 있습니다.

MCP Memory View - Akka관련 Context를 주로 다룸
- https://mcp.webnori.com/ui


## Version 1.0.2

자연어를 Cypher 쿼리로 변환하는 기능 개선과 쿼리 탐색을 위한 더 나은 UX로 GraphDB 검색 기능 개선

- [GraphDB-Prompt-Upgrade](06-GraphDB-Prompt-Upgrade.MD)

## Version 1.0.3

GraphDB 검색 및 표현 기능 추가 개선, Blog Memories 페이지 추가

- [GraphDB-Prompt-Upgrade2](07-GraphDB-Prompt-Upgrade.MD)
- [View-Blog-Upgrade](08-View-Blog-Upgrade.MD)

## Version 1.0.4

블로그 뷰 UX 및 성능 개선

- [View-Blog-Upgrade2](09-View-Blog-Upgrade2.MD)

## Version 1.0.5

Custom LLM/Embedding 서비스 구현 및 설정 개선

- [CustomLLM-New](10-CustomLLM-New.MD)

## Version 1.0.6

임베딩 차원 변경에 대한 벡터 동기화 버그 수정

- [VectorSync-Fix01](11-VectorSync-Fix01.MD)

## Version 1.0.7

동일 목적이지만 차이가 있는 중복 프롬프트 개선

- [PROMPT-Improve](12.PROMPT-Improve.md)

## Version 1.0.8

마크다운 렌더링 개선을 통한 콘텐츠 뷰 향상

- [ContentView-Upgrade](13-ContentView-Upgrade.MD)

## Version 1.0.9

하이브리드 검색 기능 개선 및 ASKBot 대화형 챗봇 구현

- [HybridSearch-Upgrade](14-HybridSerch-Upgrade.md)
- [ASKBOT-ActorModel](15.01-ASKBOT-ActorModel.md)
- [ASKBOT-SSE](15.02-ASKBOT-Websocket.md)
- [ASKBOT-IMPROVE1](15.03-ASKBOT-IMPROVE1.md)

## Version 1.1.0

메뉴 UI/UX 개선 - 반응형 햄버거 메뉴 및 애니메이션 구현

- [MenuView-Upgrade](16.MenuView-Upgrade.MD)

## Version 1.1.1

ASKBot 대화 컨텍스트 관리 기능 개선 - 세션별 대화 맥락 유지 및 추론 과정 선택적 표시

- [ASKBOT-ActorModel-Upgrade](17.ASKBOT-ActorModel-Upgrade.md)

통합 테스트 개선 - Actor 모델 테스트 업데이트 및 실제 장치 기반 테스트 강화

- [Unitest-Upgrade](18.Unitest-Upgrade.md)

## Version 1.1.2

동적 스크립트 주입 기능 구현 - 페이지에 커스텀 JavaScript 동적 삽입 및 관리 UI 제공

- [InstallScript](19.InstallScript.md)

## Version 1.1.3

AskBot 대화 공유 및 복원 기능 구현 - 숏링크 기반 대화 공유, 데이터베이스 대화 스냅샷 저장, 페이지 새로고침 시 세션 기반 대화 복원, 물결 애니메이션 생각 인디케이터 개선

- [AskBotShare](20-AskBotShare.md)

## Version 1.1.4

AI-Knowledges 페이지 구현 - 공유된 대화 리스트 뷰, 카드 기반 반응형 UI, 페이지네이션, 지식 재생성 컨셉 표시

- [AskBotShareList](21-AskBotShareList.md)

## Version 1.1.5

AskBot 공유 대화 연관 문서 표시 기능 개선 - 공유된 대화에서 참고 문서 정보 저장 및 표시, 모달을 통한 문서 상세 보기, 기존 데이터 호환성 보장

- [AskBotShare-Improve](22-AskBotShare-Improve.md)

## Version 1.1.6

ReMemorizer 기능 추가 - UI 권한 제어 개선(비인증 시 편집/삭제 버튼 숨김), 공유 대화를 메모리로 저장 기능(LLM 기반 자동 메타데이터 추출 및 관계 연결)

- [ReMemorizer](23-ReMemorizer.md)

## Version 1.1.7

ReMemorizer 다중 대화 쌍 저장 개선 - 여러 대화 쌍을 개별 메모리로 저장, 순차적 관계 연결(continues-to), 참조 문서 매핑 개선

- [ReMemorizer-Fix](24-ReMemorizer-fix.md)

## Version 1.1.8

AskBot 공유 기능 개선 - 공유 페이지에서 Mermaid 다이어그램 렌더링 수정, 기존 세션 공유 업데이트 기능, async/await 구문 오류 해결

- [ReMemorizer-Fix2](25-ReMemorizer-fix2.md)

## Version 1.1.9

ModelContextProtocol 0.3.0 -> 0.4.0 모듈 업데이트 반영

## Version 1.2.0

통합 테스트 개선 - 빠른 기능 개발로 누락된 유닛 테스트 재작동화 및 업데이트

- [Unitest-Improve](26-Unitest-Imporove.md)

## Version 1.2.1

블로그 뷰 UX 개선 - 연관 문서 직접 접근 버튼 및 자동 스크롤 애니메이션 구현

- [BlogView-Upgrade](27-BlogView-Upgrade.md)

## Version 1.2.2

멀티모달 이미지 분석 기능 구현 - ILlmService와 분리된 독립 모듈, OpenAI 호환 API 형식 지원(LM Studio), 외부 라이브러리 없는 크로스 플랫폼 PNG 생성

- [Multi-modal-OCR](28-multi-modal-ocr.MD)

## Version 1.2.3

AskBot 멀티모달 요청 기능 구현 - 이미지 업로드(버튼/드래그앤드롭/클립보드), 이미지+텍스트 동시 분석, qwen3-vl-8b 모델 연동, 3MB 제한 및 JPG/PNG 포맷 지원

- [AskBot-Multimodal](29-AskBot-Multimodal.md)

## Version 1.2.4

AskBot 공유 이미지 저장 기능 구현 - 멀티모달 요청 이미지 로컬 저장, Share 링크에서 이미지 표시, 설정 가능한 이미지 저장 경로(기본: AskBotImages)

- [AskBot-SaveImage](30-AskBot-SaveImage.md)

## Version 1.2.5

OpenAI 호환 LLM API 컨트롤러 구현 - 순수 LLM API 엔드포인트 제공, Chat Completions 및 Text Completions 지원, SSE 기반 스트리밍 구현, CORS 제한 없음, Swagger 문서화, 인증 불필요, 프론트엔드 통합 가이드 제공

- [OpenLLM](31-OpenLLM.md)

## Version 1.2.6

ASKBOT 멀티 쿼리 검색 기능 구현 - 질문 유형 자동 분석(LLM 기반), 단일/다중 주제 적응형 검색 전략, 토픽별 독립 검색 및 결과 통합, Actor 모델 상태 관리 개선

- [ASKBOT-MultiQA](32-ASKBOT-MultiQA.md)

## Version 1.2.7

멀티모달 LLM 서비스 OpenAI 확장 - MultiModalService를 Custom/OpenAI 두 가지 구현체로 분리, OpenAI SDK 기반 MultiModalOpenAIService 추가, 설정 파일에서 Type으로 구현체 선택

- [OpenLLM-fix](33-OpenLLM-fix.md)

## Version 1.2.8

MCP OAuth 2.0 인증 방식 추가 - ChatGPT MCP 연동을 위한 OAuth 2.0 표준 인증 구현, Client ID/Secret 기반 JWT 토큰 발급, 기존 ApiKey 인증과 병행 가능, 24시간 토큰 만료 설정

- [Oauth-MCP](34-Oauth-MCP.md)

## Version 1.2.9

AI 기반 아이디어 생성 - 기존 지침들을 기반으로 새로운 아이디어 구현을 위한 지침 자동 생성

- [Ai-Driven-Idea](35-Ai-Driven-Idea.md)

## Version 1.3.0

Memory Architecture 기능 구현 - 두 개의 메모리를 조합하여 창의적인 아키텍처 아이디어 생성, 스트리밍 응답, Mermaid 다이어그램 및 코드 샘플 생성, 공유 기능, 라이트 모드 UI, 무한 스크롤 공유 목록

- [Memory-Architecture](36-AskBot2.md)

## Version 1.3.1

PRD Maker 기능 구현 - LLM-EX(120B 모델) 기반 심층 분석, 이벤트 스토밍으로 PRD 해석, 가상 협업자(PM/Dev/QA/UX) 예제 맵핑 토론, 5단계 반복 정제 프로세스(PRD 입력 → 이벤트 스토밍 → 토론 → 예제 맵핑 → 보완된 PRD), SSE 스트리밍, Mermaid 다이어그램, 숏링크 공유

- [PRD-Maker](37-PRD-Maker.md)

## Version 1.3.2

LLM-EX OpenAI 지원 추가 - LLM-EX 서비스에서 Custom, Ollama, OpenAI 세 가지 공급자 선택 지원, 환경변수 기반 공급자 타입 선택, OAuth 설정 샘플 docker-compose 파일 통합, docker-compose.md 구성 가이드 문서 추가

- [LLMEX-OpenAI](38-LLMEX-OpenAI.md)

## Version 1.3.3

PRD Maker 기능 개선 - 6단계 바운디드 컨텍스트(BC) 정의 기능 추가(DDD 원칙 기반 서브도메인 분해, 유비쿼터스 언어 충돌, 트랜잭션 경계 분석), 3단계 예제 맵핑 토론에 메모리 검색 통합("메모리즈" 가상 토론자로 관련 지식 참여), Mermaid 다이어그램 포함, 숏링크 공유 기능

- [PRD-Maker-02](39-PRD-Maker-02.md)

## Version 1.3.4

PRD 데모 와이어프레임 생성 기능 - 정제된 PRD 기반 LLM-EX 와이어프레임 HTML 페이지 생성, wwwroot/demo/{shortlink}/ 정적 파일 저장, Bootstrap 5 CDN 반응형 디자인, 전체 URL 복사 및 새창 열기 기능 모달, PRD Shares 페이지에서 정제된 PRD 존재 시 사용 가능

- [PRD-Maker-03](40-PRD-Maker-03.md)

## Version 1.3.5

Shape Up 화이트보드 기능 구현 - Shape Up 방법론 기반 통합 피치 보드(Breadboard, Fat Marker, Problem, Risk, Pitch Summary), AI 자동 생성(개별/통합 옵션), draw.io 유사 드로잉 도구, 자석 화살표 연결, 그룹핑 기능, 컨텍스트 메뉴, PNG/SVG 내보내기, 숏링크 공유

- [ShapeUp-Pitch](41-shapeup-pitch.md)
- [ShapeUp-Pitch-Improve](42-shapeup-pitch-improve.md)

## Version 1.3.6

메모리 조각 참조 기능 개선 - PRD Maker 예제 맵핑 토론에서 3개 키워드별 메모리 검색(0.3 이상 유사도) 및 LLM 유용성 판단으로 최대 3개 메모리(메모리1, 메모리2, 메모리3) 토론 참여, ShapeUp Free Board 생성시 메모리 검색 통합, 진행상태 표시(phase SSE 이벤트), 토스트 알림으로 메모리 채택 결과 표시

- [Memory-PRD-ShapeUp](43-memory-prd-shapeup.md)

## Version 1.3.7

ShapeUp SVG BOX 기능 추가 - SVG 벡터 이미지 드로잉(투명 배경, 테두리 색상/두께 조절), SVG 아이콘 라이브러리(화살표, 사용자, 클라우드, 서버, 데이터베이스 등), SVG Content Editor 모달(템플릿, 실시간 미리보기)

- [ShapeUp-SVG-Add](44-shapeup-svg-add.md)

## Version 1.3.8

ShapeUp LLM 개선 - 메모리 검색 규칙 업그레이드(PRDMaker, ShapeUp 모두 키워드당 3개씩 검색 후 LLM 판단 상위 3개 선택), PRD 와이어프레임 ShapeUp 연동(자동 프롬프트로 ShapeUp Free Board 이동), 2000글자 초과 시 프롬프트 요약 기능, 메모리 검색 옵션 체크박스, 텍스트 겹침 방지 개선

- [ShapeUp-LLM-Improve](45-shapeup-llm-improve.md)

## Version 1.3.9

ShapeUp 공유 페이지 개선 - 공유 뷰 페이지에서 Edit 버튼으로 편집 페이지 이동 및 캔버스 데이터 로드, 프롬프트 펼치기 UI(복사 기능 포함), SVG Icons/Board Templates 자동 닫힘 제거, 편집 도구 순서 재배치(Drawing Tools → Properties → SVG Icons → Board Templates), Ctrl+C/Ctrl+V 복사/붙여넣기 지원(그룹 유지), 편집 도구 패널 독립 스크롤

- [ShapeUp-Share-Improve](46-shapeup-share-improve.md)

## Claude Cli Setting : Vibe 코딩할시 활용

```
claude mcp add memorizer http://localhost:5012/sse --transport sse --header "X-API-Key: your-api-key-here"

claude mcp list

claude mcp remove memorizer
```

## Claude Desktop Setting : 지식을을 간편하게 메모리에 저장할때 다양한 MCP와 연계해 만들때 활용

```
{
  "mcpServers": {
    "mcp-memory": {
      "command": "npx",
      "args": [
        "-y",
        "mcp-remote",
        "http://localhost:5012/sse",
        "--header",
        "X-API-Key:${MCP_API_KEY}"
      ],
      "env": {
        "MCP_API_KEY": "xxxxxxxxxxxxxxxxxxx"
      }
    }
  }
}
```

### Claude Desktop Tip

활용 프롬프트 : akka 컨텐스트를 Memory에 저장하기, 학습하기 위한 페이지를 찾고 다음과 같은 심플한 요청이면 됩니다.
```
https://getakka.net/ 의 내용을 메모리화해
```

프로젝트에 설정된 지침 :
```
당신은 연결된 MCP, 특히 장기기억 메모리 활용해 답변을 하는 진보된 LLM입니다.

활용 MCP를 참고  다음 Rule을 준수해 작동할것

# Rule

- 검색대상이 활용 MCP에 포함되어 있으면 적합한 MCP를 각각활용할것
- 컨플검색 요청을하면  Atlassian MCP를 활용해 지식을 획득할것
- 메모리에서 검색 요청하면, Memory MCP를 활용해 지식을 획득할것
- 메모리 검색시 유사도는 0.7이상으로 하고, 검색이 안될시 0.4이상으로 시도
- 메모리 저장 여부는 물어보고 저장할것, "메모리 저장해" 와같이 별도 지침없을때 자동저장 금지(중요)
- URL 링크를 분석요청시 리소스접근안될시~  Browser MCP를 후속으로 활용합니다.

# 활용 MCP 종류
- 메모리 MCP
- 컨플/지라 제어가능한 Atlassian MCP
- 브라우저 탐색가능한 Browser MCP
```