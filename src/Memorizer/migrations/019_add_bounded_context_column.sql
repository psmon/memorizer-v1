-- Migration 019: Add bounded context result column to prd_share_links table
-- Stores the Bounded Context definition generated in step 6

ALTER TABLE prd_share_links
ADD COLUMN IF NOT EXISTS bounded_context_result TEXT;
