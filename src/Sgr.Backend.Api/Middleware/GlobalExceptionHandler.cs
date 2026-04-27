using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sgr.Modules.Identity.Application;
using Sgr.Modules.Surveys.Application;
using Sgr.Modules.Templates.Application;

namespace Sgr.Backend.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            LoginException ex => MapLoginError(ex),
            SurveyException ex => MapSurveyError(ex),
            TemplateNotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            InvalidOperationException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An unexpected error occurred."),
        };

        if (status >= 500)
            _logger.LogError(exception, "Unhandled exception (status {Status}).", status);
        else
            _logger.LogInformation("Domain error {ErrorType} → status {Status}: {Message}",
                exception.GetType().Name, status, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };

        if (exception is LoginException le)
            problem.Extensions["errorCode"] = le.ErrorCode.ToString();
        else if (exception is SurveyException se)
            problem.Extensions["errorCode"] = se.Code.ToString();

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int status, string title, string detail) MapLoginError(LoginException ex) =>
        ex.ErrorCode switch
        {
            LoginErrorCode.InvalidCredentials =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
            LoginErrorCode.PendingAcceptance =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            LoginErrorCode.AccountDisabled =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            LoginErrorCode.AccountDropped =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            LoginErrorCode.MobileForbiddenForRole =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            _ => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
        };

    private static (int status, string title, string detail) MapSurveyError(SurveyException ex) =>
        ex.Code switch
        {
            SurveyErrorCode.NoPublishedTemplateAvailable =>
                (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            SurveyErrorCode.TemplateVersionNotPublished =>
                (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity", ex.Message),
            SurveyErrorCode.DuplicateGuid =>
                (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            SurveyErrorCode.Forbidden =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            SurveyErrorCode.AreaUnknown =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            SurveyErrorCode.InvalidPayload =>
                (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
            SurveyErrorCode.NotFound =>
                (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            _ => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
        };
}
