-- Migration 011: Add AskBot Share Links table
-- This table stores short codes for sharing AskBot conversation sessions

CREATE TABLE IF NOT EXISTS askbot_share_links (
    short_code VARCHAR(6) PRIMARY KEY,
    session_id TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index for fast session_id lookup (to check if share link already exists)
CREATE INDEX IF NOT EXISTS idx_askbot_share_links_session_id
    ON askbot_share_links(session_id);

-- Index for created_at (for cleanup or analytics)
CREATE INDEX IF NOT EXISTS idx_askbot_share_links_created_at
    ON askbot_share_links(created_at);
