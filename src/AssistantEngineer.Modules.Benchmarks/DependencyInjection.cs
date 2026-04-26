using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Facades;
using AssistantEngineer.Modules.Benchmarks.Application.Options;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Benchmarks;

public static class DependencyInjection
{
    public static IServiceCollection AddBenchmarksModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<VerificationTolerance>, VerificationToleranceValidator>();
        services.AddScoped<IVerificationComparator, VerificationComparator>();
        services.AddScoped<EnergyPlusModelExportService>();
        services.AddScoped<VerificationService>();
        services.AddScoped<Iso52016ReferenceBenchmarkService>();
        services.AddScoped<IBenchmarksFacade>(sp => new BenchmarksFacade(
            sp.GetRequiredService<IEnergyPlusBenchmarkRunner>(),
            sp.GetRequiredService<EnergyPlusModelExportService>(),
            sp.GetRequiredService<VerificationService>(),
            sp.GetRequiredService<Iso52016ReferenceBenchmarkService>()));
        services
            .AddOptions<VerificationTolerance>()
            .Bind(configuration.GetSection("Verification"))
            .ValidateOnStart();

        return services;
    }
}
