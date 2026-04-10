using System.ComponentModel.DataAnnotations;

namespace GymManagementBackend.DTOs
{
    public class CreateMemberDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [Required]
        public DateOnly JoinDate { get; set; }

        [Required]
        public DateOnly PlanStartDate { get; set; }

        [Required]
        public DateOnly PlanEndDate { get; set; }

        [StringLength(50)]
        public string? MembershipType { get; set; }

        public decimal? AmountPaid { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "PENDING";

        [StringLength(15)]
        public string? EmergencyContact { get; set; }

        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }

        [StringLength(255)]
        public string? FitnessGoal { get; set; }

        [StringLength(100)]
        public string? TrainerAssigned { get; set; }

        [StringLength(100)]
        public string? LeadSource { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdateMemberDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public DateOnly? PlanEndDate { get; set; }

        [StringLength(50)]
        public string? MembershipType { get; set; }

        public decimal? AmountPaid { get; set; }

        [StringLength(20)]
        public string? PaymentStatus { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(15)]
        public string? EmergencyContact { get; set; }

        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }

        [StringLength(255)]
        public string? FitnessGoal { get; set; }

        [StringLength(100)]
        public string? TrainerAssigned { get; set; }

        public string? Notes { get; set; }
    }

    public class MemberDto
    {
        public Guid Id { get; set; }
        public Guid GymId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateOnly JoinDate { get; set; }
        public DateOnly PlanStartDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public DateOnly? LastPaymentDate { get; set; }
        public string? MembershipType { get; set; }
        public decimal? AmountPaid { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? EmergencyContact { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string? FitnessGoal { get; set; }
        public string? TrainerAssigned { get; set; }
        public string? LeadSource { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MemberSearchDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class MemberListQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "planEndDate";
        public string SortDirection { get; set; } = "asc";
        public bool IncludeAmount { get; set; } = false;
        public int UpcomingDays { get; set; } = 7;

        public string? SearchTerm { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? PaymentStatus { get; set; }
        public string? MembershipType { get; set; }
        public string? TrainerAssigned { get; set; }
        public string? LeadSource { get; set; }

        public DateOnly? JoinDateFrom { get; set; }
        public DateOnly? JoinDateTo { get; set; }
        public DateOnly? PlanEndDateFrom { get; set; }
        public DateOnly? PlanEndDateTo { get; set; }
        public decimal? AmountPaidMin { get; set; }
        public decimal? AmountPaidMax { get; set; }
    }

    public class MemberListItemDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateOnly JoinDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? MembershipType { get; set; }
        public string? TrainerAssigned { get; set; }
        public decimal? AmountPaid { get; set; }
    }

    public class PagedResponseDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
