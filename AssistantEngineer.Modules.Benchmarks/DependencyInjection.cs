using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Facades;
using AssistantEngineer.Modules.Benchmarks.Application.Options;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Benchmarks;

public static class DependencyInjection
{
    public static IServiceCollection AddBenchmarksModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IVerificationComparator, VerificationComparator>();
        services.AddScoped<EnergyPlusModelExportService>();
        services.AddScoped<VerificationService>();
        services.AddScoped<Iso52016ReferenceBenchmarkService>();
        services.AddScoped<IBenchmarksFacade, BenchmarksFacade>();
        services.Configure<VerificationTolerance>(options =>
        {
            configuration.GetSection("Verification").Bind(options);
        });

        return services;
    }
}
