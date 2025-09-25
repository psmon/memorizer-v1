-- Migration 010: Add custom scripts table for dynamic script injection
-- This migration adds support for managing custom scripts (e.g., Google Analytics)
-- that can be dynamically injected into pages

CREATE TABLE IF NOT EXISTS custom_scripts (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    script_content TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_custom_scripts_active
ON custom_scripts (is_active)
WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_custom_scripts_name
ON custom_scripts (name);