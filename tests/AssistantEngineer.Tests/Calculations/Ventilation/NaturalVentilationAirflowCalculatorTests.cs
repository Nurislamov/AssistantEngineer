using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationAirflowCalculatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private readonly NaturalVentilationAirflowCalculator _calculator = new(
        new NaturalVentilationOpeningGeometryNormalizer(),
        new NaturalVentilationInputValidator(),
        new NaturalVentilationPressureCalculator(),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void CalculatesWindOnlyAirflow()
    {
        var input = CreateInput(
            NaturalVentilationFlowConfiguration.WindOnly,
            openings:
            [
                CreateOpening("O1", area: 1.0, fraction: 0.5, cd: 0.6, windCp: 0.5, oppositeCp: 0.0)
            ],
            environment: CreateEnvironment(22.0, 12.0, 3.0, 1.2));

        var result = _calculator.Calculate(input);
        var opening = Assert.Single(result.Openings);

        Assert.True(opening.AirflowCubicMetersPerSecond > 0.0);
        Assert.Equal(
            opening.AirflowCubicMetersPerSecond!.Value * 3600.0,
            opening.AirflowCubicMetersPerHour!.Value,
            6);
        Assert.Equal(
            opening.AirflowCubicMetersPerSecond!.Value * 1.2,
            opening.AirflowKilogramsPerSecond!.Value,
            6);
    }

    [Fact]
    public void CalculatesStackOnlyAirflow()
    {
        var input = CreateInput(
            NaturalVentilationFlowConfiguration.StackOnly,
            openings:
            [
                CreateOpening("O1", area: 1.0, fraction: 0.8, cd: 0.6, windCp: 0.0, oppositeCp: 0.0, topHeight: 2.5, bottomHeight: 0.5)
            ],
            environment: CreateEnvironment(24.0, 12.0, 0.0, 1.2));

        var result = _calculator.Calculate(input);
        var opening = Assert.Single(result.Openings);

        Assert.True(opening.AirflowCubicMetersPerSecond > 0.0);
    }

    [Fact]
    public void CalculatesCombinedWindAndStackAirflow()
    {
        var input = CreateInput(
            NaturalVentilationFlowConfiguration.CombinedWindAndStack,
            openings:
            [
                CreateOpening("O1", area: 1.2, fraction: 0.7, cd: 0.6, windCp: 0.5, oppositeCp: 0.0, topHeight: 2.5, bottomHeight: 0.5)
            ],
            environment: CreateEnvironment(24.0, 12.0, 3.0, 1.2));

        var result = _calculator.Calculate(input);
        var opening = Assert.Single(result.Openings);

        Assert.True(opening.CombinedPressureDifferencePa > 0.0);
        Assert.True(opening.AirflowCubicMetersPerSecond > 0.0);
    }

    [Fact]
    public void CrossVentilationUsesConservativeMinFlow()
    {
        var input = CreateInput(
            NaturalVentilationFlowConfiguration.CrossVentilation,
            openings:
            [
                CreateOpening("O1", area: 1.5, fraction: 1.0, cd: 0.6, windCp: 0.5, oppositeCp: 0.0),
                CreateOpening("O2", area: 0.6, fraction: 1.0, cd: 0.6, windCp: -0.2, oppositeCp: 0.0)
            ],
            environment: CreateEnvironment(22.0, 12.0, 3.0, 1.2));

        var result = _calculator.Calculate(input);
        var positiveFlows = result.Openings
            .Select(opening => opening.AirflowCubicMetersPerSecond.GetValueOrDefault())
            .Where(flow => flow > 0.0)
            .ToArray();

        Assert.NotEmpty(positiveFlows);
        Assert.Equal(positiveFlows.Min(), result.TotalAirflowCubicMetersPerSecond, 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CROSS-VENTILATION-MIN-FLOW-USED");
    }

    [Fact]
    public void UnknownConfigurationDoesNotFallbackSilently()
    {
        var input = CreateInput(
            NaturalVentilationFlowConfiguration.Unknown,
            openings:
            [
                CreateOpening("O1", area: 1.0, fraction: 1.0, cd: 0.6, windCp: 0.5, oppositeCp: 0.0)
            ],
            environment: CreateEnvironment(22.0, 12.0, 3.0, 1.2));

        var result = _calculator.Calculate(input);

        Assert.Equal(0.0, result.TotalAirflowCubicMetersPerSecond, 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-UNKNOWN-CONFIGURATION-NO-FALLBACK");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var disclosureOverride = new StandardCalculationDisclosure(
            Family: StandardCalculationFamily.EN16798,
            Stage: StandardCalculationStage.Ventilation,
            Mode: StandardCalculationMode.StandardInspired,
            CalculationPath: "UnitTest/VentOverride",
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims:
                [
                    "safe claim",
                    "Full EN compliance",
                    "prefix Full ISO compliance suffix"
                ],
                ForbiddenClaims: [],
                Limitations: ["Unit test"],
                Assumptions: ["Unit test"]),
            Diagnostics: []);

        var input = new NaturalVentilationCalculationInput(
            CalculationId: "CALC-1",
            FlowConfiguration: NaturalVentilationFlowConfiguration.WindOnly,
            Openings:
            [
                CreateOpening("O1", area: 1.0, fraction: 0.5, cd: 0.6, windCp: 0.5, oppositeCp: 0.0)
            ],
            Environment: CreateEnvironment(22.0, 12.0, 3.0, 1.2),
            DisclosureOverride: disclosureOverride,
            Source: "UnitTest");

        var result = _calculator.Calculate(input);

        foreach (var forbiddenClaim in RequiredForbiddenClaims)
        {
            Assert.Contains(forbiddenClaim, result.Disclosure.ClaimBoundary.ForbiddenClaims, StringComparer.Ordinal);
            Assert.DoesNotContain(
                result.Disclosure.ClaimBoundary.AllowedClaims,
                claim => claim.Contains(forbiddenClaim, StringComparison.Ordinal));
        }
    }

    private static NaturalVentilationCalculationInput CreateInput(
        NaturalVentilationFlowConfiguration flowConfiguration,
        IReadOnlyList<NaturalVentilationOpeningGeometry> openings,
        NaturalVentilationEnvironment environment) =>
        new(
            CalculationId: "CALC-1",
            FlowConfiguration: flowConfiguration,
            Openings: openings,
            Environment: environment,
            DisclosureOverride: null,
            Source: "UnitTest");

    private static NaturalVentilationOpeningGeometry CreateOpening(
        string openingId,
        double area,
        double fraction,
        double cd,
        double windCp,
        double oppositeCp,
        double topHeight = 2.0,
        double bottomHeight = 0.0) =>
        new(
            OpeningId: openingId,
            RoomId: "R1",
            ZoneId: "Z1",
            SurfaceId: openingId,
            OpeningType: NaturalVentilationOpeningType.Window,
            OpeningAreaSquareMeters: area,
            OpeningHeightMeters: Math.Max(0.0, topHeight - bottomHeight),
            OpeningWidthMeters: 1.0,
            OpeningCenterHeightMeters: (topHeight + bottomHeight) / 2.0,
            BottomHeightMeters: bottomHeight,
            TopHeightMeters: topHeight,
            OpeningFraction: fraction,
            DischargeCoefficient: cd,
            WindPressureCoefficient: windCp,
            OppositeWindPressureCoefficient: oppositeCp,
            OrientationAzimuthDegrees: 180.0,
            Source: "UnitTest",
            Diagnostics: []);

    private static NaturalVentilationEnvironment CreateEnvironment(
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeed,
        double density) =>
        new(
            IndoorTemperatureCelsius: indoorTemperatureC,
            OutdoorTemperatureCelsius: outdoorTemperatureC,
            WindSpeedMetersPerSecond: windSpeed,
            WindSpeedHeightMeters: 10.0,
            OpeningReferenceHeightMeters: 0.0,
            OutdoorAirDensityKgPerCubicMeter: density,
            IndoorAirDensityKgPerCubicMeter: density,
            AtmosphericPressurePa: 101325.0,
            Source: "UnitTest",
            Diagnostics: []);
}
