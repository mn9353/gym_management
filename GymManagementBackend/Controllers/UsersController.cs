using GymManagementBackend.DTOs;
using GymManagementBackend.Extensions;
using GymManagementBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "OwnerOrAdmin")]
    public class UsersController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IAdminService adminService, ILogger<UsersController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var users = await _adminService.GetUsersAsync(effectiveGymId);
                return Ok(users);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
        {
            try
            {
                var user = await _adminService.CreateUserAsync(request);
                return Created($"/api/users/{user.Id}", user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Create user validation failed.");
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{userId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserDto request)
        {
            try
            {
                var user = await _adminService.UpdateUserAsync(userId, request);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private Guid? ResolveGymId(Guid? requestedGymId)
        {
            if (User.IsAdmin())
            {
                return requestedGymId;
            }

            var gymId = User.GetGymId();
            if (!gymId.HasValue)
            {
                throw new UnauthorizedAccessException("Gym context is missing for owner account.");
            }

            return gymId.Value;
        }
    }
}
