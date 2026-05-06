using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterApplicationIntegrationTests
{
    [Fact]
    public void DefaultOption_PreservesCompatibilityBehavior()
    {
        var service = CreateService(new DomesticHotWaterOptions
        {
            UseIso12831InspiredCalculator = false
        });

        var request = new DomesticHotWaterDemandRequest
        {
            PeopleCount = 4,
            LitersPerPersonDay = 50,
            ColdWaterTemperatureC = 10,
            HotWaterTemperatureC = 55,
            DistributionLossFactor = 0
        };

        var result = service.Calculate(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(200, result.Value.DailyVolumeLiters, precision: 6);
        Assert.Equal(10.465, result.Value.DailyEnergyKWh, precision: 3);
    }

    [Fact]
    public void OptInOption_UsesIso12831InspiredCalculatorPath()
    {
        var options = new DomesticHotWaterOptions
        {
            UseIso12831InspiredCalculator = true,
            DefaultUsageCategory = Iso12831DomesticHotWaterUsageCategory.ResidentialApartment,
            DefaultReferenceMode = Iso12831DomesticHotWaterReferenceMode.PeopleBased,
            DefaultDrawProfileKind = Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend
        };

        var calculator = new Iso12831DomesticHotWaterDemandCalculator(
            new Iso12831DomesticHotWaterReferenceDataProvider(),
            new Iso12831DomesticHotWaterDrawProfileProvider());
        var adapter = new Iso12831DomesticHotWaterApplicationAdapter();
        var service = new DomesticHotWaterDemandService(
            Options.Create(options),
            calculator,
            adapter);

        var request = new DomesticHotWaterDemandRequest
        {
            PeopleCount = 3,
            LitersPerPersonDay = 55,
            ColdWaterTemperatureC = 12,
            HotWaterTemperatureC = 58,
            DistributionLossFactor = 0.12,
            StorageLossKWhPerDay = 1.1,
            CirculationLossKWhPerDay = 0.8
        };

        var serviceResult = service.Calculate(request);
        Assert.True(serviceResult.IsSuccess, serviceResult.Error);

        var isoInput = adapter.MapToIsoInput(request, options);
        var isoResult = calculator.Calculate(isoInput);
        Assert.True(isoResult.IsSuccess, isoResult.Error);

        Assert.Equal(isoResult.Value.DailyVolumeLiters, serviceResult.Value.DailyVolumeLiters, 3);
        Assert.Equal(isoResult.Value.DailyTotalEnergyKWh, serviceResult.Value.DailyEnergyKWh, 3);
        Assert.Equal(isoResult.Value.AnnualTotalEnergyKWh, serviceResult.Value.AnnualEnergyKWh, 3);
    }

    [Fact]
    public void ZeroPeople_StillReturnsZeroDemand()
    {
        var service = CreateService(new DomesticHotWaterOptions
        {
            UseIso12831InspiredCalculator = true
        });

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 0,
            LitersPerPersonDay = 50,
            ColdWaterTemperatureC = 10,
            HotWaterTemperatureC = 55,
            DistributionLossFactor = 0
        });

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(0, result.Value.DailyVolumeLiters, 6);
    }

    [Fact]
    public void InvalidTemperatureDelta_StillReturnsValidationFailure()
    {
        var service = CreateService(new DomesticHotWaterOptions
        {
            UseIso12831InspiredCalculator = true
        });

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 1,
            LitersPerPersonDay = 40,
            ColdWaterTemperatureC = 55,
            HotWaterTemperatureC = 10
        });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void IncludeHourlyProfile_StillReturns8760Records()
    {
        var service = CreateService(new DomesticHotWaterOptions
        {
            UseIso12831InspiredCalculator = true
        });

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 2,
            LitersPerPersonDay = 45,
            ColdWaterTemperatureC = 12,
            HotWaterTemperatureC = 52,
            DistributionLossFactor = 0.1,
            IncludeHourlyProfile = true,
            Year = 2025
        });

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(8760, result.Value.HourlyDemand.Count);
    }

    [Fact]
    public void DIRegistration_UsesSafeLifetimes()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddCalculationsModule(configuration);

        AssertServiceLifetime<DomesticHotWaterDemandService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso12831DomesticHotWaterReferenceDataProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso12831DomesticHotWaterDrawProfileProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso12831DomesticHotWaterDemandCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso12831DomesticHotWaterApplicationAdapter>(services, ServiceLifetime.Singleton);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();
        var resolved = scope.ServiceProvider.GetRequiredService<DomesticHotWaterDemandService>();
        Assert.NotNull(resolved);
    }

    private static DomesticHotWaterDemandService CreateService(DomesticHotWaterOptions options) =>
        new(
            Options.Create(options),
            new Iso12831DomesticHotWaterDemandCalculator(
                new Iso12831DomesticHotWaterReferenceDataProvider(),
                new Iso12831DomesticHotWaterDrawProfileProvider()),
            new Iso12831DomesticHotWaterApplicationAdapter());

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(item => item.ServiceType == typeof(TService));
        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor!.Lifetime);
    }
}
