using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalZoneBoundaryCalculatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    [Fact]
    public void CalculatesSimpleZoneBoundaryCoefficients()
    {
        var calculator = CreateCalculator();
        var topology = CreateSimpleTopology();

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: new Dictionary<string, double>(StringComparer.Ordinal) { ["ZONE-A"] = 22.0 },
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: 5.0,
            GroundTemperatureCelsius: 12.0,
            DisclosureOverride: null));

        Assert.Equal(8.0, result.TotalHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(3.0, result.OutdoorHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(5.0, result.GroundHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(5.0, result.AdiabaticAreaSquareMeters, 6);

        var adiabaticSurface = result.Zones
            .Single()
            .Rooms
            .Single()
            .Surfaces
            .Single(surface => surface.BoundaryKind == ThermalBoundaryKind.Adiabatic);
        Assert.Equal(0.0, adiabaticSurface.HeatTransferCoefficientWPerKelvin.GetValueOrDefault(), 6);
    }

    [Fact]
    public void ResolvesOutdoorAndGroundTemperatures()
    {
        var calculator = CreateCalculator();
        var topology = CreateSimpleTopology();

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: new Dictionary<string, double>(StringComparer.Ordinal) { ["ZONE-A"] = 22.0 },
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: -2.0,
            GroundTemperatureCelsius: 11.5,
            DisclosureOverride: null));

        var zoneSurfaces = result.Zones.Single().Rooms.Single().Surfaces;
        Assert.Equal(-2.0, zoneSurfaces.Single(surface => surface.BoundaryKind == ThermalBoundaryKind.Outdoor).BoundaryTemperatureCelsius);
        Assert.Equal(11.5, zoneSurfaces.Single(surface => surface.BoundaryKind == ThermalBoundaryKind.Ground).BoundaryTemperatureCelsius);
    }

    [Fact]
    public void ResolvesAdjacentConditionedZoneTemperature()
    {
        var calculator = CreateCalculator();
        var topology = CreateAdjacentConditionedTopology(useAdjacentRoom: false);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["ZONE-A"] = 22.0,
                ["ZONE-B"] = 26.0
            },
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: null,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        var surface = result.Zones
            .Single(zone => zone.ZoneId == "ZONE-A")
            .Rooms
            .Single()
            .Surfaces
            .Single();

        Assert.Equal(26.0, surface.AdjacentTemperatureCelsius);
        Assert.Equal(26.0, surface.BoundaryTemperatureCelsius);
    }

    [Fact]
    public void ResolvesAdjacentConditionedTemperatureThroughAdjacentRoom()
    {
        var calculator = CreateCalculator();
        var topology = CreateAdjacentConditionedTopology(useAdjacentRoom: true);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["ZONE-A"] = 21.0,
                ["ZONE-B"] = 25.0
            },
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: null,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        var surface = result.Zones
            .Single(zone => zone.ZoneId == "ZONE-A")
            .Rooms
            .Single()
            .Surfaces
            .Single();

        Assert.Equal(25.0, surface.AdjacentTemperatureCelsius);
        Assert.Equal(25.0, surface.BoundaryTemperatureCelsius);
    }

    [Fact]
    public void ResolvesAdjacentUnconditionedTemperatureFromBoundarySource()
    {
        var calculator = CreateCalculator();
        var topology = CreateSingleBoundaryTopology(
            ThermalBoundaryKind.AdjacentUnconditionedZone,
            "S-UNCOND",
            area: 6.0,
            uValue: 0.4,
            boundarySource: "Stairwell");

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["Stairwell"] = 16.0
            },
            OutdoorTemperatureCelsius: null,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        var surface = result.Zones.Single().Rooms.Single().Surfaces.Single();
        Assert.Equal(16.0, surface.BoundaryTemperatureCelsius);
        Assert.Equal(16.0, surface.AdjacentTemperatureCelsius);
    }

    [Fact]
    public void MissingOutdoorTemperatureProducesDiagnostic()
    {
        var calculator = CreateCalculator();
        var topology = CreateSingleBoundaryTopology(ThermalBoundaryKind.Outdoor, "S-OUT", 8.0, 0.3);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: null,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-ZONES-OUTDOOR-TEMPERATURE-MISSING");
    }

    [Fact]
    public void MissingGroundTemperatureProducesDiagnostic()
    {
        var calculator = CreateCalculator();
        var topology = CreateSingleBoundaryTopology(ThermalBoundaryKind.Ground, "S-GRD", 8.0, 0.3);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: null,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-ZONES-GROUND-TEMPERATURE-MISSING");
    }

    [Fact]
    public void MissingUValueProducesDiagnostic()
    {
        var calculator = CreateCalculator();
        var topology = CreateSingleBoundaryTopology(ThermalBoundaryKind.Outdoor, "S-NOU", 8.0, null);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: 5.0,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-ZONES-SURFACE-UVALUE-MISSING");
    }

    [Fact]
    public void AdjacentConditionedMissingReferences_PreservesDiagnosticsOrder()
    {
        var calculator = CreateCalculator();
        var topology = CreateSingleBoundaryTopology(
            ThermalBoundaryKind.AdjacentConditionedZone,
            "S-ADJ-MISSING",
            area: 8.0,
            uValue: 0.3);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: null,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        var orderedCodes = result.Diagnostics
            .Where(diagnostic =>
                diagnostic.Code is "Topology.Resolver.AdjacentConditionedMissingReference" or
                "AE-ZONES-ADJACENT-ZONE-TEMPERATURE-MISSING" or
                "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING")
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        var firstResolver = Array.IndexOf(orderedCodes, "Topology.Resolver.AdjacentConditionedMissingReference");
        var firstAdjacentMissing = Array.IndexOf(orderedCodes, "AE-ZONES-ADJACENT-ZONE-TEMPERATURE-MISSING");
        var firstSourceMissing = Array.IndexOf(orderedCodes, "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING");

        Assert.True(firstResolver >= 0, "Resolver diagnostic is missing.");
        Assert.True(firstAdjacentMissing >= 0, "Adjacent-conditioned diagnostic is missing.");
        Assert.True(firstSourceMissing >= 0, "Source-zone diagnostic is missing.");
        Assert.True(firstResolver < firstAdjacentMissing, "Resolver diagnostic must appear before adjacent-conditioned diagnostic.");
        Assert.True(firstAdjacentMissing < firstSourceMissing, "Adjacent-conditioned diagnostic must appear before source-zone diagnostic.");
    }

    [Fact]
    public void NonPositiveAreaProducesDiagnosticAndNoCoefficient()
    {
        var calculator = CreateCalculator();
        var topology = CreateSingleBoundaryTopology(ThermalBoundaryKind.Outdoor, "S-ZERO-A", 0.0, 0.3);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: 5.0,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-ZONES-SURFACE-AREA-NONPOSITIVE");
        var surface = result.Zones.Single().Rooms.Single().Surfaces.Single();
        Assert.Null(surface.HeatTransferCoefficientWPerKelvin);
    }

    [Fact]
    public void OtherBoundaryDoesNotFallbackToOutdoor()
    {
        var calculator = CreateCalculator();
        var topology = CreateSingleBoundaryTopology(ThermalBoundaryKind.Other, "S-OTHER", 8.0, 0.3);

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: 5.0,
            GroundTemperatureCelsius: null,
            DisclosureOverride: null));

        var surface = result.Zones.Single().Rooms.Single().Surfaces.Single();
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-ZONES-BOUNDARY-OTHER-UNSUPPORTED");
        Assert.Equal(ThermalBoundaryKind.Other, surface.BoundaryKind);
        Assert.Null(surface.BoundaryTemperatureCelsius);
    }

    [Fact]
    public void BuildingDisclosureKeepsForbiddenClaims()
    {
        var calculator = CreateCalculator();
        var topology = CreateSimpleTopology();

        var result = calculator.Calculate(new ThermalZoneBoundaryCalculationInput(
            Topology: topology,
            ZoneAirTemperaturesCelsius: null,
            AdjacentUnconditionedTemperaturesCelsius: null,
            OutdoorTemperatureCelsius: 5.0,
            GroundTemperatureCelsius: 10.0,
            DisclosureOverride: null));

        foreach (var forbiddenClaim in RequiredForbiddenClaims)
        {
            Assert.Contains(
                forbiddenClaim,
                result.Disclosure.ClaimBoundary.ForbiddenClaims,
                StringComparer.Ordinal);
            Assert.DoesNotContain(
                forbiddenClaim,
                result.Disclosure.ClaimBoundary.AllowedClaims,
                StringComparer.Ordinal);
        }
    }

    private static ThermalZoneBoundaryCalculator CreateCalculator()
    {
        var resolver = new ThermalBoundaryConditionResolver();
        var validator = new ThermalTopologyValidator(resolver);
        var disclosureFactory = new StandardCalculationDisclosureFactory();
        return new ThermalZoneBoundaryCalculator(resolver, validator, disclosureFactory);
    }

    private static BuildingThermalTopology CreateSimpleTopology()
    {
        var surfaces = new[]
        {
            new ThermalTopologySurface(
                SurfaceId: "S-OUT",
                RoomId: "ROOM-A",
                ZoneId: "ZONE-A",
                BoundaryKind: ThermalBoundaryKind.Outdoor,
                AreaSquareMeters: 10.0,
                UValueWPerSquareMeterKelvin: 0.3,
                AdjacentZoneId: null,
                AdjacentRoomId: null,
                BoundarySource: "ExternalWall",
                Diagnostics: []),
            new ThermalTopologySurface(
                SurfaceId: "S-GRD",
                RoomId: "ROOM-A",
                ZoneId: "ZONE-A",
                BoundaryKind: ThermalBoundaryKind.Ground,
                AreaSquareMeters: 20.0,
                UValueWPerSquareMeterKelvin: 0.25,
                AdjacentZoneId: null,
                AdjacentRoomId: null,
                BoundarySource: "SlabOnGround",
                Diagnostics: []),
            new ThermalTopologySurface(
                SurfaceId: "S-ADI",
                RoomId: "ROOM-A",
                ZoneId: "ZONE-A",
                BoundaryKind: ThermalBoundaryKind.Adiabatic,
                AreaSquareMeters: 5.0,
                UValueWPerSquareMeterKelvin: 0.5,
                AdjacentZoneId: null,
                AdjacentRoomId: null,
                BoundarySource: "CoreWall",
                Diagnostics: [])
        };

        return new BuildingThermalTopology(
            BuildingId: "BLD-01",
            Zones:
            [
                new ThermalTopologyZone(
                    ZoneId: "ZONE-A",
                    Name: "Zone A",
                    RoomIds: ["ROOM-A"],
                    Diagnostics: [])
            ],
            Rooms:
            [
                new ThermalTopologyRoom(
                    RoomId: "ROOM-A",
                    ZoneId: "ZONE-A",
                    VolumeCubicMeters: 80.0,
                    FloorAreaSquareMeters: 25.0,
                    Surfaces: surfaces,
                    Diagnostics: [])
            ],
            Surfaces: surfaces,
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);
    }

    private static BuildingThermalTopology CreateAdjacentConditionedTopology(bool useAdjacentRoom)
    {
        var surface = new ThermalTopologySurface(
            SurfaceId: "S-ADJ",
            RoomId: "ROOM-A",
            ZoneId: "ZONE-A",
            BoundaryKind: ThermalBoundaryKind.AdjacentConditionedZone,
            AreaSquareMeters: 9.0,
            UValueWPerSquareMeterKelvin: 0.4,
            AdjacentZoneId: useAdjacentRoom ? null : "ZONE-B",
            AdjacentRoomId: useAdjacentRoom ? "ROOM-B" : null,
            BoundarySource: "SharedWall",
            Diagnostics: []);

        return new BuildingThermalTopology(
            BuildingId: "BLD-ADJ",
            Zones:
            [
                new ThermalTopologyZone("ZONE-A", "Zone A", ["ROOM-A"], []),
                new ThermalTopologyZone("ZONE-B", "Zone B", ["ROOM-B"], [])
            ],
            Rooms:
            [
                new ThermalTopologyRoom("ROOM-A", "ZONE-A", 80.0, 24.0, [surface], []),
                new ThermalTopologyRoom("ROOM-B", "ZONE-B", 75.0, 23.0, [], [])
            ],
            Surfaces: [surface],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);
    }

    private static BuildingThermalTopology CreateSingleBoundaryTopology(
        ThermalBoundaryKind boundaryKind,
        string surfaceId,
        double area,
        double? uValue,
        string? boundarySource = null)
    {
        var surface = new ThermalTopologySurface(
            SurfaceId: surfaceId,
            RoomId: "ROOM-A",
            ZoneId: "ZONE-A",
            BoundaryKind: boundaryKind,
            AreaSquareMeters: area,
            UValueWPerSquareMeterKelvin: uValue,
            AdjacentZoneId: null,
            AdjacentRoomId: null,
            BoundarySource: boundarySource,
            Diagnostics: []);

        return new BuildingThermalTopology(
            BuildingId: "BLD-SINGLE",
            Zones: [new ThermalTopologyZone("ZONE-A", "Zone A", ["ROOM-A"], [])],
            Rooms: [new ThermalTopologyRoom("ROOM-A", "ZONE-A", 50.0, 18.0, [surface], [])],
            Surfaces: [surface],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);
    }
}
