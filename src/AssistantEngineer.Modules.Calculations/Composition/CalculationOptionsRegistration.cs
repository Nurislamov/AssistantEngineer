using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class CalculationOptionsRegistration
{
    public static IServiceCollection AddCalculationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<CoolingLoadCalculationOptions>, CoolingLoadCalculationOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso52016CoolingLoadOptions>, Iso52016CoolingLoadOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso52016EnergyNeedOptions>, Iso52016EnergyNeedOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso52016MonthlyEnergyNeedOptions>, Iso52016MonthlyEnergyNeedOptionsValidator>();
        services.AddSingleton<IValidateOptions<En12831HeatingLoadOptions>, En12831HeatingLoadOptionsValidator>();
        services.AddSingleton<IValidateOptions<En16798ProfileOptions>, En16798ProfileOptionsValidator>();
        services.AddSingleton<IValidateOptions<NaturalVentilationOptions>, NaturalVentilationOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso13370GroundTemperatureOptions>, Iso13370GroundTemperatureOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso13370GroundHeatTransferOptions>, Iso13370GroundHeatTransferOptionsValidator>();

        services
            .AddOptions<CoolingLoadCalculationOptions>()
            .Bind(configuration.GetSection("Calculations:CoolingLoad"))
            .ValidateOnStart();

        services
            .AddOptions<Iso52016CoolingLoadOptions>()
            .Bind(configuration.GetSection("Calculations:Iso52016Cooling"))
            .ValidateOnStart();

        services
            .AddOptions<Iso52016EnergyNeedOptions>()
            .Bind(configuration.GetSection("Calculations:Iso52016EnergyNeed"))
            .ValidateOnStart();

        services
            .AddOptions<Iso52016MonthlyEnergyNeedOptions>()
            .Bind(configuration.GetSection("Calculations:Iso52016MonthlyEnergyNeed"))
            .ValidateOnStart();

        services
            .AddOptions<En12831HeatingLoadOptions>()
            .Bind(configuration.GetSection("Calculations:HeatingLoad"))
            .ValidateOnStart();

        services
            .AddOptions<En16798ProfileOptions>()
            .Bind(configuration.GetSection("Calculations:En16798Profiles"))
            .ValidateOnStart();

        services
            .AddOptions<NaturalVentilationOptions>()
            .Bind(configuration.GetSection("Calculations:NaturalVentilation"))
            .ValidateOnStart();

        services
            .AddOptions<Iso13370GroundTemperatureOptions>()
            .Bind(configuration.GetSection("Calculations:Iso13370Ground"))
            .ValidateOnStart();

        services
            .AddOptions<Iso13370GroundHeatTransferOptions>()
            .Bind(configuration.GetSection("Calculations:Iso13370GroundHeatTransfer"))
            .ValidateOnStart();

        return services;
    }
}