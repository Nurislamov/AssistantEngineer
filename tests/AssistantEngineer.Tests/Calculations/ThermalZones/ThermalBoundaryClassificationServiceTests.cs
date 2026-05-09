using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.ThermalZones;

public sealed class ThermalBoundaryClassificationServiceTests
{
    private readonly ThermalBoundaryClassificationService _service = new();

    [Fact]
    public void ClassifiesExteriorBoundaryCorrectly()
    {
        var request = BuildRequest(new ThermalBoundaryDefinition(
            BoundaryId: "B-EXT",
            SourceZoneId: "ZONE-A",
            AdjacentZoneId: null,
            ExposureKind: BoundaryExposureKind.ExteriorAir,
            ElementKind: BoundaryElementKind.Wall,
            AreaSquareMeters: 10.0,
            UValueWPerSquareMeterKelvin: 0.4));

        var result = _service.Classify(request);

        Assert.True(result.IsValid);
        var boundary = Assert.Single(result.Boundaries);
        Assert.True(boundary.RequiresExteriorTemperature);
        Assert.False(boundary.RequiresGroundTemperature);
        Assert.False(boundary.IsAdiabaticEquivalent);
    }

    [Fact]
    public void ClassifiesGroundBoundaryCorrectly()
    {
        var request = BuildRequest(new ThermalBoundaryDefinition(
            BoundaryId: "B-GROUND",
            SourceZoneId: "ZONE-A",
            AdjacentZoneId: null,
            ExposureKind: BoundaryExposureKind.Ground,
            ElementKind: BoundaryElementKind.Slab,
            AreaSquareMeters: 12.0,
            UValueWPerSquareMeterKelvin: 0.3));

        var result = _service.Classify(request);

        Assert.True(result.IsValid);
        var boundary = Assert.Single(result.Boundaries);
        Assert.True(boundary.RequiresGroundTemperature);
        Assert.False(boundary.RequiresExteriorTemperature);
    }

    [Fact]
    public void ClassifiesAdjacentUnconditionedBoundaryCorrectly()
    {
        var request = BuildRequest(
            new ThermalBoundaryDefinition(
                BoundaryId: "B-ADJ-UNCOND",
                SourceZoneId: "ZONE-A",
                AdjacentZoneId: "ZONE-U",
                ExposureKind: BoundaryExposureKind.AdjacentUnconditionedZone,
                ElementKind: BoundaryElementKind.Wall,
                AreaSquareMeters: 8.0,
                UValueWPerSquareMeterKelvin: 0.45),
            new ThermalZoneDefinition(
                ZoneId: "ZONE-U",
                Name: "Unconditioned",
                Kind: ThermalZoneKind.Unconditioned,
                FloorAreaSquareMeters: 20.0,
                VolumeCubicMeters: 50.0,
                HeatingSetpointProfileId: null,
                CoolingSetpointProfileId: null,
                Boundaries: []));

        var result = _service.Classify(request);

        Assert.True(result.IsValid);
        var boundary = result.Boundaries.Single(item => item.BoundaryId == "B-ADJ-UNCOND");
        Assert.True(boundary.RequiresAdjacentUnconditionedTemperature);
        Assert.False(boundary.RequiresExteriorTemperature);
    }

    [Fact]
    public void RejectsAdjacentBoundaryWithMissingZone()
    {
        var request = BuildRequest(new ThermalBoundaryDefinition(
            BoundaryId: "B-MISSING",
            SourceZoneId: "ZONE-A",
            AdjacentZoneId: "ZONE-NOT-FOUND",
            ExposureKind: BoundaryExposureKind.AdjacentConditionedZone,
            ElementKind: BoundaryElementKind.InternalPartition,
            AreaSquareMeters: 6.0,
            UValueWPerSquareMeterKelvin: 0.5));

        var result = _service.Classify(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "Topology.Classification.AdjacentZoneMissing");
    }

    [Fact]
    public void RejectsSelfReference()
    {
        var request = BuildRequest(new ThermalBoundaryDefinition(
            BoundaryId: "B-SELF",
            SourceZoneId: "ZONE-A",
            AdjacentZoneId: "ZONE-A",
            ExposureKind: BoundaryExposureKind.AdjacentConditionedZone,
            ElementKind: BoundaryElementKind.InternalPartition,
            AreaSquareMeters: 6.0,
            UValueWPerSquareMeterKelvin: 0.5));

        var result = _service.Classify(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "Topology.Classification.BoundarySelfReference");
    }

