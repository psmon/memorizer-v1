
## Backup and Restore Database Commands

## Command to Backup Database
```bash
CID="$(docker ps -q -f "ancestor=pgvector/pgvector:pg17")"; : "${CID:?컨테이너를 찾지 못했습니다}"

docker exec -e PGPASSWORD=postgres "$CID" \
  pg_dump -U postgres -d postgmem > "postgmem-$(date +%Y-%m-%d)-768.sql"
```

## Command to Restore Database
```bash
cd /mnt/d/data/postgres
CID="$(docker ps -q -f "name=postgmem-postgres")"; : "${CID:?컨테이너를 찾지 못했습니다}"
docker exec -i "$CID" psql -U postgres -v ON_ERROR_STOP=1 -c \
"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='postgmem' AND pid <> pg_backend_pid();" \
&& docker exec -i "$CID" dropdb -U postgres postgmem || true \
&& docker exec -i "$CID" createdb -U postgres postgmem \
&& docker exec -i "$CID" psql -U postgres -d postgmem -v ON_ERROR_STOP=1 < postgmem-2025-09-14-768.sql

```


## Migration to Update Embedding Dimensions in Memories Table

```bash
cd /mnt/d/data/postgres

# 1) 컨테이너 ID 조회 (이름: postgmem-postgres)
CID="$(docker ps -q -f "name=postgmem-postgres")"; : "${CID:?컨테이너를 찾지 못했습니다}"

# 2) 마이그레이션 실행 (DB: postgmem)
docker exec -i "$CID" psql -U postgres -d postgmem -v ON_ERROR_STOP=1 <<'SQL'
BEGIN;
-- 벡터 타입 확장 보장
CREATE EXTENSION IF NOT EXISTS vector;

-- embedding 컬럼 재생성
ALTER TABLE memories DROP COLUMN IF EXISTS embedding;
ALTER TABLE memories ADD COLUMN IF NOT EXISTS embedding VECTOR(768);

-- embedding_metadata 컬럼 재생성
ALTER TABLE memories DROP COLUMN IF EXISTS embedding_metadata;
ALTER TABLE memories ADD COLUMN IF NOT EXISTS embedding_metadata VECTOR(768);

-- ivfflat 인덱스 생성 (코사인 유사도)
CREATE INDEX IF NOT EXISTS idx_memories_embedding_metadata_cosine
    ON memories USING ivfflat (embedding_metadata vector_cosine_ops)
    WITH (lists = 100);
COMMIT;
SQL

```

## Sync All Memories in Admin Tool
