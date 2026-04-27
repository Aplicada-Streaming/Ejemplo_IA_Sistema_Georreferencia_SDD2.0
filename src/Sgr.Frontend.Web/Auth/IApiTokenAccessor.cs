using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Sgr.Frontend.Web.Auth;

public interface IApiTokenAccessor
{
    string? GetAccessToken();
    Guid? GetUserId();
    string? GetRole();
    Guid? GetAreaId();
    string? GetEmail();
    string? GetFullName();
}

public sealed class HttpContextApiTokenAccessor : IApiTokenAccessor
{
    private readonly IHttpContextAccessor _http;

    public HttpContextApiTokenAccessor(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public string? GetAccessToken() =>
        Principal?.FindFirst(SgrClaims.AccessToken)?.Value;

    public Guid? GetUserId()
    {
        var raw = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var g) ? g : null;
    }

    public string? GetRole() => Principal?.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? GetAreaId()
    {
        var raw = Principal?.FindFirst(SgrClaims.AreaId)?.Value;
        return Guid.TryParse(raw, out var g) ? g : null;
    }

    public string? GetEmail() => Principal?.FindFirst(ClaimTypes.Email)?.Value;
    public string? GetFullName() => Principal?.FindFirst(ClaimTypes.Name)?.Value;
}

public static class SgrClaims
{
    public const string AccessToken = "sgr.access_token";
    public const string AreaId = "sgr.area_id";
}
