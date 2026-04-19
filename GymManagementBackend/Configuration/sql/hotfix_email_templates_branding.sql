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
  '<div style=''background:#f3f5f9;padding:18px 10px;font-family:Segoe UI,Arial,sans-serif;color:#0f1c2e''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:620px;margin:0 auto;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;overflow:hidden''>
       <tr><td style=''padding:16px 22px;border-bottom:1px solid #eef2f7''><span style=''font-size:30px;color:#0b7a75;vertical-align:middle''>?</span><span style=''font-weight:800;font-size:32px;letter-spacing:2px;margin-left:8px;color:#0c6f87;vertical-align:middle''>GYMMANAGER9353</span></td></tr>
       <tr><td style=''padding:24px 22px''>
         <div style=''background:#f7f9fc;border:1px solid #e5ebf3;border-radius:14px;padding:18px''>
           <h2 style=''margin:0 0 10px;font-size:38px;line-height:1.2;color:#0f1c2e''>Welcome to the GymManager9353 family, {OwnerName}!</h2>
           <p style=''margin:0;color:#334155;font-size:25px;line-height:1.55''>Your gym <strong>{GymName}</strong> is now live. Your dashboard is ready to manage members, schedules, and growth.</p>
         </div>
       </td></tr>
       <tr><td style=''padding:0 22px 18px''><img src=''{BrandImageUrl}'' alt=''Gym banner'' style=''width:100%;display:block;border-radius:14px''/></td></tr>
       <tr><td style=''padding:0 22px 22px''>
         <table role=''presentation'' width=''100%'' style=''border-collapse:separate;border-spacing:0 10px''>
           <tr><td style=''background:#f8fafc;border:1px solid #e5ebf2;border-radius:12px;padding:12px 14px;font-size:24px;line-height:1.4''>Set up your gym profile and branding.</td></tr>
           <tr><td style=''background:#f8fafc;border:1px solid #e5ebf2;border-radius:12px;padding:12px 14px;font-size:24px;line-height:1.4''>Add owners, staff, and trainers.</td></tr>
           <tr><td style=''background:#f8fafc;border:1px solid #e5ebf2;border-radius:12px;padding:12px 14px;font-size:24px;line-height:1.4''>Track renewals, attendance, and revenue.</td></tr>
         </table>
       </td></tr>
       <tr><td style=''padding:0 22px 28px;text-align:center''><a href=''{LoginUrl}'' style=''display:inline-block;background:#0a809a;color:#ffffff;text-decoration:none;font-weight:700;font-size:28px;padding:14px 30px;border-radius:999px''>Login to Dashboard</a></td></tr>
     </table>
   </div>',
  'https://images.unsplash.com/photo-1534438327276-14e5300c3a48?auto=format&fit=crop&w=1200&q=80',
  'https://gymmanager9353.com/login',
  true
),
(
  'user_welcome',
  'Your {Role} account is ready',
  '<div style=''background:#f3f5f9;padding:18px 10px;font-family:Segoe UI,Arial,sans-serif;color:#0f1c2e''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:620px;margin:0 auto;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;overflow:hidden''>
       <tr><td style=''padding:16px 22px;border-bottom:1px solid #eef2f7''><span style=''font-size:30px;color:#0b7a75;vertical-align:middle''>?</span><span style=''font-weight:800;font-size:32px;letter-spacing:1px;margin-left:8px;color:#0c6f87;vertical-align:middle''>GymManager9353</span></td></tr>
       <tr><td style=''padding:24px 22px''>
         <div style=''background:#f7f9fc;border:1px solid #e5ebf3;border-radius:14px;padding:18px''>
           <h2 style=''margin:0 0 10px;font-size:40px;line-height:1.2;color:#0f1c2e''>{Headline}</h2>
           <p style=''margin:0;color:#334155;font-size:25px;line-height:1.55''>{BodyText}</p>
         </div>
       </td></tr>
       <tr><td style=''padding:0 22px 18px''><img src=''{BrandImageUrl}'' alt=''Gym banner'' style=''width:100%;display:block;border-radius:14px''/></td></tr>
       <tr><td style=''padding:0 22px 18px''>
         <div style=''background:#ffffff;border:1px solid #dce5f0;border-radius:14px;padding:16px''>
           <p style=''margin:0 0 10px;color:#334155;font-weight:700;font-size:20px;letter-spacing:.04em;text-transform:uppercase''>Access Credentials</p>
           <table role=''presentation'' width=''100%''><tr>
             <td style=''width:50%;padding-right:8px''>
               <div style=''background:#f7f8fb;border-radius:10px;border:1px solid #e7ecf3;padding:10px 12px''>
                 <div style=''font-size:18px;color:#64748b;text-transform:uppercase;font-weight:700;letter-spacing:.04em''>Email Address</div>
                 <div style=''font-size:24px;font-weight:700;color:#0c6f87;margin-top:6px''>{LoginId}</div>
               </div>
             </td>
             <td style=''width:50%;padding-left:8px''>
               <div style=''background:#f7f8fb;border-radius:10px;border:1px solid #e7ecf3;padding:10px 12px''>
                 <div style=''font-size:18px;color:#64748b;text-transform:uppercase;font-weight:700;letter-spacing:.04em''>Temporary Password</div>
                 <div style=''font-size:24px;font-weight:700;color:#0c6f87;margin-top:6px''>{TemporaryPassword}</div>
               </div>
             </td>
           </tr></table>
           <p style=''margin:12px 0 0;color:#334155;font-size:20px''>Please change your password after first login for security.</p>
         </div>
       </td></tr>
       <tr><td style=''padding:0 22px 18px''>
         <p style=''margin:0 0 10px;font-weight:800;letter-spacing:.08em;color:#475569;text-transform:uppercase;font-size:20px''>What you can do next</p>
         <table role=''presentation'' width=''100%''><tr>
           <td style=''width:33.33%;padding-right:6px;vertical-align:top''><div style=''background:#ffffff;border:1px solid #e5ebf3;border-radius:12px;padding:10px;font-size:21px;line-height:1.35''>{FeatureOne}</div></td>
           <td style=''width:33.33%;padding:0 3px;vertical-align:top''><div style=''background:#ffffff;border:1px solid #e5ebf3;border-radius:12px;padding:10px;font-size:21px;line-height:1.35''>{FeatureTwo}</div></td>
           <td style=''width:33.33%;padding-left:6px;vertical-align:top''><div style=''background:#ffffff;border:1px solid #e5ebf3;border-radius:12px;padding:10px;font-size:21px;line-height:1.35''>{FeatureThree}</div></td>
         </tr></table>
       </td></tr>
       <tr><td style=''padding:0 22px 28px;text-align:center''><a href=''{LoginUrl}'' style=''display:inline-block;background:#0a809a;color:#ffffff;text-decoration:none;font-weight:700;font-size:28px;padding:14px 30px;border-radius:999px''>Login to Dashboard</a></td></tr>
     </table>
   </div>',
  'https://images.unsplash.com/photo-1570829460005-c840387bb1ca?auto=format&fit=crop&w=1200&q=80',
  'https://gymmanager9353.com/login',
  true
),
(
  'member_welcome',
  'Your Gym Member Access Details',
  '<div style=''background:#f3f5f9;padding:18px 10px;font-family:Segoe UI,Arial,sans-serif;color:#0f1c2e''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:620px;margin:0 auto;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;overflow:hidden''>
       <tr><td style=''padding:16px 22px;border-bottom:1px solid #eef2f7''><span style=''font-size:30px;color:#0b7a75;vertical-align:middle''>?</span><span style=''font-weight:800;font-size:32px;letter-spacing:1px;margin-left:8px;color:#0c6f87;vertical-align:middle''>GymManager9353</span></td></tr>
       <tr><td style=''padding:24px 22px''>
         <div style=''background:#f7f9fc;border:1px solid #e5ebf3;border-radius:14px;padding:18px''>
           <h2 style=''margin:0 0 10px;font-size:40px;line-height:1.2;color:#0f1c2e''>Welcome to the team, {FullName}!</h2>
           <p style=''margin:0;color:#334155;font-size:25px;line-height:1.55''>Your membership at <strong>{GymName}</strong> is now active through GymManager9353.</p>
         </div>
       </td></tr>
       <tr><td style=''padding:0 22px 18px''><img src=''{BrandImageUrl}'' alt=''Gym banner'' style=''width:100%;display:block;border-radius:14px''/></td></tr>
       <tr><td style=''padding:0 22px 18px''>
         <div style=''background:#ffffff;border:1px solid #dce5f0;border-radius:14px;padding:16px''>
           <p style=''margin:0 0 10px;color:#334155;font-weight:700;font-size:20px;letter-spacing:.04em;text-transform:uppercase''>Access Credentials</p>
           <div style=''background:#f7f8fb;border-radius:10px;border:1px solid #e7ecf3;padding:10px 12px;margin-bottom:10px''>
             <div style=''font-size:18px;color:#64748b;text-transform:uppercase;font-weight:700;letter-spacing:.04em''>Email Address</div>
             <div style=''font-size:24px;font-weight:700;color:#0c6f87;margin-top:6px''>{LoginId}</div>
           </div>
           <div style=''background:#f7f8fb;border-radius:10px;border:1px solid #e7ecf3;padding:10px 12px''>
             <div style=''font-size:18px;color:#64748b;text-transform:uppercase;font-weight:700;letter-spacing:.04em''>Temporary Password</div>
             <div style=''font-size:24px;font-weight:700;color:#0c6f87;margin-top:6px''>{TemporaryPassword}</div>
           </div>
           <p style=''margin:12px 0 0;color:#334155;font-size:20px''>For security, we recommend changing your password after logging in.</p>
         </div>
       </td></tr>
       <tr><td style=''padding:0 22px 18px''>
         <p style=''margin:0 0 10px;font-weight:800;letter-spacing:.08em;color:#475569;text-transform:uppercase;font-size:20px''>What you can do</p>
         <table role=''presentation'' width=''100%'' style=''border-collapse:separate;border-spacing:0 10px''>
           <tr><td style=''background:#ffffff;border:1px solid #e5ebf3;border-radius:12px;padding:12px 14px;font-size:24px;line-height:1.4''>Book your favorite classes in seconds.</td></tr>
           <tr><td style=''background:#ffffff;border:1px solid #e5ebf3;border-radius:12px;padding:12px 14px;font-size:24px;line-height:1.4''>Track your PRs and fitness journey.</td></tr>
           <tr><td style=''background:#ffffff;border:1px solid #e5ebf3;border-radius:12px;padding:12px 14px;font-size:24px;line-height:1.4''>View your membership plan and payments.</td></tr>
         </table>
       </td></tr>
       <tr><td style=''padding:0 22px 18px;text-align:center''><a href=''{LoginUrl}'' style=''display:inline-block;background:#0a809a;color:#ffffff;text-decoration:none;font-weight:700;font-size:28px;padding:14px 30px;border-radius:999px''>Login to Dashboard</a></td></tr>
     </table>
   </div>',
  'https://images.unsplash.com/photo-1549060279-7e168fcee0c2?auto=format&fit=crop&w=1200&q=80',
  'https://gymmanager9353.com/login',
  true
),
(
  'password_reset_code',
  'Your Password Reset Code',
  '<div style=''background:#f3f5f9;padding:18px 10px;font-family:Segoe UI,Arial,sans-serif;color:#0f1c2e''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:620px;margin:0 auto;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;overflow:hidden''>
       <tr><td style=''padding:16px 22px;border-bottom:1px solid #eef2f7''><span style=''font-size:30px;color:#0b7a75;vertical-align:middle''>?</span><span style=''font-weight:800;font-size:32px;letter-spacing:1px;margin-left:8px;color:#0c6f87;vertical-align:middle''>GymManager9353</span></td></tr>
       <tr><td style=''padding:24px 22px''>
         <div style=''background:#f7f9fc;border:1px solid #e5ebf3;border-radius:14px;padding:18px''>
           <h2 style=''margin:0 0 10px;font-size:40px;line-height:1.2;color:#0f1c2e''>Password reset request</h2>
           <p style=''margin:0;color:#334155;font-size:25px;line-height:1.55''>Hi {FullName}, use this 6-digit code to reset your password. Code expires in 10 minutes.</p>
         </div>
       </td></tr>
       <tr><td style=''padding:0 22px 18px''>
         <div style=''background:#ffffff;border:1px solid #dce5f0;border-radius:14px;padding:20px;text-align:center''>
           <div style=''font-size:18px;color:#64748b;text-transform:uppercase;font-weight:700;letter-spacing:.08em''>Reset Code</div>
           <div style=''font-size:44px;font-weight:800;color:#0c6f87;letter-spacing:10px;margin-top:8px''>{Code}</div>
         </div>
       </td></tr>
       <tr><td style=''padding:0 22px 28px;text-align:center''><a href=''{LoginUrl}'' style=''display:inline-block;background:#0a809a;color:#ffffff;text-decoration:none;font-weight:700;font-size:28px;padding:14px 30px;border-radius:999px''>Back to Login</a></td></tr>
     </table>
   </div>',
  'https://images.unsplash.com/photo-1570829460005-c840387bb1ca?auto=format&fit=crop&w=1200&q=80',
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
