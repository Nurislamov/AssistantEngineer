using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalTopologyBuilderTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    [Fact]
    public void Build_CreatesCanonicalTopologyAndAddsThermalZoneDisclosure()
    {
        var builder = new ThermalTopologyBuilder(new StandardCalculationDisclosureFactory());
        var input = new ThermalTopologyBuildInput(
            BuildingId: "BLD-01",
            Zones:
            [
                new ThermalTopologyZoneInput(
                    ZoneId: "ZONE-01",
                    Name: "Primary Zone",
                    RoomIds: ["ROOM-01"])
            ],
            Rooms:
            [
                new ThermalTopologyRoomInput(
                    RoomId: "ROOM-01",
                    ZoneId: "ZONE-01",
                    VolumeCubicMeters: 90.0,
                    FloorAreaSquareMeters: 30.0)
            ],
            Surfaces:
            [
                new ThermalTopologySurfaceInput(
                    SurfaceId: "SURF-OUT-01",
                    RoomId: "ROOM-01",
                    ZoneId: "ZONE-01",
                    BoundaryKind: ThermalBoundaryKind.Outdoor,
                    AreaSquareMeters: 12.0,
                    UValueWPerSquareMeterKelvin: 0.35,
                    AdjacentZoneId: null,
                    AdjacentRoomId: null,
                    BoundarySource: "ExternalWall"),
                new ThermalTopologySurfaceInput(
                    SurfaceId: "SURF-GRD-01",
                    RoomId: "ROOM-01",
                    ZoneId: "ZONE-01",
                    BoundaryKind: ThermalBoundaryKind.Ground,
                    AreaSquareMeters: 30.0,
                    UValueWPerSquareMeterKelvin: 0.25,
                    AdjacentZoneId: null,
                    AdjacentRoomId: null,
                    BoundarySource: "SlabOnGround"),
                new ThermalTopologySurfaceInput(
                    SurfaceId: "SURF-ADI-01",
                    RoomId: "ROOM-01",
                    ZoneId: "ZONE-01",
                    BoundaryKind: ThermalBoundaryKind.Adiabatic,
                    AreaSquareMeters: 8.0,
                    UValueWPerSquareMeterKelvin: null,
                    AdjacentZoneId: null,
                    AdjacentRoomId: null,
                    BoundarySource: "InternalPartition")
            ],
            DisclosureOverride: null);

        var topology = builder.Build(input);

        Assert.Equal("BLD-01", topology.BuildingId);
        Assert.Single(topology.Zones);
        Assert.Single(topology.Rooms);
        Assert.Equal(3, topology.Surfaces.Count);
        Assert.Equal("ThermalZones/AdjacentBoundaries/Foundation", topology.Disclosure.CalculationPath);
        Assert.Empty(topology.Diagnostics);
    }

    [Fact]
    public void Build_DisclosureKeepsForbiddenClaimsAndDoesNotPromoteThemAsAllowed()
    {
        var builder = new ThermalTopologyBuilder(new StandardCalculationDisclosureFactory());
        var topology = builder.Build(new ThermalTopologyBuildInput(
            BuildingId: "BLD-02",
            Zones: [],
            Rooms: [],
            Surfaces: [],
            DisclosureOverride: null));

        foreach (var forbiddenClaim in RequiredForbiddenClaims)
        {
            Assert.Contains(
                forbiddenClaim,
                topology.Disclosure.ClaimBoundary.ForbiddenClaims,
                StringComparer.Ordinal);

            Assert.DoesNotContain(
                forbiddenClaim,
                topology.Disclosure.ClaimBoundary.AllowedClaims,
                StringComparer.Ordinal);
        }
    }
}
