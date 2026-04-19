using GymManagementBackend.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Services
{
    public class AttendanceCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceCleanupBackgroundService> _logger;
        private readonly TimeSpan _runInterval = TimeSpan.FromDays(1); // run daily

        public AttendanceCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<AttendanceCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Attendance cleanup background service started.");
            // Yield to allow application startup to complete
            await Task.Yield();
            // Initial run
            await RunCleanupAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_runInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                await RunCleanupAsync(stoppingToken);
            }

            _logger.LogInformation("Attendance cleanup background service stopping.");
        }

        private async Task RunCleanupAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
                var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

                var basicGymIds = await context.Gyms
                    .Where(g => g.SubscriptionPlan != null && g.SubscriptionPlan.ToLower() == "basic")
                    .Select(g => g.Id)
                    .ToListAsync(cancellationToken);

                foreach (var gymId in basicGymIds)
                {
                    await attendanceService.CleanupOldAttendanceForGymAsync(gymId);
                }

                _logger.LogInformation("Attendance cleanup completed for {Count} basic gyms.", basicGymIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during attendance cleanup background service.");
            }
        }
    }
}
