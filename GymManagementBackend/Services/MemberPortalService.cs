using System.Security.Claims;
using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IMemberPortalService
    {
        Task<MemberPortalSummaryDto> GetSummaryAsync(ClaimsPrincipal user);
        Task<IReadOnlyList<MemberWeightPointDto>> GetWeightHistoryAsync(ClaimsPrincipal user, int months);
        Task<MemberPortalSummaryDto> UpsertMetricAsync(ClaimsPrincipal user, MemberMetricUpdateRequestDto request);
        Task<MemberAttendanceSummaryDto> GetAttendanceAsync(ClaimsPrincipal user, int months, int limit);
        Task<IReadOnlyList<MemberMissedTrendPointDto>> GetMissedDaysTrendAsync(ClaimsPrincipal user, int weeks);
        Task<IReadOnlyList<MemberMuscleDistributionDto>> GetMonthlyMuscleDistributionAsync(ClaimsPrincipal user, int months);
        Task<MemberCheckinResultDto> CheckinByQrAsync(ClaimsPrincipal user, MemberCheckinScanRequestDto request);
        Task<MemberCheckinResultDto> AddWorkoutForCheckinAsync(ClaimsPrincipal user, Guid checkinId, MemberWorkoutLogRequestDto request);
    }

    public class MemberPortalService : IMemberPortalService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<MemberPortalService> _logger;

        public MemberPortalService(GymDbContext context, ILogger<MemberPortalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MemberPortalSummaryDto> GetSummaryAsync(ClaimsPrincipal user)
        {
            var member = await ResolveMemberAsync(user, includeGym: true);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var thirtyDaysAgo = today.AddDays(-30);

            var checkedInToday = await _context.MemberCheckins
                .AsNoTracking()
                .AnyAsync(x => x.MemberId == member.Id && x.CheckinDate == today);

            var attendanceThisMonth = await _context.MemberCheckins
                .AsNoTracking()
                .CountAsync(x => x.MemberId == member.Id && x.CheckinDate >= monthStart && x.CheckinDate <= today);

            var attendanceLast30Days = await _context.MemberCheckins
                .AsNoTracking()
                .CountAsync(x => x.MemberId == member.Id && x.CheckinDate >= thirtyDaysAgo && x.CheckinDate <= today);

            var latestWeightMetricDate = await _context.MemberBodyMetrics
                .AsNoTracking()
                .Where(x => x.MemberId == member.Id && x.WeightKg.HasValue)
                .OrderByDescending(x => x.MetricDate)
                .Select(x => (DateOnly?)x.MetricDate)
                .FirstOrDefaultAsync();

            var checkinDates = await _context.MemberCheckins
                .AsNoTracking()
                .Where(x => x.MemberId == member.Id)
                .Select(x => x.CheckinDate)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var currentStreak = ComputeCurrentStreak(checkinDates, today);
            var bestStreak = ComputeBestStreak(checkinDates);
            var totalDays = (today.DayNumber - thirtyDaysAgo.DayNumber) + 1;
            var missedLast30 = Math.Max(0, totalDays - attendanceLast30Days);

            return new MemberPortalSummaryDto
            {
                MemberId = member.Id,
                GymId = member.GymId,
                GymName = member.Gym?.GymName ?? "Gym",
                GymEmail = member.Gym?.Email,
                GymPhone = member.Gym?.Phone,
                FullName = member.FullName,
                Email = member.Email,
                Phone = member.Phone,
                Height = member.Height,
                Weight = member.Weight,
                TargetWeight = member.TargetWeight,
                LastWeightUpdateDate = latestWeightMetricDate,
                JoinDate = member.JoinDate,
                PlanEndDate = member.PlanEndDate,
                DaysUntilPlanEnd = member.PlanEndDate.DayNumber - today.DayNumber,
                CheckedInToday = checkedInToday,
                AttendanceThisMonth = attendanceThisMonth,
                AttendanceLast30Days = attendanceLast30Days,
                MissedDaysLast30Days = missedLast30,
                CurrentStreakDays = currentStreak,
                BestStreakDays = bestStreak
            };
        }

        public async Task<IReadOnlyList<MemberWeightPointDto>> GetWeightHistoryAsync(ClaimsPrincipal user, int months)
        {
            var member = await ResolveMemberAsync(user);
            var normalizedMonths = Math.Clamp(months, 1, 24);
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-normalizedMonths);

            var points = await _context.MemberBodyMetrics
                .AsNoTracking()
                .Where(x => x.MemberId == member.Id && x.MetricDate >= fromDate && x.WeightKg.HasValue)
                .OrderBy(x => x.MetricDate)
                .Select(x => new MemberWeightPointDto
                {
                    Date = x.MetricDate,
                    WeightKg = x.WeightKg!.Value
                })
                .ToListAsync();

            if (points.Count == 0 && member.Weight.HasValue)
            {
                points.Add(new MemberWeightPointDto
                {
                    Date = member.JoinDate,
                    WeightKg = member.Weight.Value
                });
            }

            return points;
        }

        public async Task<MemberPortalSummaryDto> UpsertMetricAsync(ClaimsPrincipal user, MemberMetricUpdateRequestDto request)
        {
            var member = await ResolveMemberAsync(user);
            var now = DateTime.UtcNow;
            var normalizedWeight = request.WeightKg.HasValue ? decimal.Round(request.WeightKg.Value, 2) : (decimal?)null;
            var normalizedHeight = request.HeightCm.HasValue ? decimal.Round(request.HeightCm.Value, 2) : (decimal?)null;
            var normalizedTargetWeight = request.TargetWeightKg.HasValue ? decimal.Round(request.TargetWeightKg.Value, 2) : (decimal?)null;

            if (normalizedWeight.HasValue || normalizedHeight.HasValue || normalizedTargetWeight.HasValue)
            {
                member.UpdatedAt = now;
                if (normalizedWeight.HasValue)
                {
                    member.Weight = normalizedWeight.Value;
                }
                if (normalizedHeight.HasValue)
                {
                    member.Height = normalizedHeight.Value;
                }
                if (normalizedTargetWeight.HasValue)
                {
                    member.TargetWeight = normalizedTargetWeight.Value;
                }
            }

            if (normalizedWeight.HasValue)
            {
                var metric = new MemberBodyMetric
                {
                    GymId = member.GymId,
                    MemberId = member.Id,
                    MetricDate = request.MetricDate,
                    WeightKg = normalizedWeight,
                    Source = "MEMBER",
                    Notes = request.Notes?.Trim(),
                    CreatedAt = now
                };

                _context.MemberBodyMetrics.Add(metric);
            }

            await _context.SaveChangesAsync();
            return await GetSummaryAsync(user);
        }

        public async Task<MemberAttendanceSummaryDto> GetAttendanceAsync(ClaimsPrincipal user, int months, int limit)
        {
            var member = await ResolveMemberAsync(user);
            var normalizedMonths = Math.Clamp(months, 1, 24);
            var normalizedLimit = Math.Clamp(limit, 1, 120);
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-normalizedMonths);

            var checkins = await _context.MemberCheckins
                .AsNoTracking()
                .Where(x => x.MemberId == member.Id && x.CheckinDate >= fromDate)
                .OrderByDescending(x => x.CheckinDate)
                .ThenByDescending(x => x.CheckinAt)
                .Take(normalizedLimit)
                .ToListAsync();

            var checkinIds = checkins.Select(x => x.Id).ToList();
            var workoutByCheckin = await _context.MemberWorkoutLogs
                .AsNoTracking()
                .Where(x => x.MemberId == member.Id && x.CheckinId.HasValue && checkinIds.Contains(x.CheckinId.Value))
                .ToDictionaryAsync(x => x.CheckinId!.Value, x => (IReadOnlyList<string>)x.MuscleGroups);

            return new MemberAttendanceSummaryDto
            {
                TotalCheckins = checkins.Count,
                Recent = checkins.Select(x => new MemberAttendanceItemDto
                {
                    CheckinId = x.Id,
                    CheckinDate = x.CheckinDate,
                    CheckinAt = x.CheckinAt,
                    Source = x.Source,
                    MuscleGroups = workoutByCheckin.TryGetValue(x.Id, out var groups) ? groups : []
                }).ToList()
            };
        }

        public async Task<IReadOnlyList<MemberMissedTrendPointDto>> GetMissedDaysTrendAsync(ClaimsPrincipal user, int weeks)
        {
            var member = await ResolveMemberAsync(user);
            var normalizedWeeks = Math.Clamp(weeks, 2, 12);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var windowStart = today.AddDays(-(normalizedWeeks * 7) + 1);

            var checkinDates = await _context.MemberCheckins
                .AsNoTracking()
                .Where(x => x.MemberId == member.Id && x.CheckinDate >= windowStart && x.CheckinDate <= today)
                .Select(x => x.CheckinDate)
                .Distinct()
                .ToListAsync();

            var checkinSet = checkinDates.ToHashSet();
            var points = new List<MemberMissedTrendPointDto>();
            for (var i = normalizedWeeks - 1; i >= 0; i--)
            {
                var weekStart = today.AddDays(-(i * 7 + 6));
                var weekEnd = today.AddDays(-i * 7);
                var attended = 0;
                for (var d = weekStart; d.DayNumber <= weekEnd.DayNumber; d = d.AddDays(1))
                {
                    if (checkinSet.Contains(d))
                    {
                        attended++;
                    }
                }

                points.Add(new MemberMissedTrendPointDto
                {
                    Label = $"{weekStart:dd MMM}",
                    AttendedDays = attended,
                    MissedDays = Math.Max(0, 7 - attended)
                });
            }

            return points;
        }

        public async Task<IReadOnlyList<MemberMuscleDistributionDto>> GetMonthlyMuscleDistributionAsync(ClaimsPrincipal user, int months)
        {
            var member = await ResolveMemberAsync(user);
            var normalizedMonths = Math.Clamp(months, 1, 12);
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-normalizedMonths + 1);

            var logs = await _context.MemberWorkoutLogs
                .AsNoTracking()
                .Where(x => x.MemberId == member.Id && x.WorkoutDate >= fromDate)
                .ToListAsync();

            var flattened = logs
                .SelectMany(x => x.MuscleGroups ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .GroupBy(x => x)
                .Select(g => new MemberMuscleDistributionDto
                {
                    MuscleGroup = g.Key,
                    SessionCount = g.Count()
                })
                .OrderByDescending(x => x.SessionCount)
                .ThenBy(x => x.MuscleGroup)
                .ToList();

            return flattened;
        }

        public async Task<MemberCheckinResultDto> CheckinByQrAsync(ClaimsPrincipal user, MemberCheckinScanRequestDto request)
        {
            var member = await ResolveMemberAsync(user);
            ValidateQrPayload(member.GymId, request.QrValue);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var existing = await _context.MemberCheckins
                .FirstOrDefaultAsync(x => x.MemberId == member.Id && x.CheckinDate == today);

            if (existing is not null)
            {
                var logged = await UpsertWorkoutLogAsync(member, existing, request.MuscleGroups, request.Notes);
                return new MemberCheckinResultDto
                {
                    CheckinId = existing.Id,
                    CheckinDate = existing.CheckinDate,
                    CheckinAt = existing.CheckinAt,
                    AlreadyCheckedIn = true,
                    WorkoutLogged = logged
                };
            }

            var checkin = new MemberCheckin
            {
                GymId = member.GymId,
                MemberId = member.Id,
                CheckinDate = today,
                CheckinAt = DateTime.UtcNow,
                Source = "QR_SCAN",
                Notes = request.Notes?.Trim(),
                CreatedByUserId = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.MemberCheckins.Add(checkin);
            await _context.SaveChangesAsync();

            var workoutLogged = await UpsertWorkoutLogAsync(member, checkin, request.MuscleGroups, request.Notes);
            return new MemberCheckinResultDto
            {
                CheckinId = checkin.Id,
                CheckinDate = checkin.CheckinDate,
                CheckinAt = checkin.CheckinAt,
                AlreadyCheckedIn = false,
                WorkoutLogged = workoutLogged
            };
        }

        public async Task<MemberCheckinResultDto> AddWorkoutForCheckinAsync(ClaimsPrincipal user, Guid checkinId, MemberWorkoutLogRequestDto request)
        {
            var member = await ResolveMemberAsync(user);
            var checkin = await _context.MemberCheckins
                .FirstOrDefaultAsync(x => x.Id == checkinId && x.MemberId == member.Id);

            if (checkin is null)
            {
                throw new KeyNotFoundException("Check-in not found.");
            }

            await UpsertWorkoutLogAsync(member, checkin, request.MuscleGroups, request.Notes);
            return new MemberCheckinResultDto
            {
                CheckinId = checkin.Id,
                CheckinDate = checkin.CheckinDate,
                CheckinAt = checkin.CheckinAt,
                AlreadyCheckedIn = true,
                WorkoutLogged = true
            };
        }

        private async Task<Member> ResolveMemberAsync(ClaimsPrincipal user, bool includeGym = false)
        {
            var memberIdClaim = user.FindFirstValue("member_id");
            var emailClaim = user.FindFirstValue(ClaimTypes.Email);
            var gymIdClaim = user.FindFirstValue("gym_id");
            Guid.TryParse(memberIdClaim, out var memberId);
            Guid.TryParse(gymIdClaim, out var gymId);

            Member? member = null;

            if (memberId != Guid.Empty)
            {
                var query = _context.Members.AsQueryable();
                if (includeGym)
                {
                    query = query.Include(m => m.Gym);
                }
                member = await query.FirstOrDefaultAsync(m => m.Id == memberId);
            }

            if (member is null && !string.IsNullOrWhiteSpace(emailClaim))
            {
                var query = _context.Members.AsQueryable();
                if (includeGym)
                {
                    query = query.Include(m => m.Gym);
                }
                member = await query.FirstOrDefaultAsync(m =>
                    m.Email != null
                    && EF.Functions.ILike(m.Email, emailClaim)
                    && (gymId == Guid.Empty || m.GymId == gymId));
            }

            if (member is null)
            {
                throw new UnauthorizedAccessException("Member account is not linked to a profile.");
            }

            return member;
        }

        private static void ValidateQrPayload(Guid gymId, string qrValue)
        {
            var normalized = (qrValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("QR value is required.");
            }

            var gymIdText = gymId.ToString();
            var valid =
                string.Equals(normalized, gymIdText, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, $"GYM:{gymIdText}", StringComparison.OrdinalIgnoreCase);

            if (!valid)
            {
                throw new InvalidOperationException("Invalid gym QR code.");
            }
        }

        private async Task<bool> UpsertWorkoutLogAsync(Member member, MemberCheckin checkin, List<string>? groups, string? notes)
        {
            var normalizedGroups = NormalizeMuscleGroups(groups);
            if (normalizedGroups.Count == 0 && string.IsNullOrWhiteSpace(notes))
            {
                return false;
            }

            var existing = await _context.MemberWorkoutLogs
                .FirstOrDefaultAsync(x => x.CheckinId == checkin.Id);

            if (existing is null)
            {
                _context.MemberWorkoutLogs.Add(new MemberWorkoutLog
                {
                    GymId = member.GymId,
                    MemberId = member.Id,
                    CheckinId = checkin.Id,
                    WorkoutDate = checkin.CheckinDate,
                    MuscleGroups = normalizedGroups,
                    Notes = notes?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.MuscleGroups = normalizedGroups;
                existing.Notes = notes?.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private static List<string> NormalizeMuscleGroups(List<string>? groups)
        {
            if (groups is null || groups.Count == 0)
            {
                return [];
            }

            return groups
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.ToUpperInvariant())
                .Distinct()
                .Take(10)
                .ToList();
        }

        private static int ComputeCurrentStreak(List<DateOnly> dates, DateOnly today)
        {
            if (dates.Count == 0)
            {
                return 0;
            }

            var set = dates.ToHashSet();
            var cursor = set.Contains(today) ? today : today.AddDays(-1);
            var streak = 0;

            while (set.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }

            return streak;
        }

        private static int ComputeBestStreak(List<DateOnly> dates)
        {
            if (dates.Count == 0)
            {
                return 0;
            }

            var ordered = dates.Distinct().OrderBy(x => x).ToList();
            var best = 1;
            var current = 1;
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].DayNumber == ordered[i - 1].DayNumber + 1)
                {
                    current++;
                }
                else
                {
                    current = 1;
                }

                if (current > best)
                {
                    best = current;
                }
            }

            return best;
        }
    }
}
