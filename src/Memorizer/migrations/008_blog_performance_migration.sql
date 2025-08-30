-- Migration 008: Blog performance optimization indexes
-- This migration adds indexes to optimize blog filtering, searching, and pagination

-- Drop problematic btree index if it exists (too large for btree)
DROP INDEX IF EXISTS idx_memories_title_text_search;

-- Note: Using GIN trigram indexes instead for text search (see below)

-- Index for type filtering
CREATE INDEX IF NOT EXISTS idx_memories_type 
ON memories (type);

-- GIN index for tag array queries (efficient for && operator)
CREATE INDEX IF NOT EXISTS idx_memories_tags_gin 
ON memories USING GIN (tags);

-- Composite index for type + created_at (common filter + sort combination)
CREATE INDEX IF NOT EXISTS idx_memories_type_created 
ON memories (type, created_at DESC);

-- Index for created_at alone for blog listing
CREATE INDEX IF NOT EXISTS idx_memories_created_at_desc 
ON memories (created_at DESC);

-- Composite index for common search pattern: type + tags
CREATE INDEX IF NOT EXISTS idx_memories_type_tags 
ON memories (type, tags) 
WHERE tags IS NOT NULL;

-- Full-text search index for better text searching (optional but recommended)
-- Note: This requires the pg_trgm extension for trigram similarity matching
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS idx_memories_title_trgm 
ON memories USING GIN (title gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_memories_text_trgm 
ON memories USING GIN (text gin_trgm_ops);

-- Partial index for memories with titles (blog-relevant memories)
CREATE INDEX IF NOT EXISTS idx_memories_with_titles 
ON memories (created_at DESC) 
WHERE title IS NOT NULL;

-- Statistics update to help query planner
ANALYZE memories;