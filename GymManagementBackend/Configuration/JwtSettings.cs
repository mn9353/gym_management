using System.ComponentModel.DataAnnotations;

namespace GymManagementBackend.Configuration
{
    public class JwtSettings
    {
        [Required]
        public string Secret { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 24 * 60)]
        public int ExpirationMinutes { get; set; } = 60;

        [Range(1, 365)]
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
