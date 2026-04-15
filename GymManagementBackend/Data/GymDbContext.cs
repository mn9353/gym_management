using Microsoft.EntityFrameworkCore;
using GymManagementBackend.Models;

namespace GymManagementBackend.Data
{
    public class GymDbContext : DbContext
    {
        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
        {
        }

        public DbSet<Gym> Gyms { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<ServicePlan> ServicePlans { get; set; }
        public DbSet<MemberSubscription> MemberSubscriptions { get; set; }
        public DbSet<TrainerAssignment> TrainerAssignments { get; set; }
        public DbSet<SubscriptionMonthService> SubscriptionMonthServices { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
        public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
        public DbSet<MemberCheckin> MemberCheckins { get; set; }
        public DbSet<AttendancePolicy> AttendancePolicies { get; set; }
        public DbSet<MemberBodyMetric> MemberBodyMetrics { get; set; }
        public DbSet<LoginEvent> LoginEvents { get; set; }
        public DbSet<NotificationOutbox> NotificationOutboxes { get; set; }
        public DbSet<Enquiry> Enquiries { get; set; }
        public DbSet<EnquiryFollowup> EnquiryFollowups { get; set; }
        public DbSet<EnquiryStageHistory> EnquiryStageHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Gym Configuration
            modelBuilder.Entity<Gym>()
                .HasIndex(g => g.Email)
                .IsUnique();

            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Users)
                .WithOne(u => u.Gym)
                .HasForeignKey(u => u.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Members)
                .WithOne(m => m.Gym)
                .HasForeignKey(m => m.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Payments)
                .WithOne(p => p.Gym)
                .HasForeignKey(p => p.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            // User Configuration
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion(
                    v => v,
                    v => v);

            modelBuilder.Entity<User>()
                .HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Member Configuration
            modelBuilder.Entity<Member>()
                .HasMany(m => m.Payments)
                .WithOne(p => p.Member)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Member>()
                .Property(m => m.TrainingType)
                .HasDefaultValue("GENERAL");

            modelBuilder.Entity<Member>()
                .HasIndex(m => new { m.GymId, m.TrainingType });

            // Payment Configuration - explicit foreign key setup
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Member)
                .WithMany(m => m.Payments)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Gym)
                .WithMany(g => g.Payments)
                .HasForeignKey(p => p.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            // Refresh Token Configuration
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => new { rt.UserId, rt.ExpiresAt });

            // Service Catalog
            modelBuilder.Entity<ServiceType>()
                .HasIndex(x => new { x.GymId, x.Code })
                .IsUnique();

            modelBuilder.Entity<ServiceType>()
                .HasIndex(x => new { x.GymId, x.IsActive, x.SortOrder });

            modelBuilder.Entity<ServicePlan>()
                .HasIndex(x => new { x.GymId, x.ServiceTypeId, x.IsActive });

