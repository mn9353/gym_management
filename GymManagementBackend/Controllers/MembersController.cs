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
