이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
다음에 설명된 부가설명을 확인후 개선지침 수행을 해주세요

## 개선지침수행
- 컨텐츠보기에서 컨텐츠는 주로 MD(마크다운)형식으로 작성되어 있습니다. 
- ui/view 페이지를~ ui/blog 팝업뷰와 유사하게 마크다운 스타일을 적용해주세요, 예를들면 코드뷰는 다크모드이며 언어표시및 복사기능이 있습니다.
- ui/view,ui/blog 팝업뷰 둘다 mermaid 다이어그램이 표시가 되지 않습니다. mermaid 다이어그램이 표시되도록 개선해주세요
- mermaid를 표현하는것은 style로만 해결하기 어려울수 있음으로 필요시 js라이브러리를 추가해도 되며~ 
  - marmaid가 적용된 markdown js를 활용해도 되며 github이 제공하는 markdown view와 유사해지는것이 목표입니다.
- mermaid에 제공되는 다이어그램 확대/이동 버튼을 달아죠~  스크롤업하면 확대/축소도


## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Views - UI관련 뷰파일이 있습니다.
- PageUrl : 다음과 같은 페이지 url을 가지고 있습니다.
  - ui/blog
    - ViewMore : 팝업버튼을 통해 컨텐츠보기
  - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : id별로 컨텐츠보기


## 로컬 테스트방법
- docker-compose.local-psmon.yml 를 통해 로컬에서 테스트 가능합니다. 도커구동 문제시 이파일을 사용할것..
- 이 어플리케이션은 momorizer 를 통해 빌드및 작동됩니다. 재빌드/재구동이 필요할시, dotnet cli없이 도커로만 이용해주세요
- docker를 구동후, api로 통해 테스트할때 인증은, 인증방식확인후 admin/admin123 으로 로긴후 인증 쿠키를 이용해 주세요
- 테스트가 개선되는동안 db스키마 변경은 허용하지 않으며, postgres 스키마파악및 읽기만 활용합니다. postgres는 다시구동필요없으며 개선되면 조회만 합니다.

## 테스트시 API인증방법
- src/Memorizer/Controllers/AuthController.cs 코드확인후.. api인증을 통과한후 등록 API를 통해 진행.. 
- 참고로 뷰는 미인증, 메모리등록은 인증된 API로만 가능 로긴시  admin/admin123 로 성공한후 반환된  쿠키값을 사용
- src/Memorizer/Controllers/MemoryController.cs 메모리 등록은 CreateMemory 사용 ... db를 조작하지 말고 제공되는 API를 파악해 활용할것

