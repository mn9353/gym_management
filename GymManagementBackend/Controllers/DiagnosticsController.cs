using GymManagementBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly GymDbContext _context;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(GymDbContext context, ILogger<DiagnosticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("db")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckDatabaseConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        success = false,
                        message = "Database is not reachable",
                        timestamp = DateTime.UtcNow
                    });
                }

                var usersCount = await _context.Users.AsNoTracking().CountAsync();
                var gymsCount = await _context.Gyms.AsNoTracking().CountAsync();
                var membersCount = await _context.Members.AsNoTracking().CountAsync();
                var refreshTokensCount = await _context.RefreshTokens.AsNoTracking().CountAsync();

                return Ok(new
                {
                    success = true,
                    message = "Database connection is healthy",
                    timestamp = DateTime.UtcNow,
                    stats = new
                    {
                        usersCount,
                        gymsCount,
                        membersCount,
                        refreshTokensCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database diagnostics endpoint failed.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Database diagnostics failed",
                    errorType = ex.GetType().Name,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
