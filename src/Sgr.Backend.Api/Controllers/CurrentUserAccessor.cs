using System.Security.Claims;
using Sgr.Modules.Surveys.Application;

namespace Sgr.Backend.Api.Controllers;

internal static class CurrentUserAccessor
{
    public static CurrentUserContext FromPrincipal(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("JWT does not contain a user id claim.");
        var role = principal.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("JWT does not contain a role claim.");
        var areaIdClaim = principal.FindFirstValue("area_id");

        return new CurrentUserContext(
            UserId: Guid.Parse(userId),
            Role: role,
            AreaId: Guid.TryParse(areaIdClaim, out var areaGuid) ? areaGuid : null);
    }
}
