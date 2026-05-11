using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalSurfaceModelBuilderTests
{
    private readonly Iso52016PhysicalRoomModelBuilder _builder = new();

    [Fact]
    public void Build_WithExplicitSurfacesCreatesSurfaceAndMassNodePairs()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 21,
                    TimeStepSeconds: 3600),
                ModelOptions: new Iso52016PhysicalNodeModelOptions(
                    SurfaceNodeHeatCapacityFraction: 0.20,
                    DefaultSurfaceToAirConductanceWPerM2K: 3.0,
                    SurfaceToMassConductanceMultiplier: 2.0),
                Surfaces: CreateNorthWallAndGroundSlabSurfaces()));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);

        var request = result.Value;

        Assert.Equal("room-surface-1", request.ZoneCode);
        Assert.Equal(5, request.Nodes.Count);
        Assert.Equal(4, request.InternalConductances.Count);
        Assert.Equal(3, request.BoundaryConductances.Count);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "air" &&
            node.IsAirNode &&
            Math.Abs(node.HeatCapacityJPerK - 100_000.0) < 1e-9);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "surface:north-wall" &&
            Math.Abs(node.HeatCapacityJPerK - 400_000.0) < 1e-9);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "mass:north-wall" &&
            Math.Abs(node.HeatCapacityJPerK - 1_600_000.0) < 1e-9);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "surface:ground-slab" &&
            Math.Abs(node.HeatCapacityJPerK - 800_000.0) < 1e-9);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "mass:ground-slab" &&
            Math.Abs(node.HeatCapacityJPerK - 3_200_000.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "air" &&
            link.ToNodeId == "surface:north-wall" &&
            Math.Abs(link.ConductanceWPerK - 30.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "surface:north-wall" &&
            link.ToNodeId == "mass:north-wall" &&
            Math.Abs(link.ConductanceWPerK - 100.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "air" &&
            link.ToNodeId == "surface:ground-slab" &&
            Math.Abs(link.ConductanceWPerK - 60.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "surface:ground-slab" &&
            link.ToNodeId == "mass:ground-slab" &&
            Math.Abs(link.ConductanceWPerK - 560.0) < 1e-9);

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:north-wall" &&
            boundary.BoundaryId == "outdoor" &&
            Math.Abs(boundary.ConductanceWPerK - 50.0) < 1e-9);

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:ground-slab" &&
            boundary.BoundaryId == "ground" &&
            Math.Abs(boundary.ConductanceWPerK - 280.0) < 1e-9);

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "air" &&
            boundary.BoundaryId == "ventilation-air" &&
            Math.Abs(boundary.ConductanceWPerK - 25.0) < 1e-9);

        var firstHour = request.Hours[0];

        Assert.Equal(7.0, firstHour.BoundaryTemperaturesC["outdoor"], precision: 6);
        Assert.Equal(12.0, firstHour.BoundaryTemperaturesC["ground"], precision: 6);
        Assert.Equal(7.0, firstHour.BoundaryTemperaturesC["ventilation-air"], precision: 6);
        Assert.Equal(100.0, firstHour.NodeHeatGainsW["air"], precision: 6);
        Assert.Equal(250.0, firstHour.NodeHeatGainsW["surface:north-wall"], precision: 6);
        Assert.Equal(250.0, firstHour.NodeHeatGainsW["surface:ground-slab"], precision: 6);
    }

    [Fact]
    public void Build_WithConfiguredSurfaceFractions_UsesConfiguredSolarAndRadiativeDistribution()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                ModelOptions: new Iso52016PhysicalNodeModelOptions(
                    SurfaceNodeHeatCapacityFraction: 0.20),
                Surfaces: new[]
                {
                    CreateSurface(
                        surfaceId: "solar-wall",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                        areaM2: 10,
                        solarFraction: 0.75,
                        radiativeFraction: 0.60),
                    CreateSurface(
                        surfaceId: "mass-wall",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                        areaM2: 10,
                        solarFraction: 0.25,
                        radiativeFraction: 0.40)
                }));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);

        var firstHour = result.Value.Hours[0];

        Assert.Equal(100.0, firstHour.NodeHeatGainsW["air"], precision: 6);
        Assert.Equal(360.0, firstHour.NodeHeatGainsW["surface:solar-wall"], precision: 6);
        Assert.Equal(140.0, firstHour.NodeHeatGainsW["surface:mass-wall"], precision: 6);
    }

    [Fact]
    public void Build_WithAdjacentConditionedAndUnconditionedSurfaces_MapsBoundaryIdsAndTemperatures()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface(
                        surfaceId: "party-wall",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned,
                        areaM2: 8,
                        adjacentBoundaryTemperatureC: 22),
                    CreateSurface(
                        surfaceId: "garage-wall",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned,
                        areaM2: 6,
                        adjacentBoundaryTemperatureC: 15)
                }));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);

        var request = result.Value;
        var firstHour = request.Hours[0];

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:party-wall" &&
            boundary.BoundaryId == "adjacent-conditioned-zone");

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:garage-wall" &&
            boundary.BoundaryId == "adjacent-unconditioned-zone");

        Assert.Equal(22.0, firstHour.BoundaryTemperaturesC["adjacent-conditioned-zone"], precision: 6);
        Assert.Equal(15.0, firstHour.BoundaryTemperaturesC["adjacent-unconditioned-zone"], precision: 6);
    }

    [Fact]
    public void Build_SurfaceExpandedPath_PreservesDeterministicNodeAndBoundaryOrdering()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface("alpha", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10),
                    CreateSurface("beta", Iso52016PhysicalSurfaceBoundaryType.Ground, 20),
                    CreateSurface("gamma", Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned, 8)
                }));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);

        var request = result.Value;
        var nodeIds = request.Nodes.Select(item => item.NodeId).ToArray();
        var boundaryIds = request.BoundaryConductances.Select(item => item.BoundaryId).ToArray();

        Assert.Equal(
            new[]
            {
                "air",
                "surface:alpha",
                "mass:alpha",
                "surface:beta",
                "mass:beta",
                "surface:gamma",
                "mass:gamma"
            },
            nodeIds);

        Assert.Equal(
            new[]
            {
                "outdoor",
                "ground",
                "adjacent-conditioned-zone",
                "ventilation-air"
            },
            boundaryIds);
    }

    [Fact]
    public void Build_WithoutSurfaces_KeepsStep01ThreeNodeFallback()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 21,
                    TimeStepSeconds: 3600)));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
        Assert.Equal(3, result.Value.Nodes.Count);
        Assert.Contains(result.Value.Nodes, node => node.NodeId == "air");
        Assert.Contains(result.Value.Nodes, node => node.NodeId == "internal-surface");
        Assert.Contains(result.Value.Nodes, node => node.NodeId == "thermal-mass");
    }

    [Fact]
    public void Build_RejectsSurfaceWithoutConstructionLayers()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
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
    public void Build_RejectsConfiguredSolarFractionsThatDoNotSumToOne()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface(
                        surfaceId: "a",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                        areaM2: 10,
                        solarFraction: 0.40),
                    CreateSurface(
                        surfaceId: "b",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                        areaM2: 10,
                        solarFraction: 0.40)
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model solar gains distribution fractions must sum to 1.0.",
            result.Error);
    }

    private static Iso52016RoomHourlyInputProfile CreateInputProfile()
    {
        var hours = Enumerable
            .Range(0, 24)
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
                VentilationHeatTransferCoefficientWPerK: 25,
                TotalHeatTransferCoefficientWPerK: 125,
                ThermalCapacityJPerK: 5_000_000,
                SolarGainsW: 400,
                InternalGainsW: 200,
                TotalGainsW: 600))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: "room-surface-1",
            TransmissionHeatTransferCoefficientWPerK: 100,
            VentilationHeatTransferCoefficientWPerK: 25,
            ThermalCapacityJPerK: 5_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: hours);
    }

    private static IReadOnlyList<Iso52016PhysicalSurface> CreateNorthWallAndGroundSlabSurfaces() =>
        new[]
        {
            CreateSurface(
                surfaceId: "north-wall",
                boundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                areaM2: 10,
                boundaryConductanceWPerK: 50,
                surfaceToAirConductanceWPerK: 30,
                surfaceToMassConductanceWPerK: 100,
                heatCapacityJPerK: 400_000,
                massHeatCapacityJPerK: 1_600_000,
                solarFraction: 0.50,
                radiativeFraction: 0.50),
            CreateSurface(
                surfaceId: "ground-slab",
                boundaryType: Iso52016PhysicalSurfaceBoundaryType.Ground,
                areaM2: 20,
                boundaryConductanceWPerK: 280,
                surfaceToAirConductanceWPerK: 60,
                surfaceToMassConductanceWPerK: 560,
                heatCapacityJPerK: 800_000,
                massHeatCapacityJPerK: 3_200_000,
                solarFraction: 0.50,
                radiativeFraction: 0.50)
        };

    private static Iso52016PhysicalSurface CreateSurface(
        string surfaceId,
        Iso52016PhysicalSurfaceBoundaryType boundaryType,
        double areaM2,
        double thicknessM = 0.20,
        double conductivityWPerMK = 1.0,
        double densityKgPerM3 = 1000,
        double specificHeatJPerKgK = 1000,
        double? boundaryConductanceWPerK = null,
        double? surfaceToAirConductanceWPerK = null,
        double? surfaceToMassConductanceWPerK = null,
        double? heatCapacityJPerK = null,
        double? massHeatCapacityJPerK = null,
        double? solarFraction = null,
        double? radiativeFraction = null,
        double? adjacentBoundaryTemperatureC = null) =>
        new(
            SurfaceId: surfaceId,
            BoundaryType: boundaryType,
            AreaM2: areaM2,
            ConstructionLayers: new[]
            {
                new Iso52016PhysicalConstructionLayer(
                    LayerId: $"{surfaceId}-layer",
                    ThicknessM: thicknessM,
                    ConductivityWPerMK: conductivityWPerMK,
                    DensityKgPerM3: densityKgPerM3,
                    SpecificHeatCapacityJPerKgK: specificHeatJPerKgK)
            },
            BoundaryConductanceWPerK: boundaryConductanceWPerK,
            SurfaceToAirConductanceWPerK: surfaceToAirConductanceWPerK,
            SurfaceToMassConductanceWPerK: surfaceToMassConductanceWPerK,
            HeatCapacityJPerK: heatCapacityJPerK,
            MassHeatCapacityJPerK: massHeatCapacityJPerK,
            SolarGainsDistributionFraction: solarFraction,
            InternalRadiativeGainsDistributionFraction: radiativeFraction,
            AdjacentBoundaryTemperatureC: adjacentBoundaryTemperatureC);
}
