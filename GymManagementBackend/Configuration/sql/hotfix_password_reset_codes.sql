-- Password reset code table for OTP-style reset flow.
-- Run once in Supabase SQL editor.

create table if not exists password_reset_codes (
    id uuid primary key default gen_random_uuid(),
    user_id uuid null references users(id) on delete cascade,
    member_id uuid null references members(id) on delete cascade,
    email varchar(100) not null,
    code_hash varchar(128) not null,
    expires_at timestamptz not null,
    used_at timestamptz null,
    created_at timestamptz not null default now(),
    constraint chk_password_reset_owner_or_member check (
        (user_id is not null and member_id is null)
        or (user_id is null and member_id is not null)
    )
);

create index if not exists idx_password_reset_codes_email_expiry
    on password_reset_codes(email, expires_at);
