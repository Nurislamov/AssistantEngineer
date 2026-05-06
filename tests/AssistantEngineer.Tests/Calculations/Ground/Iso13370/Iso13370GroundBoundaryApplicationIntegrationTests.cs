using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370GroundBoundaryApplicationIntegrationTests
{
    [Fact]
    public void DefaultOption_KeepsCompatibilityBehavior()
    {
        var options = Options.Create(new Iso13370GroundHeatTransferOptions
        {
            UseIso13370InspiredBoundaryCalculator = false,
            GroundConductivityWPerMK = 2.0,
            BaseCharacteristicDepthM = 1.0,
            PerimeterAmplificationFactor = 1.0,
            SlabOnGroundFactor = 1.0,
            BasementConditionedFactor = 0.65,
            BasementUnconditionedFactor = 0.80,
            CrawlSpaceFactor = 0.75,
            VentilatedCrawlSpaceFactor = 0.95
        });

        var service = new Iso13370GroundHeatTransferService(
            options,
            new Iso13370GroundBoundaryCalculator(new Iso13370GroundTemperatureProfileCalculator()),
            new Iso13370GroundBoundaryApplicationAdapter());

        var room = CreateRoom(areaM2: 40);
        SetGroundMetadata(
            room,
            GroundContactType.SlabOnGround,
            exposedPerimeterM: 20,
            burialDepthM: 0,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 0,
            perimeterInsulationDepthM: 0,
            underfloorVentilationAch: 0);

        var result = service.CalculateBoundaryCondition(room, CreateDefaults());

        Assert.Equal(19.2, result.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(1.0, result.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.0, result.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.0, result.IndoorTemperatureWeight, precision: 6);
    }

    [Fact]
    public void OptInOption_UsesIso13370InspiredCalculatorPath()
    {
        var optionsValue = new Iso13370GroundHeatTransferOptions
        {
            UseIso13370InspiredBoundaryCalculator = true,
            GroundConductivityWPerMK = 2.0,
            IndoorAnnualMeanTemperatureC = 20.0,
            OutdoorAnnualMeanTemperatureC = 10.0,
            GroundAnnualMeanTemperatureC = 12.0,
            GroundTemperatureAmplitudeC = 4.0,
            GroundTemperaturePhaseShiftMonths = 1.0
        };

        var options = Options.Create(optionsValue);
        var calculator = new Iso13370GroundBoundaryCalculator(new Iso13370GroundTemperatureProfileCalculator());
        var adapter = new Iso13370GroundBoundaryApplicationAdapter();

        var service = new Iso13370GroundHeatTransferService(options, calculator, adapter);
        var room = CreateRoom(areaM2: 45);
        SetGroundMetadata(
            room,
            GroundContactType.VentilatedCrawlSpace,
            exposedPerimeterM: 24,
            burialDepthM: 0.4,
            wallHeightBelowGradeM: 0.3,
            horizontalInsulationWidthM: 0.2,
            perimeterInsulationDepthM: 0.2,
            underfloorVentilationAch: 4.0);

        var defaults = CreateDefaults();
        var actual = service.CalculateBoundaryCondition(room, defaults);

        var expectedInput = adapter.BuildInput(
            room,
            floorUValueWPerM2K: defaults.FloorUValueWPerM2K,
            indoorAnnualMeanTemperatureC: optionsValue.IndoorAnnualMeanTemperatureC,
            outdoorAnnualMeanTemperatureC: optionsValue.OutdoorAnnualMeanTemperatureC,
            outdoorMonthlyMeanTemperaturesC: null,
            groundAnnualMeanTemperatureC: optionsValue.GroundAnnualMeanTemperatureC,
            groundTemperatureAmplitudeC: optionsValue.GroundTemperatureAmplitudeC,
            groundTemperaturePhaseShiftMonths: optionsValue.GroundTemperaturePhaseShiftMonths,
            groundConductivityWPerMK: optionsValue.GroundConductivityWPerMK);
        var expected = calculator.Calculate(expectedInput);

        Assert.Equal(expected.HeatTransferCoefficientWPerK, actual.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(expected.GroundWeight, actual.GroundTemperatureWeight, precision: 6);
        Assert.Equal(expected.OutdoorWeight, actual.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(expected.IndoorWeight, actual.IndoorTemperatureWeight, precision: 6);
    }

    [Fact]
    public void MissingMetadata_KeepsMatrixFallbackEvenInOptInMode()
    {
        var service = new Iso13370GroundHeatTransferService(
            Options.Create(new Iso13370GroundHeatTransferOptions
            {
                UseIso13370InspiredBoundaryCalculator = true
            }),
            new Iso13370GroundBoundaryCalculator(new Iso13370GroundTemperatureProfileCalculator()),
            new Iso13370GroundBoundaryApplicationAdapter());

        var room = CreateRoom(areaM2: 40);
        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            ThermalTransmittance.FromValue(0.8).Value,
            CardinalDirection.North,
            WallBoundaryType.Ground).IsSuccess);

        var result = service.CalculateBoundaryCondition(room, CreateDefaults());
        Assert.Equal(19.6, result.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(1.0, result.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.0, result.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.0, result.IndoorTemperatureWeight, precision: 6);
    }

    [Fact]
    public void DIRegistration_UsesSingletonLifetimeForGroundIntegrationServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddCalculationsModule(configuration);

        AssertServiceLifetime<IGroundHeatTransferService>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso13370GroundBoundaryCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso13370GroundTemperatureProfileCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso13370GroundBoundaryApplicationAdapter>(services, ServiceLifetime.Singleton);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        var resolved = provider.GetRequiredService<IGroundHeatTransferService>();
        Assert.IsType<Iso13370GroundHeatTransferService>(resolved);
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(item => item.ServiceType == typeof(TService));
        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor!.Lifetime);
    }

    private static BuildingEnvelopeDefaults CreateDefaults() =>
        new(
            FloorUValueWPerM2K: 0.25,
            CeilingUValueWPerM2K: 0.18,
            FloorHeatCapacityKjPerM2K: 90,
            CeilingHeatCapacityKjPerM2K: 70);

    private static Room CreateRoom(double areaM2)
    {
        var project = DomainInvariantTests.CreateProject("Ground integration project");
        var building = Building.Create("Ground integration building", project).Value;
        var floor = building.AddFloor("Level 1").Value;

        return floor.AddRoom(
            "Ground room",
            Area.FromSquareMeters(areaM2).Value,
            heightM: 3,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: Temperature.FromCelsius(-5).Value).Value;
    }

    private static void SetGroundMetadata(
        Room room,
        GroundContactType contactType,
        double exposedPerimeterM,
        double burialDepthM,
        double wallHeightBelowGradeM,
        double horizontalInsulationWidthM,
        double perimeterInsulationDepthM,
        double underfloorVentilationAch)
    {
        var metadata = GroundContactMetadata.Create(
            contactType,
            exposedPerimeterM,
            burialDepthM,
            wallHeightBelowGradeM,
            horizontalInsulationWidthM,
            perimeterInsulationDepthM,
            underfloorVentilationAch);

        Assert.True(metadata.IsSuccess, metadata.Error);
        Assert.True(room.SetGroundContactMetadata(metadata.Value).IsSuccess);
    }
}
