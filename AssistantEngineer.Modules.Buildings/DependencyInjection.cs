using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.Modules.Buildings.Application.Services.Floors;
using AssistantEngineer.Modules.Buildings.Application.Services.Projects;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
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
        services.AddScoped<EpwAnnualClimateDataImportService>();

        return services;
    }
}
