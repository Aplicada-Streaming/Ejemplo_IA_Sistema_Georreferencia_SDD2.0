using Microsoft.Extensions.DependencyInjection;
using Sgr.Modules.Sync.Application;

namespace Sgr.Modules.Sync;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSgrSync(this IServiceCollection services)
    {
        services.AddScoped<IEventApplier, EventApplier>();
        services.AddScoped<IPullDiffService, PullDiffService>();
        services.AddScoped<IPointAccessChecker, PointAccessChecker>();
        services.AddScoped<IConflictsService, ConflictsService>();
        services.AddScoped<IMergeCandidateDetector, MergeCandidateDetector>();
        services.AddScoped<IMergeCandidatesService, MergeCandidatesService>();
        return services;
    }
}
