

## minilm 모델명

- all-minilm:33m-l12-v2-fp1    384  

## OepanAI 모델명
- text-embedding-3-small	1,536	가볍고 빠름, 비용 저렴
- text-embedding-3-large	3,072	더 정밀한 표현, 성능 우수

``` 
-- Step 1: Drop the existing embedding_metadata column
ALTER TABLE memories DROP COLUMN IF EXISTS embedding;

-- Step 2: Add the new embedding_metadata column with 1536 dimensions
ALTER TABLE memories ADD COLUMN embedding VECTOR(1536);

-- Step 1: Drop the existing embedding_metadata column
ALTER TABLE memories DROP COLUMN IF EXISTS embedding_metadata;

-- Step 2: Add the new embedding_metadata column with 1536 dimensions
ALTER TABLE memories ADD COLUMN embedding_metadata VECTOR(1536);

-- Create index for metadata embedding searches
CREATE INDEX IF NOT EXISTS idx_memories_embedding_metadata_cosine
    ON memories USING ivfflat (embedding_metadata vector_cosine_ops)
    WITH (lists = 100); 
```