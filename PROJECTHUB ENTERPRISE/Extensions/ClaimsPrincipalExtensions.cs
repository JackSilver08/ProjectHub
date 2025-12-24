using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                return null;

            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        public static Guid GetUserIdOrThrow(this ClaimsPrincipal principal)
        {
            var userId = principal.GetUserId();

            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User ID not found in claims");

            return userId.Value;
        }

        public static string GetUserEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.Identity?.Name
                ?? string.Empty;
        }

        public static string GetUserName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.Identity?.Name
                ?? string.Empty;
        }
    }
}