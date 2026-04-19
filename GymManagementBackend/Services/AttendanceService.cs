using GymManagementBackend.Data;
using GymManagementBackend.DTOs;
using GymManagementBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceResultDto> MarkAttendanceAsync(MarkAttendanceDto dto);
        Task<List<MemberAttendanceDto>> GetTodayAttendanceAsync(Guid gymId);
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(GymDbContext context, ILogger<AttendanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AttendanceResultDto> MarkAttendanceAsync(MarkAttendanceDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            // 1. Find the member by phone or ID
            var member = await _context.Members
                .Where(m => m.GymId == dto.GymId && (m.Phone == dto.Identifier || m.Id.ToString() == dto.Identifier))
                .FirstOrDefaultAsync();

            if (member == null)
            {
                return new AttendanceResultDto { Success = false, Message = "Member not found. Please check your ID/Phone." };
            }

            if (member.Status != "ACTIVE")
            {
                return new AttendanceResultDto { Success = false, Message = $"Your membership is currently {member.Status}. Please contact the gym owner.", MemberName = member.FullName };
            }

            // 2. Check if already checked in today
            var existing = await _context.MemberCheckins
                .AnyAsync(x => x.GymId == dto.GymId && x.MemberId == member.Id && x.CheckinDate == today);

            if (existing)
            {
                return new AttendanceResultDto 
                { 
                    Success = true, 
                    Message = "You are already checked in for today. Have a great workout!", 
                    MemberName = member.FullName,
                    ProfileImageUrl = member.ProfileImageUrl
                };
            }

            // 3. Record attendance
            var checkin = new MemberCheckin
            {
                GymId = dto.GymId,
                MemberId = member.Id,
                CheckinDate = today,
                CheckinAt = DateTime.UtcNow,
                Source = "QR_SCAN"
            };

            _context.MemberCheckins.Add(checkin);
            await _context.SaveChangesAsync();

            // 4. Cleanup old data for Basic plans (Retention: 1 Month)
            await CleanupOldAttendanceIfBasicAsync(dto.GymId);

            return new AttendanceResultDto
            {
                Success = true,
                Message = "Check-in successful! Welcome to the gym.",
                MemberName = member.FullName,
                CheckinAt = checkin.CheckinAt,
                ProfileImageUrl = member.ProfileImageUrl
            };
        }

        public async Task<List<MemberAttendanceDto>> GetTodayAttendanceAsync(Guid gymId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var checkins = await _context.MemberCheckins
                .AsNoTracking()
                .Where(x => x.GymId == gymId && x.CheckinDate == today)
                .Join(_context.Members,
                    c => c.MemberId,
                    m => m.Id,
                    (c, m) => new MemberAttendanceDto
                    {
                        Id = c.Id,
                        MemberId = c.MemberId,
                        MemberName = m.FullName,
                        MemberPhone = m.Phone,
                        CheckinAt = c.CheckinAt,
                        Source = c.Source,
                        ProfileImageUrl = m.ProfileImageUrl
                    })
                .OrderByDescending(x => x.CheckinAt)
                .ToListAsync();

            return checkins;
        }

        private async Task CleanupOldAttendanceIfBasicAsync(Guid gymId)
        {
            try 
            {
                var gym = await _context.Gyms.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gymId);
                if (gym != null && gym.SubscriptionPlan.ToLower() == "basic")
                {
                    var oneMonthAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
                    var oldRecords = await _context.MemberCheckins
                        .Where(x => x.GymId == gymId && x.CheckinDate < oneMonthAgo)
                        .ToListAsync();

                    if (oldRecords.Any())
                    {
                        _context.MemberCheckins.RemoveRange(oldRecords);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Cleaned up {oldRecords.Count} old attendance records for gym {gymId} (Basic Plan).");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during attendance cleanup for gym {gymId}");
            }
        }
    }
}
