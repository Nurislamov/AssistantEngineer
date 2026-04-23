using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.Modules.Buildings.Application.Services.Floors;
using AssistantEngineer.Modules.Buildings.Application.Services.Projects;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
using AssistantEngineer.Modules.Buildings.Application.Services.ThermalZones;
using AssistantEngineer.Modules.Buildings.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Buildings;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddSingleton<IValidateOptions<BuildingArchetypeCatalogOptions>, BuildingArchetypeCatalogOptionsValidator>();
        services
            .AddOptions<BuildingArchetypeCatalogOptions>()
            .Bind(configuration.GetSection("Buildings:ArchetypeCatalog"))
            .ValidateOnStart();

        services
            .AddOptions<PvgisApiOptions>()
            .Bind(configuration.GetSection(PvgisApiOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl), "Buildings:Pvgis:BaseUrl is required.")
            .Validate(options => options.TimeoutSeconds > 0 && options.TimeoutSeconds <= 300, "Buildings:Pvgis:TimeoutSeconds must be between 1 and 300.")
            .ValidateOnStart();

        services.AddHttpClient<PvgisAnnualClimateDataImportService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PvgisApiOptions>>().Value;
            client.BaseAddress = new Uri(
                options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
                    ? options.BaseUrl
                    : options.BaseUrl + "/",
                UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScoped<ProjectCommandService>();
        services.AddScoped<ProjectQueryService>();

        services.AddScoped<BuildingCommandService>();
        services.AddScoped<BuildingQueryService>();
        services.AddScoped<BuildingArchetypeService>();
        services.AddScoped<BuildingCalculationReadinessService>();

        services.AddScoped<FloorCommandService>();
        services.AddScoped<FloorQueryService>();

        services.AddScoped<RoomCommandService>();
        services.AddScoped<RoomQueryService>();
        services.AddScoped<RoomVentilationCommandService>();
        services.AddScoped<RoomVentilationQueryService>();

        services.AddScoped<ThermalZoneCommandService>();
        services.AddScoped<ThermalZoneQueryService>();

        services.AddScoped<EpwAnnualClimateDataImportService>();

        return services;
    }
}