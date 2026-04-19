-- One-time reminder lock per membership cycle.
-- Run this once in Supabase SQL editor.

alter table if exists members
    add column if not exists expiring_reminder_sent_at timestamptz null,
    add column if not exists expiring_reminder_plan_end_date date null,
    add column if not exists inactive_reminder_sent_at timestamptz null,
    add column if not exists inactive_reminder_plan_end_date date null;

create index if not exists idx_members_expiring_reminder_plan_end
    on members (gym_id, expiring_reminder_plan_end_date);

create index if not exists idx_members_inactive_reminder_plan_end
    on members (gym_id, inactive_reminder_plan_end_date);
