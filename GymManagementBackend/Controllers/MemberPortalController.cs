using GymManagementBackend.DTOs;
using GymManagementBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/member-portal")]
    [Authorize(Policy = "MemberOnly")]
    public class MemberPortalController : ControllerBase
    {
        private readonly IMemberPortalService _memberPortalService;
        private readonly ILogger<MemberPortalController> _logger;

        public MemberPortalController(IMemberPortalService memberPortalService, ILogger<MemberPortalController> logger)
        {
            _memberPortalService = memberPortalService;
            _logger = logger;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var result = await _memberPortalService.GetSummaryAsync(User);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("weight-history")]
        public async Task<IActionResult> GetWeightHistory([FromQuery] int months = 6)
        {
            try
            {
                var result = await _memberPortalService.GetWeightHistoryAsync(User, months);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("metrics")]
        public async Task<IActionResult> UpsertMetric([FromBody] MemberMetricUpdateRequestDto request)
        {
            try
            {
                var result = await _memberPortalService.UpsertMetricAsync(User, request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendance([FromQuery] int months = 3, [FromQuery] int limit = 45)
        {
            try
            {
                var result = await _memberPortalService.GetAttendanceAsync(User, months, limit);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("missed-trend")]
        public async Task<IActionResult> GetMissedTrend([FromQuery] int weeks = 6)
        {
            try
            {
                var result = await _memberPortalService.GetMissedDaysTrendAsync(User, weeks);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("muscle-distribution")]
        public async Task<IActionResult> GetMuscleDistribution([FromQuery] int months = 1)
        {
            try
            {
                var result = await _memberPortalService.GetMonthlyMuscleDistributionAsync(User, months);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("checkin/scan")]
        public async Task<IActionResult> CheckinByQr([FromBody] MemberCheckinScanRequestDto request)
        {
            try
            {
                var result = await _memberPortalService.CheckinByQrAsync(User, request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during member QR check-in");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unable to check in right now." });
            }
        }

        [HttpPost("checkin/{checkinId:guid}/workout")]
        public async Task<IActionResult> AddWorkout(Guid checkinId, [FromBody] MemberWorkoutLogRequestDto request)
        {
            try
            {
                var result = await _memberPortalService.AddWorkoutForCheckinAsync(User, checkinId, request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
