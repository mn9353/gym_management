using GymManagementBackend.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementBackend.Extensions
{
    public static class ControllerErrorExtensions
    {
        public static ObjectResult ApiError(
            this ControllerBase controller,
            int statusCode,
            string code,
            string message,
            object? details = null)
        {
            return controller.StatusCode(statusCode, new ApiErrorResponseDto
            {
                Code = code,
                Message = message,
                Details = details,
                TraceId = controller.HttpContext.TraceIdentifier
            });
        }
    }
}

