using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundBoundaryCalculatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private readonly GroundBoundaryCalculator _calculator = new(
        new GroundGeometryNormalizer(),
        new GroundBoundaryInputValidator(),
        new GroundTemperatureProfileProvider(new AnnualProfileShapeValidator()),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void CalculatesSlabOnGroundEquivalentUAndH()
    {
        var input = CreateInput(
            contactKind: GroundContactKind.SlabOnGround,
            area: 100.0,
            floorU: 0.25,
            wallU: 0.35);

        var result = _calculator.Calculate(input);

        Assert.NotNull(result.EquivalentUValueWPerSquareMeterKelvin);
        Assert.True(result.EquivalentUValueWPerSquareMeterKelvin > 0.0);
        Assert.Equal(
            result.EquivalentUValueWPerSquareMeterKelvin.Value * 100.0,
            result.HeatTransferCoefficientWPerKelvin.GetValueOrDefault(),
            6);
    }

    [Fact]
    public void CalculatesSuspendedFloorResult()
    {
        var input = CreateInput(
            contactKind: GroundContactKind.SuspendedFloor,
            area: 60.0,
            floorU: 0.30,
            wallU: null);

        var result = _calculator.Calculate(input);

        Assert.NotNull(result.EquivalentUValueWPerSquareMeterKelvin);
        Assert.True(result.EquivalentUValueWPerSquareMeterKelvin > 0.0);
    }

    [Fact]
    public void CalculatesBuriedWallResult()
    {
        var input = CreateInput(
            contactKind: GroundContactKind.BuriedWall,
            area: 40.0,
            floorU: null,
            wallU: 0.45);

        var result = _calculator.Calculate(input);

        Assert.NotNull(result.EquivalentUValueWPerSquareMeterKelvin);
        Assert.True(result.EquivalentUValueWPerSquareMeterKelvin > 0.0);
    }

    [Fact]
    public void OtherContactKindDoesNotFallbackToSlab()
    {
        var input = CreateInput(
            contactKind: GroundContactKind.Other,
            area: 100.0,
            floorU: 0.25,
            wallU: 0.35);

        var result = _calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-OTHER-CONTACT-UNSUPPORTED");
        Assert.Null(result.EquivalentUValueWPerSquareMeterKelvin);
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var input = CreateInput(
            contactKind: GroundContactKind.SlabOnGround,
            area: 100.0,
            floorU: 0.25,
            wallU: 0.35);

        var result = _calculator.Calculate(input);

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

    private static GroundBoundaryCalculationInput CreateInput(
        GroundContactKind contactKind,
        double area,
        double? floorU,
        double? wallU) =>
        new(
            BoundaryId: "GROUND-1",
            BuildingId: "BLD-1",
            ZoneId: "ZONE-1",
            RoomId: "ROOM-1",
            SurfaceId: "SURF-GROUND-1",
            ContactKind: contactKind,
            Geometry: new GroundContactGeometry(
                AreaSquareMeters: area,
                ExposedPerimeterMeters: 40.0,
                CharacteristicDimensionMeters: null,
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
                AnnualMeanOutdoorTemperatureCelsius: 12.0,
                GroundTemperatureAmplitudeCelsius: 3.0,
                GroundTemperaturePhaseShiftDays: 30.0,
                Source: "UnitTest",
                Diagnostics: []),
            DisclosureOverride: null,
            Source: "UnitTest");
}
