-- Member portal schema additions: workout logs linked with attendance check-ins.
-- Run this once in Supabase SQL editor before using member workout capture APIs.

create table if not exists member_workout_logs (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_id uuid not null references members(id) on delete cascade,
    checkin_id uuid null references member_checkins(id) on delete set null,
    workout_date date not null,
    muscle_groups text[] not null default '{}',
    notes text null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists idx_member_workout_logs_member_date
    on member_workout_logs(gym_id, member_id, workout_date);

create unique index if not exists uq_member_workout_logs_checkin_id
    on member_workout_logs(checkin_id)
    where checkin_id is not null;

create table if not exists member_rest_days (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_id uuid not null references members(id) on delete cascade,
    rest_date date not null,
    notes varchar(300) null,
    created_at timestamptz not null default now()
);

create unique index if not exists uq_member_rest_days_member_date
    on member_rest_days(gym_id, member_id, rest_date);
