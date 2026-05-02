using AssistantEngineer.Infrastructure.Providers.Climate;
using AssistantEngineer.Infrastructure.Providers.Equipment;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Composition;

internal static class ProviderRegistration
{
    public static IServiceCollection AddApplicationProviders(
        this IServiceCollection services)
    {
        services.AddScoped<IAnnualClimateDataProvider, AnnualClimateDataProvider>();
        services.AddScoped<ICoolingEquipmentCatalogSizingProvider, CoolingEquipmentCatalogSizingProvider>();

        return services;
    }
}