namespace Sgr.Modules.Surveys.Application;

/// <summary>
/// Authenticated user context as seen by the Surveys module.
/// Populated by the API from the JWT claims.
/// </summary>
public sealed record CurrentUserContext(
    Guid UserId,
    string Role,
    Guid? AreaId);
