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
