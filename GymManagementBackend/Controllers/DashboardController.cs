using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManagementBackend.DTOs;
using GymManagementBackend.Extensions;
using GymManagementBackend.Services;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "OwnerOrAdmin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var stats = await _dashboardService.GetDashboardStatsAsync(effectiveGymId);
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting dashboard stats: {ex.Message}");
                return StatusCode(500, new { message = "Error getting dashboard stats" });
            }
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetDashboardOverview([FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var dashboard = await _dashboardService.GetDashboardDataAsync(effectiveGymId);
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting dashboard overview: {ex.Message}");
                return StatusCode(500, new { message = "Error getting dashboard overview" });
            }
        }

        [HttpGet("trends")]
        public async Task<IActionResult> GetMonthlyTrends([FromQuery] int months = 6, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var trends = await _dashboardService.GetMonthlyJoinTrendAsync(effectiveGymId, months);
                return Ok(trends);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trends: {ex.Message}");
                return StatusCode(500, new { message = "Error getting trends" });
            }
        }

        [HttpGet("recent-members")]
        public async Task<IActionResult> GetRecentMembers([FromQuery] int limit = 5, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var members = await _dashboardService.GetRecentMembersAsync(effectiveGymId, limit);
                return Ok(members);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent members: {ex.Message}");
                return StatusCode(500, new { message = "Error getting recent members" });
            }
        }

        private Guid ResolveGymId(Guid? requestedGymId)
        {
            if (User.IsAdmin())
            {
                if (!requestedGymId.HasValue)
                {
                    throw new UnauthorizedAccessException("gymId is required for admin requests.");
                }
                return requestedGymId.Value;
            }

            var gymId = User.GetGymId();
            if (!gymId.HasValue)
            {
                throw new UnauthorizedAccessException("Invalid gym ID");
            }
            return gymId.Value;
        }
    }
}
