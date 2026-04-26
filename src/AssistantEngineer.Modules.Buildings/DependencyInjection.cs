using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.Modules.Buildings.Application.Services.Floors;
using AssistantEngineer.Modules.Buildings.Application.Services.Projects;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
using AssistantEngineer.Modules.Buildings.Application.Services.ThermalZones;
using AssistantEngineer.SharedKernel.Resilience;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Buildings;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.TryAddSingleton<ResilientOperationExecutor>();

        services.AddSingleton<IValidateOptions<BuildingArchetypeCatalogOptions>, BuildingArchetypeCatalogOptionsValidator>();
        services.AddSingleton<IValidateOptions<PvgisApiOptions>, PvgisApiOptionsValidator>();

        services
            .AddOptions<BuildingArchetypeCatalogOptions>()
            .Bind(configuration.GetSection("Buildings:ArchetypeCatalog"))
            .ValidateOnStart();

        services
            .AddOptions<PvgisApiOptions>()
            .Bind(configuration.GetSection(PvgisApiOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient<PvgisAnnualClimateDataImportService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PvgisApiOptions>>().Value;
            client.BaseAddress = new Uri(
                options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                    ? options.BaseUrl
                    : options.BaseUrl + "/",
                UriKind.Absolute);
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AssistantEngineer/1.0");
        });

        services.AddScoped<ProjectCommandService>();
        services.AddScoped<ProjectQueryService>();

        services.AddScoped<BuildingCommandService>();
        services.AddScoped<BuildingQueryService>();
        services.AddScoped<BuildingArchetypeService>();
        services.AddScoped<BuildingCalculationReadinessService>();
        services.AddScoped<BuildingAutocorrectionPlanner>();
        services.AddScoped<BuildingModelValidationService>();
        services.AddScoped<BuildingModelAutocorrectionService>();

        services.AddScoped<FloorCommandService>();
        services.AddScoped<FloorQueryService>();

        services.AddScoped<RoomCommandService>();
        services.AddScoped<RoomQueryService>();
        services.AddScoped<RoomVentilationCommandService>();
        services.AddScoped<RoomVentilationQueryService>();
        services.AddScoped<RoomVentilationDefaultsService>();
        services.AddScoped<RoomGroundContactService>();

        services.AddScoped<ThermalZoneCommandService>();
        services.AddScoped<ThermalZoneQueryService>();

        services.AddScoped<EpwAnnualClimateDataImportService>();
        services.AddScoped<IBuildingsFacade, BuildingsFacade>();

        return services;
    }
}