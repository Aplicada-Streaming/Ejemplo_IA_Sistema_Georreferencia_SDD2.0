using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Surveys.Application;

namespace Sgr.Modules.Surveys;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSgrSurveys(this IServiceCollection services)
    {
        services.AddScoped<ICreateSurveyService, CreateSurveyService>();
        services.AddScoped<IListSurveysService, ListSurveysService>();
        services.AddScoped<IListSurveyPointsService, ListSurveyPointsService>();
        services.AddScoped<IGetSurveyService, GetSurveyService>();
        services.AddScoped<ICloseSurveyService, CloseSurveyService>();
        return services;
    }
}
