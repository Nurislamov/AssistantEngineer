using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalRoomModelDiagnosticsBuilderTests
{
    private readonly Iso52016PhysicalRoomModelDiagnosticsBuilder _diagnosticsBuilder =
        new(new Iso52016PhysicalRoomModelBuilder());

    [Fact]
    public void Build_AggregatedFallback_ReportsThreeNodeTopologyAndGainConservation()
    {
        var result = _diagnosticsBuilder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(
                    roomCode: "diagnostics-aggregated-room",
                    hourCount: 3,
                    ventilationHeatTransferCoefficientWPerK: 25)));

        Assert.True(result.IsSuccess);

        var diagnostics = result.Value;

        Assert.Equal("diagnostics-aggregated-room", diagnostics.ZoneCode);
        Assert.Equal("air", diagnostics.AirNodeId);
        Assert.Equal(3, diagnostics.NodeCount);
        Assert.Equal(2, diagnostics.InternalConductanceLinkCount);
        Assert.Equal(4, diagnostics.BoundaryConductanceLinkCount);
        Assert.Equal(3, diagnostics.HourCount);

        Assert.Contains("air", diagnostics.NodeIds);
        Assert.Contains("internal-surface", diagnostics.NodeIds);
        Assert.Contains("thermal-mass", diagnostics.NodeIds);

        Assert.Contains("outdoor", diagnostics.BoundaryIds);
        Assert.Contains("ground", diagnostics.BoundaryIds);
        Assert.Contains("adjacent-zone", diagnostics.BoundaryIds);
        Assert.Contains("ventilation-air", diagnostics.BoundaryIds);

        Assert.Equal(5_000_000.0, diagnostics.TotalHeatCapacityJPerK, precision: 6);
        Assert.True(diagnostics.TotalInternalConductanceWPerK > 0);
        Assert.True(diagnostics.TotalBoundaryConductanceWPerK > 0);
        Assert.Equal(0.0, diagnostics.MaxAbsoluteNodeGainBalanceErrorW, precision: 6);
    }

    [Fact]
    public void Build_SurfaceAndOperationProfiles_ReportsExpandedTopologyOverridesAndGainConservation()
    {
        var result = _diagnosticsBuilder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(
                    roomCode: "diagnostics-surface-room",
                    hourCount: 3,
                    ventilationHeatTransferCoefficientWPerK: 0),
                Surfaces: new[]
                {
                    CreateSurface("wall-east", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10),
                    CreateSurface("slab", Iso52016PhysicalSurfaceBoundaryType.Ground, 20)
                },
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 0,
                        VentilationHeatTransferCoefficientWPerK: 35,
                        VentilationBoundaryTemperatureC: 18,
                        InternalGainsConvectiveFraction: 0.60,
                        SolarGainsToAirFraction: 0.10),
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 1,
                        VentilationHeatTransferCoefficientWPerK: 0,
                        InternalGainsConvectiveFraction: 0.40,
                        SolarGainsToAirFraction: 0.20)
                }));

        Assert.True(result.IsSuccess);

        var diagnostics = result.Value;

        Assert.Equal(5, diagnostics.NodeCount);
        Assert.Equal(4, diagnostics.InternalConductanceLinkCount);
        Assert.Equal(3, diagnostics.BoundaryConductanceLinkCount);
        Assert.Equal(3, diagnostics.HourCount);

        Assert.Contains("surface:wall-east", diagnostics.NodeIds);
        Assert.Contains("mass:wall-east", diagnostics.NodeIds);
        Assert.Contains("surface:slab", diagnostics.NodeIds);
        Assert.Contains("mass:slab", diagnostics.NodeIds);

        Assert.Contains("outdoor", diagnostics.BoundaryIds);
        Assert.Contains("ground", diagnostics.BoundaryIds);
        Assert.Contains("ventilation-air", diagnostics.BoundaryIds);

        Assert.Equal(0.0, diagnostics.MaxAbsoluteNodeGainBalanceErrorW, precision: 6);
        Assert.Equal(35.0, diagnostics.MaxBoundaryConductanceOverrideWPerK, precision: 6);

        var firstHour = diagnostics.Hours.Single(hour => hour.HourOfYear == 0);

        Assert.Equal(600.0, firstHour.SourceTotalGainsW, precision: 6);
        Assert.Equal(600.0, firstHour.DistributedNodeHeatGainsW, precision: 6);
        Assert.Equal(0.0, firstHour.NodeGainBalanceErrorW, precision: 6);
        Assert.Equal(1, firstHour.BoundaryConductanceOverrideCount);
        Assert.Equal(35.0, firstHour.MaxBoundaryConductanceOverrideWPerK, precision: 6);
    }

    [Fact]
    public void Build_ReturnsValidationFailureWhenPhysicalBuilderRejectsRequest()
    {
        var result = _diagnosticsBuilder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: null!));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model requires an hourly input profile.",
            result.Error);
    }

    private static Iso52016RoomHourlyInputProfile CreateInputProfile(
        string roomCode,
        int hourCount,
        double ventilationHeatTransferCoefficientWPerK)
    {
        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016RoomHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: 7,
                GroundBoundaryTemperatureC: 12,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26,
                TransmissionHeatTransferCoefficientWPerK: 100,
                VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
                TotalHeatTransferCoefficientWPerK: 100 + ventilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: 5_000_000,
                SolarGainsW: 400,
                InternalGainsW: 200,
                TotalGainsW: 600))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: roomCode,
            TransmissionHeatTransferCoefficientWPerK: 100,
            VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
            ThermalCapacityJPerK: 5_000_000,
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
