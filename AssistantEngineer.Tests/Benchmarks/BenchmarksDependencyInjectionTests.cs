using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Facades;
using AssistantEngineer.Modules.Benchmarks.Application.Options;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class BenchmarksDependencyInjectionTests
{
    [Fact]
    public void AddBenchmarksModuleRegistersBenchmarkServicesAsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddBenchmarksModule(configuration);

        AssertServiceLifetime<IVerificationComparator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EnergyPlusModelExportService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<VerificationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso52016ReferenceBenchmarkService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBenchmarksFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IValidateOptions<VerificationTolerance>>(services, ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddBenchmarksModuleRejectsInvalidVerificationTolerance()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Verification:RmseTolerance"] = "0",
                ["Verification:PeakLoadTolerancePercent"] = "101"
            })
            .Build();

        services.AddBenchmarksModule(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<VerificationTolerance>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains("Verification:RmseTolerance", StringComparison.Ordinal));
        Assert.Contains(exception.Failures, failure => failure.Contains("Verification:PeakLoadTolerancePercent", StringComparison.Ordinal));
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(service => service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }
}
