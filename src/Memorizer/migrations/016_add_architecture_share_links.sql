-- Migration 016: Add Architecture Share Links table
-- Separate table for Memory Architecture feature shares

CREATE TABLE IF NOT EXISTS architecture_share_links (
    short_code VARCHAR(6) PRIMARY KEY,
    memory1_id UUID NOT NULL,
    memory2_id UUID NOT NULL,
    memory1_title TEXT,
    memory2_title TEXT,
    idea_prompt TEXT NOT NULL,
    architecture TEXT NOT NULL,
    language VARCHAR(20) DEFAULT 'C#',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index for created_at (for cleanup or analytics)
CREATE INDEX IF NOT EXISTS idx_architecture_share_links_created_at
    ON architecture_share_links(created_at);

-- Index for memory lookups
CREATE INDEX IF NOT EXISTS idx_architecture_share_links_memory1
    ON architecture_share_links(memory1_id);

CREATE INDEX IF NOT EXISTS idx_architecture_share_links_memory2
    ON architecture_share_links(memory2_id);
