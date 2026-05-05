using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalSurfaceModelBuilderTests
{
    private readonly Iso52016PhysicalRoomModelBuilder _builder = new();

    [Fact]
    public void Build_WithPhysicalSurfaces_CreatesSurfaceMassNodesAndBoundaryLinks()
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
                Surfaces: CreateOutdoorAndGroundSurfaces()));

        Assert.True(result.IsSuccess);

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
            node.NodeId == "surface:wall-north" &&
            Math.Abs(node.HeatCapacityJPerK - 400_000.0) < 1e-9);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "mass:wall-north" &&
            Math.Abs(node.HeatCapacityJPerK - 1_600_000.0) < 1e-9);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "surface:slab" &&
            Math.Abs(node.HeatCapacityJPerK - 200_000.0) < 1e-9);

        Assert.Contains(request.Nodes, node =>
            node.NodeId == "mass:slab" &&
            Math.Abs(node.HeatCapacityJPerK - 800_000.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "air" &&
            link.ToNodeId == "surface:wall-north" &&
            Math.Abs(link.ConductanceWPerK - 30.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "surface:wall-north" &&
            link.ToNodeId == "mass:wall-north" &&
            Math.Abs(link.ConductanceWPerK - 100.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "air" &&
            link.ToNodeId == "surface:slab" &&
            Math.Abs(link.ConductanceWPerK - 15.0) < 1e-9);

        Assert.Contains(request.InternalConductances, link =>
            link.FromNodeId == "surface:slab" &&
            link.ToNodeId == "mass:slab" &&
            Math.Abs(link.ConductanceWPerK - 50.0) < 1e-9);

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:wall-north" &&
            boundary.BoundaryId == "outdoor" &&
            Math.Abs(boundary.ConductanceWPerK - 50.0) < 1e-9);

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:slab" &&
            boundary.BoundaryId == "ground" &&
            Math.Abs(boundary.ConductanceWPerK - 25.0) < 1e-9);

        Assert.Contains(request.BoundaryConductances, boundary =>
            boundary.NodeId == "air" &&
            boundary.BoundaryId == "ventilation-air" &&
            Math.Abs(boundary.ConductanceWPerK - 25.0) < 1e-9);

        var firstHour = request.Hours[0];

        Assert.Equal(7.0, firstHour.BoundaryTemperaturesC["outdoor"], precision: 6);
        Assert.Equal(12.0, firstHour.BoundaryTemperaturesC["ground"], precision: 6);
        Assert.Equal(7.0, firstHour.BoundaryTemperaturesC["ventilation-air"], precision: 6);
        Assert.Equal(100.0, firstHour.NodeHeatGainsW["air"], precision: 6);
        Assert.Equal(333.33333333333331, firstHour.NodeHeatGainsW["surface:wall-north"], precision: 6);
        Assert.Equal(166.66666666666666, firstHour.NodeHeatGainsW["surface:slab"], precision: 6);
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

        Assert.True(result.IsSuccess);

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

        Assert.True(result.IsSuccess);

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
    public void Build_WithoutSurfaces_KeepsStep01ThreeNodeFallback()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 21,
                    TimeStepSeconds: 3600)));

        Assert.True(result.IsSuccess);
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

    private static IReadOnlyList<Iso52016PhysicalSurface> CreateOutdoorAndGroundSurfaces() =>
        new[]
        {
            CreateSurface(
                surfaceId: "wall-north",
                boundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                areaM2: 10,
                thicknessM: 0.20,
                conductivityWPerMK: 1.0,
                densityKgPerM3: 1000,
                specificHeatJPerKgK: 1000),
            CreateSurface(
                surfaceId: "slab",
                boundaryType: Iso52016PhysicalSurfaceBoundaryType.Ground,
                areaM2: 5,
                thicknessM: 0.10,
                conductivityWPerMK: 0.5,
                densityKgPerM3: 2000,
                specificHeatJPerKgK: 1000)
        };

    private static Iso52016PhysicalSurface CreateSurface(
        string surfaceId,
        Iso52016PhysicalSurfaceBoundaryType boundaryType,
        double areaM2,
        double thicknessM = 0.20,
        double conductivityWPerMK = 1.0,
        double densityKgPerM3 = 1000,
        double specificHeatJPerKgK = 1000,
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
            SolarGainsDistributionFraction: solarFraction,
            InternalRadiativeGainsDistributionFraction: radiativeFraction,
            AdjacentBoundaryTemperatureC: adjacentBoundaryTemperatureC);
}