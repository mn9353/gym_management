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
    [Authorize(Policy = "OwnerOrAdmin")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetPaymentsList([FromQuery] PaymentListQueryDto queryDto, [FromQuery] Guid? gymId = null)
        {
            try
            {
                var effectiveGymId = ResolveGymId(gymId);
                var result = await _paymentService.GetPaymentsAsync(effectiveGymId, queryDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return this.ApiError(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting payments list: {Message}", ex.Message);
                return this.ApiError(StatusCodes.Status500InternalServerError, "PAYMENTS_LIST_ERROR", "Error getting payments list");
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