            modelBuilder.Entity<ServicePlan>()
                .HasOne<ServiceType>()
                .WithMany()
                .HasForeignKey(x => x.ServiceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Subscriptions and trainer assignments
            modelBuilder.Entity<MemberSubscription>()
                .HasIndex(x => new { x.GymId, x.MemberId, x.Status });

            modelBuilder.Entity<MemberSubscription>()
                .HasIndex(x => new { x.GymId, x.StartDate, x.EndDate });

            modelBuilder.Entity<MemberSubscription>()
                .HasOne<Member>()
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MemberSubscription>()
                .HasOne<ServicePlan>()
                .WithMany()
                .HasForeignKey(x => x.ServicePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrainerAssignment>()
                .HasIndex(x => new { x.GymId, x.MemberId, x.FromDate });

            modelBuilder.Entity<TrainerAssignment>()
                .HasIndex(x => new { x.GymId, x.TrainerUserId, x.FromDate });

            modelBuilder.Entity<TrainerAssignment>()
                .HasOne<Member>()
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrainerAssignment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.TrainerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrainerAssignment>()
                .HasOne<MemberSubscription>()
                .WithMany()
                .HasForeignKey(x => x.MemberSubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SubscriptionMonthService>()
                .HasIndex(x => new { x.GymId, x.MemberSubscriptionId, x.MonthIndex });

            modelBuilder.Entity<SubscriptionMonthService>()
                .HasIndex(x => new { x.GymId, x.ServiceTypeId, x.MonthIndex });

            modelBuilder.Entity<SubscriptionMonthService>()
                .HasIndex(x => new { x.MemberSubscriptionId, x.MonthIndex, x.ServiceTypeId })
                .IsUnique();

            modelBuilder.Entity<SubscriptionMonthService>()
                .HasOne<MemberSubscription>()
                .WithMany()
                .HasForeignKey(x => x.MemberSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubscriptionMonthService>()
                .HasOne<ServiceType>()
                .WithMany()
                .HasForeignKey(x => x.ServiceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Billing foundation
            modelBuilder.Entity<Invoice>()
                .HasIndex(x => new { x.GymId, x.InvoiceNumber })
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasIndex(x => new { x.GymId, x.MemberId, x.InvoiceDate });

            modelBuilder.Entity<Invoice>()
                .HasOne<Member>()
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceLineItem>()
                .HasIndex(x => new { x.GymId, x.InvoiceId });

            modelBuilder.Entity<InvoiceLineItem>()
                .HasOne<Invoice>()
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceLineItem>()
                .HasOne<ServiceType>()
                .WithMany()
                .HasForeignKey(x => x.ServiceTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InvoiceLineItem>()
                .HasOne<ServicePlan>()
                .WithMany()
                .HasForeignKey(x => x.ServicePlanId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PaymentAllocation>()
                .HasIndex(x => new { x.GymId, x.PaymentId });

            modelBuilder.Entity<PaymentAllocation>()
                .HasIndex(x => new { x.GymId, x.InvoiceId });

            modelBuilder.Entity<PaymentAllocation>()
                .HasOne<Payment>()
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentAllocation>()
                .HasOne<Invoice>()
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentAllocation>()
                .HasOne<InvoiceLineItem>()
                .WithMany()
                .HasForeignKey(x => x.InvoiceLineItemId)
                .OnDelete(DeleteBehavior.SetNull);

            // Attendance and body metrics
            modelBuilder.Entity<MemberCheckin>()
                .HasIndex(x => new { x.GymId, x.MemberId, x.CheckinDate })
                .IsUnique();

            modelBuilder.Entity<MemberCheckin>()
                .HasIndex(x => new { x.GymId, x.CheckinDate });

            modelBuilder.Entity<MemberCheckin>()
                .HasOne<Member>()
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MemberBodyMetric>()
                .HasIndex(x => new { x.GymId, x.MemberId, x.MetricDate });

            modelBuilder.Entity<MemberBodyMetric>()
                .HasOne<Member>()
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttendancePolicy>()
                .HasIndex(x => new { x.GymId, x.IsActive });

            // Auth and notifications
            modelBuilder.Entity<LoginEvent>()
                .HasIndex(x => new { x.GymId, x.UserId, x.OccurredAt });

            modelBuilder.Entity<LoginEvent>()
                .HasIndex(x => new { x.Email, x.OccurredAt });

            modelBuilder.Entity<NotificationOutbox>()
                .HasIndex(x => new { x.Status, x.NextAttemptAt });

            modelBuilder.Entity<NotificationOutbox>()
                .HasIndex(x => new { x.GymId, x.CreatedAt });

            modelBuilder.Entity<NotificationOutbox>()
                .HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"idempotency_key\" IS NOT NULL");

            // Enquiry CRM
            modelBuilder.Entity<Enquiry>()
                .HasIndex(x => new { x.GymId, x.Stage, x.NextFollowupAt });

            modelBuilder.Entity<Enquiry>()
                .HasIndex(x => new { x.GymId, x.Phone });

            modelBuilder.Entity<Enquiry>()
                .HasOne<ServiceType>()
                .WithMany()
                .HasForeignKey(x => x.InterestedServiceTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EnquiryFollowup>()
                .HasIndex(x => new { x.GymId, x.EnquiryId, x.FollowupAt });

            modelBuilder.Entity<EnquiryFollowup>()
                .HasOne<Enquiry>()
                .WithMany()
                .HasForeignKey(x => x.EnquiryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EnquiryStageHistory>()
                .HasIndex(x => new { x.GymId, x.EnquiryId, x.ChangedAt });

            modelBuilder.Entity<EnquiryStageHistory>()
                .HasOne<Enquiry>()
                .WithMany()
                .HasForeignKey(x => x.EnquiryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
