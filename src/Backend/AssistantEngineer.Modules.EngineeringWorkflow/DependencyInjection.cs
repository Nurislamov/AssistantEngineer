using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.EngineeringWorkflow;

public static class DependencyInjection
{
    public static IServiceCollection AddEngineeringWorkflowModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        // Skeleton phase: registrations will be added incrementally during migration slices.
        return services;
    }
}
