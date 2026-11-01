using System.Security.Claims;

namespace MediCore.Services
{
    public static class ClaimsExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
