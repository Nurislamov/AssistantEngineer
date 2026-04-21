using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
