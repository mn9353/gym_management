using System.Text;
using GymManagementBackend.Configuration;
using GymManagementBackend.Constants;
using GymManagementBackend.Data;
using GymManagementBackend.Services;
using GymManagementBackend.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MembershipStatusJobSettings>(builder.Configuration.GetSection("MembershipStatusJob"));
builder.Services.Configure<EmailNotificationSettings>(builder.Configuration.GetSection("EmailNotifications"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddHttpClient("ResendApi", client =>
{
    client.BaseAddress = new Uri("https://api.resend.com");
});

if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be configured and at least 32 characters.");
}

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(rawConnectionString))
{
    rawConnectionString = builder.Configuration["DATABASE_URL"];
}
if (string.IsNullOrWhiteSpace(rawConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is required. Set it via user-secrets, env var, appsettings.Local.json, or DATABASE_URL.");
}

var connectionString = BuildNpgsqlConnectionString(rawConnectionString);
var connectionInfo = DescribeConnectionString(connectionString);
Console.WriteLine($"[DB] Environment={builder.Environment.EnvironmentName}");
Console.WriteLine($"[DB] Host={connectionInfo.Host}; Port={connectionInfo.Port}; Database={connectionInfo.Database}; Username={connectionInfo.Username}");

if (connectionInfo.Host.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase) &&
    string.Equals(connectionInfo.Username, "postgres", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Supabase pooler requires Username format 'postgres.<project-ref>'. " +
        "Current username resolved to 'postgres'. Check ConnectionStrings__DefaultConnection overrides.");
}

builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseNpgsql(connectionString));

var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});



builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
    options.AddPolicy("OwnerOrAdmin", policy => policy.RequireRole(AppRoles.Owner, AppRoles.Admin));
    options.AddPolicy("StaffOrAbove", policy => policy.RequireRole(AppRoles.Staff, AppRoles.Trainer, AppRoles.Owner, AppRoles.Admin));
    options.AddPolicy("TrainerOrAbove", policy => policy.RequireRole(AppRoles.Trainer, AppRoles.Staff, AppRoles.Owner, AppRoles.Admin));
    options.AddPolicy("MemberOnly", policy => policy.RequireRole(AppRoles.Member));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        var normalizedOrigins = allowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .ToArray();

        var hasWildcard = normalizedOrigins.Any(origin => origin == "*");

        if (hasWildcard)
        {
            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins cannot contain '*' outside Development. Configure explicit frontend origin(s).");
            }

            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
            return;
        }

        if (normalizedOrigins.Length == 0)
        {
            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must include at least one explicit origin in non-development environments.");
            }

            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
            return;
        }

        var wildcardSubdomainOrigins = normalizedOrigins
            .Where(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                return uri.Host.StartsWith("*.");
            })
            .ToArray();

        var explicitOrigins = normalizedOrigins
            .Except(wildcardSubdomainOrigins, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (wildcardSubdomainOrigins.Length > 0)
        {
            var wildcardDomainRules = wildcardSubdomainOrigins
                .Select(origin =>
                {
                    var uri = new Uri(origin);
                    return new
                    {
                        Scheme = uri.Scheme,
                        Domain = uri.Host[2..] // strip "*."
                    };
                })
                .ToArray();

            policy.SetIsOriginAllowed(requestOrigin =>
            {
                if (string.IsNullOrWhiteSpace(requestOrigin) || !Uri.TryCreate(requestOrigin, UriKind.Absolute, out var requestUri))
                {
                    return false;
                }

                var explicitMatch = explicitOrigins.Any(explicitOrigin =>
                    Uri.TryCreate(explicitOrigin, UriKind.Absolute, out var explicitUri)
                    && string.Equals(explicitUri.Scheme, requestUri.Scheme, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(explicitUri.Host, requestUri.Host, StringComparison.OrdinalIgnoreCase)
                    && explicitUri.Port == requestUri.Port);

                if (explicitMatch)
                {
                    return true;
                }

                return wildcardDomainRules.Any(rule =>
                    string.Equals(rule.Scheme, requestUri.Scheme, StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(requestUri.Host, rule.Domain, StringComparison.OrdinalIgnoreCase)
                        || requestUri.Host.EndsWith($".{rule.Domain}", StringComparison.OrdinalIgnoreCase)));
            });
        }
        else
        {
            policy.WithOrigins(explicitOrigins);
        }

        policy.AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<JwtTokenUtil>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEnquiryService, EnquiryService>();
builder.Services.AddScoped<IMemberPortalService, MemberPortalService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddHostedService<MembershipStatusSyncBackgroundService>();
builder.Services.AddHostedService<AttendanceCleanupBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token only",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Render terminates TLS at the edge; inside container traffic is HTTP.
    // Redirecting to HTTPS from inside the app can break health checks.
    var isRender = string.Equals(
        Environment.GetEnvironmentVariable("RENDER"),
        "true",
        StringComparison.OrdinalIgnoreCase);

    if (!isRender)
    {
        app.UseHttpsRedirection();
    }
}


app.UseCors("AppCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        service = "GymManagementBackend",
        status = "running",
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow
    });
});

app.MapControllers();
app.Run();

static string BuildNpgsqlConnectionString(string rawConnectionString)
{
    if (rawConnectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
    {
        return rawConnectionString;
    }

    if (!Uri.TryCreate(rawConnectionString, UriKind.Absolute, out var uri))
    {
        return rawConnectionString;
    }

    if (!uri.Scheme.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
    {
        return rawConnectionString;
    }

    var userInfo = uri.UserInfo.Split(':', 2);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.Trim('/');

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = string.IsNullOrWhiteSpace(database) ? "postgres" : database,
        Username = username,
        Password = password,
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}

static (string Host, int Port, string Database, string Username) DescribeConnectionString(string connectionString)
{
    var parsed = new NpgsqlConnectionStringBuilder(connectionString);
    return (parsed.Host ?? string.Empty, parsed.Port, parsed.Database ?? string.Empty, parsed.Username ?? string.Empty);
}
