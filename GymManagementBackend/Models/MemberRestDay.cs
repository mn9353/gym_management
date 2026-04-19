using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("member_rest_days")]
    public class MemberRestDay
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("member_id")]
        public Guid MemberId { get; set; }

        [Required]
        [Column("rest_date")]
        public DateOnly RestDate { get; set; }

        [Column("notes")]
        [StringLength(300)]
        public string? Notes { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
