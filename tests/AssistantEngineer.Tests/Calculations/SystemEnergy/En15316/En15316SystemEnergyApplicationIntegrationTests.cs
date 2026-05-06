using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316SystemEnergyApplicationIntegrationTests
{
    [Fact]
    public void DefaultOption_PreservesCompatibilityBehavior()
    {
        var service = CreateService(new SystemEnergyOptions
        {
            UseEn15316InspiredChain = false
        });

        var input = new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 1000,
            HeatingEfficiency: 0.8,
            FanEnergyKWh: 100,
            PrimaryEnergyFactor: 2.0,
            DiagnosticsContext: "system-energy-default");

        var result = service.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1250, result.Value.FinalHeatingEnergyKWh, precision: 6);
        Assert.Equal(100, result.Value.FinalFanEnergyKWh, precision: 6);
        Assert.Equal(1350, result.Value.TotalFinalEnergyKWh, precision: 6);
        Assert.Equal(2700, result.Value.PrimaryEnergyKWh!.Value, precision: 6);
    }

    [Fact]
    public void OptInOption_UsesEn15316ChainAndAdapterMapping()
    {
        var options = new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true,
            DefaultHeatingTechnology = En15316GenerationTechnology.Boiler,
            DefaultCoolingTechnology = En15316GenerationTechnology.Chiller,
            DefaultDhwTechnology = En15316GenerationTechnology.Boiler,
            DefaultHeatingCarrier = En15316EnergyCarrier.NaturalGas,
            DefaultCoolingCarrier = En15316EnergyCarrier.Electricity,
            DefaultDhwCarrier = En15316EnergyCarrier.NaturalGas
        };

        var calculator = new En15316SystemEnergyChainCalculator(new En15316SystemEnergyReferenceDataProvider());
        var adapter = new En15316SystemEnergyApplicationAdapter();
        var service = new SystemEnergyEngine(
            Options.Create(options),
            calculator,
            adapter);

        var input = new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 5000,
            UsefulCoolingEnergyKWh: 2400,
            UsefulDhwEnergyKWh: 900,
            HeatingEfficiency: 0.9,
            CoolingCop: 3.0,
            DhwEfficiency: 0.8,
            FanEnergyKWh: 250,
            PrimaryEnergyFactor: 1.9,
            DiagnosticsContext: "system-energy-opt-in");

        var actual = service.Calculate(input);
        Assert.True(actual.IsSuccess, actual.Error);

        var enInput = adapter.MapToEn15316Input(input, options);
        var enResult = calculator.Calculate(enInput);
        Assert.True(enResult.IsSuccess, enResult.Error);

        var expected = adapter.MapToSystemEnergyResult(enResult.Value, input);

        Assert.Equal(expected.FinalHeatingEnergyKWh, actual.Value.FinalHeatingEnergyKWh, precision: 6);
        Assert.Equal(expected.FinalCoolingEnergyKWh, actual.Value.FinalCoolingEnergyKWh, precision: 6);
        Assert.Equal(expected.FinalDhwEnergyKWh, actual.Value.FinalDhwEnergyKWh, precision: 6);
        Assert.Equal(expected.FinalFanEnergyKWh, actual.Value.FinalFanEnergyKWh, precision: 6);
        Assert.Equal(expected.TotalFinalEnergyKWh, actual.Value.TotalFinalEnergyKWh, precision: 6);
        Assert.Equal(expected.PrimaryEnergyKWh, actual.Value.PrimaryEnergyKWh);
    }

    [Fact]
    public void OptIn_HeatingEfficiencyPath_Works()
    {
        var service = CreateService(new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true
        });

        var result = service.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 900,
            HeatingEfficiency: 0.75));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1200, result.Value.FinalHeatingEnergyKWh, precision: 6);
    }

    [Fact]
    public void OptIn_HeatingCopPath_Works()
    {
        var service = CreateService(new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true,
            DefaultHeatingTechnology = En15316GenerationTechnology.HeatPump,
            DefaultHeatingCarrier = En15316EnergyCarrier.Electricity
        });

        var result = service.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 900,
            HeatingCop: 3.0));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(300, result.Value.FinalHeatingEnergyKWh, precision: 6);
    }

    [Fact]
    public void OptIn_CoolingCopPath_Works()
    {
        var service = CreateService(new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true
        });

        var result = service.Calculate(new SystemEnergyInput(
            UsefulCoolingEnergyKWh: 1500,
            CoolingCop: 2.5));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(600, result.Value.FinalCoolingEnergyKWh, precision: 6);
    }

    [Fact]
    public void OptIn_DhwEfficiencyAndCopPath_WorksWithEfficiencyPrecedence()
    {
        var service = CreateService(new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true
        });

        var result = service.Calculate(new SystemEnergyInput(
            UsefulDhwEnergyKWh: 1000,
            DhwEfficiency: 0.8,
            DhwCop: 2.0));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1250, result.Value.FinalDhwEnergyKWh, precision: 6);
    }

    [Fact]
    public void OptIn_FanEnergy_IsNotDoubleCounted()
    {
        var service = CreateService(new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true
        });

        var result = service.Calculate(new SystemEnergyInput(
            UsefulCoolingEnergyKWh: 1200,
            CoolingCop: 3.0,
            FanEnergyKWh: 180));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(400, result.Value.FinalCoolingEnergyKWh, precision: 6);
        Assert.Equal(180, result.Value.FinalFanEnergyKWh, precision: 6);
        Assert.Equal(580, result.Value.TotalFinalEnergyKWh, precision: 6);
    }

    [Fact]
    public void OptIn_MissingAssumptions_StillProduceDiagnostics()
    {
        var service = CreateService(new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true
        });

        var result = service.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 1000,
            DiagnosticsContext: "missing-assumptions"));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1000, result.Value.FinalHeatingEnergyKWh, precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code.Contains("DefaultedToPassThrough", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DIRegistration_UsesSafeLifetimes()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddCalculationsModule(configuration);

        AssertServiceLifetime<SystemEnergyEngine>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<En15316SystemEnergyReferenceDataProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<En15316SystemEnergyChainCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<En15316SystemEnergyApplicationAdapter>(services, ServiceLifetime.Singleton);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        var resolved = provider.GetRequiredService<SystemEnergyEngine>();
        Assert.NotNull(resolved);
    }

    private static SystemEnergyEngine CreateService(SystemEnergyOptions options) =>
        new(
            Options.Create(options),
            new En15316SystemEnergyChainCalculator(new En15316SystemEnergyReferenceDataProvider()),
            new En15316SystemEnergyApplicationAdapter());

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(item => item.ServiceType == typeof(TService));
        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor!.Lifetime);
    }
}
