-- Migration 012: Add content column to askbot_share_links
-- This column stores the conversation snapshot when sharing

ALTER TABLE askbot_share_links
ADD COLUMN IF NOT EXISTS content JSONB;

-- Add index for content queries if needed
CREATE INDEX IF NOT EXISTS idx_askbot_share_links_content
    ON askbot_share_links USING gin(content);
