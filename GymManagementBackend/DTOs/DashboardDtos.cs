namespace GymManagementBackend.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalActiveMembers { get; set; }
        public int TotalActiveMembersLastMonth { get; set; }
        public int NewJoinsThisMonth { get; set; }
        public int NewJoinsLastMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal PendingAmountTotal { get; set; }
        public int ExpiringInNext7Days { get; set; }
        public int ExpiringThisMonth { get; set; }
        public int ExpiredMembers { get; set; }
        public int InactiveThisMonth { get; set; }
    }

    public class MonthlyJoinTrendDto
    {
        public string Month { get; set; } = string.Empty;
        public int JoinCount { get; set; }
    }

    public class MonthlyRevenueTrendDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class MonthlyMemberFlowDto
    {
        public string Month { get; set; } = string.Empty;
        public int NewJoinees { get; set; }
        public int InactiveMembers { get; set; }
    }

    public class RecentMemberDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateOnly JoinDate { get; set; }
        public DateOnly PlanStartDate { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MembershipType { get; set; }
        public decimal? AmountPaid { get; set; }
    }

    public class WeeklyMemberGrowthDto
    {
        public string Week { get; set; } = string.Empty;
        public int NewJoinees { get; set; }
        public int InactiveMembers { get; set; }
    }

    public class DashboardDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<MonthlyJoinTrendDto> MonthlyTrends { get; set; } = [];
        public List<RecentMemberDto> RecentMembers { get; set; } = [];
    }

    public class IrregularMemberDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly JoinDate { get; set; }
        public DateOnly? LastCheckinDate { get; set; }
        public int DaysAbsent { get; set; }
        public string? Phone { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateOnly PlanEndDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PaginatedIrregularMembersDto
    {
        public List<IrregularMemberDto> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
