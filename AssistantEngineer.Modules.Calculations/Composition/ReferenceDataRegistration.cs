using AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Performance;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class ReferenceDataRegistration
{
    public static IServiceCollection AddCalculationReferenceData(
        this IServiceCollection services)
    {
        services.AddSingleton<IInternalLoadStandardProvider, InternalLoadStandardProvider>();
        services.AddSingleton<IDomesticHotWaterStandardProvider, DomesticHotWaterStandardProvider>();
        services.AddSingleton<ITb14ReferenceDataProvider, Tb14ReferenceDataProvider>();

        services.AddSingleton<IRoomStandardDefaultsProvider, RoomStandardDefaultsProvider>();
        services.AddSingleton<IRoomVentilationDefaultsProvider, RoomVentilationDefaultsProvider>();

        services.AddSingleton<IIso16798ReferenceData, Iso16798ReferenceData>();
        services.AddSingleton<IEn16798ProfileCatalog, En16798ProfileCatalog>();

        services.AddSingleton<IBuildingEnvelopeReferenceData, BuildingEnvelopeReferenceData>();
        services.AddSingleton<ICoolingLoadReferenceData, CoolingLoadReferenceData>();

        services.AddSingleton<IHolidayCalendarProvider, UzbekistanHolidayCalendarProvider>();
        services.AddSingleton<IAnnualProfileTemplateProvider, AnnualProfileTemplateProvider>();

        services.AddSingleton<IEnergyCarrierFactorProvider, EnergyCarrierFactorProvider>();

        services.AddSingleton<StandardTableCatalogService>();

        return services;
    }
}