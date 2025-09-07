
## Backup and Restore Database Commands

## Command to Backup Database
```bash
docker exec -e PGPASSWORD=postgres ca18b44d2463 \
pg_dump -U postgres -d postgmem > postgmem-2025-09-07_2040.sql
```

## Command to Restore Database
```bash
docker exec -i ca18b44d2463 psql -U postgres -v ON_ERROR_STOP=1 -c \
"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='postgmem' AND pid <> pg_backend_pid();" \
&& docker exec -i ca18b44d2463 dropdb -U postgres postgmem || true \
&& docker exec -i ca18b44d2463 createdb -U postgres postgmem \
&& docker exec -i ca18b44d2463 psql -U postgres -d postgmem -v ON_ERROR_STOP=1 < postgmem-2025-09-07_2040.sql
```


## Migration to Update Embedding Dimensions in Memories Table

```postgresql
ALTER TABLE memories DROP COLUMN IF EXISTS embedding;
ALTER TABLE memories ADD COLUMN embedding VECTOR(768);


ALTER TABLE memories DROP COLUMN IF EXISTS embedding_metadata;
ALTER TABLE memories ADD COLUMN embedding_metadata VECTOR(768);


CREATE INDEX IF NOT EXISTS idx_memories_embedding_metadata_cosine
    ON memories USING ivfflat (embedding_metadata vector_cosine_ops)
    WITH (lists = 100); 
```

## Sync All Memories in Admin Tool
