using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class GroundSimplifiedEngineeringCoreV1ClosureTests
{
    [Fact]
    public void MatrixGroundBoundaryWithoutMetadataUsesGroundWallsAndDefaultFloorUValue()
    {
        var service = CreateService();
        var room = CreateRoom(areaM2: 40);

        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            ThermalTransmittance.FromValue(0.8).Value,
            CardinalDirection.North,
            WallBoundaryType.Ground).IsSuccess);

        var boundary = service.CalculateBoundaryCondition(
            room,
            new BuildingEnvelopeDefaults(
                FloorUValueWPerM2K: 0.25,
                CeilingUValueWPerM2K: 0.18,
                FloorHeatCapacityKjPerM2K: 90,
                CeilingHeatCapacityKjPerM2K: 70));

        Assert.Equal(19.6, boundary.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(1.0, boundary.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.IndoorTemperatureWeight, precision: 6);
    }

    [Fact]
    public void SlabOnGroundMetadataUsesEquivalentUAreaModelAndGroundOnlyBoundaryWeight()
    {
        var service = CreateService();
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

        var boundary = service.CalculateBoundaryCondition(
            room,
            CreateDefaults());

        Assert.Equal(19.2, boundary.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(1.0, boundary.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.IndoorTemperatureWeight, precision: 6);
    }

    [Fact]
    public void SlabOnGroundInsulationReducesEquivalentGroundHeatTransfer()
    {
        var service = CreateService();

        var uninsulated = CreateRoom(areaM2: 40);
        SetGroundMetadata(
            uninsulated,
            GroundContactType.SlabOnGround,
            exposedPerimeterM: 20,
            burialDepthM: 0,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 0,
            perimeterInsulationDepthM: 0,
            underfloorVentilationAch: 0);

        var insulated = CreateRoom(areaM2: 40);
        SetGroundMetadata(
            insulated,
            GroundContactType.SlabOnGround,
            exposedPerimeterM: 20,
            burialDepthM: 0,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 2,
            perimeterInsulationDepthM: 1,
            underfloorVentilationAch: 0);

        var uninsulatedBoundary = service.CalculateBoundaryCondition(
            uninsulated,
            CreateDefaults());

        var insulatedBoundary = service.CalculateBoundaryCondition(
            insulated,
            CreateDefaults());

        Assert.Equal(19.2, uninsulatedBoundary.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(13.056, insulatedBoundary.HeatTransferCoefficientWPerK, precision: 6);

        Assert.True(
            insulatedBoundary.HeatTransferCoefficientWPerK <
            uninsulatedBoundary.HeatTransferCoefficientWPerK);
    }

    [Fact]
    public void BasementUnconditionedMetadataSplitsBoundaryBetweenGroundAndOutdoor()
    {
        var service = CreateService();
        var room = CreateRoom(areaM2: 40);

        SetGroundMetadata(
            room,
            GroundContactType.BasementUnconditioned,
            exposedPerimeterM: 20,
            burialDepthM: 2,
            wallHeightBelowGradeM: 1,
            horizontalInsulationWidthM: 0,
            perimeterInsulationDepthM: 0,
            underfloorVentilationAch: 0);

        var boundary = service.CalculateBoundaryCondition(
            room,
            CreateDefaults());

        Assert.Equal(20.2752, boundary.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(0.5, boundary.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.5, boundary.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.IndoorTemperatureWeight, precision: 6);
    }

    [Fact]
    public void BasementConditionedMetadataAddsIndoorBoundaryWeight()
    {
        var service = CreateService();
        var room = CreateRoom(areaM2: 40);

        SetGroundMetadata(
            room,
            GroundContactType.BasementConditioned,
            exposedPerimeterM: 20,
            burialDepthM: 0,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 0,
            perimeterInsulationDepthM: 0,
            underfloorVentilationAch: 0);

        var boundary = service.CalculateBoundaryCondition(
            room,
            CreateDefaults());

        Assert.Equal(12.48, boundary.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(0.35, boundary.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.65, boundary.IndoorTemperatureWeight, precision: 6);
    }

    [Fact]
    public void VentilatedCrawlSpaceMetadataIncreasesOutdoorBoundaryWeightAndVentilationModifier()
    {
        var service = CreateService();
        var room = CreateRoom(areaM2: 40);

        SetGroundMetadata(
            room,
            GroundContactType.VentilatedCrawlSpace,
            exposedPerimeterM: 20,
            burialDepthM: 0,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 0,
            perimeterInsulationDepthM: 0,
            underfloorVentilationAch: 4);

        var boundary = service.CalculateBoundaryCondition(
            room,
            CreateDefaults());

        Assert.Equal(21.888, boundary.HeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(0.19, boundary.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.81, boundary.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.IndoorTemperatureWeight, precision: 6);

        Assert.Equal(
            1.0,
            boundary.GroundTemperatureWeight +
            boundary.OutdoorTemperatureWeight +
            boundary.IndoorTemperatureWeight,
            precision: 6);
    }

    [Fact]
    public void SimplifiedGroundModelNeverReturnsZeroHeatTransferForValidMetadata()
    {
        var service = CreateService();
        var room = CreateRoom(areaM2: 40);

        SetGroundMetadata(
            room,
            GroundContactType.SlabOnGround,
            exposedPerimeterM: 20,
            burialDepthM: 0,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 100,
            perimeterInsulationDepthM: 100,
            underfloorVentilationAch: 0);

        var boundary = service.CalculateBoundaryCondition(
            room,
            CreateDefaults());

        Assert.True(boundary.HeatTransferCoefficientWPerK >= 0.01);
        Assert.Equal(1.0, boundary.GroundTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.OutdoorTemperatureWeight, precision: 6);
        Assert.Equal(0.0, boundary.IndoorTemperatureWeight, precision: 6);
    }

    private static Iso13370GroundHeatTransferService CreateService() =>
        new(Options.Create(new Iso13370GroundHeatTransferOptions
        {
            GroundConductivityWPerMK = 2.0,
            BaseCharacteristicDepthM = 1.0,
            PerimeterAmplificationFactor = 1.0,
            SlabOnGroundFactor = 1.0,
            BasementConditionedFactor = 0.65,
            BasementUnconditionedFactor = 0.80,
            CrawlSpaceFactor = 0.75,
            VentilatedCrawlSpaceFactor = 0.95
        }));

    private static BuildingEnvelopeDefaults CreateDefaults() =>
        new(
            FloorUValueWPerM2K: 0.25,
            CeilingUValueWPerM2K: 0.18,
            FloorHeatCapacityKjPerM2K: 90,
            CeilingHeatCapacityKjPerM2K: 70);

    private static Room CreateRoom(double areaM2)
    {
        var project = DomainInvariantTests.CreateProject("Ground simplified project");
        var building = Building.Create("Ground simplified building", project).Value;
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