using System.ComponentModel.DataAnnotations;

namespace GymManagementBackend.DTOs
{
    public class EnquiryListQueryDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; }
        public string? Stage { get; set; }
        public DateOnly? NextFollowupFrom { get; set; }
        public DateOnly? NextFollowupTo { get; set; }
    }

    public class EnquiryListItemDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Source { get; set; }
        public string Stage { get; set; } = "NEW";
        public DateTime? NextFollowupAt { get; set; }
        public Guid? InterestedServiceTypeId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public int FollowupCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EnquiryTimelineItemDto
    {
        public string Type { get; set; } = string.Empty; // FOLLOWUP / STAGE
        public DateTime At { get; set; }
        public string? Outcome { get; set; }
        public string? Notes { get; set; }
        public string? FromStage { get; set; }
        public string? ToStage { get; set; }
        public Guid ActorUserId { get; set; }
    }

    public class EnquiryDetailsDto : EnquiryListItemDto
    {
        public string? Notes { get; set; }
        public Guid? ConvertedMemberId { get; set; }
        public IReadOnlyList<EnquiryTimelineItemDto> Timeline { get; set; } = [];
    }

    public class CreateEnquiryDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Source { get; set; }

        public Guid? InterestedServiceTypeId { get; set; }
        public DateTime? NextFollowupAt { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
    }

    public class AddEnquiryFollowupDto
    {
        public DateTime? FollowupAt { get; set; }
        public DateTime? NextFollowupAt { get; set; }

        [StringLength(50)]
        public string? Outcome { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateEnquiryStageDto
    {
        [Required]
        [StringLength(20)]
        public string ToStage { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }
}

