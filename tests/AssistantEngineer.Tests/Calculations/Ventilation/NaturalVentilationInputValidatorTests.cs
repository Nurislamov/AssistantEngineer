using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationInputValidatorTests
{
    private readonly NaturalVentilationInputValidator _validator = new();

    [Fact]
    public void AcceptsValidSingleSidedInput()
    {
        var input = CreateInput(
            "CALC-1",
            NaturalVentilationFlowConfiguration.SingleSided,
            openings:
            [
                CreateOpening("O1")
            ]);

        var validation = _validator.Validate(input);

        Assert.True(validation.IsValid);
    }

    [Fact]
    public void RejectsMissingCalculationId()
    {
        var input = CreateInput(
            "",
            NaturalVentilationFlowConfiguration.SingleSided,
            openings:
            [
                CreateOpening("O1")
            ]);

        var validation = _validator.Validate(input);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CALCULATION-ID-MISSING");
    }

    [Fact]
    public void RejectsUnknownFlowConfiguration()
    {
        var input = CreateInput(
            "CALC-1",
            NaturalVentilationFlowConfiguration.Unknown,
            openings:
            [
                CreateOpening("O1")
            ]);

        var validation = _validator.Validate(input);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-FLOW-CONFIGURATION-UNKNOWN");
    }

    [Fact]
    public void RejectsMissingOpenings()
    {
        var input = CreateInput(
            "CALC-1",
            NaturalVentilationFlowConfiguration.SingleSided,
            openings: []);

        var validation = _validator.Validate(input);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-OPENINGS-MISSING");
    }

    [Fact]
    public void ReportsCrossVentilationInsufficientOpenings()
    {
        var input = CreateInput(
            "CALC-1",
            NaturalVentilationFlowConfiguration.CrossVentilation,
            openings:
            [
                CreateOpening("O1", oppositeCp: null)
            ]);

        var validation = _validator.Validate(input);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CROSS-VENTILATION-INSUFFICIENT-OPENINGS");
    }

    [Fact]
    public void RejectsInvalidWindSpeed()
    {
        var input = CreateInput(
            "CALC-1",
            NaturalVentilationFlowConfiguration.WindOnly,
            openings:
            [
                CreateOpening("O1")
            ],
            environment: CreateEnvironment(windSpeed: -1.0));

        var validation = _validator.Validate(input);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-WIND-SPEED-INVALID");
    }

    private static NaturalVentilationCalculationInput CreateInput(
        string calculationId,
        NaturalVentilationFlowConfiguration flowConfiguration,
        IReadOnlyList<NaturalVentilationOpeningGeometry> openings,
        NaturalVentilationEnvironment? environment = null) =>
        new(
            CalculationId: calculationId,
            FlowConfiguration: flowConfiguration,
            Openings: openings,
            Environment: environment ?? CreateEnvironment(3.0),
            DisclosureOverride: null,
            Source: "UnitTest");

    private static NaturalVentilationOpeningGeometry CreateOpening(
        string openingId,
        double? oppositeCp = 0.0) =>
        new(
            OpeningId: openingId,
            RoomId: "R1",
            ZoneId: "Z1",
            SurfaceId: "S1",
            OpeningType: NaturalVentilationOpeningType.Window,
            OpeningAreaSquareMeters: 1.0,
            OpeningHeightMeters: 1.5,
            OpeningWidthMeters: 1.0,
            OpeningCenterHeightMeters: 1.5,
            BottomHeightMeters: 1.0,
            TopHeightMeters: 2.5,
            OpeningFraction: 0.5,
            DischargeCoefficient: 0.60,
            WindPressureCoefficient: 0.5,
            OppositeWindPressureCoefficient: oppositeCp,
            OrientationAzimuthDegrees: 180.0,
            Source: "UnitTest",
            Diagnostics: []);

    private static NaturalVentilationEnvironment CreateEnvironment(double windSpeed) =>
        new(
            IndoorTemperatureCelsius: 22.0,
            OutdoorTemperatureCelsius: 12.0,
            WindSpeedMetersPerSecond: windSpeed,
            WindSpeedHeightMeters: 10.0,
            OpeningReferenceHeightMeters: 0.0,
            OutdoorAirDensityKgPerCubicMeter: 1.2,
            IndoorAirDensityKgPerCubicMeter: 1.2,
            AtmosphericPressurePa: 101325.0,
            Source: "UnitTest",
            Diagnostics: []);
}
