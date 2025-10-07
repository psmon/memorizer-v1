-- Migration 013: Add referenced_memories column to askbot_share_links
-- This column stores the referenced memory IDs for each message in the conversation

-- Add column to store referenced memories per message
-- Format: JSONB array of objects with messageIndex and memoryIds
-- Example: [{"messageIndex": 1, "memoryIds": ["uuid1", "uuid2"]}, ...]
ALTER TABLE askbot_share_links
ADD COLUMN IF NOT EXISTS referenced_memories JSONB;

-- Add index for referenced_memories queries if needed
CREATE INDEX IF NOT EXISTS idx_askbot_share_links_referenced_memories
    ON askbot_share_links USING gin(referenced_memories);
