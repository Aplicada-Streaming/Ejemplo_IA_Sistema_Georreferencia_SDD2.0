namespace Sgr.Modules.Identity.Application;

public enum LoginErrorCode
{
    InvalidCredentials = 1,
    PendingAcceptance = 2,
    AccountDisabled = 3,
    AccountDropped = 4,
    MobileForbiddenForRole = 5,
}

public sealed class LoginException : Exception
{
    public LoginErrorCode ErrorCode { get; }

    public LoginException(LoginErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
