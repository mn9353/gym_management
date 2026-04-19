using System.ComponentModel.DataAnnotations;

namespace GymManagementBackend.DTOs
{
    public class MarkAttendanceDto
    {
        [Required]
        public Guid GymId { get; set; }

        [Required]
        [StringLength(100)]
        public string Identifier { get; set; } = string.Empty; // Phone number or Member ID
    }

    public class AttendanceResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MemberName { get; set; }
        public DateTime? CheckinAt { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    public class MemberAttendanceDto
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string? MemberPhone { get; set; }
        public DateTime CheckinAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }
}
