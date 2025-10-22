-- Migration 014: Add updated_at column to askbot_share_links
-- This column tracks when a shared conversation was last updated

ALTER TABLE askbot_share_links
ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ;

-- Create index for updated_at (useful for sorting by last update)
CREATE INDEX IF NOT EXISTS idx_askbot_share_links_updated_at
    ON askbot_share_links(updated_at);
