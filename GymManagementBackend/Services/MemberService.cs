using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq.Expressions;

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
        Task<PagedResponseDto<MemberListItemDto>> GetMembersListAsync(Guid gymId, MemberListQueryDto queryDto, string segment);
        Task<PagedResponseDto<MemberListItemDto>> GetMembersGridAsync(Guid gymId, MemberGridRequestDto request, string segment);
    }

    public class MemberService : IMemberService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<MemberService> _logger;
        private static readonly Expression<Func<Member, MemberDto>> MemberToDtoProjection = m => new MemberDto
        {
            Id = m.Id,
            GymId = m.GymId,
            FullName = m.FullName,
            Phone = m.Phone,
            Gender = m.Gender,
            DateOfBirth = m.DateOfBirth,
            JoinDate = m.JoinDate,
            PlanStartDate = m.PlanStartDate,
            PlanEndDate = m.PlanEndDate,
            LastPaymentDate = m.LastPaymentDate,
            MembershipType = m.MembershipType,
            AmountPaid = m.AmountPaid,
            PaymentStatus = m.PaymentStatus,
            Status = m.Status,
            Notes = m.Notes,
            EmergencyContact = m.EmergencyContact,
            Height = m.Height,
            Weight = m.Weight,
            FitnessGoal = m.FitnessGoal,
            TrainerAssigned = m.TrainerAssigned,
            LeadSource = m.LeadSource,
            ProfileImageUrl = m.ProfileImageUrl,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };

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

        public async Task<PagedResponseDto<MemberListItemDto>> GetMembersListAsync(Guid gymId, MemberListQueryDto queryDto, string segment)
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
                        Gender = m.Gender,
                        JoinDate = m.JoinDate,
                        PlanStartDate = m.PlanStartDate,
                        PlanEndDate = m.PlanEndDate,
                        Status = m.Status,
                        PaymentStatus = m.PaymentStatus,
                        MembershipType = m.MembershipType,
                        TrainerAssigned = m.TrainerAssigned,
                        AmountPaid = normalized.IncludeAmount ? m.AmountPaid : null
                    })
                    .ToListAsync();

                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)normalized.PageSize));
                return new PagedResponseDto<MemberListItemDto>
                {
                    Items = items,
                    PageNumber = normalized.PageNumber,
                    PageSize = normalized.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting members list for segment {segment}: {ex.Message}");
                throw;
            }
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
                    .Select(MemberToDtoProjection)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting upcoming renewals: {ex.Message}");
                throw;
            }
        }

        public async Task<PagedResponseDto<MemberListItemDto>> GetMembersGridAsync(Guid gymId, MemberGridRequestDto request, string segment)
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
                    query = query.Where(m =>
                        EF.Functions.ILike(m.FullName, $"%{term}%")
                        || (m.Phone != null && EF.Functions.ILike(m.Phone, $"%{term}%"))
                        || (m.MembershipType != null && EF.Functions.ILike(m.MembershipType, $"%{term}%")));
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
                        Gender = m.Gender,
                        JoinDate = m.JoinDate,
                        PlanStartDate = m.PlanStartDate,
                        PlanEndDate = m.PlanEndDate,
                        Status = m.Status,
                        PaymentStatus = m.PaymentStatus,
                        MembershipType = m.MembershipType,
                        TrainerAssigned = m.TrainerAssigned,
                        AmountPaid = request.IncludeAmount ? m.AmountPaid : null
                    })
                    .ToListAsync();

                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)request.PageSize));
                return new PagedResponseDto<MemberListItemDto>
                {
                    Items = items,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting AG Grid members list for segment {segment}: {ex.Message}");
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
                query = query.Where(m =>
                    EF.Functions.ILike(m.FullName, $"%{term}%")
                    || (m.Phone != null && EF.Functions.ILike(m.Phone, $"%{term}%"))
                    || (m.MembershipType != null && EF.Functions.ILike(m.MembershipType, $"%{term}%"))
                    || (m.TrainerAssigned != null && EF.Functions.ILike(m.TrainerAssigned, $"%{term}%"))
                    || (m.LeadSource != null && EF.Functions.ILike(m.LeadSource, $"%{term}%")));
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

            if (!string.IsNullOrWhiteSpace(filters.Gender))
            {
                var gender = filters.Gender.Trim().ToUpperInvariant();
                query = query.Where(m => m.Gender != null && m.Gender.ToUpper() == gender);
            }

            if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
            {
                var paymentStatus = filters.PaymentStatus.Trim().ToUpperInvariant();
                query = query.Where(m => m.PaymentStatus.ToUpper() == paymentStatus);
            }

            if (!string.IsNullOrWhiteSpace(filters.MembershipType))
            {
                var membershipType = filters.MembershipType.Trim();
                query = query.Where(m => m.MembershipType != null && EF.Functions.ILike(m.MembershipType, $"%{membershipType}%"));
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

            return query;
        }

        private static IQueryable<Member> ApplySorting(IQueryable<Member> query, string sortBy, string sortDirection)
        {
            var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var field = sortBy.Trim().ToLowerInvariant();

            return (field, isDesc) switch
            {
                ("name", false) => query.OrderBy(m => m.FullName).ThenBy(m => m.Id),
                ("name", true) => query.OrderByDescending(m => m.FullName).ThenBy(m => m.Id),
                ("phone", false) => query.OrderBy(m => m.Phone).ThenBy(m => m.Id),
                ("phone", true) => query.OrderByDescending(m => m.Phone).ThenBy(m => m.Id),
                ("joindate", false) => query.OrderBy(m => m.JoinDate).ThenBy(m => m.Id),
                ("joindate", true) => query.OrderByDescending(m => m.JoinDate).ThenBy(m => m.Id),
                ("planstartdate", false) => query.OrderBy(m => m.PlanStartDate).ThenBy(m => m.Id),
                ("planstartdate", true) => query.OrderByDescending(m => m.PlanStartDate).ThenBy(m => m.Id),
                ("amountpaid", false) => query.OrderBy(m => m.AmountPaid).ThenBy(m => m.Id),
                ("amountpaid", true) => query.OrderByDescending(m => m.AmountPaid).ThenBy(m => m.Id),
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

        private static string NormalizeGridField(string field)
        {
            return field.Trim().ToLowerInvariant() switch
            {
                "name" => "fullname",
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

        private static string ResolveStatusFromPlanEndDate(DateOnly planEndDate)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return planEndDate < today ? "EXPIRED" : "ACTIVE";
        }
    }
}
