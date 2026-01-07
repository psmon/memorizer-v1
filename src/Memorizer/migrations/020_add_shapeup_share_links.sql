-- Migration 020: Add Shape Up Share Links table
-- Shape Up 화이트보드 보드 공유를 위한 테이블

CREATE TABLE IF NOT EXISTS shapeup_share_links (
    short_code VARCHAR(6) PRIMARY KEY,
    title TEXT NOT NULL DEFAULT 'Shape Up Board',
    board_data JSONB NOT NULL,
    board_type VARCHAR(50),
    original_prompt TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- 인덱스
CREATE INDEX IF NOT EXISTS idx_shapeup_share_links_created_at
    ON shapeup_share_links(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_shapeup_share_links_board_type
    ON shapeup_share_links(board_type);
