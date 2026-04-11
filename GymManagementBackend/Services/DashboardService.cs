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
        Task<List<WeeklyMemberGrowthDto>> GetWeeklyGrowthAsync(Guid gymId, int weeks = 4);
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
                var currentMonthEnd = currentMonth.AddMonths(1);
                var lastMonthEnd = currentMonth.AddDays(-1);
                var lastMonthStart = lastMonth;
                var nextWeek = today.AddDays(7);

                var stats = new DashboardStatsDto();

                // Total active members (date-driven; paused members are excluded)
                stats.TotalActiveMembers = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.Status != "PAUSED" &&
                                      m.PlanEndDate >= today);

                stats.TotalActiveMembersLastMonth = await _context.Members
                    .CountAsync(m => m.GymId == gymId
                                     && m.Status != "PAUSED"
                                     && m.PlanEndDate >= DateOnly.FromDateTime(lastMonthEnd));

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

                stats.RevenueLastMonth = (await _context.Payments
                    .Where(p => p.GymId == gymId
                                && p.PaymentDate >= DateOnly.FromDateTime(lastMonthStart)
                                && p.PaymentDate < DateOnly.FromDateTime(currentMonth))
                    .Select(p => (decimal?)p.Amount)
                    .SumAsync()) ?? 0m;

                // Expiring in next 7 days (date-driven; paused members are excluded)
                stats.ExpiringInNext7Days = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.PlanEndDate >= today && 
                                      m.PlanEndDate <= nextWeek &&
                                      m.Status != "PAUSED");

                stats.ExpiringThisMonth = await _context.Members
                    .CountAsync(m => m.GymId == gymId
                                     && m.PlanEndDate >= DateOnly.FromDateTime(currentMonth)
                                     && m.PlanEndDate < DateOnly.FromDateTime(currentMonthEnd)
                                     && m.Status != "PAUSED");

                // Expired members (strictly date-driven)
                stats.ExpiredMembers = await _context.Members
                    .CountAsync(m => m.GymId == gymId && 
                                      m.PlanEndDate < today);

                stats.InactiveThisMonth = await _context.Members
                    .CountAsync(m => m.GymId == gymId
                                     && m.PlanEndDate >= DateOnly.FromDateTime(currentMonth)
                                     && m.PlanEndDate < DateOnly.FromDateTime(currentMonthEnd)
                                     && m.PlanEndDate < today);

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
                var flow = new List<MonthlyMemberFlowDto>(months);
                var connection = _context.Database.GetDbConnection();

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                await using var command = connection.CreateCommand();
                command.CommandText = @"
WITH months AS (
    SELECT generate_series(
        date_trunc('month', CURRENT_DATE) - ((@months - 1) * INTERVAL '1 month'),
        date_trunc('month', CURRENT_DATE),
        INTERVAL '1 month'
    )::date AS month_start
)
SELECT
    to_char(m.month_start, 'Mon YYYY') AS month,
    COUNT(mem.id) FILTER (
        WHERE mem.join_date >= m.month_start
          AND mem.join_date < (m.month_start + INTERVAL '1 month')::date
    ) AS new_joinees,
    COUNT(mem.id) FILTER (
        WHERE mem.plan_end_date >= m.month_start
          AND mem.plan_end_date < (m.month_start + INTERVAL '1 month')::date
    ) AS inactive_members
FROM months m
LEFT JOIN members mem ON mem.gym_id = @gymId
GROUP BY m.month_start
ORDER BY m.month_start;";

                var gymIdParam = command.CreateParameter();
                gymIdParam.ParameterName = "@gymId";
                gymIdParam.Value = gymId;
                command.Parameters.Add(gymIdParam);

                var monthsParam = command.CreateParameter();
                monthsParam.ParameterName = "@months";
                monthsParam.Value = months;
                command.Parameters.Add(monthsParam);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    flow.Add(new MonthlyMemberFlowDto
                    {
                        Month = reader.GetString(0),
                        NewJoinees = Convert.ToInt32(reader.GetInt64(1)),
                        InactiveMembers = Convert.ToInt32(reader.GetInt64(2))
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
                        ProfileImageUrl = m.ProfileImageUrl,
                        JoinDate = m.JoinDate,
                        PlanEndDate = m.PlanEndDate,
                        Status = m.Status,
                        MembershipType = m.MembershipType,
                        AmountPaid = m.AmountPaid
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent members: {ex.Message}");
                throw;
            }
        }

        public async Task<List<WeeklyMemberGrowthDto>> GetWeeklyGrowthAsync(Guid gymId, int weeks = 4)
        {
            try
            {
                weeks = Math.Clamp(weeks, 1, 8);
                var now = DateTime.UtcNow;
                var startOfMonth = new DateOnly(now.Year, now.Month, 1);
                var monthEndExclusive = DateOnly.FromDateTime(new DateTime(now.Year, now.Month, 1).AddMonths(1));

                var data = new List<WeeklyMemberGrowthDto>(weeks);
                for (var i = 0; i < weeks; i++)
                {
                    var weekStart = startOfMonth.AddDays(i * 7);
                    if (weekStart >= monthEndExclusive)
                    {
                        data.Add(new WeeklyMemberGrowthDto
                        {
                            Week = $"Week {i + 1}",
                            NewJoinees = 0,
                            InactiveMembers = 0
                        });
                        continue;
                    }

                    var computedEnd = weekStart.AddDays(7);
                    var weekEndExclusive = computedEnd < monthEndExclusive ? computedEnd : monthEndExclusive;

                    var joins = await _context.Members
                        .CountAsync(m => m.GymId == gymId
                                         && m.JoinDate >= weekStart
                                         && m.JoinDate < weekEndExclusive);

                    var inactive = await _context.Members
                        .CountAsync(m => m.GymId == gymId
                                         && m.PlanEndDate >= weekStart
                                         && m.PlanEndDate < weekEndExclusive);

                    data.Add(new WeeklyMemberGrowthDto
                    {
                        Week = $"Week {i + 1}",
                        NewJoinees = joins,
                        InactiveMembers = inactive
                    });
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting weekly growth: {ex.Message}");
                throw;
            }
        }
    }
}
