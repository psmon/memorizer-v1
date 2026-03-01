-- Add web_search_references column to askbot_share_links table
-- Stores web search reference data (title, URL, snippet) for shared conversations
-- Format: [{"messageIndex": 1, "references": [{"title":"...", "url":"...", "snippet":"..."}]}]
ALTER TABLE askbot_share_links
ADD COLUMN IF NOT EXISTS web_search_references JSONB;
