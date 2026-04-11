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
    public class MembersController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly ILogger<MembersController> _logger;

        public MembersController(IMemberService memberService, ILogger<MembersController> logger)
        {
            _memberService = memberService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var members = await _memberService.GetMembersAsync(effectiveGymId, pageNumber, pageSize);
                return Ok(members);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting members: {ex.Message}");
                return StatusCode(500, new { message = "Error getting members" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMember(Guid id, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var member = await _memberService.GetMemberAsync(effectiveGymId, id);
                return Ok(member);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Member not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting member: {ex.Message}");
                return StatusCode(500, new { message = "Error getting member" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMember([FromBody] CreateMemberDto createMemberDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var member = await _memberService.CreateMemberAsync(effectiveGymId, createMemberDto);
                return Created($"/api/members/{member.Id}", member);
            }
            catch (DuplicateMemberException ex)
            {
                return Conflict(new
                {
                    message = ex.Message,
                    existingMember = ex.ExistingMember
                });
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
                _logger.LogError($"Error creating member: {ex.Message}");
                return StatusCode(500, new { message = "Error creating member" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMember(Guid id, [FromBody] UpdateMemberDto updateMemberDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var member = await _memberService.UpdateMemberAsync(effectiveGymId, id, updateMemberDto);
                return Ok(member);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Member not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating member: {ex.Message}");
                return StatusCode(500, new { message = "Error updating member" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMember(Guid id, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.DeleteMemberAsync(effectiveGymId, id);
                
                if (!result)
                {
                    return NotFound(new { message = "Member not found" });
                }

                return Ok(new { message = "Member deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting member: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting member" });
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchMembers([FromBody] MemberSearchDto searchDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var members = await _memberService.SearchMembersAsync(effectiveGymId, searchDto);
                return Ok(members);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching members: {ex.Message}");
                return StatusCode(500, new { message = "Error searching members" });
            }
        }

        [HttpPost("{id}/renew")]
        public async Task<IActionResult> RenewMember(Guid id, [FromBody] RenewMemberDto renewMemberDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var member = await _memberService.RenewMemberAsync(effectiveGymId, id, renewMemberDto);
                return Ok(member);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Member not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error renewing member: {ex.Message}");
                return StatusCode(500, new { message = "Error renewing member" });
            }
        }

        [HttpPost("{id}/payments")]
        public async Task<IActionResult> AddMemberPayment(Guid id, [FromBody] AddMemberPaymentDto addPaymentDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.AddMemberPaymentAsync(effectiveGymId, id, addPaymentDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Member not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding member payment: {ex.Message}");
                return StatusCode(500, new { message = "Error adding member payment" });
            }
        }

        [HttpGet("upcoming-renewals")]
        public async Task<IActionResult> GetUpcomingRenewals(
            [FromQuery] int days = 7,
            [FromQuery] int limit = 100,
            [FromQuery] int skip = 0,
            [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var members = await _memberService.GetUpcomingRenewalsAsync(effectiveGymId, days, limit, skip);
                return Ok(members);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting upcoming renewals: {ex.Message}");
                return StatusCode(500, new { message = "Error getting upcoming renewals" });
            }
        }

        [HttpGet("segment-counts")]
        public async Task<IActionResult> GetSegmentCounts(
            [FromQuery] int upcomingDays = 7,
            [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var counts = await _memberService.GetSegmentCountsAsync(effectiveGymId, upcomingDays);
                return Ok(counts);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting segment counts: {ex.Message}");
                return StatusCode(500, new { message = "Error getting segment counts" });
            }
        }

        [HttpGet("active/list")]
        public async Task<IActionResult> GetActiveMembersList([FromQuery] MemberListQueryDto queryDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.GetMembersListAsync(effectiveGymId, queryDto, "active");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active members list: {ex.Message}");
                return StatusCode(500, new { message = "Error getting active members list" });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetMembersList(
            [FromQuery] MemberListQueryDto queryDto,
            [FromQuery] string segment = "all",
            [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var normalized = (segment ?? "all").Trim().ToLowerInvariant();
                if (normalized is not ("all" or "active" or "expiring" or "inactive"))
                {
                    normalized = "all";
                }

                var result = await _memberService.GetMembersListAsync(effectiveGymId, queryDto, normalized);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting members list: {ex.Message}");
                return StatusCode(500, new { message = "Error getting members list" });
            }
        }

        [HttpGet("inactive/list")]
        public async Task<IActionResult> GetInactiveMembersList([FromQuery] MemberListQueryDto queryDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.GetMembersListAsync(effectiveGymId, queryDto, "inactive");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting inactive members list: {ex.Message}");
                return StatusCode(500, new { message = "Error getting inactive members list" });
            }
        }

        [HttpGet("upcoming-renewals/list")]
        public async Task<IActionResult> GetUpcomingRenewalsList([FromQuery] MemberListQueryDto queryDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.GetMembersListAsync(effectiveGymId, queryDto, "upcoming");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting upcoming renewals list: {ex.Message}");
                return StatusCode(500, new { message = "Error getting upcoming renewals list" });
            }
        }

        [HttpPost("active/grid")]
        public async Task<IActionResult> GetActiveMembersGrid([FromBody] MemberGridRequestDto request, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.GetMembersGridAsync(effectiveGymId, request, "active");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active members grid: {ex.Message}");
                return StatusCode(500, new { message = "Error getting active members grid" });
            }
        }

        [HttpPost("inactive/grid")]
        public async Task<IActionResult> GetInactiveMembersGrid([FromBody] MemberGridRequestDto request, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.GetMembersGridAsync(effectiveGymId, request, "inactive");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting inactive members grid: {ex.Message}");
                return StatusCode(500, new { message = "Error getting inactive members grid" });
            }
        }

        [HttpPost("upcoming-renewals/grid")]
        public async Task<IActionResult> GetUpcomingRenewalsGrid([FromBody] MemberGridRequestDto request, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _memberService.GetMembersGridAsync(effectiveGymId, request, "upcoming");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting upcoming renewals grid: {ex.Message}");
                return StatusCode(500, new { message = "Error getting upcoming renewals grid" });
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
