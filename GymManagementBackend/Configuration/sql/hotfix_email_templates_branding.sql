-- DB-driven email templates + branding fields.
-- Run this once in Supabase SQL editor.

create table if not exists email_templates (
    id uuid primary key default gen_random_uuid(),
    template_key varchar(80) not null unique,
    subject_template varchar(255) not null,
    html_template text not null,
    hero_image_url varchar(500) null,
    login_url varchar(500) null,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

-- Seed default templates (safe to run repeatedly due to ON CONFLICT).
insert into email_templates (template_key, subject_template, html_template, hero_image_url, login_url, is_active)
values
(
  'gym_created',
  'Gym Created: {GymName}',
  '<div style=''font-family:Segoe UI,Arial,sans-serif;line-height:1.55;color:#12263f''>
     <h2>Welcome to Gym Manager</h2>
     <p>Hi {OwnerName},</p>
     <p>Your gym <strong>{GymName}</strong> was created successfully.</p>
     <p><a href=''{LoginUrl}'' style=''color:#0b7a75;font-weight:700''>Sign in to Gym Manager</a></p>
   </div>',
  'https://gymmanager9353.com/logo.png',
  'https://gymmanager9353.com/login',
  true
),
(
  'user_welcome',
  'Your {Role} account is ready',
  '<div style=''font-family:Segoe UI,Arial,sans-serif;line-height:1.55;color:#12263f''>
     <h2>Welcome to Gym Manager</h2>
     <p>Hi {FullName},</p>
     <p>{Headline}</p>
     <p>{BodyText}</p>
     <div style=''background:#f7fafe;border:1px solid #d9e6ff;border-radius:12px;padding:12px 14px''>
       <p style=''margin:0 0 6px''><strong>Gym:</strong> {GymName}</p>
       <p style=''margin:0 0 6px''><strong>Role:</strong> {Role}</p>
       <p style=''margin:0 0 6px''><strong>User ID:</strong> {LoginId}</p>
       <p style=''margin:0''><strong>Temporary Password:</strong> {TemporaryPassword}</p>
     </div>
     <p style=''margin-top:10px''><a href=''{LoginUrl}'' style=''color:#0b7a75;font-weight:700''>Sign in now</a></p>
   </div>',
  'https://gymmanager9353.com/logo.png',
  'https://gymmanager9353.com/login',
  true
),
(
  'member_welcome',
  'Your Gym Member Access Details',
  '<div style=''font-family:Segoe UI,Arial,sans-serif;line-height:1.55;color:#12263f''>
     <h2>Welcome to {GymName}</h2>
     <p>Hi {FullName},</p>
     <div style=''background:#f7fafe;border:1px solid #d9e6ff;border-radius:12px;padding:12px 14px''>
       <p style=''margin:0 0 6px''><strong>User ID:</strong> {LoginId}</p>
       <p style=''margin:0''><strong>Temporary Password:</strong> {TemporaryPassword}</p>
     </div>
     <p style=''margin-top:10px''><a href=''{LoginUrl}'' style=''color:#0b7a75;font-weight:700''>Sign in here</a></p>
   </div>',
  'https://gymmanager9353.com/logo.png',
  'https://gymmanager9353.com/login',
  true
)
on conflict (template_key) do update
set subject_template = excluded.subject_template,
    html_template = excluded.html_template,
    hero_image_url = excluded.hero_image_url,
    login_url = excluded.login_url,
    is_active = excluded.is_active,
    updated_at = now();

