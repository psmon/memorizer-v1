
## minilm 모델명
- all-minilm:33m-l12-v2-fp1    384  
- text-embedding-all-minilm-l12-v2  384
- text-embedding-nomic-embed-text-v1.5 768

## OepanAI 모델명
- text-embedding-3-small	1,536	가볍고 빠름, 비용 저렴
- text-embedding-3-large	3,072	더 정밀한 표현, 성능 우수

## Migration to Update Embedding Dimensions in Memories Table

모델이 384 이상일때, 사이즈및 호환성을 고려 다음을 수행후 Admin툴에서 전체 sync

``` 
ALTER TABLE memories DROP COLUMN IF EXISTS embedding;
ALTER TABLE memories ADD COLUMN embedding VECTOR(768);


ALTER TABLE memories DROP COLUMN IF EXISTS embedding_metadata;
ALTER TABLE memories ADD COLUMN embedding_metadata VECTOR(768);


CREATE INDEX IF NOT EXISTS idx_memories_embedding_metadata_cosine
    ON memories USING ivfflat (embedding_metadata vector_cosine_ops)
    WITH (lists = 100); 
```