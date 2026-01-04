-- Migration 018: Add refined PRD result column to prd_share_links table
-- Stores the refined/improved PRD generated in step 5

ALTER TABLE prd_share_links
ADD COLUMN IF NOT EXISTS refined_prd_result TEXT;
