# Gym Management Backend (ASP.NET Core 8)

Secure multi-tenant API for gym owners and admins.

## Stack
- ASP.NET Core 8
- Entity Framework Core + PostgreSQL (Npgsql)
- JWT access tokens + DB-backed refresh token rotation/revocation

## Run Locally
1. Go to project:
```bash
cd GymManagementBackend
```

2. Set environment variables (PowerShell):
```powershell
$env:ConnectionStrings__DefaultConnection="postgresql://postgres:<YOUR_PASSWORD>@<YOUR_SUPABASE_HOST>:5432/postgres"
$env:Jwt__Secret="your-very-strong-secret-at-least-32-characters"
$env:Jwt__Issuer="GymManagementAPI"
$env:Jwt__Audience="GymManagementApp"
$env:Jwt__ExpirationMinutes="60"
$env:Jwt__RefreshTokenExpirationDays="7"
```

3. Apply migrations:
```bash
dotnet ef database update
```

4. Run API:
```bash
dotnet run
```

## Deploy On Render (Docker)
1. Create a `Web Service` and select `Docker` runtime.
2. Keep Dockerfile path as `Dockerfile` (repo root).
3. Add these environment variables in Render:
```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<your-project-ref>;Password=<your-db-password>;SSL Mode=Require;Trust Server Certificate=true
Jwt__Secret=<at-least-32-char-random-secret>
Jwt__Issuer=GymManagementAPI
Jwt__Audience=GymManagementApp
Jwt__ExpirationMinutes=60
Jwt__RefreshTokenExpirationDays=7
Cors__AllowedOrigins__0=https://<your-frontend>.vercel.app
```
4. Do not add DB/JWT secrets into `appsettings.json`; keep them only in Render env vars.

## Auth Endpoints
- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout` (requires access token + refresh token in body)
- `GET /api/auth/me`
- `GET /api/auth/verify`

## Core Endpoints
- Admin gyms: `GET/POST/PUT /api/gyms`
- Users: `GET /api/users` (owner sees own gym; admin can filter all), admin `POST/PUT /api/users`
- Members: `GET/POST/PUT/DELETE /api/members`, `POST /api/members/search`
- Dashboard: `GET /api/dashboard/stats|overview|trends|recent-members`

## Security Notes
- No DB credentials are stored in tracked config.
- JWT role policies enforced:
  - `AdminOnly`
  - `OwnerOrAdmin`
  - `StaffOrAbove`
- Refresh tokens are hashed before storage and rotated on refresh.
- CORS is restricted by configured allowed origins.
