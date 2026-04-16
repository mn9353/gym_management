-- Safety migration for older environments where users.password_hash may be missing or null.
-- Backfill value is bcrypt hash of default temporary password: 123456789.

create extension if not exists pgcrypto;

alter table users
    add column if not exists password_hash text null;

update users
set password_hash = crypt('123456789', gen_salt('bf', 10))
where password_hash is null
   or btrim(password_hash) = '';

-- Optional hardening after all rows are migrated:
-- alter table users alter column password_hash set not null;
