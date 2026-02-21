-- Migration 021: Add ClaudeCode Skill Share Links table
-- Claude Code 스킬 공유를 위한 테이블

CREATE TABLE IF NOT EXISTS claudecode_skill_share_links (
    short_code VARCHAR(6) PRIMARY KEY,
    title TEXT NOT NULL DEFAULT 'Claude Skill',
    skill_content TEXT NOT NULL,
    job_category VARCHAR(100),
    skill_name VARCHAR(200),
    conversation_data JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- 인덱스
CREATE INDEX IF NOT EXISTS idx_claudecode_skill_share_links_created_at
    ON claudecode_skill_share_links(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_claudecode_skill_share_links_job_category
    ON claudecode_skill_share_links(job_category);
