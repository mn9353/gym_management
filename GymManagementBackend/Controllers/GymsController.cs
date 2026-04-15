using GymManagementBackend.DTOs;
using GymManagementBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class GymsController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<GymsController> _logger;

        public GymsController(IAdminService adminService, ILogger<GymsController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetGyms()
        {
            var gyms = await _adminService.GetGymsAsync();
            return Ok(gyms);
        }

        [HttpGet("{gymId:guid}/revenue-trend")]
        public async Task<IActionResult> GetGymRevenueTrend(Guid gymId, [FromQuery] int months = 12)
        {
            var trend = await _adminService.GetGymMonthlyRevenueAsync(gymId, months);
            return Ok(trend);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGym([FromBody] CreateGymDto request)
        {
            try
            {
                var gym = await _adminService.CreateGymAsync(request);
                return Created($"/api/gyms/{gym.Id}", gym);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Create gym validation failed.");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("with-owners")]
        public async Task<IActionResult> CreateGymWithOwners([FromBody] CreateGymWithOwnersDto request)
        {
            try
            {
                var response = await _adminService.CreateGymWithOwnersAsync(request);
                return Created($"/api/gyms/{response.Gym.Id}", response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Create gym with owners validation failed.");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{gymId:guid}")]
        public async Task<IActionResult> UpdateGym(Guid gymId, [FromBody] UpdateGymDto request)
        {
            try
            {
                var gym = await _adminService.UpdateGymAsync(gymId, request);
                return Ok(gym);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{gymId:guid}")]
        public async Task<IActionResult> DeleteGym(Guid gymId)
        {
            try
            {
                await _adminService.DeleteGymAsync(gymId);
                return Ok(new { message = "Gym deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gym.");
                return StatusCode(500, new { message = "Error deleting gym." });
            }
        }
    }
}
