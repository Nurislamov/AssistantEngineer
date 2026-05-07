using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundBoundaryTopologyMapperTests
{
    private readonly GroundBoundaryTopologyMapper _mapper = new();

    [Fact]
    public void MapsGroundSurfaceToGroundBoundaryInput()
    {
        var topology = CreateTopology(
            buildingId: "B1",
            surfaceId: "S1",
            boundaryKind: ThermalBoundaryKind.Ground,
            area: 50.0,
            uValue: 0.25);
        var surface = topology.Surfaces.Single();
        var metadata = CreateMetadata(
            surfaceId: "S1",
            contactKind: GroundContactKind.SlabOnGround,
            area: 0.0,
            floorU: null,
            wallU: null);

        var input = _mapper.Map(topology, surface, metadata);

        Assert.Equal("S1", input.BoundaryId);
        Assert.Equal("B1", input.BuildingId);
        Assert.Equal(50.0, input.Geometry.AreaSquareMeters, 6);
        Assert.Equal(0.25, input.Geometry.FloorUValueWPerSquareMeterKelvin!.Value, 6);
    }

    [Fact]
    public void PreservesMetadataGeometryWhenProvided()
    {
        var topology = CreateTopology(
            buildingId: "B1",
            surfaceId: "S1",
            boundaryKind: ThermalBoundaryKind.Ground,
            area: 50.0,
            uValue: 0.25);
        var surface = topology.Surfaces.Single();
        var metadata = CreateMetadata(
            surfaceId: "S1",
            contactKind: GroundContactKind.SlabOnGround,
            area: 42.0,
            floorU: 0.19,
            wallU: null);

        var input = _mapper.Map(topology, surface, metadata);

        Assert.Equal(42.0, input.Geometry.AreaSquareMeters, 6);
        Assert.Equal(0.19, input.Geometry.FloorUValueWPerSquareMeterKelvin!.Value, 6);
    }

    [Fact]
    public void AddsDiagnosticForNonGroundSurface()
    {
        var topology = CreateTopology(
            buildingId: "B1",
            surfaceId: "S1",
            boundaryKind: ThermalBoundaryKind.Outdoor,
            area: 50.0,
            uValue: 0.25);
        var surface = topology.Surfaces.Single();
        var metadata = CreateMetadata(
            surfaceId: "S1",
            contactKind: GroundContactKind.SlabOnGround,
            area: 50.0,
            floorU: 0.20,
            wallU: null);

        var input = _mapper.Map(topology, surface, metadata);

        Assert.Contains(input.Geometry.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-SURFACE-NOT-GROUND");
    }

    [Fact]
    public void AddsDiagnosticForMissingSurfaceUValueWhenMetadataUValueMissing()
    {
        var topology = CreateTopology(
            buildingId: "B1",
            surfaceId: "S1",
            boundaryKind: ThermalBoundaryKind.Ground,
            area: 50.0,
            uValue: null);
        var surface = topology.Surfaces.Single();
        var metadata = CreateMetadata(
            surfaceId: "S1",
            contactKind: GroundContactKind.SlabOnGround,
            area: 50.0,
            floorU: null,
            wallU: null);

        var input = _mapper.Map(topology, surface, metadata);

        Assert.Contains(input.Geometry.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-SURFACE-UVALUE-MISSING");
    }

    private static BuildingThermalTopology CreateTopology(
        string buildingId,
        string surfaceId,
        ThermalBoundaryKind boundaryKind,
        double area,
        double? uValue)
    {
        var surface = new ThermalTopologySurface(
            SurfaceId: surfaceId,
            RoomId: "R1",
            ZoneId: "Z1",
            BoundaryKind: boundaryKind,
            AreaSquareMeters: area,
            UValueWPerSquareMeterKelvin: uValue,
            AdjacentZoneId: null,
            AdjacentRoomId: null,
            BoundarySource: "UnitTest",
            Diagnostics: []);

        return new BuildingThermalTopology(
            BuildingId: buildingId,
            Zones: [new ThermalTopologyZone("Z1", "Zone 1", ["R1"], [])],
            Rooms: [new ThermalTopologyRoom("R1", "Z1", 100.0, 40.0, [surface], [])],
            Surfaces: [surface],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);
    }

    private static GroundSurfaceMetadata CreateMetadata(
        string surfaceId,
        GroundContactKind contactKind,
        double area,
        double? floorU,
        double? wallU) =>
        new(
            SurfaceId: surfaceId,
            ContactKind: contactKind,
            Geometry: new GroundContactGeometry(
                AreaSquareMeters: area,
                ExposedPerimeterMeters: 20.0,
                CharacteristicDimensionMeters: 5.0,
                DepthBelowGroundMeters: 1.5,
                BasementWallHeightMeters: 2.0,
                CrawlspaceHeightMeters: 0.8,
                FloorUValueWPerSquareMeterKelvin: floorU,
                WallUValueWPerSquareMeterKelvin: wallU,
                EdgeInsulationThicknessMeters: null,
                EdgeInsulationConductivityWPerMeterKelvin: null,
                InsulationPlacement: GroundInsulationPlacement.None,
                Diagnostics: []),
            Soil: new GroundSoilProperties(
                ConductivityWPerMeterKelvin: 2.0,
                DensityKgPerCubicMeter: 1800.0,
                SpecificHeatJPerKgKelvin: 900.0,
                ThermalDiffusivitySquareMetersPerSecond: null,
                Source: "UnitTest",
                Diagnostics: []),
            Climate: new GroundClimateInput(
                MonthlyOutdoorTemperaturesCelsius: null,
                HourlyOutdoorTemperaturesCelsius: null,
                AnnualMeanOutdoorTemperatureCelsius: 10.0,
                GroundTemperatureAmplitudeCelsius: 3.0,
                GroundTemperaturePhaseShiftDays: 30.0,
                Source: "UnitTest",
                Diagnostics: []),
            Source: "UnitTest",
            Diagnostics: []);
}
