using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace GymManagementBackend.DTOs
{
    public class CreateMemberDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [Required]
        public DateOnly JoinDate { get; set; }

        [Required]
        public DateOnly PlanStartDate { get; set; }

        public DateOnly? PlanEndDate { get; set; }

        [Range(1, 24)]
        public int? PlanDurationMonths { get; set; }

        [StringLength(50)]
        public string? MembershipType { get; set; }

        [StringLength(20)]
        public string? TrainingType { get; set; } = "GENERAL";

        public decimal? AmountPaid { get; set; }
        public decimal? AmountToPay { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "PENDING";

        [StringLength(20)]
        public string? PaymentMode { get; set; }

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

    public class RenewMemberDto
    {
        [Required]
        public DateOnly PlanStartDate { get; set; }

        [Range(1, 24)]
        public int PlanDurationMonths { get; set; } = 1;

        public decimal? AmountPaid { get; set; }
        public decimal? AmountToPay { get; set; }

        [StringLength(20)]
        public string? PaymentStatus { get; set; } = "PAID";

        public DateOnly? PaymentDate { get; set; }

        [StringLength(20)]
        public string? PaymentMode { get; set; }

        public string? Remarks { get; set; }
    }

    public class AddMemberPaymentDto
    {
        [Range(typeof(decimal), "0.01", "9999999999")]
        public decimal Amount { get; set; }

        public DateOnly? PaymentDate { get; set; }

        [StringLength(20)]
        public string? PaymentMode { get; set; }

        public string? Remarks { get; set; }
    }

    public class OwnerPaymentUpdateDto
    {
        [Range(typeof(decimal), "0.01", "9999999999")]
        public decimal AmountPaidNow { get; set; }

        public DateOnly? PaymentDate { get; set; }

        [StringLength(20)]
        public string? PaymentMode { get; set; }

        public string? Remarks { get; set; }
    }

    public class OwnerRenewMemberDto
    {
        [Required]
        public DateOnly PlanStartDate { get; set; }

        [Range(1, 24)]
        public int PlanDurationMonths { get; set; } = 1;

        [Range(typeof(decimal), "0.01", "9999999999")]
        public decimal AmountToPayIncrement { get; set; }

        [Range(typeof(decimal), "0", "9999999999")]
        public decimal AmountPaidNow { get; set; } = 0m;

        public DateOnly? PaymentDate { get; set; }

        [StringLength(20)]
        public string? PaymentMode { get; set; }

        public string? Remarks { get; set; }
    }

    public class PaymentTransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateOnly PaymentDate { get; set; }
        public string? PaymentMode { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MemberPaymentUpdateDto
    {
        public Guid MemberId { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountToPay { get; set; }
        public decimal PendingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateOnly? LastPaymentDate { get; set; }
        public PaymentTransactionDto Payment { get; set; } = new();
    }

    public class MemberRenewalUpdateDto
    {
        public Guid MemberId { get; set; }
        public DateOnly PlanStartDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public string? MembershipType { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountToPay { get; set; }
        public decimal PendingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateOnly? LastPaymentDate { get; set; }
        public PaymentTransactionDto? Payment { get; set; }
    }

    public class UpdateMemberDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public DateOnly? PlanEndDate { get; set; }

        [StringLength(50)]
        public string? MembershipType { get; set; }

        [StringLength(20)]
        public string? TrainingType { get; set; }

        public decimal? AmountPaid { get; set; }
        public decimal? AmountToPay { get; set; }

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
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateOnly JoinDate { get; set; }
        public DateOnly PlanStartDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public DateOnly? LastPaymentDate { get; set; }
        public string? MembershipType { get; set; }
        public string TrainingType { get; set; } = "GENERAL";
        public decimal? AmountPaid { get; set; }
        public decimal? AmountToPay { get; set; }
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
        public string? Email { get; set; }
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
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? PaymentStatus { get; set; }
        public string? MembershipType { get; set; }
        public string? TrainingType { get; set; }
        public string? TrainerAssigned { get; set; }
        public string? LeadSource { get; set; }

        public DateOnly? JoinDateFrom { get; set; }
        public DateOnly? JoinDateTo { get; set; }
        public DateOnly? PlanStartDate { get; set; }
        public DateOnly? PlanStartDateFrom { get; set; }
        public DateOnly? PlanStartDateTo { get; set; }
        public DateOnly? PlanEndDate { get; set; }
        public DateOnly? PlanEndDateFrom { get; set; }
        public DateOnly? PlanEndDateTo { get; set; }
        public decimal? AmountPaidMin { get; set; }
        public decimal? AmountPaidMax { get; set; }
        public decimal? AmountToPayMin { get; set; }
        public decimal? AmountToPayMax { get; set; }
    }

    public class MemberListItemDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Gender { get; set; }
        public DateOnly JoinDate { get; set; }
        public DateOnly PlanStartDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? MembershipType { get; set; }
        public string TrainingType { get; set; } = "GENERAL";
        public string? TrainerAssigned { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountToPay { get; set; }
    }

    public class PagedResponseDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class MemberSegmentCountsDto
    {
        public int All { get; set; }
        public int Active { get; set; }
        public int Expiring { get; set; }
        public int Inactive { get; set; }
    }

    public class ExistingMemberSummaryDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateOnly PlanStartDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MembershipType { get; set; }
        public string TrainingType { get; set; } = "GENERAL";
    }

    public class MemberGridRequestDto
    {
        public Dictionary<string, JsonElement>? Filters { get; set; } = new();
        public MemberGridSortDto? Sort { get; set; }
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool IncludeAmount { get; set; } = false;
        public int UpcomingDays { get; set; } = 7;
    }

    public class MemberGridSortDto
    {
        public string Field { get; set; } = "planEndDate";
        public string Direction { get; set; } = "asc";
    }
}
