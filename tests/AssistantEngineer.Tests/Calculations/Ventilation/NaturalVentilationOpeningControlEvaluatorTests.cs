using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationOpeningControlEvaluatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private readonly NaturalVentilationOpeningControlEvaluator _evaluator = new(
        new NaturalVentilationControlRuleValidator(),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void AlwaysClosedProducesZeroFraction()
    {
        var operation = _evaluator.Evaluate(
            CreateRule("R1", NaturalVentilationControlMode.AlwaysClosed),
            CreateContext(hour: 0));

        Assert.Equal(0.0, operation.OpeningFraction, 6);
        Assert.False(operation.IsOpen);
    }

    [Fact]
    public void AlwaysOpenProducesOneFraction()
    {
        var operation = _evaluator.Evaluate(
            CreateRule("R1", NaturalVentilationControlMode.AlwaysOpen),
            CreateContext(hour: 0));

        Assert.Equal(1.0, operation.OpeningFraction, 6);
        Assert.True(operation.IsOpen);
    }

    [Fact]
    public void FixedFractionUsesConfiguredFraction()
    {
        var operation = _evaluator.Evaluate(
            CreateRule("R1", NaturalVentilationControlMode.FixedFraction, fixedFraction: 0.35),
            CreateContext(hour: 0));

        Assert.Equal(0.35, operation.OpeningFraction, 6);
    }

    [Fact]
    public void ScheduleUsesContextScheduleFraction()
    {
        var operation = _evaluator.Evaluate(
            CreateRule("R1", NaturalVentilationControlMode.Schedule),
            CreateContext(hour: 0, scheduleFraction: 0.6));

        Assert.Equal(0.6, operation.OpeningFraction, 6);
    }

    [Fact]
    public void OccupancyRequiresOccupancyWhenConfigured()
    {
        var rule = CreateRule(
            "R1",
            NaturalVentilationControlMode.Occupancy,
            requiresOccupancy: true);

        var closed = _evaluator.Evaluate(rule, CreateContext(hour: 0, occupancyFraction: 0.0));
        var open = _evaluator.Evaluate(rule, CreateContext(hour: 1, occupancyFraction: 1.0));

        Assert.Equal(0.0, closed.OpeningFraction, 6);
        Assert.True(open.OpeningFraction > 0.0);
    }

    [Fact]
    public void TemperatureOpensAboveIndoorThreshold()
    {
        var operation = _evaluator.Evaluate(
            CreateRule(
                "R1",
                NaturalVentilationControlMode.TemperatureDriven,
                indoorOpenAbove: 24.0),
            CreateContext(hour: 0, indoorTemperature: 26.0));

        Assert.True(operation.OpeningFraction > 0.0);
    }

    [Fact]
    public void TemperatureBlockedByOutdoorMinimum()
    {
        var operation = _evaluator.Evaluate(
            CreateRule(
                "R1",
                NaturalVentilationControlMode.Temperature,
                indoorOpenAbove: 24.0,
                outdoorMin: 15.0),
            CreateContext(hour: 0, indoorTemperature: 26.0, outdoorTemperature: 10.0));

        Assert.Equal(0.0, operation.OpeningFraction, 6);
        Assert.Contains(operation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-TEMPERATURE-BLOCKED");
    }

    [Fact]
    public void OccupancyAndTemperatureRequiresBoth()
    {
        var rule = CreateRule(
            "R1",
            NaturalVentilationControlMode.OccupancyAndTemperature,
            requiresOccupancy: true,
            indoorOpenAbove: 24.0);

        var occupancyOnly = _evaluator.Evaluate(
            rule,
            CreateContext(hour: 0, occupancyFraction: 1.0, indoorTemperature: 20.0));
        var temperatureOnly = _evaluator.Evaluate(
            rule,
            CreateContext(hour: 1, occupancyFraction: 0.0, indoorTemperature: 26.0));
        var both = _evaluator.Evaluate(
            rule,
            CreateContext(hour: 2, occupancyFraction: 1.0, indoorTemperature: 26.0));

        Assert.Equal(0.0, occupancyOnly.OpeningFraction, 6);
        Assert.Equal(0.0, temperatureOnly.OpeningFraction, 6);
        Assert.True(both.OpeningFraction > 0.0);
    }

    [Fact]
    public void NightVentilationOnlyActiveAtNight()
    {
        var rule = CreateRule(
            "R1",
            NaturalVentilationControlMode.NightVentilation,
            nightMode: NaturalVentilationNightVentilationMode.TemperatureDriven,
            indoorOpenAbove: 24.0);

        var day = _evaluator.Evaluate(
            rule,
            CreateContext(hour: 12, indoorTemperature: 26.0, isNight: false));
        var night = _evaluator.Evaluate(
            rule,
            CreateContext(hour: 2, indoorTemperature: 26.0, isNight: true));

        Assert.Equal(0.0, day.OpeningFraction, 6);
        Assert.False(day.IsNightVentilationActive);
        Assert.True(night.OpeningFraction > 0.0);
        Assert.True(night.IsNightVentilationActive);
    }

    [Fact]
    public void CoolingAssistDoesNotOpenWhenOutdoorHotterThanIndoor()
    {
        var operation = _evaluator.Evaluate(
            CreateRule(
                "R1",
                NaturalVentilationControlMode.CoolingAssist,
                indoorOpenAbove: 24.0),
            CreateContext(hour: 0, indoorTemperature: 26.0, outdoorTemperature: 30.0));

        Assert.Equal(0.0, operation.OpeningFraction, 6);
        Assert.Contains(operation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-COOLING-ASSIST-OUTDOOR-HOTTER");
    }

    [Fact]
    public void NightPurgeFollowsNightMaskAndTemperature()
    {
        var rule = CreateRule(
            "R1",
            NaturalVentilationControlMode.NightPurge,
            nightMode: NaturalVentilationNightVentilationMode.TemperatureDriven,
            indoorOpenAbove: 24.0);

        var day = _evaluator.Evaluate(rule, CreateContext(hour: 14, indoorTemperature: 26.0, isNight: false));
        var night = _evaluator.Evaluate(rule, CreateContext(hour: 2, indoorTemperature: 26.0, isNight: true));

        Assert.Equal(0.0, day.OpeningFraction, 6);
        Assert.True(night.OpeningFraction > 0.0);
    }

    [Fact]
    public void WindLimitClosesOpening()
    {
        var operation = _evaluator.Evaluate(
            CreateRule(
                "R1",
                NaturalVentilationControlMode.AlwaysOpen,
                maxWindSpeed: 3.0),
            CreateContext(hour: 0, windSpeed: 5.0));

        Assert.Equal(0.0, operation.OpeningFraction, 6);
        Assert.Contains(operation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-WIND-LIMIT-BLOCKED");
    }

    [Fact]
    public void HeatingLockoutClosesOpening()
    {
        var operation = _evaluator.Evaluate(
            CreateRule(
                "R1",
                NaturalVentilationControlMode.AlwaysOpen,
                heatingLockoutEnabled: true),
            CreateContext(hour: 0, heatingModeActive: true));

        Assert.Equal(0.0, operation.OpeningFraction, 6);
        Assert.Contains(operation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-HEATING-LOCKOUT-BLOCKED");
    }

    [Fact]
    public void AppliesMinimumAndMaximumOpeningFraction()
    {
        var raisedToMin = _evaluator.Evaluate(
            CreateRule(
                "R1",
                NaturalVentilationControlMode.FixedFraction,
                fixedFraction: 0.1,
                minFraction: 0.3),
            CreateContext(hour: 0));
        var loweredToMax = _evaluator.Evaluate(
            CreateRule(
                "R2",
                NaturalVentilationControlMode.AlwaysOpen,
                maxFraction: 0.6),
            CreateContext(hour: 0));

        Assert.Equal(0.3, raisedToMin.OpeningFraction, 6);
        Assert.Equal(0.6, loweredToMax.OpeningFraction, 6);
    }

    [Fact]
    public void UnknownControlDoesNotFallbackSilently()
    {
        var operation = _evaluator.Evaluate(
            CreateRule("R1", NaturalVentilationControlMode.Unknown),
            CreateContext(hour: 0));

        Assert.Equal(0.0, operation.OpeningFraction, 6);
        Assert.Contains(operation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-UNKNOWN-NO-FALLBACK");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var disclosureOverride = new StandardCalculationDisclosure(
            Family: StandardCalculationFamily.EN16798,
            Stage: StandardCalculationStage.Ventilation,
            Mode: StandardCalculationMode.StandardInspired,
            CalculationPath: "UnitTest/VentControlOverride",
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims:
                [
                    "safe claim",
                    "Full ISO compliance",
                    "prefix Full EN compliance suffix"
                ],
                ForbiddenClaims: [],
                Limitations: ["Unit test"],
                Assumptions: ["Unit test"]),
            Diagnostics: []);

        var result = _evaluator.Evaluate(
            new NaturalVentilationControlEvaluationInput(
                Rules: [CreateRule("R1", NaturalVentilationControlMode.AlwaysOpen)],
                HourlyContexts: [CreateContext(hour: 0)],
                DisclosureOverride: disclosureOverride,
                Source: "UnitTest"));

        foreach (var forbiddenClaim in RequiredForbiddenClaims)
        {
            Assert.Contains(forbiddenClaim, result.Disclosure.ClaimBoundary.ForbiddenClaims, StringComparer.Ordinal);
            Assert.DoesNotContain(
                result.Disclosure.ClaimBoundary.AllowedClaims,
                claim => claim.Contains(forbiddenClaim, StringComparison.Ordinal));
        }
    }

    private static NaturalVentilationOpeningControlRule CreateRule(
        string id,
        NaturalVentilationControlMode mode,
        double? fixedFraction = null,
        double? minFraction = null,
        double? maxFraction = null,
        bool? requiresOccupancy = null,
        double? indoorOpenAbove = null,
        double? outdoorMin = null,
        NaturalVentilationNightVentilationMode nightMode = NaturalVentilationNightVentilationMode.Disabled,
        double? maxWindSpeed = null,
        bool? heatingLockoutEnabled = null) =>
        new(
            RuleId: id,
            OpeningId: "O1",
            RoomId: "R1",
            ZoneId: "Z1",
            ControlMode: mode,
            NightVentilationMode: nightMode,
            FixedOpeningFraction: fixedFraction,
            MinimumOpeningFraction: minFraction,
            MaximumOpeningFraction: maxFraction,
            IndoorTemperatureOpenAboveCelsius: indoorOpenAbove,
            IndoorTemperatureCloseBelowCelsius: null,
            OutdoorTemperatureMinimumCelsius: outdoorMin,
            OutdoorTemperatureMaximumCelsius: null,
            IndoorOutdoorTemperatureDifferenceMinimumKelvin: null,
            RequiresOccupancy: requiresOccupancy,
            ScheduleId: null,
            OccupancyProfileId: null,
            Source: "UnitTest",
            Diagnostics: [],
            MaximumWindSpeedMetersPerSecond: maxWindSpeed,
            HeatingLockoutEnabled: heatingLockoutEnabled);

    private static NaturalVentilationHourlyControlContext CreateContext(
        int hour,
        double indoorTemperature = 22.0,
        double outdoorTemperature = 18.0,
        double? windSpeed = 2.0,
        double? scheduleFraction = 0.0,
        double? occupancyFraction = 0.0,
        bool isNight = false,
        bool? heatingModeActive = null) =>
        new(
            HourIndex: hour,
            IndoorTemperatureCelsius: indoorTemperature,
            OutdoorTemperatureCelsius: outdoorTemperature,
            WindSpeedMetersPerSecond: windSpeed,
            OccupancyFraction: occupancyFraction,
            ScheduleFraction: scheduleFraction,
            IsNightHour: isNight,
            RoomId: "R1",
            ZoneId: "Z1",
            Diagnostics: [],
            HeatingModeActive: heatingModeActive);
}
