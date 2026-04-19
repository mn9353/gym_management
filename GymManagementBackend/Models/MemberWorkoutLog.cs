using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("member_workout_logs")]
    public class MemberWorkoutLog
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

        [Column("checkin_id")]
        public Guid? CheckinId { get; set; }

        [Required]
        [Column("workout_date")]
        public DateOnly WorkoutDate { get; set; }

        [Required]
        [Column("muscle_groups", TypeName = "text[]")]
        public List<string> MuscleGroups { get; set; } = new();

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

