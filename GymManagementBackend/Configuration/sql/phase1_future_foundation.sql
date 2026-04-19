-- Future foundation schema (idempotent)
-- Safe to run multiple times on PostgreSQL.

create extension if not exists pgcrypto;

-- ------------------------------------------------------------
-- Existing members table compatibility columns
-- ------------------------------------------------------------
alter table if exists members
    add column if not exists amount_to_pay numeric(10,2) null;

alter table if exists members
    add column if not exists email varchar(100) null;

alter table if exists members
    add column if not exists training_type varchar(20) not null default 'GENERAL';

create index if not exists idx_members_gym_training_type
    on members(gym_id, training_type);

-- ------------------------------------------------------------
-- Service catalog
-- ------------------------------------------------------------
create table if not exists service_types (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    code varchar(50) not null,
    display_name varchar(100) not null,
    is_active boolean not null default true,
    sort_order integer not null default 0,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now(),
    constraint uq_service_types_gym_code unique(gym_id, code)
);

create index if not exists idx_service_types_gym_active_sort
    on service_types(gym_id, is_active, sort_order);

create table if not exists service_plans (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    service_type_id uuid not null references service_types(id) on delete restrict,
    name varchar(100) not null,
    duration_months integer not null,
    price numeric(10,2) not null,
    is_active boolean not null default true,
    rules_json text null,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now()
);

create index if not exists idx_service_plans_gym_service_active
    on service_plans(gym_id, service_type_id, is_active);

-- ------------------------------------------------------------
-- Subscriptions + optional monthly overlays
-- ------------------------------------------------------------
create table if not exists member_subscriptions (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_id uuid not null references members(id) on delete cascade,
    service_plan_id uuid not null references service_plans(id) on delete restrict,
    start_date date not null,
    end_date date not null,
    status varchar(20) not null default 'ACTIVE',
    amount_to_pay numeric(10,2) not null default 0,
    amount_paid numeric(10,2) not null default 0,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now()
);

create index if not exists idx_member_subscriptions_gym_member_status
    on member_subscriptions(gym_id, member_id, status);

create index if not exists idx_member_subscriptions_gym_dates
    on member_subscriptions(gym_id, start_date, end_date);

-- Month-level service selection inside a subscription.
-- Example: base 6 months + PT only in month_index 2,3,5.
create table if not exists subscription_month_services (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_subscription_id uuid not null references member_subscriptions(id) on delete cascade,
    service_type_id uuid not null references service_types(id) on delete restrict,
    month_index integer not null check (month_index >= 1 and month_index <= 120),
    trainer_user_id uuid null references users(id) on delete set null,
    amount_to_pay numeric(10,2) not null default 0,
    amount_paid numeric(10,2) not null default 0,
    status varchar(20) not null default 'ACTIVE',
    notes text null,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now(),
    constraint uq_subscription_month_service unique(member_subscription_id, month_index, service_type_id)
);

create index if not exists idx_subscription_month_services_member
    on subscription_month_services(gym_id, member_subscription_id, month_index);

create index if not exists idx_subscription_month_services_service
    on subscription_month_services(gym_id, service_type_id, month_index);

create table if not exists trainer_assignments (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_id uuid not null references members(id) on delete cascade,
    trainer_user_id uuid null references users(id) on delete set null,
    member_subscription_id uuid null references member_subscriptions(id) on delete set null,
    from_date date not null,
    to_date date null,
    notes text null,
    assigned_by_user_id uuid not null references users(id) on delete restrict,
    created_at timestamp with time zone not null default now()
);

create index if not exists idx_trainer_assignments_gym_member_from
    on trainer_assignments(gym_id, member_id, from_date);

create index if not exists idx_trainer_assignments_gym_trainer_from
    on trainer_assignments(gym_id, trainer_user_id, from_date);

-- ------------------------------------------------------------
-- Billing foundation
-- ------------------------------------------------------------
create table if not exists invoices (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_id uuid not null references members(id) on delete cascade,
    invoice_number varchar(40) not null,
    invoice_date date not null,
    due_date date null,
    status varchar(20) not null default 'ISSUED',
    total_amount numeric(10,2) not null default 0,
    paid_amount numeric(10,2) not null default 0,
    balance_amount numeric(10,2) not null default 0,
    notes text null,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now(),
    constraint uq_invoices_gym_invoice_no unique(gym_id, invoice_number)
);

create index if not exists idx_invoices_gym_member_date
    on invoices(gym_id, member_id, invoice_date);

create table if not exists invoice_line_items (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    invoice_id uuid not null references invoices(id) on delete cascade,
    service_type_id uuid null references service_types(id) on delete set null,
    service_plan_id uuid null references service_plans(id) on delete set null,
    description varchar(255) not null,
    quantity numeric(10,2) not null default 1,
    unit_price numeric(10,2) not null default 0,
    line_total numeric(10,2) not null default 0,
    coverage_start date null,
    coverage_end date null,
    created_at timestamp with time zone not null default now()
);

create index if not exists idx_invoice_items_gym_invoice
    on invoice_line_items(gym_id, invoice_id);

create table if not exists payment_allocations (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    payment_id uuid not null references payments(id) on delete cascade,
    invoice_id uuid not null references invoices(id) on delete cascade,
    invoice_line_item_id uuid null references invoice_line_items(id) on delete set null,
    amount numeric(10,2) not null,
    created_at timestamp with time zone not null default now()
);

create index if not exists idx_payment_allocations_gym_payment
    on payment_allocations(gym_id, payment_id);

create index if not exists idx_payment_allocations_gym_invoice
    on payment_allocations(gym_id, invoice_id);

