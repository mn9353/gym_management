using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("members")]
    public class Member
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("full_name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Column("phone")]
        [StringLength(15)]
        public string? Phone { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("gender")]
        [StringLength(20)]
        public string? Gender { get; set; }

        [Column("date_of_birth")]
        public DateOnly? DateOfBirth { get; set; }

        [Required]
        [Column("join_date")]
        public DateOnly JoinDate { get; set; }

        [Required]
        [Column("plan_start_date")]
        public DateOnly PlanStartDate { get; set; }

        [Required]
        [Column("plan_end_date")]
        public DateOnly PlanEndDate { get; set; }

        [Column("last_payment_date")]
        public DateOnly? LastPaymentDate { get; set; }

        [Column("membership_type")]
        [StringLength(50)]
        public string? MembershipType { get; set; }

        [Column("training_type")]
        [StringLength(20)]
        public string TrainingType { get; set; } = "GENERAL"; // GENERAL, PERSONAL, HYBRID

        [Column("amount_paid")]
        public decimal? AmountPaid { get; set; }

        [Column("amount_to_pay")]
        public decimal? AmountToPay { get; set; }

        [Column("payment_status")]
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "PENDING"; // PAID, PENDING, PARTIAL

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, EXPIRED, PAUSED

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("emergency_contact")]
        [StringLength(15)]
        public string? EmergencyContact { get; set; }

        [Column("height")]
        public decimal? Height { get; set; }

        [Column("weight")]
        public decimal? Weight { get; set; }

        [Column("fitness_goal")]
        [StringLength(255)]
        public string? FitnessGoal { get; set; }

        [Column("trainer_assigned")]
        [StringLength(100)]
        public string? TrainerAssigned { get; set; }

        [Column("lead_source")]
        [StringLength(100)]
        public string? LeadSource { get; set; }

        [Column("profile_image_url")]
        public string? ProfileImageUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("GymId")]
        public Gym Gym { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
