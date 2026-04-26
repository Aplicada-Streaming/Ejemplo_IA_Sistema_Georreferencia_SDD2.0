using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sgr.Modules.Identity.Application;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginService _login;

    public AuthController(ILoginService login) => _login = login;

    /// <summary>Authenticate with email and password (ROPC).</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequestDto body,
        CancellationToken ct)
    {
        var request = new LoginRequest(
            Email: body.Email,
            Password: body.Password,
            Client: body.Client,
            DeviceId: body.DeviceId);

        var response = await _login.LoginAsync(request, ct);
        return Ok(response);
    }
}

public sealed class LoginRequestDto
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = default!;

    [Required, MinLength(1), MaxLength(256)]
    public string Password { get; set; } = default!;

    [Required]
    public ClientFront Client { get; set; }

    [MaxLength(64)]
    public string? DeviceId { get; set; }
}
