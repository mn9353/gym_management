using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManagementBackend.DTOs;
using GymManagementBackend.Services;
using GymManagementBackend.Extensions;
using System.Security.Claims;

namespace GymManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request, GetIpAddress());
            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var (success, message) = await _authService.SendPasswordResetCodeAsync(request);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }

        [HttpPost("verify-reset-code")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeRequest request)
        {
            var (success, message) = await _authService.VerifyPasswordResetCodeAsync(request);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithCodeRequest request)
        {
            var (success, message) = await _authService.ResetPasswordWithCodeAsync(request);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, GetIpAddress());
            return response.Success ? Ok(response) : Unauthorized(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var userId = User.GetUserId();
            var revoked = await _authService.RevokeRefreshTokenAsync(userId, request.RefreshToken, GetIpAddress());
            if (!revoked)
            {
                return BadRequest(new { message = "Refresh token not found or already revoked" });
            }

            _logger.LogInformation("User logged out: {UserId}", userId);
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.GetUserId();
            var user = await _authService.GetByIdAsync(userId);
            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [HttpGet("verify")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            return Ok(new { message = "Token is valid" });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.GetUserId();
            var (success, message) = await _authService.ChangePasswordAsync(userId, request);
            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }

        [HttpGet("debug-auth")]
        [Authorize]
        public IActionResult DebugAuthContext()
        {
            var claims = User.Claims
                .Select(c => new { type = c.Type, value = c.Value })
                .ToList();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
            var gymIdClaim = User.FindFirstValue("gym_id");

            return Ok(new
            {
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                resolved = new
                {
                    userId = userIdClaim,
                    role = roleClaim,
                    gymId = gymIdClaim
                },
                claims
            });
        }

        private string? GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }
    }
}
