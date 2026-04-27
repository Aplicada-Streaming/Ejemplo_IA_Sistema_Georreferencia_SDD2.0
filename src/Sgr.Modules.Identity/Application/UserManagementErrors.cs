namespace Sgr.Modules.Identity.Application;

public enum UserManagementErrorCode
{
    InvalidInput,
    EmailTaken,
    AreaNotFound,
    UserNotFound,
    Forbidden,
    InvalidStateTransition,
}

public sealed class UserManagementException : Exception
{
    public UserManagementErrorCode Code { get; }

    public UserManagementException(UserManagementErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }
}
