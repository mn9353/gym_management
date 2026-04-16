using System.Net;
using GymManagementBackend.Configuration;
using Microsoft.Extensions.Options;
using Resend;

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
        Task<EmailDeliveryResult> SendMemberWelcomeEmailAsync(string toEmail, string memberName, string loginId, string temporaryPassword, string gymName);
    }

    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IResend _resend;
        private readonly EmailNotificationSettings _settings;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            IResend resend,
            IOptions<EmailNotificationSettings> settings,
            ILogger<EmailNotificationService> logger)
        {
            _resend = resend;
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
            var subject = $"Gym Created: {gymName}";
            var html = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.5'>
                  <h2>Welcome to Gym Manager</h2>
                  <p>Hi {safeOwner},</p>
                  <p>Your gym <strong>{safeGym}</strong> was created successfully by the admin.</p>
                  <p>You can now add owners/staff/trainers and start onboarding members.</p>
                </div>";

            return await SendAsync(toEmail, subject, html);
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

            var subject = $"Your {role} account is ready";
            var html = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.55'>
                  <h2>Welcome to Gym Manager</h2>
                  <p>Hi {safeName},</p>
                  <p>Your <strong>{safeRole}</strong> account for <strong>{safeGym}</strong> has been created.</p>
                  <p><strong>User ID:</strong> {safeLogin}</p>
                  <p><strong>Temporary Password:</strong> {safePassword}</p>
                  <p>Please change your password after first login.</p>
                </div>";

            return await SendAsync(toEmail, subject, html);
        }

        public async Task<EmailDeliveryResult> SendMemberWelcomeEmailAsync(
            string toEmail,
            string memberName,
            string loginId,
            string temporaryPassword,
            string gymName)
        {
            if (!CanSend(toEmail, out var reason))
            {
                return new EmailDeliveryResult { Success = false, Message = reason };
            }

            var safeName = Html(memberName);
            var safeLogin = Html(loginId);
            var safePassword = Html(temporaryPassword);
            var safeGym = Html(gymName);

            var subject = "Your Gym Member Access Details";
            var html = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.55'>
                  <h2>Welcome to {safeGym}</h2>
                  <p>Hi {safeName},</p>
                  <p>Your member profile has been created.</p>
                  <p><strong>User ID:</strong> {safeLogin}</p>
                  <p><strong>Temporary Password:</strong> {safePassword}</p>
                  <p>Please keep these credentials safe. Password reset email flow can be enabled next.</p>
                </div>";

            return await SendAsync(toEmail, subject, html);
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
                var message = new EmailMessage
                {
                    From = $"{_settings.FromName} <{_settings.FromEmail}>",
                    Subject = subject,
                    HtmlBody = htmlBody
                };
                message.To.Add(toEmail);

                await _resend.EmailSendAsync(message);
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
    }
}
