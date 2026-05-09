using AssistantEngineer.Modules.Reporting;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public class ReportingDependencyInjectionTests
{
    [Fact]
    public void AddReportingModuleRegistersReportCalculationServices()
    {
        var services = new ServiceCollection();

        services.AddReportingModule();

        AssertServiceLifetime<BuildingCoolingReportCalculationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingHeatingReportCalculationService>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddReportingModuleRegistersReportGenerators()
    {
        var services = new ServiceCollection();

        services.AddReportingModule();

        AssertServiceLifetime<BuildingCoolingReportGenerator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingHeatingReportGenerator>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddReportingModuleRegistersReportDataServices()
    {
        var services = new ServiceCollection();

        services.AddReportingModule();

        AssertServiceLifetime<BuildingCoolingReportDataService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingHeatingReportDataService>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddReportingModuleRegistersReportFacades()
    {
        var services = new ServiceCollection();

        services.AddReportingModule();

        AssertServiceLifetime<IBuildingCoolingReportsFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingHeatingReportsFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingEnergyBalanceReportsFacade>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddReportingModuleRegistersEngineeringReportFoundationServices()
    {
        var services = new ServiceCollection();

        services.AddReportingModule();

        AssertServiceLifetime<IEngineeringReportBuilder>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEngineeringReportJsonExporter>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEngineeringReportMarkdownExporter>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEngineeringReportDiagnosticAggregator>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddReportingModuleDoesNotRegisterGenericReportServices()
    {
        var services = new ServiceCollection();

        services.AddReportingModule();

        AssertNoRegistrationNamed(services, "BuildingReportCalculationService");
        AssertNoRegistrationNamed(services, "BuildingReportGenerator");
        AssertNoRegistrationNamed(services, "BuildingReportDataService");
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(service => service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    private static void AssertNoRegistrationNamed(
        IServiceCollection services,
        string typeName)
    {
        var matches = services
            .Where(service =>
                string.Equals(service.ServiceType.Name, typeName, StringComparison.Ordinal) ||
                string.Equals(service.ImplementationType?.Name, typeName, StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(matches);
    }
}
