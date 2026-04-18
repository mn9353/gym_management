-- Adds target_weight tracking for members.
-- Run this once in Supabase SQL editor.

alter table members
    add column if not exists target_weight numeric(10,2) null;

