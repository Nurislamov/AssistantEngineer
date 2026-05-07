using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalZoneBoundaryAggregationTests
{
    [Fact]
    public void AggregatesByRoomZoneAndBuilding()
    {
        var calculator = CreateCalculator();
        var topology = new BuildingThermalTopology(
            BuildingId: "BLD-AGG-01",
            Zones: [new ThermalTopologyZone("ZONE-A", "Zone A", ["ROOM-1", "ROOM-2"], [])],
            Rooms:
            [
                new ThermalTopologyRoom("ROOM-1", "ZONE-A", 70.0, 24.0, [], []),
                new ThermalTopologyRoom("ROOM-2", "ZONE-A", 65.0, 22.0, [], [])
            ],
            Surfaces:
            [
                new ThermalTopologySurface("S-1", "ROOM-1", "ZONE-A", ThermalBoundaryKind.Outdoor, 10.0, 0.3, null, null, "EW", []),
                new ThermalTopologySurface("S-2", "ROOM-2", "ZONE-A", ThermalBoundaryKind.Ground, 12.0, 0.25, null, null, "Slab", [])
            ],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: 4.0,
            GroundTemperatureCelsius: 11.0,
            DisclosureOverride: null));

        var room1 = result.Zones.Single().Rooms.Single(room => room.RoomId == "ROOM-1");
        var room2 = result.Zones.Single().Rooms.Single(room => room.RoomId == "ROOM-2");

        Assert.Equal(3.0, room1.TotalHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(3.0, room1.OutdoorHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(3.0, room2.TotalHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(3.0, room2.GroundHeatTransferCoefficientWPerKelvin, 6);

        Assert.Equal(6.0, result.Zones.Single().TotalHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(6.0, result.TotalHeatTransferCoefficientWPerKelvin, 6);
    }

    [Fact]
    public void SeparatesBoundaryCategoryTotals()
    {
        var calculator = CreateCalculator();
        var topology = new BuildingThermalTopology(
            BuildingId: "BLD-AGG-02",
            Zones:
            [
                new ThermalTopologyZone("ZONE-A", "Zone A", ["ROOM-A"], []),
                new ThermalTopologyZone("ZONE-B", "Zone B", ["ROOM-B"], [])
            ],
            Rooms:
            [
                new ThermalTopologyRoom("ROOM-A", "ZONE-A", 80.0, 26.0, [], []),
                new ThermalTopologyRoom("ROOM-B", "ZONE-B", 75.0, 25.0, [], [])
            ],
            Surfaces:
            [
                new ThermalTopologySurface("S-OUT", "ROOM-A", "ZONE-A", ThermalBoundaryKind.Outdoor, 10.0, 0.3, null, null, "EW", []),
                new ThermalTopologySurface("S-GRD", "ROOM-A", "ZONE-A", ThermalBoundaryKind.Ground, 20.0, 0.25, null, null, "Slab", []),
                new ThermalTopologySurface("S-ADJC", "ROOM-A", "ZONE-A", ThermalBoundaryKind.AdjacentConditionedZone, 5.0, 0.4, "ZONE-B", null, "Shared", []),
                new ThermalTopologySurface("S-ADJU", "ROOM-A", "ZONE-A", ThermalBoundaryKind.AdjacentUnconditionedZone, 8.0, 0.2, null, null, "Stairwell", []),
                new ThermalTopologySurface("S-INT", "ROOM-A", "ZONE-A", ThermalBoundaryKind.InternalPartition, 6.0, 0.5, "ZONE-B", null, "Core", []),
                new ThermalTopologySurface("S-ADI", "ROOM-A", "ZONE-A", ThermalBoundaryKind.Adiabatic, 4.0, 0.6, null, null, "Core", [])
            ],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["ZONE-A"] = 22.0,
                ["ZONE-B"] = 24.0
            },
            AdjacentUnconditionedTemperaturesCelsius: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["Stairwell"] = 15.0
            },
            OutdoorTemperatureCelsius: 3.0,
            GroundTemperatureCelsius: 12.0,
            DisclosureOverride: null));

        Assert.Equal(3.0, result.OutdoorHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(5.0, result.GroundHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(2.0, result.AdjacentConditionedHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(1.6, result.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(3.0, result.InternalPartitionHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(4.0, result.AdiabaticAreaSquareMeters, 6);
        Assert.Equal(14.6, result.TotalHeatTransferCoefficientWPerKelvin, 6);
    }

    [Fact]
    public void KeepsUnassignedRoomAndSurfaceBuckets()
    {
        var calculator = CreateCalculator();
        var topology = new BuildingThermalTopology(
            BuildingId: "BLD-AGG-03",
            Zones: [new ThermalTopologyZone("ZONE-A", "Zone A", ["ROOM-1"], [])],
            Rooms:
            [
                new ThermalTopologyRoom("ROOM-1", "ZONE-A", 70.0, 24.0, [], []),
                new ThermalTopologyRoom("ROOM-2", null, 50.0, 18.0, [], [])
            ],
            Surfaces:
            [
                new ThermalTopologySurface("S-R2", "ROOM-2", null, ThermalBoundaryKind.Outdoor, 8.0, 0.3, null, null, "EW", []),
                new ThermalTopologySurface("S-Z1", null, "ZONE-A", ThermalBoundaryKind.Ground, 6.0, 0.2, null, null, "Slab", []),
                new ThermalTopologySurface("S-UN", null, null, ThermalBoundaryKind.Outdoor, 4.0, 0.5, null, null, "EW", [])
            ],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: 4.0,
            GroundTemperatureCelsius: 10.0,
            DisclosureOverride: null));

        Assert.Contains(result.UnassignedRooms, room => room.RoomId == "ROOM-2");
        Assert.Contains(result.UnassignedSurfaces, surface => surface.SurfaceId == "S-UN");

        var zone = result.Zones.Single(zoneResult => zoneResult.ZoneId == "ZONE-A");
        Assert.Contains(zone.UnassignedSurfaces, surface => surface.SurfaceId == "S-Z1");
    }

    private static ThermalZoneBoundaryCalculator CreateCalculator()
    {
        var resolver = new ThermalBoundaryConditionResolver();
        var validator = new ThermalTopologyValidator(resolver);
        var disclosureFactory = new StandardCalculationDisclosureFactory();
        return new ThermalZoneBoundaryCalculator(resolver, validator, disclosureFactory);
    }
}
