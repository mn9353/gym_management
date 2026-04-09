using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("gyms")]
    public class Gym
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_name")]
        [StringLength(150)]
        public string GymName { get; set; } = string.Empty;

        [Required]
        [Column("owner_name")]
        [StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [Column("phone")]
        [StringLength(15)]
        public string? Phone { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("city")]
        [StringLength(100)]
        public string? City { get; set; }

        [Column("state")]
        [StringLength(100)]
        public string? State { get; set; }

        [Column("subscription_plan")]
        [StringLength(50)]
        public string SubscriptionPlan { get; set; } = "basic";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Member> Members { get; set; } = new List<Member>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
