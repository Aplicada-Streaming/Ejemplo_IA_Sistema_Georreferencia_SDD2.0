using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sgr.Domain.Identity;
using Sgr.Modules.Identity.Application;

namespace Sgr.Backend.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginService _login;
    private readonly IUserRegistrationService _register;

    public AuthController(ILoginService login, IUserRegistrationService register)
    {
        _login = login;
        _register = register;
    }

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

    /// <summary>
    /// Self-registration de jefes y relevadores (US-13). Queda en estado
    /// pendiente_aceptacion hasta que su superior jerárquico lo acepte.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserRegistrationResponse>> Register(
        [FromBody] RegisterRequestDto body,
        CancellationToken ct)
    {
        var request = new UserRegistrationRequest(
            Email: body.Email,
            Password: body.Password,
            FullName: body.FullName,
            Role: body.Role,
            AreaId: body.AreaId);

        var response = await _register.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
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

public sealed class RegisterRequestDto
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = default!;

    [Required, MinLength(8), MaxLength(256)]
    public string Password { get; set; } = default!;

    [Required, MinLength(2), MaxLength(200)]
    public string FullName { get; set; } = default!;

    /// <summary>jefe_area | relevador (admin_raiz se inicializa en el seed).</summary>
    [Required]
    public string Role { get; set; } = default!;

    [Required]
    public Guid? AreaId { get; set; }
}
