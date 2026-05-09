using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundBoundaryInputValidatorTests
{
    private readonly GroundBoundaryInputValidator _validator = new();

    [Fact]
    public void AcceptsValidSlabInput()
    {
        var input = CreateValidInput();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsUnknownContactKind()
    {
        var input = CreateValidInput() with { ContactKind = GroundContactKind.Unknown };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-CONTACT-KIND-UNKNOWN");
    }

    [Fact]
    public void RejectsNonPositiveArea()
    {
        var input = CreateValidInput() with
        {
            Geometry = CreateGeometry(area: 0.0)
        };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-AREA-NONPOSITIVE");
    }

    [Fact]
    public void RejectsNonPositiveSoilConductivity()
    {
        var input = CreateValidInput() with
        {
            Soil = CreateSoil(conductivity: 0.0)
        };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-SOIL-CONDUCTIVITY-NONPOSITIVE");
    }

    [Fact]
    public void RejectsGroundBoundaryWithAdjacentZone()
    {
        var input = CreateValidInput() with
        {
            AdjacentZoneId = "ZONE-B"
        };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-ADJACENT-ZONE-FORBIDDEN");
    }

    [Fact]
    public void RejectsNegativeAmplitude()
    {
        var input = CreateValidInput() with
        {
            Climate = CreateClimate(monthly: null, hourly: null, annualMean: 12.0, amplitude: -1.0, phaseShiftDays: 30.0)
        };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-AMPLITUDE-NEGATIVE");
    }

    [Fact]
    public void RejectsInvalidPhaseShift()
    {
        var input = CreateValidInput() with
        {
            Climate = CreateClimate(monthly: null, hourly: null, annualMean: 12.0, amplitude: 2.0, phaseShiftDays: 370.0)
        };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-PHASE-SHIFT-OUT-OF-RANGE");
    }

    [Theory]
    [InlineData(11)]
    [InlineData(13)]
    public void RejectsInvalidMonthlyProfile(int monthCount)
    {
        var monthly = Enumerable.Repeat(10.0, monthCount).ToArray();
        var input = CreateValidInput() with
        {
            Climate = CreateClimate(monthly: monthly, hourly: null, annualMean: null)
        };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-MONTHLY-PROFILE-INVALID");
    }

    [Theory]
    [InlineData(8759)]
    [InlineData(8761)]
    public void RejectsInvalidHourlyProfile(int hourCount)
    {
        var hourly = Enumerable.Repeat(10.0, hourCount).ToArray();
        var input = CreateValidInput() with
        {
            Climate = CreateClimate(monthly: null, hourly: hourly, annualMean: null)
        };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-HOURLY-PROFILE-INVALID");
    }

    private static GroundBoundaryCalculationInput CreateValidInput() =>
        new(
            BoundaryId: "GND-1",
            BuildingId: "BLD-1",
            ZoneId: "ZONE-1",
            RoomId: "ROOM-1",
            SurfaceId: "S-1",
            ContactKind: GroundContactKind.SlabOnGround,
            Geometry: CreateGeometry(area: 100.0),
            Soil: CreateSoil(conductivity: 2.0),
            Climate: CreateClimate(monthly: null, hourly: null, annualMean: 12.0),
            DisclosureOverride: null,
            Source: "UnitTest");

    private static GroundContactGeometry CreateGeometry(double area) =>
        new(
            AreaSquareMeters: area,
            ExposedPerimeterMeters: 40.0,
            CharacteristicDimensionMeters: null,
            DepthBelowGroundMeters: 0.0,
            BasementWallHeightMeters: 2.2,
            CrawlspaceHeightMeters: 0.8,
            FloorUValueWPerSquareMeterKelvin: 0.25,
            WallUValueWPerSquareMeterKelvin: 0.35,
            EdgeInsulationThicknessMeters: null,
            EdgeInsulationConductivityWPerMeterKelvin: null,
            InsulationPlacement: GroundInsulationPlacement.None,
            Diagnostics: []);

    private static GroundSoilProperties CreateSoil(double conductivity) =>
        new(
            ConductivityWPerMeterKelvin: conductivity,
            DensityKgPerCubicMeter: 1800.0,
            SpecificHeatJPerKgKelvin: 900.0,
            ThermalDiffusivitySquareMetersPerSecond: null,
            Source: "UnitTest",
            Diagnostics: []);

    private static GroundClimateInput CreateClimate(
        IReadOnlyList<double>? monthly,
        IReadOnlyList<double>? hourly,
        double? annualMean,
        double? amplitude = 4.0,
        double? phaseShiftDays = 30.0) =>
        new(
            MonthlyOutdoorTemperaturesCelsius: monthly,
            HourlyOutdoorTemperaturesCelsius: hourly,
            AnnualMeanOutdoorTemperatureCelsius: annualMean,
            GroundTemperatureAmplitudeCelsius: amplitude,
            GroundTemperaturePhaseShiftDays: phaseShiftDays,
            Source: "UnitTest",
            Diagnostics: []);
}
