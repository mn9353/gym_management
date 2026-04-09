using System.Security.Claims;
using GymManagementBackend.Constants;

namespace GymManagementBackend.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("Invalid user claim.");
        }

        public static Guid? GetGymId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("gym_id");
            return Guid.TryParse(value, out var gymId) ? gymId : null;
        }

        public static string GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role) ??
                   user.FindFirstValue("role") ??
                   string.Empty;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return string.Equals(user.GetRole(), AppRoles.Admin, StringComparison.OrdinalIgnoreCase);
        }
    }
}
