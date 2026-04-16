using AssistantEngineer.Application.Services.Buildings;
using AssistantEngineer.Application.Services.Equipment;
using AssistantEngineer.Application.Services.Floors;
using AssistantEngineer.Application.Services.Projects;
using AssistantEngineer.Application.Services.Rooms;
using AssistantEngineer.Domain.Services.Calculations;
using AssistantEngineer.Domain.Services.Equipment;
using AssistantEngineer.Services.Calculations;
using AssistantEngineer.Services.Reports;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RoomCalculationService>();
        services.AddScoped<CoolingEquipmentSelector>();
        services.AddScoped<AggregateCalculationService>();
        services.AddScoped<EquipmentSelectionService>();
        services.AddScoped<BuildingReportDataService>();
        services.AddScoped<ProjectApplicationService>();
        services.AddScoped<BuildingApplicationService>();
        services.AddScoped<FloorApplicationService>();
        services.AddScoped<RoomApplicationService>();
        services.AddScoped<CoolingEquipmentCatalogService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
