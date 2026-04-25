using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Infrastructure.Persistence.Repositories;
using AssistantEngineer.Infrastructure.Providers.Climate;
using AssistantEngineer.Infrastructure.Integrations.Benchmarks;
using AssistantEngineer.Infrastructure.Integrations.Reports;
using AssistantEngineer.Infrastructure.Configuration;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        ConfigurationSecurityValidator.Validate(configuration, environmentName);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });

            if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IClimateZoneRepository, ClimateZoneRepository>();
        services.AddScoped<IClimateDataRepository, ClimateDataRepository>();
        services.AddScoped<IBuildingHeatingReadModelRepository, BuildingHeatingReadModelRepository>();
        services.AddScoped<IAnnualClimateDataRepository, AnnualClimateDataRepository>();
        services.AddScoped<ICalculationPreferencesRepository, CalculationPreferencesRepository>();
        services.AddScoped<IEquipmentCatalogRepository, EquipmentCatalogRepository>();

        services.AddScoped<IAnnualClimateDataProvider, AnnualClimateDataProvider>();
        services.AddScoped<IBuildingReportExporter, ExcelReportService>();

        services.TryAddSingleton<ResilientOperationExecutor>();
        services.AddSingleton<IValidateOptions<EnergyPlusBenchmarkOptions>, EnergyPlusBenchmarkOptionsValidator>();
        services
            .AddOptions<EnergyPlusBenchmarkOptions>()
            .Bind(configuration.GetSection("EnergyPlus"))
            .ValidateOnStart();

        services.AddScoped<IEnergyPlusArtifactStore, LocalEnergyPlusArtifactStore>();
        services.AddScoped<IEnergyPlusModelExporter, EnergyPlusModelExporter>();
        services.AddScoped<IEnergyPlusResultParser, EnergyPlusResultParser>();
        services.AddScoped<IEnergyPlusBenchmarkRunner, EnergyPlusBenchmarkRunner>();

        return services;
    }
}
