-- Hotfix: allow TRAINER (and MEMBER) roles in users.role check constraint
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_role_check;

ALTER TABLE users
    ADD CONSTRAINT users_role_check
    CHECK (UPPER(role) IN ('ADMIN', 'OWNER', 'STAFF', 'TRAINER', 'MEMBER'));

