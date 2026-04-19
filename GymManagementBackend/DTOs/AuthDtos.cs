using System.ComponentModel.DataAnnotations;
using GymManagementBackend.Validation;

namespace GymManagementBackend.DTOs
{
    public class LoginRequest
    {
        [Required]
        [MaxLength(100)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [MaxLength(100)]
        public string Identifier { get; set; } = string.Empty;
    }

    public class ResetPasswordWithCodeRequest
    {
        [Required]
        [MaxLength(100)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits.")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StrongPassword(ErrorMessage = "Password must be 8-100 chars and include uppercase, lowercase, number, and special character.")]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class VerifyResetCodeRequest
    {
        [Required]
        [MaxLength(100)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiresIn { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public Guid? GymId { get; set; }
        public string? GymName { get; set; }
        public string? GymSubscriptionPlan { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiresIn { get; set; }
    }
}
