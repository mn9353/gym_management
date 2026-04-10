using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardDataAsync(Guid gymId);
        Task<DashboardStatsDto> GetDashboardStatsAsync(Guid gymId);
        Task<List<MonthlyJoinTrendDto>> GetMonthlyJoinTrendAsync(Guid gymId, int months = 6);
        Task<List<MonthlyRevenueTrendDto>> GetMonthlyRevenueTrendAsync(Guid gymId, int months = 6);
        Task<List<MonthlyMemberFlowDto>> GetMonthlyMemberFlowAsync(Guid gymId, int months = 6);
        Task<List<RecentMemberDto>> GetRecentMembersAsync(Guid gymId, int limit = 5);
    }

    public class DashboardService : IDashboardService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(GymDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardDto> GetDashboardDataAsync(Guid gymId)
        {
            try
            {
                var stats = await GetDashboardStatsAsync(gymId);
                var trends = await GetMonthlyJoinTrendAsync(gymId);
                var recentMembers = await GetRecentMembersAsync(gymId);

                return new DashboardDto
                {
                    Stats = stats,
                    MonthlyTrends = trends,
                    RecentMembers = recentMembers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting dashboard data: {ex.Message}");
                throw;
            }
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(Guid gymId)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var lastMonth = currentMonth.AddMonths(-1);
                var nextWeek = today.AddDays(7);

                var stats = new DashboardStatsDto();

                // Total active members (date-driven; paused members are excluded)
                stats.TotalActiveMembers = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.Status != "PAUSED" &&
                                      m.PlanEndDate >= today);

                // New joins this month
                stats.NewJoinsThisMonth = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.JoinDate >= DateOnly.FromDateTime(currentMonth) &&
                                      m.JoinDate < DateOnly.FromDateTime(currentMonth.AddMonths(1)));

                // New joins last month
                stats.NewJoinsLastMonth = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.JoinDate >= DateOnly.FromDateTime(lastMonth) &&
                                      m.JoinDate < DateOnly.FromDateTime(currentMonth));

                // Revenue this month
                stats.RevenueThisMonth = (await _context.Payments
                    .Where(p => p.GymId == gymId && 
                                p.PaymentDate >= DateOnly.FromDateTime(currentMonth) &&
                                p.PaymentDate < DateOnly.FromDateTime(currentMonth.AddMonths(1)))
                    .Select(p => (decimal?)p.Amount)
                    .SumAsync()) ?? 0m;

                // Expiring in next 7 days (date-driven; paused members are excluded)
                stats.ExpiringInNext7Days = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.PlanEndDate >= today && 
                                      m.PlanEndDate <= nextWeek &&
                                      m.Status != "PAUSED");

                // Expired members (strictly date-driven)
                stats.ExpiredMembers = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.PlanEndDate < today);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting dashboard stats: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MonthlyJoinTrendDto>> GetMonthlyJoinTrendAsync(Guid gymId, int months = 6)
        {
            try
            {
                months = Math.Clamp(months, 1, 24);
                var trends = new List<MonthlyJoinTrendDto>();

                for (int i = months - 1; i >= 0; i--)
                {
                    var month = DateTime.UtcNow.AddMonths(-i);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);

                    var count = await _context.Members
                        .CountAsync(m => m.GymId == gymId &&
                                          m.JoinDate >= DateOnly.FromDateTime(monthStart) &&
                                          m.JoinDate < DateOnly.FromDateTime(monthEnd));

                    trends.Add(new MonthlyJoinTrendDto
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        JoinCount = count
                    });
                }

                return trends;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting monthly join trend: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MonthlyRevenueTrendDto>> GetMonthlyRevenueTrendAsync(Guid gymId, int months = 6)
        {
            try
            {
                months = Math.Clamp(months, 1, 24);
                var trends = new List<MonthlyRevenueTrendDto>();

                for (int i = months - 1; i >= 0; i--)
                {
                    var month = DateTime.UtcNow.AddMonths(-i);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);

                    var revenue = (await _context.Payments
                        .Where(p => p.GymId == gymId
                                    && p.PaymentDate >= DateOnly.FromDateTime(monthStart)
                                    && p.PaymentDate < DateOnly.FromDateTime(monthEnd))
                        .Select(p => (decimal?)p.Amount)
                        .SumAsync()) ?? 0m;

                    trends.Add(new MonthlyRevenueTrendDto
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        Revenue = revenue
                    });
                }

                return trends;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting monthly revenue trend: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MonthlyMemberFlowDto>> GetMonthlyMemberFlowAsync(Guid gymId, int months = 6)
        {
            try
            {
                months = Math.Clamp(months, 1, 24);
                var flow = new List<MonthlyMemberFlowDto>();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                for (int i = months - 1; i >= 0; i--)
                {
                    var month = DateTime.UtcNow.AddMonths(-i);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);
                    var monthStartDate = DateOnly.FromDateTime(monthStart);
                    var monthEndDate = DateOnly.FromDateTime(monthEnd);

                    var newJoinees = await _context.Members
                        .CountAsync(m => m.GymId == gymId
                                         && m.JoinDate >= monthStartDate
                                         && m.JoinDate < monthEndDate);

                    // Count inactive members strictly by plan-end month.
                    // If a membership ended in this month, it contributes to this month's inactive flow.
                    var inactiveMembers = await _context.Members
                        .CountAsync(m => m.GymId == gymId
                                         && m.PlanEndDate >= monthStartDate
                                         && m.PlanEndDate < monthEndDate);

                    flow.Add(new MonthlyMemberFlowDto
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        NewJoinees = newJoinees,
                        InactiveMembers = inactiveMembers
                    });
                }

                return flow;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting monthly member flow: {ex.Message}");
                throw;
            }
        }

        public async Task<List<RecentMemberDto>> GetRecentMembersAsync(Guid gymId, int limit = 5)
        {
            try
            {
                limit = Math.Clamp(limit, 1, 50);
                return await _context.Members
                    .Where(m => m.GymId == gymId)
                    .OrderByDescending(m => m.JoinDate)
                    .Take(limit)
                    .Select(m => new RecentMemberDto
                    {
                        Id = m.Id,
                        FullName = m.FullName,
                        Phone = m.Phone,
                        JoinDate = m.JoinDate,
                        PlanEndDate = m.PlanEndDate,
                        Status = m.Status,
                        MembershipType = m.MembershipType
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent members: {ex.Message}");
                throw;
            }
        }
    }
}
