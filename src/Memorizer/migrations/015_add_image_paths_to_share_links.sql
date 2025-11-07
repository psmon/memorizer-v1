-- Migration 015: Add image paths to askbot_share_links
-- This column stores image file paths associated with messages in shared conversations

ALTER TABLE askbot_share_links
ADD COLUMN IF NOT EXISTS image_paths JSONB;

-- Add index for image paths queries if needed
CREATE INDEX IF NOT EXISTS idx_askbot_share_links_image_paths
    ON askbot_share_links USING gin(image_paths);

-- Comment explaining the structure
-- image_paths JSONB format:
-- {
--   "messageIndex": "relative/path/to/image.jpg",
--   ...
-- }
-- Where messageIndex is the index of the user message that contained the image
