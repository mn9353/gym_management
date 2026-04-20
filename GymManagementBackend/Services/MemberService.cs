using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace GymManagementBackend.Services
{
    public sealed class DuplicateMemberException : Exception
    {
        public ExistingMemberSummaryDto ExistingMember { get; }

        public DuplicateMemberException(string message, ExistingMemberSummaryDto existingMember) : base(message)
        {
            ExistingMember = existingMember;
        }
    }

    public interface IMemberService
    {
        Task<MemberDto> CreateMemberAsync(Guid gymId, CreateMemberDto createMemberDto);
        Task<MemberDto> RenewMemberAsync(Guid gymId, Guid memberId, RenewMemberDto renewMemberDto);
        Task<MemberPaymentUpdateDto> AddMemberPaymentAsync(Guid gymId, Guid memberId, AddMemberPaymentDto addPaymentDto);
        Task<MemberPaymentUpdateDto> UpdatePaidAmountWithTransactionAsync(Guid gymId, Guid memberId, OwnerPaymentUpdateDto ownerPaymentUpdateDto);
        Task<MemberRenewalUpdateDto> RenewMemberWithTransactionAsync(Guid gymId, Guid memberId, OwnerRenewMemberDto ownerRenewMemberDto);
        Task<MemberDto> UpdateMemberAsync(Guid gymId, Guid memberId, UpdateMemberDto updateMemberDto);
        Task<bool> DeleteMemberAsync(Guid gymId, Guid memberId);
        Task<MemberDto> GetMemberAsync(Guid gymId, Guid memberId);
        Task<List<MemberDto>> GetMembersAsync(Guid gymId, int pageNumber = 1, int pageSize = 10);
        Task<List<MemberDto>> SearchMembersAsync(Guid gymId, MemberSearchDto searchDto);
        Task<List<MemberDto>> GetUpcomingRenewalsAsync(Guid gymId, int days = 7, int limit = 100, int skip = 0);
        Task<MemberSegmentCountsDto> GetSegmentCountsAsync(Guid gymId, int upcomingDays = 7);
        Task<MemberPagedResponseDto<MemberListItemDto>> GetMembersListAsync(Guid gymId, MemberListQueryDto queryDto, string segment);
        Task<MemberPagedResponseDto<MemberListItemDto>> GetMembersGridAsync(Guid gymId, MemberGridRequestDto request, string segment);
        Task<SubscriptionReminderDispatchResultDto> SendSubscriptionRemindersAsync(Guid gymId, SendSubscriptionReminderRequestDto request);
    }

    public class MemberService : IMemberService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<MemberService> _logger;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IProfileImageStorageService _profileImageStorageService;
        private static readonly Expression<Func<Member, MemberDto>> MemberToDtoProjection = m => new MemberDto
        {
            Id = m.Id,
            GymId = m.GymId,
            FullName = m.FullName,
            Phone = m.Phone,
            Email = m.Email,
            Gender = m.Gender,
            DateOfBirth = m.DateOfBirth,
            JoinDate = m.JoinDate,
            PlanStartDate = m.PlanStartDate,
            PlanEndDate = m.PlanEndDate,
            LastPaymentDate = m.LastPaymentDate,
            MembershipType = m.MembershipType,
            TrainingType = m.TrainingType,
            AmountPaid = m.AmountPaid,
            AmountToPay = m.AmountToPay,
            PaymentStatus = m.PaymentStatus,
            Status = m.Status,
            Notes = m.Notes,
            EmergencyContact = m.EmergencyContact,
            Height = m.Height,
            Weight = m.Weight,
            TargetWeight = m.TargetWeight,
            FitnessGoal = m.FitnessGoal,
            TrainerAssigned = m.TrainerAssigned,
            LeadSource = m.LeadSource,
            ProfileImageUrl = m.ProfileImageUrl,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };

        public MemberService(
            GymDbContext context,
            ILogger<MemberService> logger,
            IEmailNotificationService emailNotificationService,
            IProfileImageStorageService profileImageStorageService)
        {
            _context = context;
            _logger = logger;
            _emailNotificationService = emailNotificationService;
            _profileImageStorageService = profileImageStorageService;
        }

        public async Task<MemberDto> CreateMemberAsync(Guid gymId, CreateMemberDto createMemberDto)
        {
            try
            {
                var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId);
                if (gym == null) throw new KeyNotFoundException("Gym not found");

                var duplicateMember = await FindDuplicateMemberAsync(gymId, createMemberDto.Phone, createMemberDto.Email);
                if (duplicateMember is not null)
                {
                    throw new DuplicateMemberException(
                        "A member with this phone number or email already exists. Use renew for existing member.",
                        MapExistingMemberSummary(duplicateMember));
                }

                var planEndDate = ResolvePlanEndDate(createMemberDto.PlanStartDate, createMemberDto.PlanDurationMonths, createMemberDto.PlanEndDate);
                ValidateMembershipDates(createMemberDto.PlanStartDate, planEndDate);
                var membershipType = ResolveMembershipType(createMemberDto.PlanDurationMonths, createMemberDto.MembershipType);
                var trainingType = NormalizeTrainingTypeValue(createMemberDto.TrainingType) ?? "GENERAL";
                var trainerAssigned = string.IsNullOrWhiteSpace(createMemberDto.TrainerAssigned)
                    ? null
                    : createMemberDto.TrainerAssigned.Trim();
                if (trainingType == "PERSONAL" && string.IsNullOrWhiteSpace(trainerAssigned))
                {
                    throw new InvalidOperationException("Trainer selection is required for PERSONAL training.");
                }
                if (trainingType != "PERSONAL")
                {
                    trainerAssigned = null;
                }
                var initialAmountPaid = decimal.Round(createMemberDto.AmountPaid ?? 0m, 2, MidpointRounding.AwayFromZero);
                var initialPaymentMode = NormalizePaymentMode(createMemberDto.PaymentMode);
                if (initialAmountPaid < 0m)
                {
                    throw new InvalidOperationException("Amount paid cannot be negative.");
                }
                if (initialAmountPaid > 0m && string.IsNullOrWhiteSpace(initialPaymentMode))
                {
                    throw new InvalidOperationException("Payment mode is required when amount paid is greater than 0.");
                }
                var resolvedPaymentStatus = ResolvePaymentStatus(initialAmountPaid, createMemberDto.AmountToPay);

                var normalizedEmail = createMemberDto.Email?.Trim().ToLowerInvariant();
                var temporaryPassword = !string.IsNullOrWhiteSpace(normalizedEmail)
                    ? GenerateTemporaryPassword()
                    : null;

                var member = new Member
                {
                    GymId = gymId,
                    FullName = createMemberDto.FullName.Trim(),
                    Phone = createMemberDto.Phone?.Trim(),
                    Email = normalizedEmail,
                    PasswordHash = temporaryPassword is null ? null : BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                    Gender = createMemberDto.Gender?.Trim(),
                    DateOfBirth = createMemberDto.DateOfBirth,
                    JoinDate = createMemberDto.JoinDate,
                    PlanStartDate = createMemberDto.PlanStartDate,
                    PlanEndDate = planEndDate,
                    MembershipType = membershipType,
                    TrainingType = trainingType,
                    AmountPaid = initialAmountPaid,
                    AmountToPay = createMemberDto.AmountToPay,
                    PaymentStatus = resolvedPaymentStatus,
                    LastPaymentDate = initialAmountPaid > 0m ? createMemberDto.JoinDate : null,
                    EmergencyContact = createMemberDto.EmergencyContact?.Trim(),
                    Height = createMemberDto.Height,
                    Weight = createMemberDto.Weight,
                    TargetWeight = createMemberDto.TargetWeight,
                    FitnessGoal = createMemberDto.FitnessGoal?.Trim(),
                    TrainerAssigned = trainerAssigned,
                    LeadSource = createMemberDto.LeadSource?.Trim(),
                    Notes = createMemberDto.Notes?.Trim(),
                    ProfileImageUrl = null,
                    Status = ResolveStatusFromPlanEndDate(planEndDate)
                };
                member.ProfileImageUrl = await _profileImageStorageService.StoreMemberImageAsync(member.Id, createMemberDto.ProfileImageUrl);

                await using var transaction = await _context.Database.BeginTransactionAsync();

                _context.Members.Add(member);

                Payment? initialPayment = null;
                if (initialAmountPaid > 0m)
                {
                    initialPayment = new Payment
                    {
                        GymId = gymId,
                        MemberId = member.Id,
                        Amount = initialAmountPaid,
                        PaymentDate = createMemberDto.JoinDate,
                        PaymentMode = initialPaymentMode,
                        PlanDurationMonths = createMemberDto.PlanDurationMonths ?? GetPlanDurationMonths(createMemberDto.PlanStartDate, planEndDate),
                        Remarks = "Initial member payment",
                        CreatedAt = GetDbTimestampNow()
                    };
                    _context.Payments.Add(initialPayment);
                }

                await CreateSubscriptionLedgerForCycleAsync(
                    member,
                    createMemberDto.PlanStartDate,
                    planEndDate,
                    createMemberDto.PlanDurationMonths ?? GetPlanDurationMonths(createMemberDto.PlanStartDate, planEndDate),
                    createMemberDto.AmountToPay,
                    initialAmountPaid,
                    initialPayment,
                    $"Initial {membershipType ?? "membership"}");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Member created: {member.Id}");

                EmailDeliveryResult? emailResult = null;

                if (!string.IsNullOrWhiteSpace(normalizedEmail) && !string.IsNullOrWhiteSpace(temporaryPassword))
                {
                    var gymName = gym.GymName ?? "Gym";

                    emailResult = await _emailNotificationService.SendMemberWelcomeEmailAsync(
                        normalizedEmail,
                        member.FullName,
                        normalizedEmail,
                        temporaryPassword,
                        gymName,
                        member.JoinDate,
                        member.PlanEndDate,
                        member.AmountToPay ?? 0m,
                        member.AmountPaid ?? 0m,
                        member.PaymentStatus ?? "PENDING");
                }

                var dto = MapMemberToDto(member);
                if (emailResult is not null)
                {
                    dto.WelcomeEmailSent = emailResult.Success;
                    dto.WelcomeEmailMessage = emailResult.Message;
                }
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating member: {ex.Message}");
                throw;
            }
        }

        public async Task<MemberDto> RenewMemberAsync(Guid gymId, Guid memberId, RenewMemberDto renewMemberDto)
        {
            try
            {
                var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId);

                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                {
                    throw new KeyNotFoundException("Member not found");
                }

                var planEndDate = ResolvePlanEndDate(renewMemberDto.PlanStartDate, renewMemberDto.PlanDurationMonths, null);
                ValidateMembershipDates(renewMemberDto.PlanStartDate, planEndDate);
                var membershipType = ResolveMembershipType(renewMemberDto.PlanDurationMonths, null);
                var paymentDate = renewMemberDto.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var paymentStatus = string.IsNullOrWhiteSpace(renewMemberDto.PaymentStatus)
                    ? "PAID"
                    : renewMemberDto.PaymentStatus.Trim().ToUpperInvariant();

                member.PlanStartDate = renewMemberDto.PlanStartDate;
                member.PlanEndDate = planEndDate;
                member.MembershipType = membershipType;
                member.AmountPaid = renewMemberDto.AmountPaid ?? member.AmountPaid;
                member.AmountToPay = renewMemberDto.AmountToPay ?? member.AmountToPay;
                member.PaymentStatus = string.IsNullOrWhiteSpace(renewMemberDto.PaymentStatus)
                    ? ResolvePaymentStatus(member.AmountPaid ?? 0m, member.AmountToPay)
                    : paymentStatus;
                member.LastPaymentDate = paymentDate;
                member.Status = ResolveStatusFromPlanEndDate(planEndDate);
                member.UpdatedAt = GetDbTimestampNow();

                var paymentAmount = renewMemberDto.AmountPaid ?? member.AmountPaid ?? 0m;
                Payment? payment = null;
                if (paymentAmount > 0m)
                {
                    payment = new Payment
                    {
                        GymId = gymId,
                        MemberId = member.Id,
                        Amount = paymentAmount,
                        PaymentDate = paymentDate,
                        PaymentMode = NormalizePaymentMode(renewMemberDto.PaymentMode),
                        PlanDurationMonths = renewMemberDto.PlanDurationMonths,
                        Remarks = renewMemberDto.Remarks?.Trim(),
                        CreatedAt = GetDbTimestampNow()
                    };
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                if (payment != null)
                {
                    _context.Payments.Add(payment);
                }

                await CreateSubscriptionLedgerForCycleAsync(
                    member,
                    renewMemberDto.PlanStartDate,
                    planEndDate,
                    renewMemberDto.PlanDurationMonths,
                    member.AmountToPay,
                    paymentAmount,
                    payment,
                    renewMemberDto.Remarks?.Trim() ?? "Renewal");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Member renewed: {MemberId}", memberId);
                return MapMemberToDto(member);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error renewing member: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<MemberPaymentUpdateDto> AddMemberPaymentAsync(Guid gymId, Guid memberId, AddMemberPaymentDto addPaymentDto)
        {
            try
            {
                if (addPaymentDto.Amount <= 0m)
                {
                    throw new InvalidOperationException("Payment amount must be greater than zero.");
                }

                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                {
                    throw new KeyNotFoundException("Member not found");
                }

                var paymentDate = addPaymentDto.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var paymentMode = NormalizePaymentMode(addPaymentDto.PaymentMode);
                var paymentAmount = decimal.Round(addPaymentDto.Amount, 2, MidpointRounding.AwayFromZero);

                var currentPaid = member.AmountPaid ?? 0m;
                var nextPaid = currentPaid + paymentAmount;
                var amountToPay = member.AmountToPay ?? 0m;
                var pendingAmount = amountToPay > 0m ? Math.Max(0m, amountToPay - nextPaid) : 0m;
                var paymentStatus = ResolvePaymentStatus(nextPaid, member.AmountToPay);

                Payment payment = new Payment
                {
                    GymId = gymId,
                    MemberId = member.Id,
                    Amount = paymentAmount,
                    PaymentDate = paymentDate,
                    PaymentMode = paymentMode,
                    PlanDurationMonths = GetPlanDurationMonths(member.PlanStartDate, member.PlanEndDate),
                    Remarks = addPaymentDto.Remarks?.Trim(),
                    CreatedAt = GetDbTimestampNow()
                };

                await using var transaction = await _context.Database.BeginTransactionAsync();

                member.AmountPaid = nextPaid;
                member.PaymentStatus = paymentStatus;
                member.LastPaymentDate = paymentDate;
                member.UpdatedAt = GetDbTimestampNow();

                if (payment != null)
                {
                    _context.Payments.Add(payment);
                }
                await ApplyPaymentToCurrentLedgerAsync(member, paymentAmount, payment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new MemberPaymentUpdateDto
                {
                    MemberId = member.Id,
                    AmountPaid = member.AmountPaid ?? 0m,
                    AmountToPay = amountToPay,
                    PendingAmount = pendingAmount,
                    PaymentStatus = member.PaymentStatus,
                    LastPaymentDate = member.LastPaymentDate,
                    Payment = payment == null ? null : new PaymentTransactionDto
                    {
                        Id = payment.Id,
                        Amount = payment.Amount,
                        PaymentDate = payment.PaymentDate,
                        PaymentMode = payment.PaymentMode,
                        Remarks = payment.Remarks,
                        CreatedAt = payment.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member payment");
                throw;
            }
        }

        public async Task<MemberPaymentUpdateDto> UpdatePaidAmountWithTransactionAsync(Guid gymId, Guid memberId, OwnerPaymentUpdateDto ownerPaymentUpdateDto)
        {
            try
            {
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                {
                    throw new KeyNotFoundException("Member not found");
                }

                var currentPaid = member.AmountPaid ?? 0m;
                var amountPaidNow = decimal.Round(ownerPaymentUpdateDto.AmountPaidNow, 2, MidpointRounding.AwayFromZero);
                if (amountPaidNow <= 0m)
                {
                    throw new InvalidOperationException("Amount paid now must be greater than zero.");
                }

                var nextPaid = currentPaid + amountPaidNow;
                var paymentDate = ownerPaymentUpdateDto.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var paymentMode = NormalizePaymentMode(ownerPaymentUpdateDto.PaymentMode);
                var amountToPay = member.AmountToPay ?? 0m;
                if (amountToPay > 0m && nextPaid > amountToPay)
                {
                    var remaining = Math.Max(0m, amountToPay - currentPaid);
                    throw new InvalidOperationException($"Overpayment not allowed. Remaining amount is {remaining:0.##}.");
                }

                Payment payment = new Payment
                {
                    GymId = gymId,
                    MemberId = member.Id,
                    Amount = amountPaidNow,
                    PaymentDate = paymentDate,
                    PaymentMode = paymentMode,
                    PlanDurationMonths = GetPlanDurationMonths(member.PlanStartDate, member.PlanEndDate),
                    Remarks = ownerPaymentUpdateDto.Remarks?.Trim(),
                    CreatedAt = EnsureUtc(GetDbTimestampNow())
                };

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var nextPaymentStatus = ResolvePaymentStatus(nextPaid, member.AmountToPay);
                var updatedAt = EnsureUtc(GetDbTimestampNow());

                if (payment != null)
                {
                    _context.Payments.Add(payment);
                }
                await ApplyPaymentToCurrentLedgerAsync(member, amountPaidNow, payment);
                await _context.SaveChangesAsync();

                await _context.Members
                    .Where(m => m.Id == memberId && m.GymId == gymId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.AmountPaid, nextPaid)
                        .SetProperty(m => m.PaymentStatus, nextPaymentStatus)
                        .SetProperty(m => m.LastPaymentDate, paymentDate)
                        .SetProperty(m => m.UpdatedAt, updatedAt));

                await transaction.CommitAsync();

                var pendingAmount = amountToPay > 0m ? Math.Max(0m, amountToPay - nextPaid) : 0m;
                return new MemberPaymentUpdateDto
                {
                    MemberId = member.Id,
                    AmountPaid = nextPaid,
                    AmountToPay = amountToPay,
                    PendingAmount = pendingAmount,
                    PaymentStatus = nextPaymentStatus,
                    LastPaymentDate = paymentDate,
                    Payment = payment == null ? null : new PaymentTransactionDto
                    {
                        Id = payment.Id,
                        Amount = payment.Amount,
                        PaymentDate = payment.PaymentDate,
                        PaymentMode = payment.PaymentMode,
                        Remarks = payment.Remarks,
                        CreatedAt = payment.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating paid amount with transaction");
                throw;
            }
        }
public async Task<MemberRenewalUpdateDto> RenewMemberWithTransactionAsync(Guid gymId, Guid memberId, OwnerRenewMemberDto ownerRenewMemberDto)
        {
            try
            {
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                {
                    throw new KeyNotFoundException("Member not found");
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var isExpand = member.PlanEndDate >= today;

                var amountToPayIncrement = decimal.Round(ownerRenewMemberDto.AmountToPayIncrement, 2, MidpointRounding.AwayFromZero);
                var amountPaidNow = decimal.Round(ownerRenewMemberDto.AmountPaidNow, 2, MidpointRounding.AwayFromZero);
                if (amountPaidNow < 0m)
                {
                    throw new InvalidOperationException("Amount paid now cannot be negative.");
                }
                if (amountPaidNow > amountToPayIncrement)
                {
                    throw new InvalidOperationException($"Overpayment not allowed. For this renewal, maximum payable is {amountToPayIncrement:0.##}.");
                }

                var paymentDate = ownerRenewMemberDto.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var paymentMode = NormalizePaymentMode(ownerRenewMemberDto.PaymentMode);

                DateOnly nextPlanStart;
                DateOnly nextPlanEnd;
                decimal nextAmountToPay;
                decimal nextAmountPaid;

                if (isExpand)
                {
                    var extensionStart = member.PlanEndDate.AddDays(1);
                    nextPlanStart = member.PlanStartDate;
                    nextPlanEnd = ResolvePlanEndDate(extensionStart, ownerRenewMemberDto.PlanDurationMonths, null);
                    nextAmountToPay = (member.AmountToPay ?? 0m) + amountToPayIncrement;
                    nextAmountPaid = (member.AmountPaid ?? 0m) + amountPaidNow;
                }
                else
                {
                    var selectedStart = ownerRenewMemberDto.PlanStartDate < today ? today : ownerRenewMemberDto.PlanStartDate;
                    nextPlanStart = selectedStart;
                    nextPlanEnd = ResolvePlanEndDate(selectedStart, ownerRenewMemberDto.PlanDurationMonths, null);
                    nextAmountToPay = amountToPayIncrement;
                    nextAmountPaid = amountPaidNow;
                }

                ValidateMembershipDates(nextPlanStart, nextPlanEnd);
                var totalMonths = GetPlanDurationMonths(nextPlanStart, nextPlanEnd);
                var newMembershipType = ResolveMembershipType(totalMonths, null);

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var nextPaymentStatus = ResolvePaymentStatus(nextAmountPaid, nextAmountToPay);
                var nextStatus = ResolveStatusFromPlanEndDate(nextPlanEnd);
                var nextLastPaymentDate = amountPaidNow > 0m ? paymentDate : member.LastPaymentDate;
                var updatedAt = EnsureUtc(GetDbTimestampNow());

                Payment? payment = null;
                if (amountPaidNow > 0m)
                {
                    payment = new Payment
                    {
                        GymId = gymId,
                        MemberId = member.Id,
                        Amount = amountPaidNow,
                        PaymentDate = paymentDate,
                        PaymentMode = paymentMode,
                        PlanDurationMonths = ownerRenewMemberDto.PlanDurationMonths,
                        Remarks = ownerRenewMemberDto.Remarks?.Trim(),
                        CreatedAt = EnsureUtc(GetDbTimestampNow())
                    };
                    _context.Payments.Add(payment);
                }

                var cycleStart = isExpand ? member.PlanEndDate.AddDays(1) : nextPlanStart;
                var cycleEnd = nextPlanEnd;
                await CreateSubscriptionLedgerForCycleAsync(
                    member,
                    cycleStart,
                    cycleEnd,
                    ownerRenewMemberDto.PlanDurationMonths,
                    amountToPayIncrement,
                    amountPaidNow,
                    payment,
                    ownerRenewMemberDto.Remarks?.Trim() ?? "Owner renewal");

                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE members
                    SET plan_start_date = {nextPlanStart},
                        plan_end_date = {nextPlanEnd},
                        membership_type = CAST({newMembershipType} AS ""membershipType""),
                        amount_to_pay = {nextAmountToPay},
                        amount_paid = {nextAmountPaid},
                        payment_status = {nextPaymentStatus},
                        status = {nextStatus},
                        last_payment_date = {nextLastPaymentDate},
                        updated_at = {updatedAt}
                    WHERE id = {memberId} AND gym_id = {gymId};");

                await transaction.CommitAsync();

                var pendingAmount = Math.Max(0m, nextAmountToPay - nextAmountPaid);
                return new MemberRenewalUpdateDto
                {
                    MemberId = memberId,
                    PlanStartDate = nextPlanStart,
                    PlanEndDate = nextPlanEnd,
                    MembershipType = newMembershipType,
                    AmountPaid = nextAmountPaid,
                    AmountToPay = nextAmountToPay,
                    PendingAmount = pendingAmount,
                    PaymentStatus = nextPaymentStatus,
                    LastPaymentDate = nextLastPaymentDate,
                    Payment = payment == null
                        ? null
                        : new PaymentTransactionDto
                        {
                            Id = payment.Id,
                            Amount = payment.Amount,
                            PaymentDate = payment.PaymentDate,
                            PaymentMode = payment.PaymentMode,
                            Remarks = payment.Remarks,
                            CreatedAt = payment.CreatedAt
                        }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing member with transaction");
                throw;
            }
        }

        public async Task<MemberDto> UpdateMemberAsync(Guid gymId, Guid memberId, UpdateMemberDto updateMemberDto)
        {
            try
            {
                var member = await _context.Members
                    .Include(m => m.Gym)
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                {
                    throw new KeyNotFoundException($"Member not found");
                }

                bool emailChanged = false;
                string newTempPassword = string.Empty;

                if (!string.IsNullOrEmpty(updateMemberDto.FullName))
                    member.FullName = updateMemberDto.FullName;

                if (!string.IsNullOrEmpty(updateMemberDto.Phone))
                    member.Phone = updateMemberDto.Phone;

                if (!string.IsNullOrEmpty(updateMemberDto.Email))
                {
                    var newEmail = updateMemberDto.Email.Trim().ToLowerInvariant();
                    if (member.Email != newEmail)
                    {
                        member.Email = newEmail;
                        emailChanged = true;
                        newTempPassword = GenerateTemporaryPassword();
                        member.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newTempPassword);
                    }
                }

                if (!string.IsNullOrEmpty(updateMemberDto.Gender))
                    member.Gender = updateMemberDto.Gender;

                if (updateMemberDto.DateOfBirth.HasValue)
                    member.DateOfBirth = updateMemberDto.DateOfBirth;

                if (updateMemberDto.PlanEndDate.HasValue)
                {
                    ValidateMembershipDates(member.PlanStartDate, updateMemberDto.PlanEndDate.Value);
                    member.PlanEndDate = updateMemberDto.PlanEndDate.Value;
                }

                if (!string.IsNullOrEmpty(updateMemberDto.MembershipType))
                {
                    var normalizedMembershipType = NormalizeMembershipTypeValue(updateMemberDto.MembershipType);
                    if (normalizedMembershipType == null)
                    {
                        throw new InvalidOperationException("Membership type must be monthly, quarterly, half_yearly, or yearly.");
                    }
                    member.MembershipType = normalizedMembershipType;
                }

                if (!string.IsNullOrWhiteSpace(updateMemberDto.TrainingType))
                {
                    var normalizedTrainingType = NormalizeTrainingTypeValue(updateMemberDto.TrainingType);
                    if (normalizedTrainingType == null)
                    {
                        throw new InvalidOperationException("Training type must be GENERAL, PERSONAL, or HYBRID.");
                    }
                    member.TrainingType = normalizedTrainingType;
                    if (normalizedTrainingType != "PERSONAL")
                    {
                        member.TrainerAssigned = null;
                    }
                }

                if (updateMemberDto.AmountPaid.HasValue)
                    member.AmountPaid = updateMemberDto.AmountPaid;

                if (updateMemberDto.AmountToPay.HasValue)
                    member.AmountToPay = updateMemberDto.AmountToPay;

                if (!string.IsNullOrEmpty(updateMemberDto.PaymentStatus))
                    member.PaymentStatus = updateMemberDto.PaymentStatus;

                if (!string.IsNullOrEmpty(updateMemberDto.Status))
                    member.Status = updateMemberDto.Status;
                else if (!string.Equals(member.Status, "PAUSED", StringComparison.OrdinalIgnoreCase))
                    member.Status = ResolveStatusFromPlanEndDate(member.PlanEndDate);

                if (!string.IsNullOrEmpty(updateMemberDto.EmergencyContact))
                    member.EmergencyContact = updateMemberDto.EmergencyContact;

                if (updateMemberDto.Height.HasValue)
                    member.Height = updateMemberDto.Height;

                if (updateMemberDto.Weight.HasValue)
                    member.Weight = updateMemberDto.Weight;

                if (updateMemberDto.TargetWeight.HasValue)
                    member.TargetWeight = updateMemberDto.TargetWeight;

                if (!string.IsNullOrEmpty(updateMemberDto.FitnessGoal))
                    member.FitnessGoal = updateMemberDto.FitnessGoal;

                if (!string.IsNullOrEmpty(updateMemberDto.TrainerAssigned))
                {
                    var nextTrainer = updateMemberDto.TrainerAssigned.Trim();
                    if (string.Equals(member.TrainingType, "PERSONAL", StringComparison.OrdinalIgnoreCase))
                    {
                        member.TrainerAssigned = nextTrainer;
                    }
                }

                if (!string.IsNullOrEmpty(updateMemberDto.Notes))
                    member.Notes = updateMemberDto.Notes;

                if (updateMemberDto.ProfileImageUrl != null)
                    member.ProfileImageUrl = await _profileImageStorageService.StoreMemberImageAsync(member.Id, updateMemberDto.ProfileImageUrl);

                member.UpdatedAt = GetDbTimestampNow();
                await _context.SaveChangesAsync();

                if (emailChanged && member.Gym != null)
                {
                    try
                    {
                        var gymName = member.Gym.GymName ?? "Your Gym";
                        await _emailNotificationService.SendMemberWelcomeEmailAsync(
                            member.Email,
                            member.FullName,
                            member.Email,
                            newTempPassword,
                            gymName,
                            member.JoinDate,
                            member.PlanEndDate,
                            member.AmountToPay ?? 0m,
                            member.AmountPaid ?? 0m,
                            member.PaymentStatus ?? "PENDING"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Email changed, but failed to send welcome email to {member.Email}: {ex.Message}");
                    }
                }

                _logger.LogInformation($"Member updated: {memberId}");
                return MapMemberToDto(member);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating member: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteMemberAsync(Guid gymId, Guid memberId)
        {
            try
            {
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                {
                    return false;
                }

                _context.Members.Remove(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Member deleted: {memberId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting member: {ex.Message}");
                throw;
            }
        }

        public async Task<MemberDto> GetMemberAsync(Guid gymId, Guid memberId)
        {
            try
            {
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                    throw new KeyNotFoundException("Member not found");

                return MapMemberToDto(member);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting member: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MemberDto>> GetMembersAsync(Guid gymId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Clamp(pageSize, 1, 100);

                return await _context.Members
                    .Where(m => m.GymId == gymId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MemberToDtoProjection)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting members: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MemberDto>> SearchMembersAsync(Guid gymId, MemberSearchDto searchDto)
        {
            try
            {
                var query = _context.Members
                    .Where(m => m.GymId == gymId)
                    .AsQueryable();

                // Search by name, phone, or email
                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    var searchTerm = searchDto.SearchTerm.Trim();
                    query = query.Where(m =>
                        EF.Functions.ILike(m.FullName, $"%{searchTerm}%") ||
                        (m.Phone != null && EF.Functions.ILike(m.Phone, $"%{searchTerm}%")) ||
                        (m.Email != null && EF.Functions.ILike(m.Email, $"%{searchTerm}%")));
                }

                if (!string.IsNullOrWhiteSpace(searchDto.Email))
                {
                    var email = searchDto.Email.Trim().ToLowerInvariant();
                    query = query.Where(m => m.Email != null && m.Email.ToLower() == email);
                }

                // Filter by status
                if (!string.IsNullOrEmpty(searchDto.Status))
                {
                    query = query.Where(m => m.Status == searchDto.Status);
                }

                return await query
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((Math.Max(1, searchDto.PageNumber) - 1) * Math.Clamp(searchDto.PageSize, 1, 100))
                    .Take(Math.Clamp(searchDto.PageSize, 1, 100))
                    .Select(MemberToDtoProjection)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching members: {ex.Message}");
                throw;
            }
        }

        private MemberDto MapMemberToDto(Member member)
        {
            return new MemberDto
            {
                Id = member.Id,
                GymId = member.GymId,
                FullName = member.FullName,
                Phone = member.Phone,
                Email = member.Email,
                Gender = member.Gender,
                DateOfBirth = member.DateOfBirth,
                JoinDate = member.JoinDate,
                PlanStartDate = member.PlanStartDate,
                PlanEndDate = member.PlanEndDate,
                LastPaymentDate = member.LastPaymentDate,
                MembershipType = member.MembershipType,
                TrainingType = member.TrainingType,
                AmountPaid = member.AmountPaid,
                AmountToPay = member.AmountToPay,
                PaymentStatus = member.PaymentStatus,
                Status = member.Status,
                Notes = member.Notes,
                EmergencyContact = member.EmergencyContact,
                Height = member.Height,
                Weight = member.Weight,
                TargetWeight = member.TargetWeight,
                FitnessGoal = member.FitnessGoal,
                TrainerAssigned = member.TrainerAssigned,
                LeadSource = member.LeadSource,
                ProfileImageUrl = member.ProfileImageUrl,
                CreatedAt = member.CreatedAt,
                UpdatedAt = member.UpdatedAt
            };
        }

        public async Task<MemberPagedResponseDto<MemberListItemDto>> GetMembersListAsync(Guid gymId, MemberListQueryDto queryDto, string segment)
        {
            try
            {
                var normalized = NormalizeQuery(queryDto);
                var query = _context.Members
                    .AsNoTracking()
                    .Where(m => m.GymId == gymId);

                query = ApplySegmentFilter(query, normalized, segment);
                query = ApplyCommonFilters(query, normalized);

                var totalCount = await query.CountAsync();
                query = ApplySorting(query, normalized.SortBy, normalized.SortDirection);

                var items = await query
                    .Skip((normalized.PageNumber - 1) * normalized.PageSize)
                    .Take(normalized.PageSize)
                    .Select(m => new MemberListItemDto
                    {
                        Id = m.Id,
                        FullName = m.FullName,
                        Phone = m.Phone,
                        Email = m.Email,
                        ProfileImageUrl = m.ProfileImageUrl,
                        Gender = m.Gender,
                        JoinDate = m.JoinDate,
                        PlanStartDate = m.PlanStartDate,
                        PlanEndDate = m.PlanEndDate,
                        Status = m.Status,
                        PaymentStatus = m.PaymentStatus,
                        MembershipType = m.MembershipType,
                        TrainingType = m.TrainingType,
                        TrainerAssigned = m.TrainerAssigned,
                        AmountPaid = normalized.IncludeAmount ? m.AmountPaid : null,
                        AmountToPay = normalized.IncludeAmount ? m.AmountToPay : null
                    })
                    .ToListAsync();

                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalized.PageSize));
                var pendingAmount = await query.Where(m => m.AmountToPay > (m.AmountPaid ?? 0)).SumAsync(m => m.AmountToPay - (m.AmountPaid ?? 0));
                return new MemberPagedResponseDto<MemberListItemDto>
                {
                    Items = items,
                    PageNumber = normalized.PageNumber,
                    PageSize = normalized.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    TotalPendingAmount = pendingAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members list for segment {Segment}", segment);
                throw;
            }
        }

        public async Task<List<MemberDto>> GetUpcomingRenewalsAsync(Guid gymId, int days = 7, int limit = 100, int skip = 0)
        {
            try
            {
                days = Math.Clamp(days, 1, 90);
                limit = Math.Clamp(limit, 1, 500);
                skip = Math.Max(0, skip);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var endDate = today.AddDays(days);

                return await _context.Members
                    .Where(m => m.GymId == gymId
                                && m.PlanEndDate >= today
                                && m.PlanEndDate <= endDate
                                && m.Status != "PAUSED")
                    .OrderBy(m => m.PlanEndDate)
                    .Skip(skip)
                    .Take(limit)
                    .Select(MemberToDtoProjection)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting upcoming renewals: {ex.Message}");
                throw;
            }
        }

        public async Task<MemberSegmentCountsDto> GetSegmentCountsAsync(Guid gymId, int upcomingDays = 7)
        {
            try
            {
                upcomingDays = Math.Clamp(upcomingDays, 1, 90);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var endDate = today.AddDays(upcomingDays);

                var query = _context.Members
                    .AsNoTracking()
                    .Where(m => m.GymId == gymId && m.Status != "PAUSED");

                return new MemberSegmentCountsDto
                {
                    All = await query.CountAsync(),
                    Active = await query.CountAsync(m => m.Status == "ACTIVE"),
                    Expiring = await query.CountAsync(m => m.PlanEndDate >= today && m.PlanEndDate <= endDate),
                    Inactive = await query.CountAsync(m => m.Status == "EXPIRED")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting segment counts: {ex.Message}");
                throw;
            }
        }

        public async Task<MemberPagedResponseDto<MemberListItemDto>> GetMembersGridAsync(Guid gymId, MemberGridRequestDto request, string segment)
        {
            try
            {
                request.PageNumber = Math.Max(1, request.PageNumber);
                request.PageSize = Math.Clamp(request.PageSize, 5, 200);
                request.UpcomingDays = Math.Clamp(request.UpcomingDays, 1, 90);

                var query = _context.Members
                    .AsNoTracking()
                    .Where(m => m.GymId == gymId);

                query = ApplyGridSegmentFilter(query, request.UpcomingDays, segment);
                query = ApplyAgGridFilters(query, request.Filters);

                if (!string.IsNullOrWhiteSpace(request.SearchText))
                {
                    var term = request.SearchText.Trim();
                    var termUpper = term.ToUpperInvariant();
                    var matchActive = "ACTIVE".Contains(termUpper);
                    var matchInactive = "INACTIVE".Contains(termUpper) || "EXPIRED".Contains(termUpper) || "LAPSED".Contains(termUpper);
                    var matchPaused = "PAUSED".Contains(termUpper);
                    query = query.Where(m =>
                        EF.Functions.ILike(m.FullName, $"%{term}%")
                        || (m.Phone != null && EF.Functions.ILike(m.Phone, $"%{term}%"))
                        || (m.Email != null && EF.Functions.ILike(m.Email, $"%{term}%"))
                        || (matchActive && m.Status == "ACTIVE")
                        || (matchInactive && m.Status == "EXPIRED")
                        || (matchPaused && m.Status == "PAUSED"));
                }

                var totalCount = await query.CountAsync();
                var sortField = request.Sort?.Field ?? "planEndDate";
                var sortDirection = request.Sort?.Direction ?? "asc";
                query = ApplySorting(query, sortField, sortDirection);

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(m => new MemberListItemDto
                    {
                        Id = m.Id,
                        FullName = m.FullName,
                        Phone = m.Phone,
                        Email = m.Email,
                        ProfileImageUrl = m.ProfileImageUrl,
                        Gender = m.Gender,
                        JoinDate = m.JoinDate,
                        PlanStartDate = m.PlanStartDate,
                        PlanEndDate = m.PlanEndDate,
                        Status = m.Status,
                        PaymentStatus = m.PaymentStatus,
                        MembershipType = m.MembershipType,
                        TrainingType = m.TrainingType,
                        TrainerAssigned = m.TrainerAssigned,
                        AmountPaid = request.IncludeAmount ? m.AmountPaid : null,
                        AmountToPay = request.IncludeAmount ? m.AmountToPay : null
                    })
                    .ToListAsync();

                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)request.PageSize));
                var pendingAmount = await query.Where(m => m.AmountToPay > (m.AmountPaid ?? 0)).SumAsync(m => m.AmountToPay - (m.AmountPaid ?? 0));
                return new MemberPagedResponseDto<MemberListItemDto>
                {
                    Items = items,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    TotalPendingAmount = pendingAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting AG Grid members list for segment {segment}: {ex.Message}");
                throw;
            }
        }

        public async Task<SubscriptionReminderDispatchResultDto> SendSubscriptionRemindersAsync(Guid gymId, SendSubscriptionReminderRequestDto request)
        {
            try
            {
                request ??= new SendSubscriptionReminderRequestDto();
                request.Filters ??= new MemberListQueryDto();

                var normalizedStage = (request.Stage ?? string.Empty).Trim().ToUpperInvariant();
                if (normalizedStage is not ("AUTO" or "EXPIRING" or "INACTIVE"))
                {
                    throw new InvalidOperationException("Stage must be AUTO, EXPIRING, or INACTIVE.");
                }

                var effectiveSegment = normalizedStage switch
                {
                    "INACTIVE" => "inactive",
                    "EXPIRING" => "upcoming",
                    _ => string.IsNullOrWhiteSpace(request.Segment) ? "all" : request.Segment.Trim().ToLowerInvariant()
                };
                if (!request.SelectAll && (request.MemberIds == null || request.MemberIds.Count == 0))
                {
                    throw new InvalidOperationException("Select at least one member or use Select All.");
                }

                var gymName = await _context.Gyms
                    .AsNoTracking()
                    .Where(g => g.Id == gymId)
                    .Select(g => g.GymName)
                    .FirstOrDefaultAsync() ?? "Gym";

                var result = new SubscriptionReminderDispatchResultDto
                {
                    Stage = normalizedStage,
                    SelectAll = request.SelectAll,
                    RequestedCount = request.SelectAll ? 0 : request.MemberIds.Distinct().Count()
                };

                var targetQuery = BuildReminderTargetQuery(gymId, request.Filters, effectiveSegment);
                List<ReminderTarget> targets;

                if (request.SelectAll)
                {
                    targets = await targetQuery.ToListAsync();
                    result.MatchedCount = targets.Count;
                }
                else
                {
                    var requestedIds = request.MemberIds.Distinct().ToHashSet();
                    targets = await targetQuery
                        .Where(m => requestedIds.Contains(m.Id))
                        .ToListAsync();
                    result.MatchedCount = targets.Count;
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                foreach (var target in targets)
                {
                    var memberStage = normalizedStage == "AUTO"
                        ? (target.Status?.ToUpperInvariant() == "EXPIRED" || target.PlanEndDate < today ? "INACTIVE" : "EXPIRING")
                        : normalizedStage;

                    var alreadySentForCurrentPlan = memberStage switch
                    {
                        "EXPIRING" => target.ExpiringReminderPlanEndDate.HasValue
                                      && target.ExpiringReminderPlanEndDate.Value == target.PlanEndDate,
                        "INACTIVE" => target.InactiveReminderPlanEndDate.HasValue
                                      && target.InactiveReminderPlanEndDate.Value == target.PlanEndDate,
                        _ => false
                    };

                    if (alreadySentForCurrentPlan)
                    {
                        result.SkippedAlreadySentCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(target.Email))
                    {
                        result.SkippedNoEmailCount++;
                        continue;
                    }

                    var sendResult = await _emailNotificationService.SendSubscriptionReminderEmailAsync(
                        target.Email!,
                        target.FullName,
                        gymName,
                        target.PlanEndDate,
                        target.AmountToPay ?? 0m,
                        target.AmountPaid ?? 0m,
                        memberStage);

                    if (sendResult.Success)
                    {
                        var sentAt = DateTime.UtcNow;
                        if (memberStage == "EXPIRING")
                        {
                            await _context.Members
                                .Where(m => m.Id == target.Id && m.GymId == gymId)
                                .ExecuteUpdateAsync(setters => setters
                                    .SetProperty(m => m.ExpiringReminderSentAt, sentAt)
                                    .SetProperty(m => m.ExpiringReminderPlanEndDate, target.PlanEndDate)
                                    .SetProperty(m => m.UpdatedAt, sentAt));
                        }
                        else
                        {
                            await _context.Members
                                .Where(m => m.Id == target.Id && m.GymId == gymId)
                                .ExecuteUpdateAsync(setters => setters
                                    .SetProperty(m => m.InactiveReminderSentAt, sentAt)
                                    .SetProperty(m => m.InactiveReminderPlanEndDate, target.PlanEndDate)
                                    .SetProperty(m => m.UpdatedAt, sentAt));
                        }

                        result.SentCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                        if (result.ErrorMessages.Count < 5)
                        {
                            result.ErrorMessages.Add($"{target.FullName}: {sendResult.Message}");
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending subscription reminders.");
                throw;
            }
        }

        private static MemberListQueryDto NormalizeQuery(MemberListQueryDto query)
        {
            query.PageNumber = Math.Max(1, query.PageNumber);
            query.PageSize = Math.Clamp(query.PageSize, 5, 200);
            query.UpcomingDays = Math.Clamp(query.UpcomingDays, 1, 90);
            query.SortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "planEndDate" : query.SortBy.Trim();
            query.SortDirection = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
            query.Gender = NormalizeGender(query.Gender);
            return query;
        }

        private static string? NormalizeGender(string? gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
            {
                return null;
            }

            var g = gender.Trim().ToUpperInvariant();
            return g switch
            {
                "M" => "MALE",
                "F" => "FEMALE",
                _ => g
            };
        }

        private static IQueryable<Member> ApplySegmentFilter(
            IQueryable<Member> query,
            MemberListQueryDto filters,
            string segment)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var normalizedSegment = (segment ?? string.Empty).Trim().ToLowerInvariant();

            return normalizedSegment switch
            {
                "active" => query.Where(m => m.Status == "ACTIVE"),
                "inactive" => query.Where(m => m.Status == "EXPIRED"),
                "expiring" => query.Where(m =>
                    m.PlanEndDate >= today
                    && m.PlanEndDate <= today.AddDays(filters.UpcomingDays)
                    && m.Status != "PAUSED"),
                "upcoming" => query.Where(m =>
                    m.PlanEndDate >= today
                    && m.PlanEndDate <= today.AddDays(filters.UpcomingDays)
                    && m.Status != "PAUSED"),
                _ => query
            };
        }

        private static IQueryable<Member> ApplyCommonFilters(IQueryable<Member> query, MemberListQueryDto filters)
        {
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var term = filters.SearchTerm.Trim();
                var termUpper = term.ToUpperInvariant();
                var matchActive = "ACTIVE".Contains(termUpper);
                var matchInactive = "INACTIVE".Contains(termUpper) || "EXPIRED".Contains(termUpper) || "LAPSED".Contains(termUpper);
                var matchPaused = "PAUSED".Contains(termUpper);
                query = query.Where(m =>
                    EF.Functions.ILike(m.FullName, $"%{term}%")
                    || (m.Phone != null && EF.Functions.ILike(m.Phone, $"%{term}%"))
                    || (m.Email != null && EF.Functions.ILike(m.Email, $"%{term}%"))
                    || (matchActive && m.Status == "ACTIVE")
                    || (matchInactive && m.Status == "EXPIRED")
                    || (matchPaused && m.Status == "PAUSED"));
            }

            if (!string.IsNullOrWhiteSpace(filters.FullName))
            {
                var fullName = filters.FullName.Trim();
                query = query.Where(m => EF.Functions.ILike(m.FullName, $"%{fullName}%"));
            }

            if (!string.IsNullOrWhiteSpace(filters.Phone))
            {
                var phone = filters.Phone.Trim();
                query = query.Where(m => m.Phone != null && EF.Functions.ILike(m.Phone, $"%{phone}%"));
            }

            if (!string.IsNullOrWhiteSpace(filters.Email))
            {
                var email = filters.Email.Trim();
                query = query.Where(m => m.Email != null && EF.Functions.ILike(m.Email, $"%{email}%"));
            }

            if (!string.IsNullOrWhiteSpace(filters.Gender))
            {
                var gender = filters.Gender.Trim().ToUpperInvariant();
                query = query.Where(m => m.Gender != null && m.Gender.ToUpper() == gender);
            }

            if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
            {
                var statuses = filters.PaymentStatus
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpperInvariant())
                    .Distinct()
                    .ToList();

                if (statuses.Count == 1)
                {
                    var paymentStatus = statuses[0];
                    query = query.Where(m => m.PaymentStatus != null && m.PaymentStatus.ToUpper() == paymentStatus);
                }
                else if (statuses.Count > 1)
                {
                    query = query.Where(m => m.PaymentStatus != null && statuses.Contains(m.PaymentStatus.ToUpper()));
                }
            }

            if (!string.IsNullOrWhiteSpace(filters.MembershipType))
            {
                var membershipType = NormalizeMembershipTypeValue(filters.MembershipType);
                if (membershipType == null)
                {
                    return query.Where(_ => false);
                }
                query = query.Where(m => m.MembershipType != null && m.MembershipType == membershipType);
            }

            if (!string.IsNullOrWhiteSpace(filters.TrainingType))
            {
                var trainingType = NormalizeTrainingTypeValue(filters.TrainingType);
                if (trainingType == null)
                {
                    return query.Where(_ => false);
                }
                query = query.Where(m => m.TrainingType == trainingType);
            }

            if (!string.IsNullOrWhiteSpace(filters.TrainerAssigned))
            {
                var trainer = filters.TrainerAssigned.Trim();
                query = query.Where(m => m.TrainerAssigned != null && EF.Functions.ILike(m.TrainerAssigned, $"%{trainer}%"));
            }

            if (!string.IsNullOrWhiteSpace(filters.LeadSource))
            {
                var leadSource = filters.LeadSource.Trim();
                query = query.Where(m => m.LeadSource != null && EF.Functions.ILike(m.LeadSource, $"%{leadSource}%"));
            }

            if (filters.JoinDateFrom.HasValue)
            {
                query = query.Where(m => m.JoinDate >= filters.JoinDateFrom.Value);
            }

            if (filters.JoinDateTo.HasValue)
            {
                query = query.Where(m => m.JoinDate <= filters.JoinDateTo.Value);
            }

            if (filters.PlanStartDate.HasValue)
            {
                query = query.Where(m => m.PlanStartDate == filters.PlanStartDate.Value);
            }

            if (filters.PlanStartDateFrom.HasValue)
            {
                query = query.Where(m => m.PlanStartDate >= filters.PlanStartDateFrom.Value);
            }

            if (filters.PlanStartDateTo.HasValue)
            {
                query = query.Where(m => m.PlanStartDate <= filters.PlanStartDateTo.Value);
            }

            if (filters.PlanEndDate.HasValue)
            {
                query = query.Where(m => m.PlanEndDate == filters.PlanEndDate.Value);
            }

            if (filters.PlanEndDateFrom.HasValue)
            {
                query = query.Where(m => m.PlanEndDate >= filters.PlanEndDateFrom.Value);
            }

            if (filters.PlanEndDateTo.HasValue)
            {
                query = query.Where(m => m.PlanEndDate <= filters.PlanEndDateTo.Value);
            }

            if (filters.AmountPaidMin.HasValue)
            {
                query = query.Where(m => m.AmountPaid.HasValue && m.AmountPaid.Value >= filters.AmountPaidMin.Value);
            }

            if (filters.AmountPaidMax.HasValue)
            {
                query = query.Where(m => m.AmountPaid.HasValue && m.AmountPaid.Value <= filters.AmountPaidMax.Value);
            }

            if (filters.AmountToPayMin.HasValue)
            {
                query = query.Where(m => m.AmountToPay.HasValue && m.AmountToPay.Value >= filters.AmountToPayMin.Value);
            }

            if (filters.AmountToPayMax.HasValue)
            {
                query = query.Where(m => m.AmountToPay.HasValue && m.AmountToPay.Value <= filters.AmountToPayMax.Value);
            }

            return query;
        }

        private static IQueryable<Member> ApplySorting(IQueryable<Member> query, string sortBy, string sortDirection)
        {
            var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var field = string.IsNullOrWhiteSpace(sortBy) ? "planenddate" : sortBy.Trim().ToLowerInvariant();

            return (field, isDesc) switch
            {
                ("name", false) => query.OrderBy(m => m.FullName).ThenBy(m => m.Id),
                ("name", true) => query.OrderByDescending(m => m.FullName).ThenBy(m => m.Id),
                ("phone", false) => query.OrderBy(m => m.Phone).ThenBy(m => m.Id),
                ("phone", true) => query.OrderByDescending(m => m.Phone).ThenBy(m => m.Id),
                ("email", false) => query.OrderBy(m => m.Email).ThenBy(m => m.Id),
                ("email", true) => query.OrderByDescending(m => m.Email).ThenBy(m => m.Id),
                ("joindate", false) => query.OrderBy(m => m.JoinDate).ThenBy(m => m.Id),
                ("joindate", true) => query.OrderByDescending(m => m.JoinDate).ThenBy(m => m.Id),
                ("planstartdate", false) => query.OrderBy(m => m.PlanStartDate).ThenBy(m => m.Id),
                ("planstartdate", true) => query.OrderByDescending(m => m.PlanStartDate).ThenBy(m => m.Id),
                ("amountpaid", false) => query.OrderBy(m => m.AmountPaid).ThenBy(m => m.Id),
                ("amountpaid", true) => query.OrderByDescending(m => m.AmountPaid).ThenBy(m => m.Id),
                ("amounttopay", false) => query.OrderBy(m => m.AmountToPay).ThenBy(m => m.Id),
                ("amounttopay", true) => query.OrderByDescending(m => m.AmountToPay).ThenBy(m => m.Id),
                ("status", false) => query.OrderBy(m => m.Status).ThenBy(m => m.Id),
                ("status", true) => query.OrderByDescending(m => m.Status).ThenBy(m => m.Id),
                ("planenddate", true) => query.OrderByDescending(m => m.PlanEndDate).ThenBy(m => m.Id),
                _ => query.OrderBy(m => m.PlanEndDate).ThenBy(m => m.Id)
            };
        }

        private static IQueryable<Member> ApplyGridSegmentFilter(IQueryable<Member> query, int upcomingDays, string segment)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var normalizedSegment = (segment ?? string.Empty).Trim().ToLowerInvariant();
            return normalizedSegment switch
            {
                "active" => query.Where(m => m.Status == "ACTIVE"),
                "inactive" => query.Where(m => m.Status == "EXPIRED"),
                "expiring" => query.Where(m =>
                    m.PlanEndDate >= today
                    && m.PlanEndDate <= today.AddDays(upcomingDays)
                    && m.Status != "PAUSED"),
                "upcoming" => query.Where(m =>
                    m.PlanEndDate >= today
                    && m.PlanEndDate <= today.AddDays(upcomingDays)
                    && m.Status != "PAUSED"),
                _ => query
            };
        }

        private static IQueryable<Member> ApplyAgGridFilters(IQueryable<Member> query, Dictionary<string, JsonElement>? filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return query;
            }

            foreach (var filter in filters)
            {
                var key = NormalizeGridField(filter.Key);
                var value = filter.Value;

                if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("filterType", out var filterTypeEl))
                {
                    continue;
                }

                var filterType = filterTypeEl.GetString()?.ToLowerInvariant();
                query = filterType switch
                {
                    "text" => ApplyTextFilter(query, key, value),
                    "date" => ApplyDateFilter(query, key, value),
                    "set" => ApplySetFilter(query, key, value),
                    _ => query
                };
            }

            return query;
        }

        private IQueryable<ReminderTarget> BuildReminderTargetQuery(Guid gymId, MemberListQueryDto filters, string segment)
        {
            var normalizedFilters = NormalizeQuery(filters);
            var query = _context.Members
                .AsNoTracking()
                .Where(m => m.GymId == gymId);

            query = ApplySegmentFilter(query, normalizedFilters, segment);
            query = ApplyCommonFilters(query, normalizedFilters);

            return query.Select(m => new ReminderTarget
            {
                Id = m.Id,
                FullName = m.FullName,
                Email = m.Email,
                Status = m.Status,
                PlanEndDate = m.PlanEndDate,
                AmountPaid = m.AmountPaid,
                AmountToPay = m.AmountToPay,
                ExpiringReminderPlanEndDate = m.ExpiringReminderPlanEndDate,
                InactiveReminderPlanEndDate = m.InactiveReminderPlanEndDate
            });
        }

        private sealed class ReminderTarget
        {
            public Guid Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Status { get; set; }
            public DateOnly PlanEndDate { get; set; }
            public decimal? AmountPaid { get; set; }
            public decimal? AmountToPay { get; set; }
            public DateOnly? ExpiringReminderPlanEndDate { get; set; }
            public DateOnly? InactiveReminderPlanEndDate { get; set; }
        }

        private static string NormalizeGridField(string field)
        {
            return field.Trim().ToLowerInvariant() switch
            {
                "name" => "fullname",
                "emailaddress" => "email",
                _ => field.Trim().ToLowerInvariant()
            };
        }

        private static IQueryable<Member> ApplyTextFilter(IQueryable<Member> query, string field, JsonElement filterObj)
        {
            var type = filterObj.TryGetProperty("type", out var typeEl) ? typeEl.GetString()?.ToLowerInvariant() : "contains";
            var filter = filterObj.TryGetProperty("filter", out var valueEl) ? valueEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(filter))
            {
                return query;
            }

            var value = filter.Trim();
            return (field, type) switch
            {
                ("fullname", "equals") => query.Where(m => m.FullName == value),
                ("fullname", "startswith") => query.Where(m => EF.Functions.ILike(m.FullName, $"{value}%")),
                ("fullname", "endswith") => query.Where(m => EF.Functions.ILike(m.FullName, $"%{value}")),
                ("fullname", _) => query.Where(m => EF.Functions.ILike(m.FullName, $"%{value}%")),

                ("phone", "equals") => query.Where(m => m.Phone != null && m.Phone == value),
                ("phone", "startswith") => query.Where(m => m.Phone != null && EF.Functions.ILike(m.Phone, $"{value}%")),
                ("phone", "endswith") => query.Where(m => m.Phone != null && EF.Functions.ILike(m.Phone, $"%{value}")),
                ("phone", _) => query.Where(m => m.Phone != null && EF.Functions.ILike(m.Phone, $"%{value}%")),

                ("email", "equals") => query.Where(m => m.Email != null && m.Email.ToLower() == value.ToLower()),
                ("email", "startswith") => query.Where(m => m.Email != null && EF.Functions.ILike(m.Email, $"{value}%")),
                ("email", "endswith") => query.Where(m => m.Email != null && EF.Functions.ILike(m.Email, $"%{value}")),
                ("email", _) => query.Where(m => m.Email != null && EF.Functions.ILike(m.Email, $"%{value}%")),

                ("status", "equals") => query.Where(m => m.Status.ToUpper() == value.ToUpper()),
                ("paymentstatus", "equals") => query.Where(m => m.PaymentStatus.ToUpper() == value.ToUpper()),
                _ => query
            };
        }

        private static IQueryable<Member> ApplyDateFilter(IQueryable<Member> query, string field, JsonElement filterObj)
        {
            var type = filterObj.TryGetProperty("type", out var typeEl) ? typeEl.GetString()?.ToLowerInvariant() : "equals";
            var dateFromString = filterObj.TryGetProperty("dateFrom", out var fromEl) ? fromEl.GetString() : null;
            var dateToString = filterObj.TryGetProperty("dateTo", out var toEl) ? toEl.GetString() : null;

            if (!TryParseDateOnly(dateFromString, out var dateFrom))
            {
                return query;
            }

            var hasDateTo = TryParseDateOnly(dateToString, out var dateTo);
            return (field, type) switch
            {
                ("planstartdate", "inrange") when hasDateTo => query.Where(m => m.PlanStartDate >= dateFrom && m.PlanStartDate <= dateTo),
                ("planstartdate", "lessthan") => query.Where(m => m.PlanStartDate < dateFrom),
                ("planstartdate", "greaterthan") => query.Where(m => m.PlanStartDate > dateFrom),
                ("planstartdate", _) => query.Where(m => m.PlanStartDate == dateFrom),

                ("planenddate", "inrange") when hasDateTo => query.Where(m => m.PlanEndDate >= dateFrom && m.PlanEndDate <= dateTo),
                ("planenddate", "lessthan") => query.Where(m => m.PlanEndDate < dateFrom),
                ("planenddate", "greaterthan") => query.Where(m => m.PlanEndDate > dateFrom),
                ("planenddate", _) => query.Where(m => m.PlanEndDate == dateFrom),

                ("joindate", "inrange") when hasDateTo => query.Where(m => m.JoinDate >= dateFrom && m.JoinDate <= dateTo),
                ("joindate", "lessthan") => query.Where(m => m.JoinDate < dateFrom),
                ("joindate", "greaterthan") => query.Where(m => m.JoinDate > dateFrom),
                ("joindate", _) => query.Where(m => m.JoinDate == dateFrom),
                _ => query
            };
        }

        private static IQueryable<Member> ApplySetFilter(IQueryable<Member> query, string field, JsonElement filterObj)
        {
            if (!filterObj.TryGetProperty("values", out var valuesEl) || valuesEl.ValueKind != JsonValueKind.Array)
            {
                return query;
            }

            var values = valuesEl.EnumerateArray()
                .Select(v => v.GetString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .ToList();

            if (values.Count == 0)
            {
                return query;
            }

            if (field == "gender")
            {
                var normalized = values.Select(v => v.ToUpperInvariant() switch
                {
                    "M" => "MALE",
                    "F" => "FEMALE",
                    _ => v.ToUpperInvariant()
                }).ToList();
                return query.Where(m => m.Gender != null && normalized.Contains(m.Gender.ToUpper()));
            }

            if (field == "status")
            {
                var normalized = values.Select(v => v.ToUpperInvariant()).ToList();
                return query.Where(m => normalized.Contains(m.Status.ToUpper()));
            }

            if (field == "paymentstatus")
            {
                var normalized = values.Select(v => v.ToUpperInvariant()).ToList();
                return query.Where(m => normalized.Contains(m.PaymentStatus.ToUpper()));
            }

            return query;
        }

        private static bool TryParseDateOnly(string? input, out DateOnly value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var datePart = input.Contains(' ') ? input.Split(' ')[0] : input;
            return DateOnly.TryParse(datePart, out value);
        }

        private static void ValidateMembershipDates(DateOnly planStartDate, DateOnly planEndDate)
        {
            if (planEndDate < planStartDate)
            {
                throw new InvalidOperationException("Plan end date must be after or equal to plan start date.");
            }
        }

        private static DateOnly ResolvePlanEndDate(DateOnly planStartDate, int? planDurationMonths, DateOnly? planEndDate)
        {
            if (planDurationMonths.HasValue)
            {
                if (planDurationMonths.Value <= 0)
                {
                    throw new InvalidOperationException("Plan duration must be at least 1 month.");
                }

                if (planDurationMonths.Value > 24)
                {
                    throw new InvalidOperationException("Plan duration cannot exceed 24 months.");
                }

                // Inclusive plan window: 1 month starting Apr 1 ends Apr 30.
                return planStartDate.AddMonths(planDurationMonths.Value).AddDays(-1);
            }

            if (!planEndDate.HasValue)
            {
                throw new InvalidOperationException("Either plan duration months or plan end date is required.");
            }

            return planEndDate.Value;
        }

        private static int GetPlanDurationMonths(DateOnly planStartDate, DateOnly planEndDate)
        {
            if (planEndDate < planStartDate)
            {
                return 1;
            }

            for (var months = 1; months <= 24; months++)
            {
                var computedEnd = planStartDate.AddMonths(months).AddDays(-1);
                if (computedEnd == planEndDate)
                {
                    return months;
                }
            }

            var inclusiveDays = (planEndDate.DayNumber - planStartDate.DayNumber) + 1;
            return Math.Max(1, (int)Math.Round(inclusiveDays / 30.0, MidpointRounding.AwayFromZero));
        }

        private static string? ResolveMembershipType(int? planDurationMonths, string? providedMembershipType)
        {
            if (planDurationMonths.HasValue)
            {
                var months = planDurationMonths.Value;
                return months switch
                {
                    >= 12 => "yearly",
                    >= 6 => "half_yearly",
                    >= 3 => "quarterly",
                    1 => "monthly",
                    _ => months >= 2 ? "quarterly" : "monthly"
                };
            }

            return NormalizeMembershipTypeValue(providedMembershipType);
        }

        private static string? NormalizeMembershipTypeValue(string? membershipType)
        {
            if (string.IsNullOrWhiteSpace(membershipType))
            {
                return null;
            }

            var normalized = membershipType.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
            return normalized switch
            {
                "monthly" => "monthly",
                "quarterly" => "quarterly",
                "half_yearly" => "half_yearly",
                "halfyearly" => "half_yearly",
                "yearly" => "yearly",
                _ => null
            };
        }

        private static string? NormalizeTrainingTypeValue(string? trainingType)
        {
            if (string.IsNullOrWhiteSpace(trainingType))
            {
                return null;
            }

            var normalized = trainingType.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");
            return normalized switch
            {
                "GENERAL" => "GENERAL",
                "PERSONAL" => "PERSONAL",
                "HYBRID" => "HYBRID",
                _ => null
            };
        }

        private static string? NormalizePaymentMode(string? paymentMode)
        {
            if (string.IsNullOrWhiteSpace(paymentMode))
            {
                return null;
            }

            var normalized = paymentMode.Trim().ToUpperInvariant();
            if (normalized is not ("CASH" or "UPI" or "CARD"))
            {
                throw new InvalidOperationException("Payment mode must be CASH, UPI, or CARD.");
            }

            return normalized;
        }

        private static string ResolvePaymentStatus(decimal amountPaid, decimal? amountToPay)
        {
            if (amountPaid <= 0m)
            {
                return "PENDING";
            }

            if (!amountToPay.HasValue || amountToPay.Value <= 0m)
            {
                return "PAID";
            }

            return amountPaid < amountToPay.Value ? "PARTIAL" : "PAID";
        }

        private async Task CreateSubscriptionLedgerForCycleAsync(
            Member member,
            DateOnly planStartDate,
            DateOnly planEndDate,
            int planDurationMonths,
            decimal? amountToPay,
            decimal amountPaid,
            Payment? payment,
            string remarks)
        {
            var (serviceTypeId, servicePlanId) = await EnsureServicePlanForMemberAsync(
                member.GymId,
                member.TrainingType,
                member.MembershipType,
                planDurationMonths,
                amountToPay ?? 0m);

            var resolvedAmountToPay = decimal.Round(amountToPay ?? 0m, 2, MidpointRounding.AwayFromZero);
            var resolvedAmountPaid = decimal.Round(amountPaid, 2, MidpointRounding.AwayFromZero);
            var status = ResolvePaymentStatus(resolvedAmountPaid, resolvedAmountToPay);

            var subscription = new MemberSubscription
            {
                GymId = member.GymId,
                MemberId = member.Id,
                ServicePlanId = servicePlanId,
                StartDate = planStartDate,
                EndDate = planEndDate,
                Status = ResolveStatusFromPlanEndDate(planEndDate),
                AmountToPay = resolvedAmountToPay,
                AmountPaid = resolvedAmountPaid,
                CreatedAt = GetDbTimestampNow(),
                UpdatedAt = GetDbTimestampNow()
            };
            _context.MemberSubscriptions.Add(subscription);

            var invoice = new Invoice
            {
                GymId = member.GymId,
                MemberId = member.Id,
                InvoiceNumber = GenerateInvoiceNumber(member.GymId),
                InvoiceDate = planStartDate,
                DueDate = planStartDate,
                Status = status == "PAID" ? "PAID" : "ISSUED",
                TotalAmount = resolvedAmountToPay,
                PaidAmount = resolvedAmountPaid,
                BalanceAmount = Math.Max(0m, resolvedAmountToPay - resolvedAmountPaid),
                Notes = remarks,
                CreatedAt = GetDbTimestampNow(),
                UpdatedAt = GetDbTimestampNow()
            };
            _context.Invoices.Add(invoice);

            var lineItem = new InvoiceLineItem
            {
                GymId = member.GymId,
                InvoiceId = invoice.Id,
                ServiceTypeId = serviceTypeId,
                ServicePlanId = servicePlanId,
                Description = $"{member.TrainingType} - {member.MembershipType ?? $"{planDurationMonths} month"}",
                Quantity = 1,
                UnitPrice = resolvedAmountToPay,
                LineTotal = resolvedAmountToPay,
                CoverageStart = planStartDate,
                CoverageEnd = planEndDate,
                CreatedAt = GetDbTimestampNow()
            };
            _context.InvoiceLineItems.Add(lineItem);

            if (payment is not null && resolvedAmountPaid > 0m)
            {
                var allocation = new PaymentAllocation
                {
                    GymId = member.GymId,
                    PaymentId = payment.Id,
                    InvoiceId = invoice.Id,
                    InvoiceLineItemId = lineItem.Id,
                    Amount = resolvedAmountPaid,
                    CreatedAt = GetDbTimestampNow()
                };
                _context.PaymentAllocations.Add(allocation);
            }
        }

        private async Task<(Guid ServiceTypeId, Guid ServicePlanId)> EnsureServicePlanForMemberAsync(
            Guid gymId,
            string? trainingType,
            string? membershipType,
            int durationMonths,
            decimal price)
        {
            var normalizedTraining = NormalizeTrainingTypeValue(trainingType) ?? "GENERAL";
            var serviceCode = normalizedTraining == "PERSONAL" ? "PERSONAL_TRAINING" : "GENERAL_MEMBERSHIP";
            var serviceDisplay = normalizedTraining == "PERSONAL" ? "Personal Training" : "General Membership";

            var serviceType = await _context.ServiceTypes
                .FirstOrDefaultAsync(x => x.GymId == gymId && x.Code == serviceCode);
            if (serviceType is null)
            {
                serviceType = new ServiceType
                {
                    GymId = gymId,
                    Code = serviceCode,
                    DisplayName = serviceDisplay,
                    IsActive = true,
                    SortOrder = 1,
                    CreatedAt = GetDbTimestampNow(),
                    UpdatedAt = GetDbTimestampNow()
                };
                _context.ServiceTypes.Add(serviceType);
            }

            var membershipLabel = string.IsNullOrWhiteSpace(membershipType)
                ? $"{durationMonths} Month"
                : membershipType.Replace("_", " ", StringComparison.OrdinalIgnoreCase);
            var planName = $"{membershipLabel} {serviceDisplay}".Trim();

            var servicePlan = await _context.ServicePlans
                .FirstOrDefaultAsync(x =>
                    x.GymId == gymId
                    && x.ServiceTypeId == serviceType.Id
                    && x.DurationMonths == durationMonths
                    && x.Name == planName);

            if (servicePlan is null)
            {
                servicePlan = new ServicePlan
                {
                    GymId = gymId,
                    ServiceTypeId = serviceType.Id,
                    Name = planName,
                    DurationMonths = durationMonths,
                    Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero),
                    IsActive = true,
                    CreatedAt = GetDbTimestampNow(),
                    UpdatedAt = GetDbTimestampNow()
                };
                _context.ServicePlans.Add(servicePlan);
            }

            return (serviceType.Id, servicePlan.Id);
        }

        private static string GenerateInvoiceNumber(Guid gymId)
        {
            var now = DateTime.UtcNow;
            var gymToken = gymId.ToString("N")[..6].ToUpperInvariant();
            var randomToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)); // 6 chars
            return $"INV-{now:yyyyMMddHHmmssfff}-{gymToken}-{randomToken}";
        }

        private async Task ApplyPaymentToCurrentLedgerAsync(Member member, decimal paymentAmount, Payment payment)
        {
            if (paymentAmount <= 0m)
            {
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var subscription = await _context.MemberSubscriptions
                .Where(x => x.MemberId == member.Id)
                .OrderByDescending(x => x.EndDate)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (subscription is not null)
            {
                subscription.AmountPaid = decimal.Round((subscription.AmountPaid + paymentAmount), 2, MidpointRounding.AwayFromZero);
                subscription.Status = subscription.EndDate < today ? "EXPIRED" : "ACTIVE";
                subscription.UpdatedAt = GetDbTimestampNow();
            }

            var invoice = await _context.Invoices
                .Where(x => x.MemberId == member.Id)
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (invoice is null)
            {
                return;
            }

            var allocationAmount = Math.Min(paymentAmount, Math.Max(0m, invoice.BalanceAmount));
            if (allocationAmount <= 0m)
            {
                return;
            }

            invoice.PaidAmount = decimal.Round(invoice.PaidAmount + allocationAmount, 2, MidpointRounding.AwayFromZero);
            invoice.BalanceAmount = decimal.Round(Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount), 2, MidpointRounding.AwayFromZero);
            invoice.Status = invoice.BalanceAmount <= 0m ? "PAID" : "ISSUED";
            invoice.UpdatedAt = GetDbTimestampNow();

            var lineItemId = await _context.InvoiceLineItems
                .Where(x => x.InvoiceId == invoice.Id)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            _context.PaymentAllocations.Add(new PaymentAllocation
            {
                GymId = member.GymId,
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                InvoiceLineItemId = lineItemId,
                Amount = allocationAmount,
                CreatedAt = GetDbTimestampNow()
            });
        }

        private async Task<Member?> FindDuplicateMemberAsync(Guid gymId, string? phone, string? email)
        {
            var normalizedPhone = NormalizePhone(phone);
            var normalizedEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalizedPhone) && string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return null;
            }

            var candidates = await _context.Members
                .AsNoTracking()
                .Where(m => m.GymId == gymId && (m.Phone != null || m.Email != null))
                .ToListAsync();

            return candidates.FirstOrDefault(m =>
                (!string.IsNullOrWhiteSpace(normalizedPhone) && NormalizePhone(m.Phone) == normalizedPhone)
                || (!string.IsNullOrWhiteSpace(normalizedEmail) && NormalizeEmail(m.Email) == normalizedEmail));
        }

        private static ExistingMemberSummaryDto MapExistingMemberSummary(Member member)
        {
            return new ExistingMemberSummaryDto
            {
                Id = member.Id,
                FullName = member.FullName,
                Phone = member.Phone,
                Email = member.Email,
                PlanStartDate = member.PlanStartDate,
                PlanEndDate = member.PlanEndDate,
                Status = member.Status,
                MembershipType = member.MembershipType,
                TrainingType = member.TrainingType
            };
        }

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            var chars = phone.Where(char.IsDigit).ToArray();
            return new string(chars);
        }

        private static string NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "@#$%&*!";
            var all = upper + lower + digits + special;

            Span<char> password = stackalloc char[10];
            password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

            for (var i = 4; i < password.Length; i++)
            {
                password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            for (var i = password.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }

        private static DateTime GetDbTimestampNow()
        {
            return DateTime.UtcNow;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static string ResolveStatusFromPlanEndDate(DateOnly planEndDate)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return planEndDate < today ? "EXPIRED" : "ACTIVE";
        }
    }
}
