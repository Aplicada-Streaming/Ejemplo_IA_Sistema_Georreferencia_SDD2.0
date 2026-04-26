namespace Sgr.Modules.Identity.Application;

public sealed record LoginRequest(
    string Email,
    string Password,
    ClientFront Client,
    string? DeviceId);
