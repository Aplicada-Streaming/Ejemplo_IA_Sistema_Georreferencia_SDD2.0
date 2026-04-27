using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Templates.Application;

namespace Sgr.Modules.Templates;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSgrTemplates(this IServiceCollection services)
    {
        services.AddScoped<ITemplateVersionQuery, TemplateVersionQuery>();
        services.AddScoped<IListTemplatesService, ListTemplatesService>();
        services.AddScoped<IGetTemplateVersionService, GetTemplateVersionService>();
        services.AddScoped<ITemplateEditorService, TemplateEditorService>();
        return services;
    }
}
