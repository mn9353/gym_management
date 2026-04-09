using GymManagementBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        [HttpGet("db-debug")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckDatabaseConnectionDetailed()
        {
            try
            {
                await using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                var scalar = await command.ExecuteScalarAsync();

                return Ok(new
                {
                    success = true,
                    message = "Database connection and query succeeded",
                    timestamp = DateTime.UtcNow,
                    result = scalar?.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed database diagnostics endpoint failed.");

                string? sqlState = null;
                string? severity = null;
                if (ex is PostgresException pg)
                {
                    sqlState = pg.SqlState;
                    severity = pg.Severity;
                }
                else if (ex.InnerException is PostgresException innerPg)
                {
                    sqlState = innerPg.SqlState;
                    severity = innerPg.Severity;
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Detailed database diagnostics failed",
                    timestamp = DateTime.UtcNow,
                    error = new
                    {
                        type = ex.GetType().FullName,
                        message = ex.Message,
                        innerType = ex.InnerException?.GetType().FullName,
                        innerMessage = ex.InnerException?.Message,
                        sqlState,
                        severity
                    }
                });
            }
        }
    }
}
