using System.Security.Cryptography;
using System.Text;
using GymManagementBackend.Configuration;
using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using GymManagementBackend.Utils;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GymManagementBackend.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress);
        Task<(bool Success, string Message)> SendPasswordResetCodeAsync(ForgotPasswordRequest request);
        Task<(bool Success, string Message)> VerifyPasswordResetCodeAsync(VerifyResetCodeRequest request);
        Task<(bool Success, string Message)> ResetPasswordWithCodeAsync(ResetPasswordWithCodeRequest request);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? ipAddress);
        Task<bool> RevokeRefreshTokenAsync(Guid userId, string refreshToken, string? ipAddress);
        Task<UserDto?> GetByIdAsync(Guid userId);
    }

    public class AuthService : IAuthService
    {
        private readonly GymDbContext _context;
        private readonly JwtTokenUtil _jwtTokenUtil;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly ILogger<AuthService> _logger;
        private static readonly Regex EmailRegex = new(
            @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public AuthService(
            GymDbContext context,
            JwtTokenUtil jwtTokenUtil,
            JwtSettings jwtSettings,
            IEmailNotificationService emailNotificationService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtTokenUtil = jwtTokenUtil;
            _jwtSettings = jwtSettings;
            _emailNotificationService = emailNotificationService;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress)
        {
            var identifier = (request.Identifier ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Email/phone and password are required."
                };
            }

            var isEmail = EmailRegex.IsMatch(identifier);
            var normalizedEmail = isEmail ? identifier.ToLowerInvariant() : string.Empty;
            var normalizedPhone = isEmail ? string.Empty : NormalizePhone(identifier);

            IQueryable<User> userQuery = _context.Users
                .Include(u => u.Gym)
                .AsQueryable();
            if (isEmail)
            {
                userQuery = userQuery.Where(u => EF.Functions.ILike(u.Email, normalizedEmail));
            }
            else
            {
                userQuery = userQuery.Where(u =>
                    u.Phone != null
                    && (u.Phone == normalizedPhone
                        || u.Phone == $"+91{normalizedPhone}"
                        || u.Phone.EndsWith(normalizedPhone)));
            }

            var user = await userQuery.FirstOrDefaultAsync();

            if (user is not null && user.IsActive && VerifyPassword(request.Password, user.PasswordHash))
            {
                var accessToken = _jwtTokenUtil.GenerateAccessToken(user);
                var refreshTokenValue = _jwtTokenUtil.GenerateRefreshToken();
                var refreshToken = BuildRefreshTokenEntity(user.Id, refreshTokenValue, ipAddress);

                user.LastLoginAt = DateTime.UtcNow;

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    User = MapUserToDto(user),
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenValue,
                    ExpiresIn = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
                };
            }

            IQueryable<Member> memberQuery = _context.Members
                .AsNoTracking()
                .AsQueryable();
            if (isEmail)
            {
                memberQuery = memberQuery.Where(m =>
                    m.Email != null
                    && EF.Functions.ILike(m.Email, normalizedEmail)
                    && m.PasswordHash != null);
            }
            else
            {
                memberQuery = memberQuery.Where(m =>
                    m.Phone != null
                    && (m.Phone == normalizedPhone
                        || m.Phone == $"+91{normalizedPhone}"
                        || m.Phone.EndsWith(normalizedPhone))
                    && m.PasswordHash != null);
            }

            var member = await memberQuery.FirstOrDefaultAsync();

            if (member is not null && !string.IsNullOrWhiteSpace(member.PasswordHash) && VerifyPassword(request.Password, member.PasswordHash))
            {
                var accessToken = _jwtTokenUtil.GenerateMemberAccessToken(member);
                _logger.LogInformation("Member logged in successfully: {MemberId}", member.Id);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    User = MapMemberToUserDto(member),
                    AccessToken = accessToken,
                    RefreshToken = null,
                    ExpiresIn = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
                };
            }

            _logger.LogWarning("Failed login attempt for identifier: {Identifier}", identifier);
            return new LoginResponse
            {
                Success = false,
                Message = "Invalid email/phone or password"
            };
        }

        public async Task<(bool Success, string Message)> SendPasswordResetCodeAsync(ForgotPasswordRequest request)
        {
            var identifier = (request.Identifier ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return (false, "Email or phone is required.");
            }

            var isEmail = EmailRegex.IsMatch(identifier);
            var normalizedEmail = isEmail ? identifier.ToLowerInvariant() : string.Empty;
            var normalizedPhone = isEmail ? string.Empty : NormalizePhone(identifier);

            var user = isEmail
                ? await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => EF.Functions.ILike(x.Email, normalizedEmail))
                : await _context.Users.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.Phone != null
                    && (x.Phone == normalizedPhone || x.Phone == $"+91{normalizedPhone}" || x.Phone.EndsWith(normalizedPhone)));

            var member = user is null
                ? (isEmail
                    ? await _context.Members.AsNoTracking().FirstOrDefaultAsync(x => x.Email != null && EF.Functions.ILike(x.Email, normalizedEmail))
                    : await _context.Members.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.Phone != null
                        && (x.Phone == normalizedPhone || x.Phone == $"+91{normalizedPhone}" || x.Phone.EndsWith(normalizedPhone))))
                : null;

            var email = user?.Email ?? member?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "No email is linked with this account.");
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var codeHash = HashToken(code);
            var now = DateTime.UtcNow;

            await _context.PasswordResetCodes
                .Where(x =>
                    (user != null && x.UserId == user.Id)
                    || (member != null && x.MemberId == member.Id))
                .ExecuteDeleteAsync();

            _context.PasswordResetCodes.Add(new PasswordResetCode
            {
                UserId = user?.Id,
                MemberId = member?.Id,
                Email = email,
                CodeHash = codeHash,
                ExpiresAt = now.AddMinutes(10),
                CreatedAt = now
            });
            await _context.SaveChangesAsync();

            var send = await _emailNotificationService.SendPasswordResetCodeEmailAsync(email, user?.FullName ?? member?.FullName ?? "User", code);
            return send.Success
                ? (true, "Reset code sent to your email.")
                : (false, $"Unable to send reset code: {send.Message}");
        }

        public async Task<(bool Success, string Message)> ResetPasswordWithCodeAsync(ResetPasswordWithCodeRequest request)
        {
            var identifier = (request.Identifier ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return (false, "Email or phone is required.");
            }

            var isEmail = EmailRegex.IsMatch(identifier);
            var normalizedEmail = isEmail ? identifier.ToLowerInvariant() : string.Empty;
            var normalizedPhone = isEmail ? string.Empty : NormalizePhone(identifier);

            var user = isEmail
                ? await _context.Users.FirstOrDefaultAsync(x => EF.Functions.ILike(x.Email, normalizedEmail))
                : await _context.Users.FirstOrDefaultAsync(x =>
                    x.Phone != null
                    && (x.Phone == normalizedPhone || x.Phone == $"+91{normalizedPhone}" || x.Phone.EndsWith(normalizedPhone)));

            Member? member = null;
            if (user is null)
            {
                member = isEmail
                    ? await _context.Members.FirstOrDefaultAsync(x => x.Email != null && EF.Functions.ILike(x.Email, normalizedEmail))
                    : await _context.Members.FirstOrDefaultAsync(x =>
                        x.Phone != null
                        && (x.Phone == normalizedPhone || x.Phone == $"+91{normalizedPhone}" || x.Phone.EndsWith(normalizedPhone)));
            }

            if (user is null && member is null)
            {
                return (false, "Account not found.");
            }

            var resetCode = await GetActiveResetCodeAsync(user?.Id, member?.Id);
            if (resetCode is null || !IsResetCodeValid(resetCode, request.Code))
            {
                return (false, "Invalid or expired reset code.");
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            if (user is not null)
            {
                user.PasswordHash = hash;
                user.UpdatedAt = DateTime.UtcNow;
            }
            else if (member is not null)
            {
                member.PasswordHash = hash;
                member.UpdatedAt = DateTime.UtcNow;
            }

            resetCode.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Password reset successful. Please login again.");
        }

        public async Task<(bool Success, string Message)> VerifyPasswordResetCodeAsync(VerifyResetCodeRequest request)
        {
            var identifier = (request.Identifier ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return (false, "Email or phone is required.");
            }

            var isEmail = EmailRegex.IsMatch(identifier);
            var normalizedEmail = isEmail ? identifier.ToLowerInvariant() : string.Empty;
            var normalizedPhone = isEmail ? string.Empty : NormalizePhone(identifier);

            var user = isEmail
                ? await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => EF.Functions.ILike(x.Email, normalizedEmail))
                : await _context.Users.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.Phone != null
                    && (x.Phone == normalizedPhone || x.Phone == $"+91{normalizedPhone}" || x.Phone.EndsWith(normalizedPhone)));
            var member = user is null
                ? (isEmail
                    ? await _context.Members.AsNoTracking().FirstOrDefaultAsync(x => x.Email != null && EF.Functions.ILike(x.Email, normalizedEmail))
                    : await _context.Members.AsNoTracking().FirstOrDefaultAsync(x =>
                        x.Phone != null
                        && (x.Phone == normalizedPhone || x.Phone == $"+91{normalizedPhone}" || x.Phone.EndsWith(normalizedPhone))))
                : null;

            if (user is null && member is null)
            {
                return (false, "Account not found.");
            }

            var resetCode = await GetActiveResetCodeAsync(user?.Id, member?.Id);
            if (resetCode is null || !IsResetCodeValid(resetCode, request.Code))
            {
                return (false, "Invalid or expired reset code.");
            }

            return (true, "Code verified.");
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? ipAddress)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken is null || storedToken.IsRevoked || storedToken.IsExpired || !storedToken.User.IsActive)
            {
                _logger.LogWarning("Invalid refresh token attempt.");
                return new TokenResponse
                {
                    Success = false,
                    Message = "Invalid or expired refresh token"
                };
            }

            var newRefreshTokenValue = _jwtTokenUtil.GenerateRefreshToken();
            var newRefreshTokenHash = HashToken(newRefreshTokenValue);

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;
            storedToken.ReplacedByTokenHash = newRefreshTokenHash;

            var replacementToken = BuildRefreshTokenEntity(storedToken.UserId, newRefreshTokenValue, ipAddress);
            _context.RefreshTokens.Add(replacementToken);

            var accessToken = _jwtTokenUtil.GenerateAccessToken(storedToken.User);
            await _context.SaveChangesAsync();

            return new TokenResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenValue,
                ExpiresIn = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(Guid userId, string refreshToken, string? ipAddress)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.TokenHash == tokenHash);

            if (storedToken is null || storedToken.IsRevoked)
            {
                return false;
            }

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserDto?> GetByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Gym)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user is not null)
            {
                return MapUserToDto(user);
            }

            var member = await _context.Members
                .AsNoTracking()
                .Include(m => m.Gym)
                .FirstOrDefaultAsync(m => m.Id == userId);

            return member is null ? null : MapMemberToUserDto(member);
        }

        private RefreshToken BuildRefreshTokenEntity(Guid userId, string plainToken, string? ipAddress)
        {
            return new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(plainToken),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedByIp = ipAddress
            };
        }

        private static string HashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        private async Task<PasswordResetCode?> GetActiveResetCodeAsync(Guid? userId, Guid? memberId)
        {
            return await _context.PasswordResetCodes
                .Where(x =>
                    x.UsedAt == null
                    && x.ExpiresAt >= DateTime.UtcNow
                    && ((userId.HasValue && x.UserId == userId) || (memberId.HasValue && x.MemberId == memberId)))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private static bool IsResetCodeValid(PasswordResetCode resetCode, string code)
        {
            return string.Equals(resetCode.CodeHash, HashToken(code), StringComparison.Ordinal);
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("91") && digits.Length >= 12)
            {
                digits = digits.Substring(2);
            }

            return digits.Length > 10 ? digits.Substring(0, 10) : digits;
        }

        private static UserDto MapUserToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                GymId = user.GymId,
                GymName = user.Gym?.GymName,
                GymSubscriptionPlan = user.Gym?.SubscriptionPlan,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt
            };
        }

        private static UserDto MapMemberToUserDto(Member member)
        {
            return new UserDto
            {
                Id = member.Id,
                GymId = member.GymId,
                GymName = member.Gym?.GymName,
                GymSubscriptionPlan = member.Gym?.SubscriptionPlan,
                FullName = member.FullName,
                Email = member.Email ?? string.Empty,
                Phone = member.Phone,
                Role = "MEMBER",
                IsActive = true,
                ProfileImageUrl = member.ProfileImageUrl,
                CreatedAt = member.CreatedAt
            };
        }
    }
}
