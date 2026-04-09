using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("gym_id")]
        public Guid? GymId { get; set; }

        [Required]
        [Column("full_name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        [StringLength(15)]
        public string? Phone { get; set; }

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Column("role")]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty; // ADMIN, OWNER, STAFF

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Column("profile_image_url")]
        public string? ProfileImageUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("GymId")]
        public Gym? Gym { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
