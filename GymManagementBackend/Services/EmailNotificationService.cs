using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementBackend.Configuration;
using GymManagementBackend.Data;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GymManagementBackend.Services
{
    public sealed class EmailDeliveryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IEmailNotificationService
    {
        Task<EmailDeliveryResult> SendGymCreatedEmailAsync(string toEmail, string gymName, string ownerName);
        Task<EmailDeliveryResult> SendUserWelcomeEmailAsync(string toEmail, string fullName, string role, string loginId, string temporaryPassword, string gymName);
        Task<EmailDeliveryResult> SendMemberWelcomeEmailAsync(
            string toEmail,
            string memberName,
            string loginId,
            string temporaryPassword,
            string gymName,
            DateOnly joinDate,
            DateOnly planEndDate,
            decimal amountToPay,
            decimal amountPaid,
            string paymentStatus);
        Task<EmailDeliveryResult> SendPasswordResetCodeEmailAsync(string toEmail, string fullName, string code);
    }

    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GymDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailNotificationSettings _settings;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            IHttpClientFactory httpClientFactory,
            GymDbContext context,
            IConfiguration configuration,
            IOptions<EmailNotificationSettings> settings,
            ILogger<EmailNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _configuration = configuration;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<EmailDeliveryResult> SendGymCreatedEmailAsync(string toEmail, string gymName, string ownerName)
        {
            if (!CanSend(toEmail, out var reason))
            {
                return new EmailDeliveryResult { Success = false, Message = reason };
            }

            var safeGym = Html(gymName);
            var safeOwner = Html(ownerName);
            var fallbackSubject = $"Gym Created: {gymName}";
            var fallbackHtml = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.5'>
                  <h2>Welcome to Gym Manager</h2>
                  <p>Hi {safeOwner},</p>
                  <p>Your gym <strong>{safeGym}</strong> was created successfully by the admin.</p>
                  <p>You can now add owners/staff/trainers and start onboarding members.</p>
                  <p><a href='{Html(_settings.LoginUrl)}' style='color:#0b7a75;font-weight:700'>Sign in to Gym Manager</a></p>
                </div>";

            var resolved = await ResolveEmailContentAsync(
                "gym_created",
                fallbackSubject,
                fallbackHtml,
                new Dictionary<string, string>
                {
                    ["GymName"] = safeGym,
                    ["OwnerName"] = safeOwner,
                    ["LoginUrl"] = Html(_settings.LoginUrl),
                    ["BrandImageUrl"] = Html(_settings.BrandImageUrl)
                });

            return await SendAsync(toEmail, resolved.Subject, resolved.Html);
        }

        public async Task<EmailDeliveryResult> SendUserWelcomeEmailAsync(
            string toEmail,
            string fullName,
            string role,
            string loginId,
            string temporaryPassword,
            string gymName)
        {
            if (!CanSend(toEmail, out var reason))
            {
                return new EmailDeliveryResult { Success = false, Message = reason };
            }

            var safeName = Html(fullName);
            var safeRole = Html(role);
            var safeLogin = Html(loginId);
            var safePassword = Html(temporaryPassword);
            var safeGym = Html(gymName);
            var (headline, bodyText) = ResolveRoleCopy(role);
            var (featureOne, featureTwo, featureThree) = ResolveRoleFeatures(role);

            var fallbackSubject = $"Your {role} account is ready";
            var fallbackHtml = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.55;color:#12263f'>
                  <h2 style='margin:0 0 8px'>Welcome to Gym Manager</h2>
                  <p>Hi {safeName},</p>
                  <p>{headline}</p>
                  <p>{bodyText}</p>
                  <div style='background:#f7fafe;border:1px solid #d9e6ff;border-radius:12px;padding:12px 14px'>
                    <p style='margin:0 0 6px'><strong>Gym:</strong> {safeGym}</p>
                    <p style='margin:0 0 6px'><strong>Role:</strong> {safeRole}</p>
                    <p style='margin:0 0 6px'><strong>User ID:</strong> {safeLogin}</p>
                    <p style='margin:0'><strong>Temporary Password:</strong> {safePassword}</p>
                  </div>
                  <p style='margin-top:10px'><a href='{Html(_settings.LoginUrl)}' style='color:#0b7a75;font-weight:700'>Sign in now</a> and change your password immediately.</p>
                </div>";

            var resolved = await ResolveEmailContentAsync(
                "user_welcome",
                fallbackSubject,
                fallbackHtml,
                new Dictionary<string, string>
                {
                    ["FullName"] = safeName,
                    ["Role"] = safeRole,
                    ["LoginId"] = safeLogin,
                    ["TemporaryPassword"] = safePassword,
                    ["GymName"] = safeGym,
                    ["Headline"] = Html(headline),
                    ["BodyText"] = Html(bodyText),
                    ["FeatureOne"] = Html(featureOne),
                    ["FeatureTwo"] = Html(featureTwo),
                    ["FeatureThree"] = Html(featureThree),
                    ["LoginUrl"] = Html(_settings.LoginUrl),
                    ["BrandImageUrl"] = Html(_settings.BrandImageUrl)
                });

            return await SendAsync(toEmail, resolved.Subject, resolved.Html);
        }

        public async Task<EmailDeliveryResult> SendMemberWelcomeEmailAsync(
            string toEmail,
            string memberName,
            string loginId,
            string temporaryPassword,
            string gymName,
            DateOnly joinDate,
            DateOnly planEndDate,
            decimal amountToPay,
            decimal amountPaid,
            string paymentStatus)
        {
            if (!CanSend(toEmail, out var reason))
            {
                return new EmailDeliveryResult { Success = false, Message = reason };
            }

            var safeName = Html(memberName);
            var safeLogin = Html(loginId);
            var safePassword = Html(temporaryPassword);
            var safeGym = Html(gymName);
            var safeJoinDate = Html(joinDate.ToString("dd MMM yyyy"));
            var safePlanEndDate = Html(planEndDate.ToString("dd MMM yyyy"));
            var safeAmountToPay = Html(amountToPay.ToString("0.00"));
            var safeAmountPaid = Html(amountPaid.ToString("0.00"));
            var normalizedStatus = (paymentStatus ?? string.Empty).Trim().ToUpperInvariant();
            var safePaymentStatus = Html(normalizedStatus);
            var paymentStatusLabel = normalizedStatus switch
            {
                "PAID" => "Paid in full",
                "PARTIAL" => "Partially paid",
                _ => "Pending payment"
            };
            var safePaymentStatusLabel = Html(paymentStatusLabel);

            var fallbackSubject = "Your Gym Member Access Details";
            var fallbackHtml = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.55;color:#12263f'>
                  <h2 style='margin:0 0 8px'>Welcome to {safeGym}</h2>
                  <p>Hi {safeName},</p>
                  <p>Your member profile has been created.</p>
                  <div style='background:#f7fafe;border:1px solid #d9e6ff;border-radius:12px;padding:12px 14px'>
                    <p style='margin:0 0 6px'><strong>User ID:</strong> {safeLogin}</p>
                    <p style='margin:0'><strong>Temporary Password:</strong> {safePassword}</p>
                  </div>
                  <p style='margin-top:10px'><a href='{Html(_settings.LoginUrl)}' style='color:#0b7a75;font-weight:700'>Sign in here</a> and change your password after login.</p>
                </div>";

            var resolved = await ResolveEmailContentAsync(
                "member_welcome",
                fallbackSubject,
                fallbackHtml,
                new Dictionary<string, string>
                {
                    ["FullName"] = safeName,
                    ["LoginId"] = safeLogin,
                    ["TemporaryPassword"] = safePassword,
                    ["GymName"] = safeGym,
                    ["JoinDate"] = safeJoinDate,
                    ["PlanEndDate"] = safePlanEndDate,
                    ["AmountToPay"] = safeAmountToPay,
                    ["AmountPaid"] = safeAmountPaid,
                    ["PaymentStatus"] = safePaymentStatus,
                    ["PaymentStatusLabel"] = safePaymentStatusLabel,
                    ["LoginUrl"] = Html(_settings.LoginUrl),
                    ["BrandImageUrl"] = Html(_settings.BrandImageUrl)
                });

            return await SendAsync(toEmail, resolved.Subject, resolved.Html);
        }

        public async Task<EmailDeliveryResult> SendPasswordResetCodeEmailAsync(string toEmail, string fullName, string code)
        {
            if (!CanSend(toEmail, out var reason))
            {
                return new EmailDeliveryResult { Success = false, Message = reason };
            }

            var safeName = Html(fullName);
            var safeCode = Html(code);
            var fallbackSubject = "Password reset code";
            var fallbackHtml = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.55;color:#12263f'>
                  <h2>Password Reset</h2>
                  <p>Hi {safeName},</p>
                  <p>Use the code below to reset your password. This code is valid for 10 minutes.</p>
                  <div style='background:#f7fafe;border:1px solid #d9e6ff;border-radius:12px;padding:14px'>
                    <p style='margin:0;font-size:24px;font-weight:800;letter-spacing:4px'>{safeCode}</p>
                  </div>
                </div>";

            var resolved = await ResolveEmailContentAsync(
                "password_reset_code",
                fallbackSubject,
                fallbackHtml,
                new Dictionary<string, string>
                {
                    ["FullName"] = safeName,
                    ["Code"] = safeCode,
                    ["LoginUrl"] = Html(_settings.LoginUrl),
                    ["BrandImageUrl"] = Html(_settings.BrandImageUrl)
                });

            return await SendAsync(toEmail, resolved.Subject, resolved.Html);
        }

        private bool CanSend(string? toEmail, out string reason)
        {
            if (!_settings.Enabled)
            {
                reason = "Email notifications are disabled.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                reason = "Recipient email is missing.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private async Task<EmailDeliveryResult> SendAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var apiToken =
                    _configuration["Resend:ApiToken"]
                    ?? Environment.GetEnvironmentVariable("RESEND_APITOKEN")
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(apiToken))
                {
                    return new EmailDeliveryResult
                    {
                        Success = false,
                        Message = "Resend API token is missing."
                    };
                }

                var client = _httpClientFactory.CreateClient("ResendApi");
                using var request = new HttpRequestMessage(HttpMethod.Post, "/emails");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
                request.Content = JsonContent.Create(new
                {
                    from = $"{_settings.FromName} <{_settings.FromEmail}>",
                    to = new[] { toEmail },
                    subject,
                    html = htmlBody
                });

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var message = TryExtractResendErrorMessage(errorBody)
                        ?? $"Resend API failed with status {(int)response.StatusCode}";
                    return new EmailDeliveryResult
                    {
                        Success = false,
                        Message = message
                    };
                }

                return new EmailDeliveryResult
                {
                    Success = true,
                    Message = "Email sent."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", toEmail, subject);
                return new EmailDeliveryResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private static string Html(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string? TryExtractResendErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                {
                    return msg.GetString();
                }
            }
            catch
            {
                // ignore parse failure
            }

            return body;
        }

        private static (string Headline, string Body) ResolveRoleCopy(string role)
        {
            var normalized = (role ?? string.Empty).Trim().ToUpperInvariant();
            return normalized switch
            {
                "OWNER" => ("Your owner access is live.", "You can manage your gym team, members, plans, and dashboards."),
                "TRAINER" => ("Your trainer access is live.", "You can manage member progress, attendance, and personal training workflows."),
                "STAFF" => ("Your staff account is ready.", "You can help with operations, onboarding, and day-to-day gym workflows."),
                "MEMBER" => ("Your member access is ready.", "You can log attendance and track your fitness journey inside the app."),
                _ => ("Your account has been created.", "Use the credentials below to sign in.")
            };
        }

        private static (string One, string Two, string Three) ResolveRoleFeatures(string role)
        {
            var normalized = (role ?? string.Empty).Trim().ToUpperInvariant();
            return normalized switch
            {
                "OWNER" => (
                    "Set up your gym profile and branding.",
                    "Onboard your training staff.",
                    "Track revenue and member growth."
                ),
                "TRAINER" => (
                    "Manage your daily client schedule.",
                    "Track member workout progress.",
                    "Keep members engaged and consistent."
                ),
                "STAFF" => (
                    "Handle front-desk operations smoothly.",
                    "Support member onboarding and queries.",
                    "Coordinate follow-ups and renewals."
                ),
                _ => (
                    "Access your account securely.",
                    "Explore your dashboard features.",
                    "Start your fitness journey today."
                )
            };
        }

        private async Task<(string Subject, string Html)> ResolveEmailContentAsync(
            string templateKey,
            string fallbackSubject,
            string fallbackHtml,
            IReadOnlyDictionary<string, string> tokens)
        {
            var template = await _context.EmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateKey == templateKey && t.IsActive);

            if (template is null)
            {
                return (fallbackSubject, fallbackHtml);
            }

            var resolvedLoginUrl = !string.IsNullOrWhiteSpace(template.LoginUrl)
                ? template.LoginUrl!
                : _settings.LoginUrl;
            var resolvedHeroImageUrl = !string.IsNullOrWhiteSpace(template.HeroImageUrl)
                ? template.HeroImageUrl!
                : _settings.BrandImageUrl;

            var mergedTokens = new Dictionary<string, string>(tokens, StringComparer.Ordinal)
            {
                ["LoginUrl"] = Html(resolvedLoginUrl),
                ["BrandImageUrl"] = Html(resolvedHeroImageUrl)
            };

            var subject = ReplaceTokens(template.SubjectTemplate, mergedTokens);
            var html = ReplaceTokens(template.HtmlTemplate, mergedTokens);

            html += BuildEmailFooterHtml(mergedTokens);
            return (subject, html);
        }

        private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> tokens)
        {
            var output = template ?? string.Empty;
            foreach (var (key, value) in tokens)
            {
                output = output.Replace($"{{{key}}}", value ?? string.Empty, StringComparison.Ordinal);
            }
            return output;
        }

        private static string BuildEmailFooterHtml(IReadOnlyDictionary<string, string> tokens)
        {
            var year = DateTime.UtcNow.Year;
            var brandName = "GymManager9353";
            var gymName = tokens.TryGetValue("GymName", out var gym) ? gym : string.Empty;

            var copyrightLine = string.IsNullOrWhiteSpace(gymName)
                ? $"&copy; {year} {brandName}. All rights reserved."
                : $"&copy; {year} {gymName}. Powered by {brandName}.";

            return $@"
                <div style='max-width:640px;margin:10px auto 0;padding:0 12px;font-family:Segoe UI,Arial,sans-serif'>
                  <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:10px 12px;text-align:center'>
                    <p style='margin:0;font-size:11px;line-height:1.6;color:#64748b'>{copyrightLine}</p>
                  </div>
                </div>";
        }
    }
}
