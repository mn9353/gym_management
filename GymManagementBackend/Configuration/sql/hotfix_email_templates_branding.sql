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
  '<div style=''margin:0;padding:24px 12px;background:#f4f7fb;font-family:Segoe UI,Arial,sans-serif;color:#0f172a''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden''>
       <tr>
         <td style=''padding:18px 24px;border-bottom:1px solid #eef2f7''>
           <div style=''font-size:20px;font-weight:800;letter-spacing:.06em;color:#0f766e''>GYMMANAGER9353</div>
         </td>
       </tr>
       <tr>
         <td style=''padding:24px''>
           <h1 style=''margin:0 0 10px;font-size:28px;line-height:1.25;color:#0f172a''>Welcome, {OwnerName}</h1>
           <p style=''margin:0 0 16px;font-size:15px;line-height:1.7;color:#334155''>
             Your gym <strong>{GymName}</strong> is now live. You can start onboarding your team and managing operations right away.
           </p>
           <img src=''{BrandImageUrl}'' alt=''Gym manager banner'' style=''width:100%;max-width:592px;height:auto;display:block;border-radius:12px;border:1px solid #e5e7eb'' />
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 22px''>
           <div style=''background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:14px''>
             <div style=''font-size:14px;line-height:1.7;color:#334155''>Set up branding, add staff and trainers, and monitor member growth and renewals.</div>
           </div>
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 28px;text-align:center''>
           <a href=''{LoginUrl}'' style=''display:inline-block;padding:12px 24px;border-radius:999px;background:#0f766e;color:#ffffff;text-decoration:none;font-size:14px;font-weight:700''>Open Dashboard</a>
         </td>
       </tr>
     </table>
   </div>',
  'https://images.unsplash.com/photo-1534438327276-14e5300c3a48?auto=format&fit=crop&w=1200&q=80',
  'https://gymmanager9353.com/login',
  true
),
(
  'user_welcome',
  'Your {Role} account is ready',
  '<div style=''margin:0;padding:24px 12px;background:#f4f7fb;font-family:Segoe UI,Arial,sans-serif;color:#0f172a''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden''>
       <tr>
         <td style=''padding:18px 24px;border-bottom:1px solid #eef2f7''>
           <div style=''font-size:20px;font-weight:800;letter-spacing:.06em;color:#0f766e''>GYMMANAGER9353</div>
         </td>
       </tr>
       <tr>
         <td style=''padding:24px''>
           <h1 style=''margin:0 0 10px;font-size:28px;line-height:1.25;color:#0f172a''>{Headline}</h1>
           <p style=''margin:0 0 16px;font-size:15px;line-height:1.7;color:#334155''>{BodyText}</p>
           <img src=''{BrandImageUrl}'' alt=''Gym manager banner'' style=''width:100%;max-width:592px;height:auto;display:block;border-radius:12px;border:1px solid #e5e7eb'' />
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 16px''>
           <div style=''background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:14px''>
             <p style=''margin:0 0 10px;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#475569''>Access Credentials</p>
             <table role=''presentation'' width=''100%'' cellspacing=''0'' cellpadding=''0''>
               <tr>
                 <td style=''width:50%;padding-right:6px;vertical-align:top''>
                   <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px''>
                     <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Email</div>
                     <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>{LoginId}</div>
                   </div>
                 </td>
                 <td style=''width:50%;padding-left:6px;vertical-align:top''>
                   <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px''>
                     <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Temporary Password</div>
                     <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>{TemporaryPassword}</div>
                   </div>
                 </td>
               </tr>
             </table>
             <p style=''margin:10px 0 0;font-size:13px;color:#334155''>Please change your password after your first login.</p>
           </div>
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 16px''>
           <p style=''margin:0 0 8px;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#475569''>What You Can Do Next</p>
           <table role=''presentation'' width=''100%'' cellspacing=''0'' cellpadding=''0''>
             <tr>
               <td style=''width:33.33%;padding-right:4px;vertical-align:top''><div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px;font-size:13px;line-height:1.5;color:#334155''>{FeatureOne}</div></td>
               <td style=''width:33.33%;padding:0 2px;vertical-align:top''><div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px;font-size:13px;line-height:1.5;color:#334155''>{FeatureTwo}</div></td>
               <td style=''width:33.33%;padding-left:4px;vertical-align:top''><div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px;font-size:13px;line-height:1.5;color:#334155''>{FeatureThree}</div></td>
             </tr>
           </table>
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 28px;text-align:center''>
           <a href=''{LoginUrl}'' style=''display:inline-block;padding:12px 24px;border-radius:999px;background:#0f766e;color:#ffffff;text-decoration:none;font-size:14px;font-weight:700''>Open Dashboard</a>
         </td>
       </tr>
     </table>
   </div>',
  'https://images.unsplash.com/photo-1570829460005-c840387bb1ca?auto=format&fit=crop&w=1200&q=80',
  'https://gymmanager9353.com/login',
  true
),
(
  'member_welcome',
  'Your Gym Member Access Details',
  '<div style=''margin:0;padding:24px 12px;background:#f4f7fb;font-family:Segoe UI,Arial,sans-serif;color:#0f172a''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden''>
       <tr>
         <td style=''padding:18px 24px;border-bottom:1px solid #eef2f7''>
           <div style=''font-size:20px;font-weight:800;letter-spacing:.06em;color:#0f766e''>GYMMANAGER9353</div>
         </td>
       </tr>
       <tr>
         <td style=''padding:24px''>
           <h1 style=''margin:0 0 10px;font-size:28px;line-height:1.25;color:#0f172a''>Welcome, {FullName}</h1>
           <p style=''margin:0 0 16px;font-size:15px;line-height:1.7;color:#334155''>
             Your membership at <strong>{GymName}</strong> is active. Here are your login and subscription details.
           </p>
           <img src=''{BrandImageUrl}'' alt=''Gym manager banner'' style=''width:100%;max-width:592px;height:auto;display:block;border-radius:12px;border:1px solid #e5e7eb'' />
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 14px''>
           <div style=''background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:14px''>
             <p style=''margin:0 0 10px;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#475569''>Access Credentials</p>
             <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px;margin-bottom:8px''>
               <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Email</div>
               <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>{LoginId}</div>
             </div>
             <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px''>
               <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Temporary Password</div>
               <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>{TemporaryPassword}</div>
             </div>
           </div>
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 14px''>
           <div style=''background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:14px''>
             <p style=''margin:0 0 10px;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#475569''>Membership Snapshot</p>
             <table role=''presentation'' width=''100%'' cellspacing=''0'' cellpadding=''0''>
               <tr>
                 <td style=''width:50%;padding-right:6px;vertical-align:top''>
                   <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px;margin-bottom:8px''>
                     <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Join Date</div>
                     <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>{JoinDate}</div>
                   </div>
                 </td>
                 <td style=''width:50%;padding-left:6px;vertical-align:top''>
                   <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px;margin-bottom:8px''>
                     <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Plan Expiry</div>
                     <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>{PlanEndDate}</div>
                   </div>
                 </td>
               </tr>
               <tr>
                 <td style=''width:50%;padding-right:6px;vertical-align:top''>
                   <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px''>
                     <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Total Amount</div>
                     <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>Rs {AmountToPay}</div>
                   </div>
                 </td>
                 <td style=''width:50%;padding-left:6px;vertical-align:top''>
                   <div style=''background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;padding:10px''>
                     <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#64748b''>Paid So Far</div>
                     <div style=''margin-top:6px;font-size:14px;font-weight:700;color:#0f172a''>Rs {AmountPaid}</div>
                   </div>
                 </td>
               </tr>
             </table>
             <div style=''margin-top:8px;background:#ecfdf5;border:1px solid #bbf7d0;border-radius:10px;padding:10px''>
               <div style=''font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#166534''>Payment Status</div>
               <div style=''margin-top:6px;font-size:14px;font-weight:800;color:#166534''>{PaymentStatusLabel} ({PaymentStatus})</div>
             </div>
           </div>
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 16px''>
           <div style=''font-size:13px;line-height:1.7;color:#334155''>For security, please change your password after first login.</div>
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 28px;text-align:center''>
           <a href=''{LoginUrl}'' style=''display:inline-block;padding:12px 24px;border-radius:999px;background:#0f766e;color:#ffffff;text-decoration:none;font-size:14px;font-weight:700''>Open Dashboard</a>
         </td>
       </tr>
     </table>
   </div>',
  'https://images.unsplash.com/photo-1549060279-7e168fcee0c2?auto=format&fit=crop&w=1200&q=80',
  'https://gymmanager9353.com/login',
  true
),
(
  'password_reset_code',
  'Your Password Reset Code',
  '<div style=''margin:0;padding:24px 12px;background:#f4f7fb;font-family:Segoe UI,Arial,sans-serif;color:#0f172a''>
     <table role=''presentation'' cellspacing=''0'' cellpadding=''0'' width=''100%'' style=''max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden''>
       <tr>
         <td style=''padding:18px 24px;border-bottom:1px solid #eef2f7''>
           <div style=''font-size:20px;font-weight:800;letter-spacing:.06em;color:#0f766e''>GYMMANAGER9353</div>
         </td>
       </tr>
       <tr>
         <td style=''padding:24px''>
           <h1 style=''margin:0 0 10px;font-size:28px;line-height:1.25;color:#0f172a''>Password Reset Request</h1>
           <p style=''margin:0 0 12px;font-size:15px;line-height:1.7;color:#334155''>
             Hi {FullName}, use the verification code below to reset your password. This code expires in 10 minutes.
           </p>
           <div style=''background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;text-align:center''>
             <div style=''font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#64748b''>Verification Code</div>
             <div style=''margin-top:8px;font-size:34px;font-weight:800;letter-spacing:8px;color:#0f766e''>{Code}</div>
           </div>
         </td>
       </tr>
       <tr>
         <td style=''padding:0 24px 28px;text-align:center''>
           <a href=''{LoginUrl}'' style=''display:inline-block;padding:12px 24px;border-radius:999px;background:#0f766e;color:#ffffff;text-decoration:none;font-size:14px;font-weight:700''>Back to Login</a>
         </td>
       </tr>
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
