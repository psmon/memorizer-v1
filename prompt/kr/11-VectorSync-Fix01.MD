
이 프로젝트는 dotnet 9.0을 사용하며~ 버그가있어서 버그 fix를 하려고합니다.

# Bug 현상
- 벡터의 차원을 384->768로 변경하여  embedding,embedding_metadata 가 초기화된 상태입니다.
```postgresql
ALTER TABLE memories DROP COLUMN IF EXISTS embedding;
ALTER TABLE memories ADD COLUMN embedding VECTOR(768);


ALTER TABLE memories DROP COLUMN IF EXISTS embedding_metadata;
ALTER TABLE memories ADD COLUMN embedding_metadata VECTOR(768);


CREATE INDEX IF NOT EXISTS idx_memories_embedding_metadata_cosine
    ON memories USING ivfflat (embedding_metadata vector_cosine_ops)
    WITH (lists = 100); 
```
- ui/tools/metadata-embedding 에서 Start Embedding Generation을 수행하면,  embedding,embedding_metadata 가 전체 업데이트 되어야하나.. 업데이트가 안됩니다.
 -  embedding,embedding_metadata 가 null 상태로 남아있습니다.


# 개선지침
- ui/tools/start-metadata-embedding-all 컨트롤러및 서비스에서  embedding,embedding_metadata 가 업데이트 되도록 개선해주세요


## 프로젝트 위치
- src/Memorizer/ 하위디렉토리에 있습니다.

## 로컬 테스트방법
- docker-compose.local-psmon.yml 를 통해 로컬에서 테스트 가능합니다.
- 이 어플리케이션은 momorizer 를 통해 빌드및 작동됩니다. 재빌드/재구동이 필요할시, dotnet cli없이 도커로만 이용해주세요
- docker를 구동후, api로 통해 테스트할때 인증은, 인증방식확인후 admin/admin123 을 이용해 주세요
- 테스트가 개선되는동안 db스키마 변경은 허용하지 않으며, postgres 스키마파악및 읽기만 활용합니다. postgres는 다시구동필요없으며 개선되면 조회만 합니다.

## 추가개선
- 코드내 임베딩 차원수 368,768등 하드코딩된 부분이 있습니다. 모델에따라 변경될수 있음으로 embedding 차원수를 동적확인하도록 개선해주세요
- MCP 기능을 통해서~ 저장(store) 되는기능도 있습니다. 이부분을 확인해 curl로 테스트하고 DB에 벡터값이 저장되는지 확인해주세요 이미 잘작동하는 기능임으로 테스트만 수행