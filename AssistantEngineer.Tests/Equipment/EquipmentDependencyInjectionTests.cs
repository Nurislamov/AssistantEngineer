using AssistantEngineer.Modules.Equipment;
using AssistantEngineer.Modules.Equipment.Application.Abstractions;
using AssistantEngineer.Modules.Equipment.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public class EquipmentDependencyInjectionTests
{
    [Fact]
    public void AddEquipmentModuleRegistersEquipmentServicesAsScoped()
    {
        var services = new ServiceCollection();

        services.AddEquipmentModule();

        AssertServiceLifetime<ICoolingEquipmentSelector>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<CoolingEquipmentCatalogCommandService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<CoolingEquipmentCatalogQueryService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EquipmentSelectionService>(services, ServiceLifetime.Scoped);
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
