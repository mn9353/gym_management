using System.Security.Cryptography;
using System.Text;
using GymManagementBackend.Configuration;
using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using GymManagementBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken, string? ipAddress);
        Task<bool> RevokeRefreshTokenAsync(Guid userId, string refreshToken, string? ipAddress);
        Task<UserDto?> GetByIdAsync(Guid userId);
    }

    public class AuthService : IAuthService
    {
        private readonly GymDbContext _context;
        private readonly JwtTokenUtil _jwtTokenUtil;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            GymDbContext context,
            JwtTokenUtil jwtTokenUtil,
            JwtSettings jwtSettings,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtTokenUtil = jwtTokenUtil;
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Gym)
                .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, normalizedEmail));

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

            var member = await _context.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.Email != null
                    && EF.Functions.ILike(m.Email, normalizedEmail)
                    && m.PasswordHash != null);

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

            _logger.LogWarning("Failed login attempt for email: {Email}", normalizedEmail);
            return new LoginResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
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

        private static UserDto MapUserToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                GymId = user.GymId,
                GymName = user.Gym?.GymName,
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
