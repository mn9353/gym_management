-- Adds credential storage for members (for welcome credentials + future member login)
-- and backfills existing rows with bcrypt hash of default temporary password: 123456789.

create extension if not exists pgcrypto;

alter table members
    add column if not exists password_hash text null;

-- Backfill only records that do not have a password hash yet.
update members
set password_hash = crypt('123456789', gen_salt('bf', 10))
where password_hash is null
   or btrim(password_hash) = '';

-- Optional hardening after all rows are migrated:
-- alter table members alter column password_hash set not null;
