using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IPaymentService
    {
        Task<PagedResponseDto<PaymentListItemDto>> GetPaymentsAsync(Guid gymId, PaymentListQueryDto queryDto);
    }

    public class PaymentService : IPaymentService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(GymDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResponseDto<PaymentListItemDto>> GetPaymentsAsync(Guid gymId, PaymentListQueryDto queryDto)
        {
            try
            {
                var pageNumber = Math.Max(1, queryDto.PageNumber);
                var pageSize = Math.Clamp(queryDto.PageSize, 5, 100);
                var sortBy = string.IsNullOrWhiteSpace(queryDto.SortBy) ? "paymentDate" : queryDto.SortBy.Trim();
                var sortDirection = string.IsNullOrWhiteSpace(queryDto.SortDirection) ? "desc" : queryDto.SortDirection.Trim();

                var query = _context.Payments
                    .AsNoTracking()
                    .Include(p => p.Member)
                    .Where(p => p.GymId == gymId);

                query = ApplyFilters(query, queryDto);
                query = ApplySorting(query, sortBy, sortDirection);

                var totalCount = await query.CountAsync();
                var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaymentListItemDto
                    {
                        PaymentId = p.Id,
                        MemberId = p.MemberId,
                        MemberName = p.Member.FullName,
                        MemberPhone = p.Member.Phone,
                        MemberProfileImageUrl = p.Member.ProfileImageUrl,
                        MemberJoinDate = p.Member.JoinDate,
                        PlanMonths = p.PlanDurationMonths ?? GetPlanMonths(p.Member.PlanStartDate, p.Member.PlanEndDate),
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        PaymentMode = p.PaymentMode,
                        Remarks = p.Remarks,
                        MemberPaymentStatus = p.Member.PaymentStatus,
                        MemberPendingAmount = Math.Max(0m, (p.Member.AmountToPay ?? 0m) - (p.Member.AmountPaid ?? 0m)),
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                return new PagedResponseDto<PaymentListItemDto>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting payments list: {Message}", ex.Message);
                throw;
            }
        }

        private static IQueryable<Payment> ApplyFilters(IQueryable<Payment> query, PaymentListQueryDto filters)
        {
            if (filters.MemberId.HasValue)
            {
                query = query.Where(p => p.MemberId == filters.MemberId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var term = filters.SearchTerm.Trim();
                query = query.Where(p =>
                    EF.Functions.ILike(p.Member.FullName, $"%{term}%")
                    || (p.Member.Phone != null && EF.Functions.ILike(p.Member.Phone, $"%{term}%")));
            }

            if (!string.IsNullOrWhiteSpace(filters.PaymentMode))
            {
                var mode = filters.PaymentMode.Trim().ToUpperInvariant();
                query = query.Where(p => p.PaymentMode != null && p.PaymentMode.ToUpper() == mode);
            }

            if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
            {
                var status = filters.PaymentStatus.Trim().ToUpperInvariant();
                query = query.Where(p => p.Member.PaymentStatus.ToUpper() == status);
            }

            if (filters.PaymentDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate == filters.PaymentDate.Value);
            }

            if (filters.PaymentDateFrom.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= filters.PaymentDateFrom.Value);
            }

            if (filters.PaymentDateTo.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= filters.PaymentDateTo.Value);
            }

            if (filters.AmountMin.HasValue)
            {
                query = query.Where(p => p.Amount >= filters.AmountMin.Value);
            }

            if (filters.AmountMax.HasValue)
            {
                query = query.Where(p => p.Amount <= filters.AmountMax.Value);
            }

            return query;
        }

        private static IQueryable<Payment> ApplySorting(IQueryable<Payment> query, string sortBy, string sortDirection)
        {
            var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var field = sortBy.Trim().ToLowerInvariant();

            return (field, isDesc) switch
            {
                ("membername", false) => query.OrderBy(p => p.Member.FullName).ThenBy(p => p.PaymentDate),
                ("membername", true) => query.OrderByDescending(p => p.Member.FullName).ThenByDescending(p => p.PaymentDate),
                ("amount", false) => query.OrderBy(p => p.Amount).ThenByDescending(p => p.PaymentDate),
                ("amount", true) => query.OrderByDescending(p => p.Amount).ThenByDescending(p => p.PaymentDate),
                ("paymentmode", false) => query.OrderBy(p => p.PaymentMode).ThenByDescending(p => p.PaymentDate),
                ("paymentmode", true) => query.OrderByDescending(p => p.PaymentMode).ThenByDescending(p => p.PaymentDate),
                ("createdat", false) => query.OrderBy(p => p.CreatedAt),
                ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
                ("paymentdate", false) => query.OrderBy(p => p.PaymentDate).ThenByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreatedAt)
            };
        }

        private static int GetPlanMonths(DateOnly start, DateOnly end)
        {
            if (end < start)
            {
                return 1;
            }

            for (var months = 1; months <= 24; months++)
            {
                var computedEnd = start.AddMonths(months).AddDays(-1);
                if (computedEnd == end)
                {
                    return months;
                }
            }

            var inclusiveDays = (end.DayNumber - start.DayNumber) + 1;
            return Math.Max(1, (int)Math.Round(inclusiveDays / 30.0, MidpointRounding.AwayFromZero));
        }
    }
}
