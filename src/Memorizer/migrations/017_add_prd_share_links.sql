-- Migration 017: Add PRD Share Links table
-- Separate table for PRD Maker feature shares (Event Storming + Example Mapping)

CREATE TABLE IF NOT EXISTS prd_share_links (
    short_code VARCHAR(6) PRIMARY KEY,
    title TEXT NOT NULL DEFAULT 'PRD Analysis',
    prd_content TEXT NOT NULL,
    event_storming_result TEXT NOT NULL,
    discussion_result TEXT,
    example_mapping_result TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index for created_at (for cleanup, ordering, or analytics)
CREATE INDEX IF NOT EXISTS idx_prd_share_links_created_at
    ON prd_share_links(created_at DESC);

-- Index for title search
CREATE INDEX IF NOT EXISTS idx_prd_share_links_title
    ON prd_share_links(title);