-- ------------------------------------------------------------
-- Attendance + body metrics
-- ------------------------------------------------------------
create table if not exists member_checkins (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_id uuid not null references members(id) on delete cascade,
    checkin_date date not null,
    checkin_at timestamp with time zone not null default now(),
    source varchar(20) not null default 'MEMBER_SELF',
    created_by_user_id uuid null references users(id) on delete set null,
    notes text null,
    created_at timestamp with time zone not null default now(),
    constraint uq_checkin_one_per_day unique(gym_id, member_id, checkin_date)
);

create index if not exists idx_member_checkins_gym_date
    on member_checkins(gym_id, checkin_date);

create table if not exists attendance_policies (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    name varchar(100) not null,
    checkin_start_time time null,
    checkin_end_time time null,
    is_geofence_enabled boolean not null default false,
    geofence_radius_meters integer null,
    is_active boolean not null default true,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now()
);

create index if not exists idx_attendance_policy_gym_active
    on attendance_policies(gym_id, is_active);

create table if not exists member_body_metrics (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    member_id uuid not null references members(id) on delete cascade,
    metric_date date not null,
    weight_kg numeric(6,2) null,
    body_fat_percent numeric(5,2) null,
    bmi numeric(5,2) null,
    source varchar(20) not null default 'MEMBER',
    recorded_by_user_id uuid null references users(id) on delete set null,
    notes text null,
    created_at timestamp with time zone not null default now()
);

create index if not exists idx_member_body_metrics_gym_member_date
    on member_body_metrics(gym_id, member_id, metric_date);

-- ------------------------------------------------------------
-- Login events + notification outbox
-- ------------------------------------------------------------
create table if not exists login_events (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid null references gyms(id) on delete set null,
    user_id uuid null references users(id) on delete set null,
    email varchar(100) null,
    role varchar(20) null,
    ip_address varchar(60) null,
    user_agent varchar(1000) null,
    device_fingerprint varchar(120) null,
    success boolean not null,
    failure_reason varchar(255) null,
    occurred_at timestamp with time zone not null default now()
);

create index if not exists idx_login_events_gym_user_time
    on login_events(gym_id, user_id, occurred_at);

create index if not exists idx_login_events_email_time
    on login_events(email, occurred_at);

create table if not exists notification_outbox (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid null references gyms(id) on delete set null,
    user_id uuid null references users(id) on delete set null,
    member_id uuid null references members(id) on delete set null,
    event_type varchar(80) not null,
    channel varchar(20) not null default 'EMAIL',
    to_address varchar(255) not null,
    subject varchar(255) null,
    payload_json text null,
    status varchar(20) not null default 'PENDING',
    retry_count integer not null default 0,
    next_attempt_at timestamp with time zone null,
    last_error text null,
    sent_at timestamp with time zone null,
    idempotency_key varchar(120) null,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now()
);

create index if not exists idx_notification_outbox_status_retry
    on notification_outbox(status, next_attempt_at);

create index if not exists idx_notification_outbox_gym_created
    on notification_outbox(gym_id, created_at);

create unique index if not exists idx_notification_outbox_idempotency
    on notification_outbox(idempotency_key)
    where idempotency_key is not null;

-- ------------------------------------------------------------
-- Enquiry CRM (visible to owner + trainer)
-- ------------------------------------------------------------
create table if not exists enquiries (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    full_name varchar(100) not null,
    phone varchar(20) not null,
    email varchar(100) null,
    source varchar(50) null,
    interested_service_type_id uuid null references service_types(id) on delete set null,
    stage varchar(20) not null default 'NEW',
    next_followup_at timestamp with time zone null,
    assigned_to_user_id uuid null references users(id) on delete set null,
    converted_member_id uuid null references members(id) on delete set null,
    notes text null,
    created_at timestamp with time zone not null default now(),
    updated_at timestamp with time zone not null default now()
);

create index if not exists idx_enquiries_gym_stage_followup
    on enquiries(gym_id, stage, next_followup_at);

create index if not exists idx_enquiries_gym_phone
    on enquiries(gym_id, phone);

create table if not exists enquiry_followups (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    enquiry_id uuid not null references enquiries(id) on delete cascade,
    followup_at timestamp with time zone not null default now(),
    next_followup_at timestamp with time zone null,
    outcome varchar(50) null,
    notes text null,
    created_by_user_id uuid not null references users(id) on delete restrict,
    created_at timestamp with time zone not null default now()
);

create index if not exists idx_enquiry_followups_gym_enquiry_at
    on enquiry_followups(gym_id, enquiry_id, followup_at);

create table if not exists enquiry_stage_history (
    id uuid primary key default gen_random_uuid(),
    gym_id uuid not null references gyms(id) on delete cascade,
    enquiry_id uuid not null references enquiries(id) on delete cascade,
    from_stage varchar(20) null,
    to_stage varchar(20) not null,
    changed_by_user_id uuid not null references users(id) on delete restrict,
    reason text null,
    changed_at timestamp with time zone not null default now()
);

create index if not exists idx_enquiry_stage_history_gym_enquiry_at
    on enquiry_stage_history(gym_id, enquiry_id, changed_at);

-- ------------------------------------------------------------
-- Optional seed: GENERAL + PERSONAL_TRAINING service type per gym
-- ------------------------------------------------------------
insert into service_types(gym_id, code, display_name, is_active, sort_order)
select g.id, seed.code, seed.display_name, true, seed.sort_order
from gyms g
cross join (
    values
        ('GENERAL', 'General Membership', 1),
        ('PERSONAL_TRAINING', 'Personal Training', 2)
) as seed(code, display_name, sort_order)
where not exists (
    select 1
    from service_types st
    where st.gym_id = g.id
      and st.code = seed.code
);
