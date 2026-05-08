using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationControlRuleValidatorTests
{
    private readonly NaturalVentilationControlRuleValidator _validator = new();

    [Fact]
    public void AcceptsValidFixedFractionRule()
    {
        var validation = _validator.Validate(
        [
            CreateRule(
                ruleId: "R1",
                mode: NaturalVentilationControlMode.FixedFraction,
                openingId: "O1",
                fixedFraction: 0.5)
        ]);

        Assert.True(validation.IsValid);
    }

    [Fact]
    public void RejectsMissingRuleId()
    {
        var validation = _validator.Validate(
        [
            CreateRule(
                ruleId: "",
                mode: NaturalVentilationControlMode.FixedFraction,
                openingId: "O1",
                fixedFraction: 0.5)
        ]);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-RULE-ID-MISSING");
    }

    [Fact]
    public void RejectsUnknownControlMode()
    {
        var validation = _validator.Validate(
        [
            CreateRule(
                ruleId: "R1",
                mode: NaturalVentilationControlMode.Unknown,
                openingId: "O1",
                fixedFraction: 0.5)
        ]);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-MODE-UNKNOWN");
    }

    [Fact]
    public void RejectsMissingTarget()
    {
        var validation = _validator.Validate(
        [
            CreateRule(
                ruleId: "R1",
                mode: NaturalVentilationControlMode.FixedFraction,
                openingId: null,
                fixedFraction: 0.5,
                roomId: null,
                zoneId: null)
        ]);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-TARGET-MISSING");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void RejectsInvalidFraction(double fraction)
    {
        var validation = _validator.Validate(
        [
            CreateRule(
                ruleId: "R1",
                mode: NaturalVentilationControlMode.FixedFraction,
                openingId: "O1",
                fixedFraction: fraction)
        ]);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-FRACTION-INVALID");
    }

    [Fact]
    public void RejectsMinimumGreaterThanMaximum()
    {
        var validation = _validator.Validate(
        [
            CreateRule(
                ruleId: "R1",
                mode: NaturalVentilationControlMode.FixedFraction,
                openingId: "O1",
                fixedFraction: 0.5,
                minFraction: 0.8,
                maxFraction: 0.2)
        ]);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-MIN-GREATER-THAN-MAX");
    }

    [Fact]
    public void ReportsTemperatureThresholdMissing()
    {
        var validation = _validator.Validate(
        [
            CreateRule(
                ruleId: "R1",
                mode: NaturalVentilationControlMode.Temperature,
                openingId: "O1",
                fixedFraction: null)
        ]);

        Assert.Contains(validation.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-TEMPERATURE-THRESHOLD-MISSING");
    }

    private static NaturalVentilationOpeningControlRule CreateRule(
        string ruleId,
        NaturalVentilationControlMode mode,
        string? openingId,
        double? fixedFraction,
        string? roomId = "R1",
        string? zoneId = "Z1",
        double? minFraction = null,
        double? maxFraction = null) =>
        new(
            RuleId: ruleId,
            OpeningId: openingId,
            RoomId: roomId,
            ZoneId: zoneId,
            ControlMode: mode,
            NightVentilationMode: NaturalVentilationNightVentilationMode.Disabled,
            FixedOpeningFraction: fixedFraction,
            MinimumOpeningFraction: minFraction,
            MaximumOpeningFraction: maxFraction,
            IndoorTemperatureOpenAboveCelsius: null,
            IndoorTemperatureCloseBelowCelsius: null,
            OutdoorTemperatureMinimumCelsius: null,
            OutdoorTemperatureMaximumCelsius: null,
            IndoorOutdoorTemperatureDifferenceMinimumKelvin: null,
            RequiresOccupancy: null,
            ScheduleId: null,
            OccupancyProfileId: null,
            Source: "UnitTest",
            Diagnostics: []);
}
