using GymManagementBackend.DTOs;
using GymManagementBackend.Extensions;
using GymManagementBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "TrainerOrAbove")]
    public class EnquiriesController : ControllerBase
    {
        private readonly IEnquiryService _enquiryService;
        private readonly ILogger<EnquiriesController> _logger;

        public EnquiriesController(IEnquiryService enquiryService, ILogger<EnquiriesController> logger)
        {
            _enquiryService = enquiryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetEnquiries([FromQuery] EnquiryListQueryDto queryDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _enquiryService.GetEnquiriesAsync(effectiveGymId, queryDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return this.ApiError(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enquiries");
                return this.ApiError(StatusCodes.Status500InternalServerError, "ENQUIRY_LIST_ERROR", "Error getting enquiries");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEnquiry(Guid id, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _enquiryService.GetEnquiryByIdAsync(effectiveGymId, id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return this.ApiError(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return this.ApiError(StatusCodes.Status404NotFound, "ENQUIRY_NOT_FOUND", "Enquiry not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enquiry");
                return this.ApiError(StatusCodes.Status500InternalServerError, "ENQUIRY_GET_ERROR", "Error getting enquiry");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateEnquiry([FromBody] CreateEnquiryDto dto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _enquiryService.CreateEnquiryAsync(effectiveGymId, User.GetUserId(), dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return this.ApiError(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating enquiry");
                return this.ApiError(StatusCodes.Status500InternalServerError, "ENQUIRY_CREATE_ERROR", "Error creating enquiry");
            }
        }

        [HttpPost("{id:guid}/followups")]
        public async Task<IActionResult> AddFollowup(Guid id, [FromBody] AddEnquiryFollowupDto dto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _enquiryService.AddFollowupAsync(effectiveGymId, id, User.GetUserId(), dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return this.ApiError(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return this.ApiError(StatusCodes.Status404NotFound, "ENQUIRY_NOT_FOUND", "Enquiry not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding enquiry follow-up");
                return this.ApiError(StatusCodes.Status500InternalServerError, "ENQUIRY_FOLLOWUP_ERROR", "Error adding enquiry follow-up");
            }
        }

        [HttpPatch("{id:guid}/stage")]
        public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpdateEnquiryStageDto dto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _enquiryService.UpdateStageAsync(effectiveGymId, id, User.GetUserId(), dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return this.ApiError(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return this.ApiError(StatusCodes.Status404NotFound, "ENQUIRY_NOT_FOUND", "Enquiry not found");
            }
            catch (InvalidOperationException ex)
            {
                return this.ApiError(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enquiry stage");
                return this.ApiError(StatusCodes.Status500InternalServerError, "ENQUIRY_STAGE_ERROR", "Error updating enquiry stage");
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

