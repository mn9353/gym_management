using GymManagementBackend.Configuration;
using GymManagementBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymManagementBackend.Services
{
    public class MembershipStatusSyncBackgroundService : BackgroundService
    {
        private const string Expired = "EXPIRED";
        private const string Active = "ACTIVE";

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MembershipStatusSyncBackgroundService> _logger;
        private readonly MembershipStatusJobSettings _settings;

        public MembershipStatusSyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MembershipStatusSyncBackgroundService> logger,
            IOptions<MembershipStatusJobSettings> settings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("Membership status sync job is disabled.");
                return;
            }

            await RunSyncSafelyAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRunUtc = GetNextRunUtc(DateTimeOffset.UtcNow);
                var delay = nextRunUtc - DateTimeOffset.UtcNow;
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.Zero;
                }

                _logger.LogInformation(
                    "Next membership status sync scheduled at {NextRunUtc:O} UTC.",
                    nextRunUtc);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await RunSyncSafelyAsync(stoppingToken);
            }
        }

        private async Task RunSyncSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();

                var today = GetCurrentLocalDate();
                var nowUtc = DateTime.UtcNow;

                var expiredCount = await context.Members
                    .Where(m => m.PlanEndDate < today && m.Status != Expired)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.Status, Expired)
                            .SetProperty(m => m.UpdatedAt, nowUtc),
                        cancellationToken);

                var reactivatedCount = await context.Members
                    .Where(m => m.PlanEndDate >= today && m.Status == Expired)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.Status, Active)
                            .SetProperty(m => m.UpdatedAt, nowUtc),
                        cancellationToken);

                _logger.LogInformation(
                    "Membership status sync completed. Expired={ExpiredCount}, Reactivated={ReactivatedCount}, LocalDate={LocalDate}",
                    expiredCount,
                    reactivatedCount,
                    today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Membership status sync failed.");
            }
        }

        private DateOnly GetCurrentLocalDate()
        {
            var tz = ResolveTimeZone();
            var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
            return DateOnly.FromDateTime(localNow.DateTime);
        }

        private DateTimeOffset GetNextRunUtc(DateTimeOffset nowUtc)
        {
            var tz = ResolveTimeZone();
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);

            var localRunTime = new DateTime(
                localNow.Year,
                localNow.Month,
                localNow.Day,
                Math.Clamp(_settings.RunAtHour24, 0, 23),
                Math.Clamp(_settings.RunAtMinute, 0, 59),
                0,
                DateTimeKind.Unspecified);

            if (localNow.TimeOfDay >= localRunTime.TimeOfDay)
            {
                localRunTime = localRunTime.AddDays(1);
            }

            var localRunOffset = new DateTimeOffset(localRunTime, tz.GetUtcOffset(localRunTime));
            return localRunOffset.ToUniversalTime();
        }

        private TimeZoneInfo ResolveTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning(
                    "Configured timezone '{TimeZoneId}' not found. Falling back to UTC.",
                    _settings.TimeZoneId);
                return TimeZoneInfo.Utc;
            }
            catch (InvalidTimeZoneException)
            {
                _logger.LogWarning(
                    "Configured timezone '{TimeZoneId}' is invalid. Falling back to UTC.",
                    _settings.TimeZoneId);
                return TimeZoneInfo.Utc;
            }
        }
    }
}
