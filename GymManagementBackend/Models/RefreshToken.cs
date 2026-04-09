using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("refresh_tokens")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("token_hash")]
        [StringLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        [Column("replaced_by_token_hash")]
        [StringLength(128)]
        public string? ReplacedByTokenHash { get; set; }

        [Column("created_by_ip")]
        [StringLength(45)]
        public string? CreatedByIp { get; set; }

        [Column("revoked_by_ip")]
        [StringLength(45)]
        public string? RevokedByIp { get; set; }

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [NotMapped]
        public bool IsRevoked => RevokedAt.HasValue;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
