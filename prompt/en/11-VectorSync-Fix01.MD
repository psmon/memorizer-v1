This project uses dotnet 9.0 and we're trying to fix a bug.

# Bug Symptoms
- The vector dimension has been changed from 384->768 and embedding, embedding_metadata have been initialized.
```postgresql
ALTER TABLE memories DROP COLUMN IF EXISTS embedding;
ALTER TABLE memories ADD COLUMN embedding VECTOR(768);


ALTER TABLE memories DROP COLUMN IF EXISTS embedding_metadata;
ALTER TABLE memories ADD COLUMN embedding_metadata VECTOR(768);


CREATE INDEX IF NOT EXISTS idx_memories_embedding_metadata_cosine
    ON memories USING ivfflat (embedding_metadata vector_cosine_ops)
    WITH (lists = 100); 
```
- When executing Start Embedding Generation in ui/tools/metadata-embedding, embedding and embedding_metadata should be fully updated, but they are not being updated.
- embedding and embedding_metadata remain in null state.


# Improvement Guidelines
- Please improve the ui/tools/start-metadata-embedding-all controller and service so that embedding and embedding_metadata are updated properly.


## Project Location
- Located in the src/Memorizer/ subdirectory.

## Local Testing Method
- Local testing is possible through docker-compose.local-psmon.yml.
- This application is built and operated through memorizer. When rebuild/restart is needed, please use Docker only without dotnet CLI.
- After running docker, when testing through API, for authentication, check the authentication method and use admin/admin123.
- During testing improvements, database schema changes are not allowed, and postgres schema analysis and read-only access are utilized. Postgres doesn't need to be restarted, and when improved, only queries are performed.

## Additional Improvements
- There are hardcoded embedding dimension numbers like 368, 768 in the code. Since they can change depending on the model, please improve to dynamically check the embedding dimension number.
- There is also a storage (store) function through MCP functionality. Please check this part, test with curl, and confirm if vector values are saved in the DB. This is an already working feature, so only testing is required.