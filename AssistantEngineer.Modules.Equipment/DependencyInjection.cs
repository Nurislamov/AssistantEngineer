using System.Reflection;
using AssistantEngineer.Modules.Equipment.Application.Abstractions;
using AssistantEngineer.Modules.Equipment.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Equipment;

public static class DependencyInjection
{
    public static IServiceCollection AddEquipmentModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<ICoolingEquipmentSelector, CoolingEquipmentSelector>();
        services.AddScoped<CoolingEquipmentCatalogCommandService>();
        services.AddScoped<CoolingEquipmentCatalogQueryService>();
        services.AddScoped<EquipmentSelectionService>();

        return services;
    }
}
