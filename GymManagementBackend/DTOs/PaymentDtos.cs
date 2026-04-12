namespace GymManagementBackend.DTOs
{
    public class PaymentListQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "paymentDate";
        public string SortDirection { get; set; } = "desc";

        public Guid? MemberId { get; set; }
        public string? SearchTerm { get; set; }
        public string? PaymentMode { get; set; }
        public string? PaymentStatus { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public DateOnly? PaymentDateFrom { get; set; }
        public DateOnly? PaymentDateTo { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
    }

    public class PaymentListItemDto
    {
        public Guid PaymentId { get; set; }
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string? MemberPhone { get; set; }
        public string? MemberProfileImageUrl { get; set; }
        public DateOnly MemberJoinDate { get; set; }
        public int PlanMonths { get; set; }
        public decimal Amount { get; set; }
        public DateOnly PaymentDate { get; set; }
        public string? PaymentMode { get; set; }
        public string? Remarks { get; set; }
        public string MemberPaymentStatus { get; set; } = string.Empty;
        public decimal MemberPendingAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

