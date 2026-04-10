using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IMemberService
    {
        Task<MemberDto> CreateMemberAsync(Guid gymId, CreateMemberDto createMemberDto);
        Task<MemberDto> UpdateMemberAsync(Guid gymId, Guid memberId, UpdateMemberDto updateMemberDto);
        Task<bool> DeleteMemberAsync(Guid gymId, Guid memberId);
        Task<MemberDto> GetMemberAsync(Guid gymId, Guid memberId);
        Task<List<MemberDto>> GetMembersAsync(Guid gymId, int pageNumber = 1, int pageSize = 10);
        Task<List<MemberDto>> SearchMembersAsync(Guid gymId, MemberSearchDto searchDto);
        Task<List<MemberDto>> GetUpcomingRenewalsAsync(Guid gymId, int days = 7, int limit = 100);
    }

    public class MemberService : IMemberService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<MemberService> _logger;

        public MemberService(GymDbContext context, ILogger<MemberService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MemberDto> CreateMemberAsync(Guid gymId, CreateMemberDto createMemberDto)
        {
            try
            {
                ValidateMembershipDates(createMemberDto.PlanStartDate, createMemberDto.PlanEndDate);

                var member = new Member
                {
                    GymId = gymId,
                    FullName = createMemberDto.FullName.Trim(),
                    Phone = createMemberDto.Phone?.Trim(),
                    Gender = createMemberDto.Gender?.Trim(),
                    DateOfBirth = createMemberDto.DateOfBirth,
                    JoinDate = createMemberDto.JoinDate,
                    PlanStartDate = createMemberDto.PlanStartDate,
                    PlanEndDate = createMemberDto.PlanEndDate,
                    MembershipType = createMemberDto.MembershipType?.Trim(),
                    AmountPaid = createMemberDto.AmountPaid,
                    PaymentStatus = createMemberDto.PaymentStatus,
                    EmergencyContact = createMemberDto.EmergencyContact?.Trim(),
                    Height = createMemberDto.Height,
                    Weight = createMemberDto.Weight,
                    FitnessGoal = createMemberDto.FitnessGoal?.Trim(),
                    TrainerAssigned = createMemberDto.TrainerAssigned?.Trim(),
                    LeadSource = createMemberDto.LeadSource?.Trim(),
                    Notes = createMemberDto.Notes?.Trim(),
                    Status = ResolveStatusFromPlanEndDate(createMemberDto.PlanEndDate)
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Member created: {member.Id}");
                return MapMemberToDto(member);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating member: {ex.Message}");
                throw;
            }
        }

        public async Task<MemberDto> UpdateMemberAsync(Guid gymId, Guid memberId, UpdateMemberDto updateMemberDto)
        {
            try
            {
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.GymId == gymId);

                if (member == null)
                {
                    throw new KeyNotFoundException($"Member not found");
                }

                if (!string.IsNullOrEmpty(updateMemberDto.FullName))
                    member.FullName = updateMemberDto.FullName;

                if (!string.IsNullOrEmpty(updateMemberDto.Phone))
                    member.Phone = updateMemberDto.Phone;

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
                    member.MembershipType = updateMemberDto.MembershipType;

                if (updateMemberDto.AmountPaid.HasValue)
                    member.AmountPaid = updateMemberDto.AmountPaid;

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

                if (!string.IsNullOrEmpty(updateMemberDto.FitnessGoal))
                    member.FitnessGoal = updateMemberDto.FitnessGoal;

                if (!string.IsNullOrEmpty(updateMemberDto.TrainerAssigned))
                    member.TrainerAssigned = updateMemberDto.TrainerAssigned;

                if (!string.IsNullOrEmpty(updateMemberDto.Notes))
                    member.Notes = updateMemberDto.Notes;

                member.UpdatedAt = DateTime.UtcNow;
                _context.Members.Update(member);
                await _context.SaveChangesAsync();

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
                    .Select(m => MapMemberToDto(m))
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

                // Search by name or phone
                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    var searchTerm = searchDto.SearchTerm.Trim();
                    query = query.Where(m =>
                        EF.Functions.ILike(m.FullName, $"%{searchTerm}%") ||
                        (m.Phone != null && EF.Functions.ILike(m.Phone, $"%{searchTerm}%")));
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
                    .Select(m => MapMemberToDto(m))
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
                Gender = member.Gender,
                DateOfBirth = member.DateOfBirth,
                JoinDate = member.JoinDate,
                PlanStartDate = member.PlanStartDate,
                PlanEndDate = member.PlanEndDate,
                LastPaymentDate = member.LastPaymentDate,
                MembershipType = member.MembershipType,
                AmountPaid = member.AmountPaid,
                PaymentStatus = member.PaymentStatus,
                Status = member.Status,
                Notes = member.Notes,
                EmergencyContact = member.EmergencyContact,
                Height = member.Height,
                Weight = member.Weight,
                FitnessGoal = member.FitnessGoal,
                TrainerAssigned = member.TrainerAssigned,
                LeadSource = member.LeadSource,
                ProfileImageUrl = member.ProfileImageUrl,
                CreatedAt = member.CreatedAt,
                UpdatedAt = member.UpdatedAt
            };
        }

        public async Task<List<MemberDto>> GetUpcomingRenewalsAsync(Guid gymId, int days = 7, int limit = 100)
        {
            try
            {
                days = Math.Clamp(days, 1, 90);
                limit = Math.Clamp(limit, 1, 500);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var endDate = today.AddDays(days);

                return await _context.Members
                    .Where(m => m.GymId == gymId
                                && m.PlanEndDate >= today
                                && m.PlanEndDate <= endDate
                                && m.Status != "PAUSED")
                    .OrderBy(m => m.PlanEndDate)
                    .Take(limit)
                    .Select(m => MapMemberToDto(m))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting upcoming renewals: {ex.Message}");
                throw;
            }
        }

        private static void ValidateMembershipDates(DateOnly planStartDate, DateOnly planEndDate)
        {
            if (planEndDate < planStartDate)
            {
                throw new InvalidOperationException("Plan end date must be after or equal to plan start date.");
            }
        }

        private static string ResolveStatusFromPlanEndDate(DateOnly planEndDate)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return planEndDate < today ? "EXPIRED" : "ACTIVE";
        }
    }
}
