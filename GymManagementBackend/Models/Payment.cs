using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("member_id")]
        public Guid MemberId { get; set; }

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("amount")]
        public decimal Amount { get; set; }

        [Required]
        [Column("payment_date")]
        public DateOnly PaymentDate { get; set; }

        [Column("payment_mode")]
        [StringLength(20)]
        public string? PaymentMode { get; set; } // CASH, UPI, CARD

        [Column("plan_duration_months")]
        public int? PlanDurationMonths { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("MemberId")]
        public Member Member { get; set; } = null!;

        [ForeignKey("GymId")]
        public Gym Gym { get; set; } = null!;
    }
}
