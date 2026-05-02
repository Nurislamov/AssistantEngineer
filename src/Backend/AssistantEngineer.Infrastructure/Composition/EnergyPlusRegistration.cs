using AssistantEngineer.Infrastructure.Integrations.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.SharedKernel.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Infrastructure.Composition;

internal static class EnergyPlusRegistration
{
    public static IServiceCollection AddEnergyPlusIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<ResilientOperationExecutor>();

        services.AddSingleton<IValidateOptions<EnergyPlusBenchmarkOptions>, EnergyPlusBenchmarkOptionsValidator>();

        services
            .AddOptions<EnergyPlusBenchmarkOptions>()
            .Bind(configuration.GetSection("EnergyPlus"))
            .ValidateOnStart();

        services.AddScoped<IEnergyPlusArtifactStore, LocalEnergyPlusArtifactStore>();
        services.AddScoped<IEnergyPlusModelExporter, EnergyPlusModelExporter>();
        services.AddScoped<IEnergyPlusResultParser, EnergyPlusResultParser>();
        services.AddScoped<IEnergyPlusBenchmarkRunner, EnergyPlusBenchmarkRunner>();

        return services;
    }
}