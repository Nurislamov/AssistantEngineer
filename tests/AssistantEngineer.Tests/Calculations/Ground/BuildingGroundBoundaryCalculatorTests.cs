using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class BuildingGroundBoundaryCalculatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private static readonly StandardCalculationDisclosureFactory DisclosureFactory = new();

    private readonly BuildingGroundBoundaryCalculator _calculator = new(
        new GroundBoundaryTopologyMapper(),
        new GroundBoundaryCalculator(
            new GroundGeometryNormalizer(),
            new GroundBoundaryInputValidator(),
            new GroundTemperatureProfileProvider(new AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles.AnnualProfileShapeValidator()),
            DisclosureFactory),
        DisclosureFactory);

    [Fact]
    public void CalculatesAllGroundSurfaces()
    {
        var topology = CreateTopology(new[]
        {
            CreateSurface("S1", ThermalBoundaryKind.Ground, area: 40.0, uValue: 0.20),
            CreateSurface("S2", ThermalBoundaryKind.Ground, area: 30.0, uValue: 0.30)
        });

        var input = new BuildingGroundBoundaryCalculationInput(
            Topology: topology,
            GroundSurfaceMetadataBySurfaceId: new Dictionary<string, GroundSurfaceMetadata>(StringComparer.Ordinal)
            {
                ["S1"] = CreateMetadata("S1", GroundContactKind.SlabOnGround),
                ["S2"] = CreateMetadata("S2", GroundContactKind.SuspendedFloor)
            },
            DisclosureOverride: null);

        var result = _calculator.Calculate(input);

        Assert.Equal(2, result.GroundSurfaces.Count);
        var expectedTotal = result.GroundSurfaces
            .Select(surface => surface.HeatTransferCoefficientWPerKelvin.GetValueOrDefault())
            .Sum();
        Assert.Equal(expectedTotal, result.TotalGroundHeatTransferCoefficientWPerKelvin, 6);
    }

    [Fact]
    public void MissingMetadataProducesDiagnosticAndContinues()
    {
        var topology = CreateTopology(new[]
        {
            CreateSurface("S1", ThermalBoundaryKind.Ground, area: 40.0, uValue: 0.20),
            CreateSurface("S2", ThermalBoundaryKind.Ground, area: 30.0, uValue: 0.30)
        });

        var input = new BuildingGroundBoundaryCalculationInput(
            Topology: topology,
            GroundSurfaceMetadataBySurfaceId: new Dictionary<string, GroundSurfaceMetadata>(StringComparer.Ordinal)
            {
                ["S1"] = CreateMetadata("S1", GroundContactKind.SlabOnGround)
            },
            DisclosureOverride: null);

        var result = _calculator.Calculate(input);

        Assert.Single(result.GroundSurfaces);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-SURFACE-METADATA-MISSING");
    }

    [Fact]
    public void MetadataForUnknownSurfaceProducesDiagnostic()
    {
        var topology = CreateTopology(new[]
        {
            CreateSurface("S1", ThermalBoundaryKind.Ground, area: 40.0, uValue: 0.20)
        });

        var input = new BuildingGroundBoundaryCalculationInput(
            Topology: topology,
            GroundSurfaceMetadataBySurfaceId: new Dictionary<string, GroundSurfaceMetadata>(StringComparer.Ordinal)
            {
                ["S1"] = CreateMetadata("S1", GroundContactKind.SlabOnGround),
                ["UNKNOWN"] = CreateMetadata("UNKNOWN", GroundContactKind.SlabOnGround)
            },
            DisclosureOverride: null);

        var result = _calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-METADATA-SURFACE-NOT-FOUND");
    }

    [Fact]
    public void NoGroundSurfacesProducesDiagnostic()
    {
        var topology = CreateTopology(new[]
        {
            CreateSurface("S-OUT", ThermalBoundaryKind.Outdoor, area: 40.0, uValue: 0.20)
        });

        var input = new BuildingGroundBoundaryCalculationInput(
            Topology: topology,
            GroundSurfaceMetadataBySurfaceId: new Dictionary<string, GroundSurfaceMetadata>(StringComparer.Ordinal),
            DisclosureOverride: null);

        var result = _calculator.Calculate(input);

        Assert.Empty(result.GroundSurfaces);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-NO-GROUND-SURFACES");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var topology = CreateTopology(new[]
        {
            CreateSurface("S1", ThermalBoundaryKind.Ground, area: 40.0, uValue: 0.20)
        });

        var disclosureOverride = new StandardCalculationDisclosure(
            Family: StandardCalculationFamily.ISO13370,
            Stage: StandardCalculationStage.BoundaryCondition,
            Mode: StandardCalculationMode.StandardInspired,
            CalculationPath: "UnitTest/GroundOverride",
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims:
                [
                    "Safe claim",
                    "Full ISO compliance",
                    "Prefix Full EN compliance suffix"
                ],
                ForbiddenClaims: [],
                Limitations: ["Unit test"],
                Assumptions: ["Unit test"]),
            Diagnostics: []);

        var input = new BuildingGroundBoundaryCalculationInput(
            Topology: topology,
            GroundSurfaceMetadataBySurfaceId: new Dictionary<string, GroundSurfaceMetadata>(StringComparer.Ordinal)
            {
                ["S1"] = CreateMetadata("S1", GroundContactKind.SlabOnGround)
            },
            DisclosureOverride: disclosureOverride);

        var result = _calculator.Calculate(input);

        foreach (var forbiddenClaim in RequiredForbiddenClaims)
        {
            Assert.Contains(forbiddenClaim, result.Disclosure.ClaimBoundary.ForbiddenClaims, StringComparer.Ordinal);
            Assert.DoesNotContain(
                result.Disclosure.ClaimBoundary.AllowedClaims,
                claim => claim.Contains(forbiddenClaim, StringComparison.Ordinal));
        }
    }

    private static BuildingThermalTopology CreateTopology(IReadOnlyList<ThermalTopologySurface> surfaces) =>
        new(
            BuildingId: "B1",
            Zones: [new ThermalTopologyZone("Z1", "Zone 1", ["R1"], [])],
            Rooms: [new ThermalTopologyRoom("R1", "Z1", 120.0, 50.0, surfaces, [])],
            Surfaces: surfaces,
            Disclosure: DisclosureFactory.CreateThermalZonesDisclosure(),
            Diagnostics: []);

    private static ThermalTopologySurface CreateSurface(
        string surfaceId,
        ThermalBoundaryKind boundaryKind,
        double area,
        double? uValue) =>
        new(
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

    private static GroundSurfaceMetadata CreateMetadata(
        string surfaceId,
        GroundContactKind contactKind) =>
        new(
            SurfaceId: surfaceId,
            ContactKind: contactKind,
            Geometry: new GroundContactGeometry(
                AreaSquareMeters: 0.0,
                ExposedPerimeterMeters: 20.0,
                CharacteristicDimensionMeters: null,
                DepthBelowGroundMeters: 1.5,
                BasementWallHeightMeters: 2.0,
                CrawlspaceHeightMeters: 0.8,
                FloorUValueWPerSquareMeterKelvin: null,
                WallUValueWPerSquareMeterKelvin: null,
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
