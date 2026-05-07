using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalBoundaryConditionResolverTests
{
    private readonly ThermalBoundaryConditionResolver _resolver = new();
    private readonly StandardCalculationDisclosureFactory _disclosureFactory = new();

    [Fact]
    public void Resolve_Outdoor_RequiresOutdoorTemperature()
    {
        var topology = CreateTopology();
        var surface = CreateSurface(ThermalBoundaryKind.Outdoor);

        var result = _resolver.Resolve(surface, topology);

        Assert.True(result.IsHeatTransferBoundary);
        Assert.True(result.RequiresOutdoorTemperature);
        Assert.False(result.RequiresGroundTemperature);
    }

    [Fact]
    public void Resolve_Ground_RequiresGroundTemperature()
    {
        var topology = CreateTopology();
        var surface = CreateSurface(ThermalBoundaryKind.Ground);

        var result = _resolver.Resolve(surface, topology);

        Assert.True(result.IsHeatTransferBoundary);
        Assert.True(result.RequiresGroundTemperature);
        Assert.False(result.RequiresOutdoorTemperature);
    }

    [Fact]
    public void Resolve_AdjacentConditioned_RequiresAdjacentZoneTemperature()
    {
        var topology = CreateTopologyWithAdjacent();
        var surface = CreateSurface(
            ThermalBoundaryKind.AdjacentConditionedZone,
            adjacentZoneId: "ZONE-ADJ-01");

        var result = _resolver.Resolve(surface, topology);

        Assert.True(result.IsHeatTransferBoundary);
        Assert.True(result.RequiresAdjacentZoneTemperature);
        Assert.False(result.RequiresOutdoorTemperature);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Resolve_AdjacentUnconditioned_RequiresAdjacentUnconditionedTemperature()
    {
        var topology = CreateTopology();
        var surface = CreateSurface(
            ThermalBoundaryKind.AdjacentUnconditionedZone,
            boundarySource: "Stairwell");

        var result = _resolver.Resolve(surface, topology);

        Assert.True(result.IsHeatTransferBoundary);
        Assert.True(result.RequiresAdjacentUnconditionedTemperature);
        Assert.False(result.RequiresOutdoorTemperature);
    }

    [Fact]
    public void Resolve_Adiabatic_HasNoExternalTemperatureRequirement()
    {
        var topology = CreateTopology();
        var surface = CreateSurface(ThermalBoundaryKind.Adiabatic);

        var result = _resolver.Resolve(surface, topology);

        Assert.True(result.IsAdiabatic);
        Assert.False(result.RequiresOutdoorTemperature);
        Assert.False(result.RequiresGroundTemperature);
        Assert.False(result.RequiresAdjacentZoneTemperature);
    }

    [Fact]
    public void Resolve_InternalPartitionWithoutAdjacentReference_ProducesDiagnostic()
    {
        var topology = CreateTopology();
        var surface = CreateSurface(ThermalBoundaryKind.InternalPartition);

        var result = _resolver.Resolve(surface, topology);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "Topology.Resolver.InternalPartitionMissingReference");
    }

    [Fact]
    public void Resolve_Other_ProducesDiagnosticWithoutOutdoorFallback()
    {
        var topology = CreateTopology();
        var surface = CreateSurface(ThermalBoundaryKind.Other);

        var result = _resolver.Resolve(surface, topology);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "Topology.Resolver.OtherBoundaryKindRequiresExplicitResolution");
        Assert.False(result.RequiresOutdoorTemperature);
        Assert.False(result.RequiresGroundTemperature);
    }

    private BuildingThermalTopology CreateTopology() =>
        new(
            BuildingId: "BLD-RES-01",
            Zones:
            [
                new ThermalTopologyZone(
                    ZoneId: "ZONE-01",
                    Name: "Main zone",
                    RoomIds: ["ROOM-01"],
                    Diagnostics: [])
            ],
            Rooms:
            [
                new ThermalTopologyRoom(
                    RoomId: "ROOM-01",
                    ZoneId: "ZONE-01",
                    VolumeCubicMeters: 80.0,
                    FloorAreaSquareMeters: 28.0,
                    Surfaces: [],
                    Diagnostics: [])
            ],
            Surfaces: [],
            Disclosure: _disclosureFactory.CreateThermalZonesDisclosure(),
            Diagnostics: []);

    private BuildingThermalTopology CreateTopologyWithAdjacent() =>
        CreateTopology() with
        {
            Zones =
            [
                new ThermalTopologyZone(
                    ZoneId: "ZONE-01",
                    Name: "Main zone",
                    RoomIds: ["ROOM-01"],
                    Diagnostics: []),
                new ThermalTopologyZone(
                    ZoneId: "ZONE-ADJ-01",
                    Name: "Adjacent zone",
                    RoomIds: [],
                    Diagnostics: [])
            ]
        };

    private static ThermalTopologySurface CreateSurface(
        ThermalBoundaryKind kind,
        string? adjacentZoneId = null,
        string? adjacentRoomId = null,
        string? boundarySource = null) =>
        new(
            SurfaceId: $"SURF-{kind}",
            RoomId: "ROOM-01",
            ZoneId: "ZONE-01",
            BoundaryKind: kind,
            AreaSquareMeters: 12.0,
            UValueWPerSquareMeterKelvin: 0.35,
            AdjacentZoneId: adjacentZoneId,
            AdjacentRoomId: adjacentRoomId,
            BoundarySource: boundarySource,
            Diagnostics: []);
}
