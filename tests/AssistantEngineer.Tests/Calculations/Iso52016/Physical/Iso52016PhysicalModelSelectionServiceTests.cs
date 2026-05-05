using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalModelSelectionServiceTests
{
    private readonly Iso52016PhysicalModelSelectionService _service = new(
        new Iso52016MatrixReducedRoomModelBuilder(),
        new Iso52016PhysicalRoomModelBuilder(),
        new Iso52016MatrixHourlySolver());

    [Fact]
    public void Simulate_DefaultStrategy_UsesReducedMatrixPath()
    {
        var result = _service.Simulate(
            new Iso52016PhysicalModelSelectionRequest(
                HourlyInputProfile: CreateInputProfile()));

        Assert.True(result.IsSuccess);

        Assert.Equal(Iso52016PhysicalModelSelectionStrategy.ReducedMatrix, result.Value.Strategy);
        Assert.Equal("selection-room", result.Value.ZoneCode);
        Assert.Equal(3, result.Value.MatrixSolverProfile.HourCount);
        Assert.DoesNotContain(
            result.Value.MatrixSolverRequest.Nodes,
            node => node.NodeId.StartsWith("surface:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Simulate_PhysicalNodeModelStrategy_UsesPhysicalBuilderAndExistingMatrixSolver()
    {
        var result = _service.Simulate(
            new Iso52016PhysicalModelSelectionRequest(
                HourlyInputProfile: CreateInputProfile(),
                Strategy: Iso52016PhysicalModelSelectionStrategy.PhysicalNodeModel,
                Surfaces: new[]
                {
                    CreateSurface("wall-east", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10),
                    CreateSurface("slab", Iso52016PhysicalSurfaceBoundaryType.Ground, 20)
                },
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 0,
                        VentilationHeatTransferCoefficientWPerK: 30,
                        VentilationBoundaryTemperatureC: 18,
                        InternalGainsConvectiveFraction: 0.60,
                        SolarGainsToAirFraction: 0.10)
                }));

        Assert.True(result.IsSuccess);

        Assert.Equal(Iso52016PhysicalModelSelectionStrategy.PhysicalNodeModel, result.Value.Strategy);
        Assert.Equal("selection-room", result.Value.ZoneCode);
        Assert.Equal(3, result.Value.MatrixSolverProfile.HourCount);
        Assert.Contains(result.Value.MatrixSolverRequest.Nodes, node => node.NodeId == "surface:wall-east");
        Assert.Contains(result.Value.MatrixSolverRequest.Nodes, node => node.NodeId == "surface:slab");
        Assert.Contains(result.Value.MatrixSolverRequest.BoundaryConductances, boundary => boundary.BoundaryId == "ventilation-air");
    }

    [Fact]
    public void Simulate_PhysicalNodeModelStrategy_PropagatesBuilderValidationFailure()
    {
        var result = _service.Simulate(
            new Iso52016PhysicalModelSelectionRequest(
                HourlyInputProfile: CreateInputProfile(),
                Strategy: Iso52016PhysicalModelSelectionStrategy.PhysicalNodeModel,
                Surfaces: new[]
                {
                    new Iso52016PhysicalSurface(
                        SurfaceId: "invalid",
                        BoundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                        AreaM2: 10,
                        ConstructionLayers: Array.Empty<Iso52016PhysicalConstructionLayer>())
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model surface 'invalid' requires at least one construction layer.",
            result.Error);
    }

    [Fact]
    public void Simulate_RejectsUnsupportedStrategy()
    {
        var result = _service.Simulate(
            new Iso52016PhysicalModelSelectionRequest(
                HourlyInputProfile: CreateInputProfile(),
                Strategy: (Iso52016PhysicalModelSelectionStrategy)99));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical model selection strategy '99' is not supported.",
            result.Error);
    }

    private static Iso52016RoomHourlyInputProfile CreateInputProfile()
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
                VentilationHeatTransferCoefficientWPerK: 10,
                TotalHeatTransferCoefficientWPerK: 130,
                ThermalCapacityJPerK: 3_000_000,
                SolarGainsW: 900,
                InternalGainsW: 300,
                TotalGainsW: 1200))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: "selection-room",
            TransmissionHeatTransferCoefficientWPerK: 120,
            VentilationHeatTransferCoefficientWPerK: 10,
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