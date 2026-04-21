using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Persistence;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Persistence.Data;
using AssistantEngineer.Persistence.Data.Repositories;
using AssistantEngineer.Persistence.Services.Benchmarks;
using AssistantEngineer.Persistence.Services.Climate;
using AssistantEngineer.Persistence.Services.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. Use user-secrets or ConnectionStrings__DefaultConnection.");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure();
            });

            if (environmentName == "Development")
                options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IClimateZoneRepository, ClimateZoneRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IEquipmentCatalogRepository, EquipmentCatalogRepository>();
        services.AddScoped<ICalculationPreferencesRepository, CalculationPreferencesRepository>();
        services.AddScoped<IBuildingReportExporter, ExcelReportService>();
        services.AddScoped<IClimateDataRepository, ClimateDataRepository>();
        services.AddScoped<IEnergyPlusResultParser, EnergyPlusResultParser>();
        services.AddScoped<IAnnualClimateDataRepository, AnnualClimateDataRepository>();
        services.AddScoped<IAnnualClimateDataProvider, AnnualClimateDataProvider>();

        var energyPlusSection = configuration.GetSection("EnergyPlus");
        services.Configure<EnergyPlusBenchmarkOptions>(options =>
        {
            energyPlusSection.Bind(options);
        });
        services.AddScoped<IEnergyPlusBenchmarkRunner, EnergyPlusBenchmarkRunner>();
        services.AddScoped<IEnergyPlusModelExporter, EnergyPlusModelExporter>();

        return services;
    }
}
