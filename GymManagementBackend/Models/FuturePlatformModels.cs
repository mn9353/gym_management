using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementBackend.Models
{
    [Table("service_types")]
    public class ServiceType
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("code")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // GENERAL, PERSONAL_TRAINING, ...

        [Required]
        [Column("display_name")]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("sort_order")]
        public int SortOrder { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("service_plans")]
    public class ServicePlan
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("service_type_id")]
        public Guid ServiceTypeId { get; set; }

        [Required]
        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("duration_months")]
        public int DurationMonths { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("rules_json")]
        public string? RulesJson { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("member_subscriptions")]
    public class MemberSubscription
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

        [Required]
        [Column("service_plan_id")]
        public Guid ServicePlanId { get; set; }

        [Required]
        [Column("start_date")]
        public DateOnly StartDate { get; set; }

        [Required]
        [Column("end_date")]
        public DateOnly EndDate { get; set; }

        [Required]
        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE";

        [Column("amount_to_pay")]
        public decimal AmountToPay { get; set; }

        [Column("amount_paid")]
        public decimal AmountPaid { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("trainer_assignments")]
    public class TrainerAssignment
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

        [Required]
        [Column("trainer_user_id")]
        public Guid TrainerUserId { get; set; }

        [Column("member_subscription_id")]
        public Guid? MemberSubscriptionId { get; set; }

        [Required]
        [Column("from_date")]
        public DateOnly FromDate { get; set; }

        [Column("to_date")]
        public DateOnly? ToDate { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Required]
        [Column("assigned_by_user_id")]
        public Guid AssignedByUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("subscription_month_services")]
    public class SubscriptionMonthService
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("member_subscription_id")]
        public Guid MemberSubscriptionId { get; set; }

        [Required]
        [Column("service_type_id")]
        public Guid ServiceTypeId { get; set; }

        // 1-based month index within the base subscription duration.
        [Required]
        [Column("month_index")]
        public int MonthIndex { get; set; }

        [Column("trainer_user_id")]
        public Guid? TrainerUserId { get; set; }

        [Column("amount_to_pay")]
        public decimal AmountToPay { get; set; }

        [Column("amount_paid")]
        public decimal AmountPaid { get; set; }

        [Required]
        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE";

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("invoices")]
    public class Invoice
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

        [Required]
        [Column("invoice_number")]
        [StringLength(40)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        [Column("invoice_date")]
        public DateOnly InvoiceDate { get; set; }

        [Column("due_date")]
        public DateOnly? DueDate { get; set; }

        [Required]
        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "ISSUED";

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("paid_amount")]
        public decimal PaidAmount { get; set; }

        [Column("balance_amount")]
        public decimal BalanceAmount { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("invoice_line_items")]
    public class InvoiceLineItem
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("invoice_id")]
        public Guid InvoiceId { get; set; }

        [Column("service_type_id")]
        public Guid? ServiceTypeId { get; set; }

        [Column("service_plan_id")]
        public Guid? ServicePlanId { get; set; }

        [Required]
        [Column("description")]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Column("quantity")]
        public decimal Quantity { get; set; } = 1m;

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Column("line_total")]
        public decimal LineTotal { get; set; }

        [Column("coverage_start")]
        public DateOnly? CoverageStart { get; set; }

        [Column("coverage_end")]
        public DateOnly? CoverageEnd { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("payment_allocations")]
    public class PaymentAllocation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("payment_id")]
        public Guid PaymentId { get; set; }

        [Required]
        [Column("invoice_id")]
        public Guid InvoiceId { get; set; }

        [Column("invoice_line_item_id")]
        public Guid? InvoiceLineItemId { get; set; }

        [Required]
        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("member_checkins")]
    public class MemberCheckin
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

        [Required]
        [Column("checkin_date")]
        public DateOnly CheckinDate { get; set; }

        [Required]
        [Column("checkin_at")]
        public DateTime CheckinAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("source")]
        [StringLength(20)]
        public string Source { get; set; } = "MEMBER_SELF";

        [Column("created_by_user_id")]
        public Guid? CreatedByUserId { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("attendance_policies")]
    public class AttendancePolicy
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("checkin_start_time")]
        public TimeOnly? CheckinStartTime { get; set; }

        [Column("checkin_end_time")]
        public TimeOnly? CheckinEndTime { get; set; }

        [Column("is_geofence_enabled")]
        public bool IsGeofenceEnabled { get; set; }

        [Column("geofence_radius_meters")]
        public int? GeofenceRadiusMeters { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("member_body_metrics")]
    public class MemberBodyMetric
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

        [Required]
        [Column("metric_date")]
        public DateOnly MetricDate { get; set; }

        [Column("weight_kg")]
        public decimal? WeightKg { get; set; }

        [Column("body_fat_percent")]
        public decimal? BodyFatPercent { get; set; }

        [Column("bmi")]
        public decimal? Bmi { get; set; }

        [Required]
        [Column("source")]
        [StringLength(20)]
        public string Source { get; set; } = "MEMBER";

        [Column("recorded_by_user_id")]
        public Guid? RecordedByUserId { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("login_events")]
    public class LoginEvent
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("gym_id")]
        public Guid? GymId { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("role")]
        [StringLength(20)]
        public string? Role { get; set; }

        [Column("ip_address")]
        [StringLength(60)]
        public string? IpAddress { get; set; }

        [Column("user_agent")]
        [StringLength(1000)]
        public string? UserAgent { get; set; }

        [Column("device_fingerprint")]
        [StringLength(120)]
        public string? DeviceFingerprint { get; set; }

        [Required]
        [Column("success")]
        public bool Success { get; set; }

        [Column("failure_reason")]
        [StringLength(255)]
        public string? FailureReason { get; set; }

        [Required]
        [Column("occurred_at")]
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    [Table("notification_outbox")]
    public class NotificationOutbox
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("gym_id")]
        public Guid? GymId { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("member_id")]
        public Guid? MemberId { get; set; }

        [Required]
        [Column("event_type")]
        [StringLength(80)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [Column("channel")]
        [StringLength(20)]
        public string Channel { get; set; } = "EMAIL";

        [Required]
        [Column("to_address")]
        [StringLength(255)]
        public string ToAddress { get; set; } = string.Empty;

        [Column("subject")]
        [StringLength(255)]
        public string? Subject { get; set; }

        [Column("payload_json")]
        public string? PayloadJson { get; set; }

        [Required]
        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        [Column("retry_count")]
        public int RetryCount { get; set; }

        [Column("next_attempt_at")]
        public DateTime? NextAttemptAt { get; set; }

        [Column("last_error")]
        public string? LastError { get; set; }

        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        [Column("idempotency_key")]
        [StringLength(120)]
        public string? IdempotencyKey { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("enquiries")]
    public class Enquiry
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

        [Required]
        [Column("phone")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("source")]
        [StringLength(50)]
        public string? Source { get; set; }

        [Column("interested_service_type_id")]
        public Guid? InterestedServiceTypeId { get; set; }

        [Required]
        [Column("stage")]
        [StringLength(20)]
        public string Stage { get; set; } = "NEW";

        [Column("next_followup_at")]
        public DateTime? NextFollowupAt { get; set; }

        [Column("assigned_to_user_id")]
        public Guid? AssignedToUserId { get; set; }

        [Column("converted_member_id")]
        public Guid? ConvertedMemberId { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("enquiry_followups")]
    public class EnquiryFollowup
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("enquiry_id")]
        public Guid EnquiryId { get; set; }

        [Required]
        [Column("followup_at")]
        public DateTime FollowupAt { get; set; } = DateTime.UtcNow;

        [Column("next_followup_at")]
        public DateTime? NextFollowupAt { get; set; }

        [Column("outcome")]
        [StringLength(50)]
        public string? Outcome { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Required]
        [Column("created_by_user_id")]
        public Guid CreatedByUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("enquiry_stage_history")]
    public class EnquiryStageHistory
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("gym_id")]
        public Guid GymId { get; set; }

        [Required]
        [Column("enquiry_id")]
        public Guid EnquiryId { get; set; }

        [Column("from_stage")]
        [StringLength(20)]
        public string? FromStage { get; set; }

        [Required]
        [Column("to_stage")]
        [StringLength(20)]
        public string ToStage { get; set; } = string.Empty;

        [Required]
        [Column("changed_by_user_id")]
        public Guid ChangedByUserId { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
