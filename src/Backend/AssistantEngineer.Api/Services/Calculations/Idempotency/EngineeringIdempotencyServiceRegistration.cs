using Microsoft.Extensions.Options;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Idempotency;

public static class EngineeringIdempotencyServiceRegistration
{
    public static IServiceCollection AddEngineeringIdempotency(this IServiceCollection services)
    {
        services.AddOptions<EngineeringIdempotencyOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection(EngineeringIdempotencyOptions.SectionName).Bind(options);
            })
            .Validate(options => options.TtlMinutes > 0, "Engineering idempotency TTL must be positive.")
            .Validate(options => options.MaxEntries > 0, "Engineering idempotency max entries must be positive.")
            .Validate(options => options.MaxCachedResponseBytes > 0, "Engineering idempotency max cached response bytes must be positive.");

        services.AddSingleton<IEngineeringIdempotencyService>(serviceProvider =>
        {
            var persistenceOptions = serviceProvider.GetRequiredService<IOptions<EngineeringWorkflowPersistenceOptions>>().Value;
            return persistenceOptions.Provider == EngineeringWorkflowPersistenceProvider.SQLite
                ? ActivatorUtilities.CreateInstance<EfEngineeringIdempotencyService>(serviceProvider)
                : ActivatorUtilities.CreateInstance<InMemoryEngineeringIdempotencyService>(serviceProvider);
        });

        return services;
    }
}
