namespace Sgr.Modules.Surveys.Application;

public enum SurveyErrorCode
{
    NoPublishedTemplateAvailable = 1,
    TemplateVersionNotPublished = 2,
    DuplicateGuid = 3,
    InvalidPayload = 4,
    Forbidden = 5,
    AreaUnknown = 6,
}

public sealed class SurveyException : Exception
{
    public SurveyErrorCode Code { get; }

    public SurveyException(SurveyErrorCode code, string message) : base(message)
    {
        Code = code;
    }
}
