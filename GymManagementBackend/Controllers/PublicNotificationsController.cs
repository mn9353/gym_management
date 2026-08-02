using GymManagementBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/public-notifications")]
    [AllowAnonymous]
    [EnableCors("PublicCors")]
    public class PublicNotificationsController : ControllerBase
    {
        private readonly IEmailNotificationService _emailService;

        public PublicNotificationsController(IEmailNotificationService emailService)
        {
            _emailService = emailService;
        }

        public class SendCustomEmailDto
        {
            public string ToEmail { get; set; } = "mn9353780784@gmail.com";
            public string Subject { get; set; } = string.Empty;
            public string HtmlContent { get; set; } = string.Empty;
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] SendCustomEmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ToEmail) || string.IsNullOrWhiteSpace(dto.HtmlContent))
            {
                return BadRequest(new { success = false, message = "Missing required email fields." });
            }

            var result = await _emailService.SendCustomEmailAsync(
                dto.ToEmail,
                string.IsNullOrWhiteSpace(dto.Subject) ? "Hangout Notification Response" : dto.Subject,
                dto.HtmlContent
            );

            if (result.Success)
            {
                return Ok(new { success = true, message = "Email dispatched successfully." });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = result.Message });
        }
    }
}
