using System.ComponentModel.DataAnnotations;
using GymManagementBackend.Validation;

namespace GymManagementBackend.DTOs
{
    public class MemberPortalSummaryDto
    {
        public Guid MemberId { get; set; }
        public Guid GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string? GymEmail { get; set; }
        public string? GymPhone { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public decimal? TargetWeight { get; set; }
        public string? EmergencyContact { get; set; }
        public string? FitnessGoal { get; set; }
        public DateOnly? LastWeightUpdateDate { get; set; }
        public DateOnly JoinDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public int DaysUntilPlanEnd { get; set; }
        public bool CheckedInToday { get; set; }
        public int AttendanceThisMonth { get; set; }
        public int AttendanceLast30Days { get; set; }
        public int MissedDaysLast30Days { get; set; }
        public int RestDaysLast30Days { get; set; }
        public int CurrentStreakDays { get; set; }
        public int BestStreakDays { get; set; }
    }

    public class MemberWeightPointDto
    {
        public DateOnly Date { get; set; }
        public decimal WeightKg { get; set; }
    }

    public class MemberAttendanceItemDto
    {
        public Guid CheckinId { get; set; }
        public DateOnly CheckinDate { get; set; }
        public DateTime CheckinAt { get; set; }
        public string Source { get; set; } = "MEMBER_SELF";
        public IReadOnlyList<string> MuscleGroups { get; set; } = [];
    }

    public class MemberAttendanceSummaryDto
    {
        public int TotalCheckins { get; set; }
        public IReadOnlyList<MemberAttendanceItemDto> Recent { get; set; } = [];
    }

    public class MemberMissedTrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int AttendedDays { get; set; }
        public int MissedDays { get; set; }
    }

    public class MemberMuscleDistributionDto
    {
        public string MuscleGroup { get; set; } = string.Empty;
        public int SessionCount { get; set; }
    }

    public class MemberMetricUpdateRequestDto
    {
        [Required]
        public DateOnly MetricDate { get; set; }

        [Range(typeof(decimal), "0", "1000")]
        public decimal? WeightKg { get; set; }

        [Range(typeof(decimal), "0", "300")]
        public decimal? HeightCm { get; set; }

        [Range(typeof(decimal), "0", "1000")]
        public decimal? TargetWeightKg { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class MemberCheckinScanRequestDto
    {
        [Required]
        [StringLength(200)]
        public string QrValue { get; set; } = string.Empty;

        [MaxLength(10)]
        public List<string>? MuscleGroups { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class MemberWorkoutLogRequestDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(10)]
        public List<string> MuscleGroups { get; set; } = new();

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class MemberCheckinResultDto
    {
        public Guid CheckinId { get; set; }
        public DateOnly CheckinDate { get; set; }
        public DateTime CheckinAt { get; set; }
        public bool AlreadyCheckedIn { get; set; }
        public bool WorkoutLogged { get; set; }
    }

    public class MemberRestDayRequestDto
    {
        [Required]
        public DateOnly RestDate { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }
    }

    public class MemberRestDayDto
    {
        public Guid Id { get; set; }
        public DateOnly RestDate { get; set; }
        public string? Notes { get; set; }
    }

    public class MemberProfileUpdateRequestDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StrictEmail(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [IndianPhone(ErrorMessage = "Phone must be in +91XXXXXXXXXX format.")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [IndianPhone(ErrorMessage = "Emergency phone must be in +91XXXXXXXXXX format.")]
        [StringLength(20)]
        public string? EmergencyContact { get; set; }

        [StringLength(255)]
        public string? FitnessGoal { get; set; }
    }
}
