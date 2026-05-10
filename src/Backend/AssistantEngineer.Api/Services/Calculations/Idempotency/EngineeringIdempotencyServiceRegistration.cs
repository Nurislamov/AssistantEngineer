using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations.Idempotency;

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

        services.AddSingleton<IEngineeringIdempotencyService, InMemoryEngineeringIdempotencyService>();

        return services;
    }
}
