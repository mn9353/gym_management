using GymManagementBackend.DTOs;
using GymManagementBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpPost("mark")]
        [AllowAnonymous] // Members scanning QR code don't need to be logged in to the portal
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _attendanceService.MarkAttendanceAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("today/{gymId}")]
        [Authorize(Policy = "StaffOrAbove")]
        public async Task<IActionResult> GetTodayAttendance(Guid gymId)
        {
            var result = await _attendanceService.GetTodayAttendanceAsync(gymId);
            return Ok(result);
        }
    }
}
