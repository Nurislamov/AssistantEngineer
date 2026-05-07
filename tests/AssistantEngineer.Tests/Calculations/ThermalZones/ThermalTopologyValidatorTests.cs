using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalTopologyValidatorTests
{
    private readonly ThermalTopologyValidator _validator = new(new ThermalBoundaryConditionResolver());
    private readonly StandardCalculationDisclosureFactory _disclosureFactory = new();

    [Fact]
    public void Validate_AcceptsValidSimpleTopology()
    {
        var topology = CreateValidTopology();

        var result = _validator.Validate(topology);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_RejectsDuplicateZoneId()
    {
        var topology = CreateValidTopology() with
        {
            Zones =
            [
                new ThermalTopologyZone("ZONE-01", "Zone 1", ["ROOM-01"], []),
                new ThermalTopologyZone("ZONE-01", "Zone 1 duplicate", ["ROOM-01"], [])
            ]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Validator.DuplicateZoneId");
    }

    [Fact]
    public void Validate_RejectsDuplicateRoomId()
    {
        var topology = CreateValidTopology() with
        {
            Rooms =
            [
                new ThermalTopologyRoom("ROOM-01", "ZONE-01", 80.0, 28.0, [], []),
                new ThermalTopologyRoom("ROOM-01", "ZONE-01", 60.0, 22.0, [], [])
            ]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Validator.DuplicateRoomId");
    }

    [Fact]
    public void Validate_RejectsDuplicateSurfaceId()
    {
        var baseSurface = CreateValidTopology().Surfaces.Single();
        var topology = CreateValidTopology() with
        {
            Surfaces = [baseSurface, baseSurface with { SurfaceId = "SURF-01" }]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Validator.DuplicateSurfaceId");
    }

    [Fact]
    public void Validate_ReportsMissingZoneForRoom()
    {
        var topology = CreateValidTopology() with
        {
            Rooms =
            [
                new ThermalTopologyRoom("ROOM-01", "ZONE-MISSING", 80.0, 28.0, [], [])
            ]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Validator.RoomZoneMissing");
    }

    [Fact]
    public void Validate_ReportsMissingRoomForSurface()
    {
        var baseSurface = CreateValidTopology().Surfaces.Single();
        var topology = CreateValidTopology() with
        {
            Surfaces = [baseSurface with { RoomId = "ROOM-MISSING" }]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Validator.SurfaceRoomMissing");
    }

    [Fact]
    public void Validate_ReportsMissingAdjacentReferenceForAdjacentConditionedBoundary()
    {
        var baseSurface = CreateValidTopology().Surfaces.Single();
        var topology = CreateValidTopology() with
        {
            Surfaces =
            [
                baseSurface with
                {
                    BoundaryKind = ThermalBoundaryKind.AdjacentConditionedZone,
                    AdjacentZoneId = null,
                    AdjacentRoomId = null
                }
            ]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Resolver.AdjacentConditionedMissingReference");
    }

    [Fact]
    public void Validate_ReportsNonPositiveArea()
    {
        var baseSurface = CreateValidTopology().Surfaces.Single();
        var topology = CreateValidTopology() with
        {
            Surfaces = [baseSurface with { AreaSquareMeters = 0.0 }]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Validator.SurfaceAreaNonPositive");
    }

    [Fact]
    public void Validate_ReportsNonPositiveUValueWhenPresent()
    {
        var baseSurface = CreateValidTopology().Surfaces.Single();
        var topology = CreateValidTopology() with
        {
            Surfaces = [baseSurface with { UValueWPerSquareMeterKelvin = 0.0 }]
        };

        var result = _validator.Validate(topology);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Topology.Validator.SurfaceUValueNonPositive");
    }

    private BuildingThermalTopology CreateValidTopology()
    {
        var surface = new ThermalTopologySurface(
            SurfaceId: "SURF-01",
            RoomId: "ROOM-01",
            ZoneId: "ZONE-01",
            BoundaryKind: ThermalBoundaryKind.Outdoor,
            AreaSquareMeters: 12.0,
            UValueWPerSquareMeterKelvin: 0.35,
            AdjacentZoneId: null,
            AdjacentRoomId: null,
            BoundarySource: "ExternalWall",
            Diagnostics: []);

        var room = new ThermalTopologyRoom(
            RoomId: "ROOM-01",
            ZoneId: "ZONE-01",
            VolumeCubicMeters: 80.0,
            FloorAreaSquareMeters: 28.0,
            Surfaces: [surface],
            Diagnostics: []);

        return new BuildingThermalTopology(
            BuildingId: "BLD-VAL-01",
            Zones:
            [
                new ThermalTopologyZone(
                    ZoneId: "ZONE-01",
                    Name: "Main zone",
                    RoomIds: ["ROOM-01"],
                    Diagnostics: [])
            ],
            Rooms: [room],
            Surfaces: [surface],
            Disclosure: _disclosureFactory.CreateThermalZonesDisclosure(),
            Diagnostics: []);
    }
}
