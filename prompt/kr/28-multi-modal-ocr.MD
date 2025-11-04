다음 코드를 참고해~ Multi-modal 모델을 호출하는 코드를 만들고자합니다.

- src/Memorizer/Services/CustomLlmService.cs
  - SendChatRequest 메서드를 참고합니다.  

# 지침
- ILlmService 와 분리된 모듈로 만듭니다.
- 이미지 + 텍스트 입력을 받아 Multi-modal 모델에 전달하고 응답을 받는 코드를 작성합니다.
- 디폴트 모델명은 "qwen2/qwen3-vl-8b" 이며 환경변수 지정가능합니다.


다음은 파이선 코드예제로 닷넷코드 구현시 참고
```
from openai import OpenAI
import base64

client = OpenAI(base_url="http://localhost:1234/v1", api_key="lm-studio")  # 토큰은 아무 문자열이어도 OK

# 이미지 -> base64
with open("test.jpg", "rb") as f:
    b64 = base64.b64encode(f.read()).decode("utf-8")
data_uri = f"data:image/jpeg;base64,{b64}"

resp = client.chat.completions.create(
    model="qwen2-vl-2b-instruct",  # LM Studio에서 로드한 모델 ID
    messages=[{
        "role": "user",
        "content": [
            {"type": "text", "text": "이 이미지에 뭐가 보이나요?"},
            {"type": "image_url", "image_url": {"url": data_uri}}
        ]
    }],
    temperature=0.2,
)
print(resp.choices[0].message.content)
```

# 참조정보
- "src/Memorizer/ 에 프로젝트 파일일 있습니다.
- "src/Memorizer.IntegrationTests/" 에 유닛테스트 파일이 있습니다.
- "src/Memorizer/Services/Memory.cs" 에 메모리 저장소를 조회및 관리하는 서비스 코드가 있습니다.
- postgres 스키마는 "src/Memorizer/migrations" 에 있으며 넘버링 숫자에 따라 실제로 스키마 마이그레이션을 관리합니다. 전체를 순서대로 읽으면 스키마를 모두 알수 있습니다.


# 테스트 지침
- 유닛테스트를 작성하며, 이미지는 삼각형을 이용합니다.
  - 유닛테스트시 테스트 프로파일은 src/Memorizer.IntegrationTests/appsettings.json 을 이용
    - http://192.168.0.50:1234 를 유닛테스트에서 이용할수 있습니다.
    - 멀티모달을 처음 도입하며, 멀티모달을 위한 모델설정을 추가합니다.
  - 삼각형 이미지를 사용해 이 이미지에 뭐가 보이나요? 라는 질문을 던졌을때 삼각형 이라는 답변이 나오는지 확인합니다.





