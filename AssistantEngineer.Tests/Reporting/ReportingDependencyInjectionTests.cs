using AssistantEngineer.Modules.Reporting;
using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public class ReportingDependencyInjectionTests
{
    [Fact]
    public void AddReportingModuleRegistersReportingServicesAsScoped()
    {
        var services = new ServiceCollection();

        services.AddReportingModule();

        AssertServiceLifetime<BuildingReportCalculationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingReportGenerator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingReportDataService>(services, ServiceLifetime.Scoped);
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
