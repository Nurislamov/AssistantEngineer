using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalZoneBoundaryClassifierTests
{
    [Fact]
    public void Classify_OutdoorBoundary_UsesOutdoorTemperature()
    {
        var surface = CreateSurface(
            surfaceId: "S-OUT",
            boundaryKind: ThermalBoundaryKind.Outdoor);

        var result = ThermalZoneBoundaryClassifier.Classify(
            surface,
            roomsById: new Dictionary<string, ThermalTopologyRoom>(StringComparer.Ordinal),
            zoneTemperatures: new Dictionary<string, double>(StringComparer.Ordinal),
            adjacentUnconditionedTemperatures: new Dictionary<string, double>(StringComparer.Ordinal),
            outdoorTemperatureCelsius: 4.5,
            groundTemperatureCelsius: null,
            isResolved: true);

        Assert.True(result.IsResolved);
        Assert.Equal(4.5, result.BoundaryTemperatureCelsius);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Classify_AdjacentConditionedBoundary_ResolvesAdjacentZoneTemperature()
    {
        var surface = CreateSurface(
            surfaceId: "S-ADJ-COND",
            boundaryKind: ThermalBoundaryKind.AdjacentConditionedZone,
            adjacentZoneId: "ZONE-B");

        var result = ThermalZoneBoundaryClassifier.Classify(
            surface,
            roomsById: new Dictionary<string, ThermalTopologyRoom>(StringComparer.Ordinal),
            zoneTemperatures: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["ZONE-A"] = 21.0,
                ["ZONE-B"] = 25.0
            },
            adjacentUnconditionedTemperatures: new Dictionary<string, double>(StringComparer.Ordinal),
            outdoorTemperatureCelsius: null,
            groundTemperatureCelsius: null,
            isResolved: true);

        Assert.True(result.IsResolved);
        Assert.Equal(25.0, result.AdjacentTemperatureCelsius);
        Assert.Equal(25.0, result.BoundaryTemperatureCelsius);
        Assert.Equal(21.0, result.SourceZoneTemperatureCelsius);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Classify_AdjacentUnconditionedBoundary_ResolvesBoundarySourceTemperature()
    {
        var surface = CreateSurface(
            surfaceId: "S-ADJ-UNCOND",
            boundaryKind: ThermalBoundaryKind.AdjacentUnconditionedZone,
            boundarySource: "Stairwell");

        var result = ThermalZoneBoundaryClassifier.Classify(
            surface,
            roomsById: new Dictionary<string, ThermalTopologyRoom>(StringComparer.Ordinal),
            zoneTemperatures: new Dictionary<string, double>(StringComparer.Ordinal),
            adjacentUnconditionedTemperatures: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["Stairwell"] = 16.0
            },
            outdoorTemperatureCelsius: null,
            groundTemperatureCelsius: null,
            isResolved: true);

        Assert.True(result.IsResolved);
        Assert.Equal(16.0, result.AdjacentTemperatureCelsius);
        Assert.Equal(16.0, result.BoundaryTemperatureCelsius);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Classify_AdjacentConditionedBoundary_MissingAdjacencyPreservesDiagnosticOrder()
    {
        var surface = CreateSurface(
            surfaceId: "S-ADJ-MISSING",
            boundaryKind: ThermalBoundaryKind.AdjacentConditionedZone);

        var result = ThermalZoneBoundaryClassifier.Classify(
            surface,
            roomsById: new Dictionary<string, ThermalTopologyRoom>(StringComparer.Ordinal),
            zoneTemperatures: new Dictionary<string, double>(StringComparer.Ordinal),
            adjacentUnconditionedTemperatures: new Dictionary<string, double>(StringComparer.Ordinal),
            outdoorTemperatureCelsius: null,
            groundTemperatureCelsius: null,
            isResolved: false);

        Assert.False(result.IsResolved);
        Assert.Equal(
            ["AE-ZONES-ADJACENT-ZONE-TEMPERATURE-MISSING", "AE-ZONES-SOURCE-ZONE-TEMPERATURE-MISSING"],
            result.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray());
    }

    [Fact]
    public void Classify_InternalPartitionMissingAdjacency_ProducesExpectedDiagnostic()
    {
        var surface = CreateSurface(
            surfaceId: "S-INT-MISSING",
            boundaryKind: ThermalBoundaryKind.InternalPartition);

        var result = ThermalZoneBoundaryClassifier.Classify(
            surface,
            roomsById: new Dictionary<string, ThermalTopologyRoom>(StringComparer.Ordinal),
            zoneTemperatures: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["ZONE-A"] = 21.0
            },
            adjacentUnconditionedTemperatures: new Dictionary<string, double>(StringComparer.Ordinal),
            outdoorTemperatureCelsius: null,
            groundTemperatureCelsius: null,
            isResolved: false);

        Assert.False(result.IsResolved);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-ZONES-INTERNAL-PARTITION-UNRESOLVED");
    }

    private static ThermalTopologySurface CreateSurface(
        string surfaceId,
        ThermalBoundaryKind boundaryKind,
        string? adjacentZoneId = null,
        string? boundarySource = null) =>
        new(
            SurfaceId: surfaceId,
            RoomId: "ROOM-A",
            ZoneId: "ZONE-A",
            BoundaryKind: boundaryKind,
            AreaSquareMeters: 10.0,
            UValueWPerSquareMeterKelvin: 0.3,
            AdjacentZoneId: adjacentZoneId,
            AdjacentRoomId: null,
            BoundarySource: boundarySource,
            Diagnostics: []);
}