    [Fact]
    public void RejectsExteriorBoundaryWithAdjacentZoneId()
    {
        var request = BuildRequest(
            new ThermalBoundaryDefinition(
                BoundaryId: "B-EXT-INVALID",
                SourceZoneId: "ZONE-A",
                AdjacentZoneId: "ZONE-B",
                ExposureKind: BoundaryExposureKind.ExteriorAir,
                ElementKind: BoundaryElementKind.Wall,
                AreaSquareMeters: 9.0,
                UValueWPerSquareMeterKelvin: 0.3),
            new ThermalZoneDefinition(
                ZoneId: "ZONE-B",
                Name: "B",
                Kind: ThermalZoneKind.Conditioned,
                FloorAreaSquareMeters: 20.0,
                VolumeCubicMeters: 50.0,
                HeatingSetpointProfileId: null,
                CoolingSetpointProfileId: null,
                Boundaries: []));

        var result = _service.Classify(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "Topology.Classification.ExteriorBoundaryAdjacentZoneForbidden");
    }

    [Fact]
    public void RejectsGroundBoundaryWithAdjacentZoneId()
    {
        var request = BuildRequest(
            new ThermalBoundaryDefinition(
                BoundaryId: "B-GROUND-INVALID",
                SourceZoneId: "ZONE-A",
                AdjacentZoneId: "ZONE-B",
                ExposureKind: BoundaryExposureKind.Ground,
                ElementKind: BoundaryElementKind.Slab,
                AreaSquareMeters: 9.0,
                UValueWPerSquareMeterKelvin: 0.3),
            new ThermalZoneDefinition(
                ZoneId: "ZONE-B",
                Name: "B",
                Kind: ThermalZoneKind.Conditioned,
                FloorAreaSquareMeters: 20.0,
                VolumeCubicMeters: 50.0,
                HeatingSetpointProfileId: null,
                CoolingSetpointProfileId: null,
                Boundaries: []));

        var result = _service.Classify(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "Topology.Classification.GroundBoundaryAdjacentZoneForbidden");
    }

    [Fact]
    public void SameUseAdjacentBoundary_TreatedAsAdiabaticStyleByPolicy()
    {
        var request = BuildRequest(
            new ThermalBoundaryDefinition(
                BoundaryId: "B-SAME-USE",
                SourceZoneId: "ZONE-A",
                AdjacentZoneId: "ZONE-B",
                ExposureKind: BoundaryExposureKind.SameUseAdjacentZone,
                ElementKind: BoundaryElementKind.InternalPartition,
                AreaSquareMeters: 7.0,
                UValueWPerSquareMeterKelvin: 0.4),
            new ThermalZoneDefinition(
                ZoneId: "ZONE-B",
                Name: "B",
                Kind: ThermalZoneKind.Conditioned,
                FloorAreaSquareMeters: 20.0,
                VolumeCubicMeters: 50.0,
                HeatingSetpointProfileId: null,
                CoolingSetpointProfileId: null,
                Boundaries: []));

        var result = _service.Classify(request);

        Assert.True(result.IsValid);
        var boundary = result.Boundaries.Single(item => item.BoundaryId == "B-SAME-USE");
        Assert.True(boundary.IsAdiabaticEquivalent);
        Assert.Equal(0.0, boundary.ConductanceWPerKelvin);
        Assert.Contains(result.Diagnostics, item => item.Code == "Topology.Classification.SameUseAdjacentAdiabaticPolicyApplied");
    }

    [Fact]
    public void ValidationDiagnostics_AreDeterministicAndSorted()
    {
        var request = BuildRequest(new ThermalBoundaryDefinition(
            BoundaryId: "B-INVALID",
            SourceZoneId: "ZONE-A",
            AdjacentZoneId: null,
            ExposureKind: BoundaryExposureKind.AdjacentConditionedZone,
            ElementKind: BoundaryElementKind.InternalPartition,
            AreaSquareMeters: -1.0,
            UValueWPerSquareMeterKelvin: -0.1));

        var first = _service.Classify(request).Diagnostics.Select(item => item.Code).ToArray();
        var second = _service.Classify(request).Diagnostics.Select(item => item.Code).ToArray();

        Assert.Equal(first, second);
        Assert.True(first.SequenceEqual(first.OrderBy(item => item, StringComparer.Ordinal)) || first.Length <= 1);
    }

    private static ThermalBoundaryClassificationRequest BuildRequest(
        ThermalBoundaryDefinition boundary,
        params ThermalZoneDefinition[] extraZones)
    {
        var zones = new List<ThermalZoneDefinition>
        {
            new(
                ZoneId: "ZONE-A",
                Name: "A",
                Kind: ThermalZoneKind.Conditioned,
                FloorAreaSquareMeters: 30.0,
                VolumeCubicMeters: 75.0,
                HeatingSetpointProfileId: "SP-HEAT-A",
                CoolingSetpointProfileId: "SP-COOL-A",
                Boundaries: [boundary])
        };

        zones.AddRange(extraZones);

        return new ThermalBoundaryClassificationRequest(Zones: zones);
    }
}
