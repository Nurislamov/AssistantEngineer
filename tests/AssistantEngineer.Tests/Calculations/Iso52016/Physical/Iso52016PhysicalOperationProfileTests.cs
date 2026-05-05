using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalOperationProfileTests
{
    private readonly Iso52016PhysicalRoomModelBuilder _builder = new();

    [Fact]
    public void Build_AggregatedFallback_MapsHourlyVentilationConductanceAndGainSplits()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(ventilationHeatTransferCoefficientWPerK: 0),
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 0,
                        VentilationHeatTransferCoefficientWPerK: 40,
                        VentilationBoundaryTemperatureC: 18,
                        InternalGainsConvectiveFraction: 0.75,
                        SolarGainsToAirFraction: 0.10),
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 1,
                        VentilationHeatTransferCoefficientWPerK: 0,
                        InternalGainsConvectiveFraction: 0.25,
                        SolarGainsToAirFraction: 0.20)
                }));

        Assert.True(result.IsSuccess);

        var matrixRequest = result.Value;
        var ventilation = Assert.Single(matrixRequest.BoundaryConductances, boundary => boundary.BoundaryId == "ventilation-air");

        Assert.Equal("air", ventilation.NodeId);
        Assert.Equal(40.0, ventilation.ConductanceWPerK, precision: 6);

        var firstHour = matrixRequest.Hours[0];
        var secondHour = matrixRequest.Hours[1];

        Assert.Equal(18.0, firstHour.BoundaryTemperaturesC["ventilation-air"], precision: 6);
        Assert.Equal(40.0, Assert.Single(firstHour.BoundaryConductanceOverrides!).ConductanceWPerK, precision: 6);
        Assert.Equal(0.0, Assert.Single(secondHour.BoundaryConductanceOverrides!).ConductanceWPerK, precision: 6);

        Assert.Equal(250.0, firstHour.NodeHeatGainsW["air"], precision: 6);
        Assert.Equal(665.0, firstHour.NodeHeatGainsW["internal-surface"], precision: 6);
        Assert.Equal(285.0, firstHour.NodeHeatGainsW["thermal-mass"], precision: 6);

        Assert.Equal(250.0, secondHour.NodeHeatGainsW["air"], precision: 6);
    }

    [Fact]
    public void Build_SurfaceExpansion_MapsOperationProfilesToAirAndSurfaceGains()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(ventilationHeatTransferCoefficientWPerK: 10),
                Surfaces: new[]
                {
                    CreateSurface("wall-east", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10),
                    CreateSurface("slab", Iso52016PhysicalSurfaceBoundaryType.Ground, 20)
                },
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 0,
                        VentilationHeatTransferCoefficientWPerK: 25,
                        VentilationBoundaryTemperatureC: 16,
                        InternalGainsConvectiveFraction: 0.60,
                        SolarGainsToAirFraction: 0.10)
                }));

        Assert.True(result.IsSuccess);

        var matrixRequest = result.Value;
        var firstHour = matrixRequest.Hours[0];

        Assert.Equal(25.0, Assert.Single(firstHour.BoundaryConductanceOverrides!).ConductanceWPerK, precision: 6);
        Assert.Equal(16.0, firstHour.BoundaryTemperaturesC["ventilation-air"], precision: 6);
        Assert.Equal(220.0, firstHour.NodeHeatGainsW["air"], precision: 6);
        Assert.Equal(326.6666666666667, firstHour.NodeHeatGainsW["surface:wall-east"], precision: 6);
        Assert.Equal(653.3333333333334, firstHour.NodeHeatGainsW["surface:slab"], precision: 6);
    }

    [Fact]
    public void Build_RejectsDuplicateOperationConditionHour()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(HourOfYear: 0, InternalGainsConvectiveFraction: 0.5),
                    new Iso52016PhysicalHourlyOperationCondition(HourOfYear: 0, InternalGainsConvectiveFraction: 0.6)
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model duplicate operation condition for hour 0.",
            result.Error);
    }

    [Fact]
    public void Build_RejectsOperationConditionHourOutsideProfile()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(HourOfYear: 99, InternalGainsConvectiveFraction: 0.5)
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model operation condition references hour 99 that is not in the hourly profile.",
            result.Error);
    }

    [Fact]
    public void Build_RejectsInvalidOperationFractionsAndNegativeVentilation()
    {
        var invalidSolarFraction = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(HourOfYear: 0, SolarGainsToAirFraction: 1.2)
                }));

        Assert.True(invalidSolarFraction.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model operation condition solar gains to air fraction must be between 0 and 1 at hour 0.",
            invalidSolarFraction.Error);

        var negativeVentilation = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(HourOfYear: 0, VentilationHeatTransferCoefficientWPerK: -1)
                }));

        Assert.True(negativeVentilation.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model operation condition ventilation heat transfer coefficient must be finite and non-negative at hour 0.",
            negativeVentilation.Error);
    }

    private static Iso52016RoomHourlyInputProfile CreateInputProfile(
        double ventilationHeatTransferCoefficientWPerK = 10)
    {
        var hours = Enumerable
            .Range(0, 3)
            .Select(hour => new Iso52016RoomHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: 7,
                GroundBoundaryTemperatureC: 12,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26,
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
                TotalHeatTransferCoefficientWPerK: 120 + ventilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: 3_000_000,
                SolarGainsW: 1000,
                InternalGainsW: 200,
                TotalGainsW: 1200))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: "operation-room",
            TransmissionHeatTransferCoefficientWPerK: 120,
            VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: hours);
    }

    private static Iso52016PhysicalSurface CreateSurface(
        string surfaceId,
        Iso52016PhysicalSurfaceBoundaryType boundaryType,
        double areaM2) =>
        new(
            SurfaceId: surfaceId,
            BoundaryType: boundaryType,
            AreaM2: areaM2,
            ConstructionLayers: new[]
            {
                new Iso52016PhysicalConstructionLayer(
                    LayerId: $"{surfaceId}-layer",
                    ThicknessM: 0.20,
                    ConductivityWPerMK: 1.0,
                    DensityKgPerM3: 1000,
                    SpecificHeatCapacityJPerKgK: 1000)
            });
}
