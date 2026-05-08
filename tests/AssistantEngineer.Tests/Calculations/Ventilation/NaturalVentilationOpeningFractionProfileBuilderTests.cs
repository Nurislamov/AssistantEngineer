using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationOpeningFractionProfileBuilderTests
{
    private readonly NaturalVentilationOpeningFractionProfileBuilder _builder = new(
        new NaturalVentilationOpeningControlEvaluator(
            new NaturalVentilationControlRuleValidator(),
            new StandardCalculationDisclosureFactory()));

    [Fact]
    public void BuildsOpeningFractionProfileFor24Hours()
    {
        var result = _builder.BuildProfiles(new NaturalVentilationControlEvaluationInput(
            Rules: [CreateRule("R1", openingId: "O1", fixedFraction: 0.5)],
            HourlyContexts: CreateContexts(24),
            DisclosureOverride: null,
            Source: "UnitTest"));

        Assert.True(result.OpeningFractionProfilesByOpeningId.ContainsKey("O1"));
        Assert.Equal(24, result.OpeningFractionProfilesByOpeningId["O1"].Count);
    }

    [Fact]
    public void BuildsOpeningFractionProfileFor8760Hours()
    {
        var result = _builder.BuildProfiles(new NaturalVentilationControlEvaluationInput(
            Rules: [CreateRule("R1", openingId: "O1", fixedFraction: 0.5)],
            HourlyContexts: CreateContexts(8760),
            DisclosureOverride: null,
            Source: "UnitTest"));

        Assert.True(result.OpeningFractionProfilesByOpeningId.ContainsKey("O1"));
        Assert.Equal(8760, result.OpeningFractionProfilesByOpeningId["O1"].Count);
    }

    [Fact]
    public void MultipleRulesUseMaximumFraction()
    {
        var result = _builder.BuildProfiles(new NaturalVentilationControlEvaluationInput(
            Rules:
            [
                CreateRule("R1", openingId: "O1", fixedFraction: 0.3),
                CreateRule("R2", openingId: "O1", fixedFraction: 0.7)
            ],
            HourlyContexts: CreateContexts(24),
            DisclosureOverride: null,
            Source: "UnitTest"));

        Assert.Equal(0.7, result.OpeningFractionProfilesByOpeningId["O1"][0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-MULTIPLE-RULES-MAX-FRACTION-USED");
    }

    [Fact]
    public void FillsHourlyGapsFor8760Profile()
    {
        var contexts = Enumerable.Range(0, 8759)
            .Select(hour => CreateContext(hour))
            .Append(CreateContext(9000))
            .ToArray();

        var result = _builder.BuildProfiles(new NaturalVentilationControlEvaluationInput(
            Rules: [CreateRule("R1", openingId: "O1", fixedFraction: 0.5)],
            HourlyContexts: contexts,
            DisclosureOverride: null,
            Source: "UnitTest"));

        Assert.Equal(8760, result.OpeningFractionProfilesByOpeningId["O1"].Count);
        Assert.Equal(0.0, result.OpeningFractionProfilesByOpeningId["O1"][8759], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-HOURLY-GAPS-FILLED");
    }

    [Fact]
    public void BuildsRoomAndZoneProfiles()
    {
        var result = _builder.BuildProfiles(new NaturalVentilationControlEvaluationInput(
            Rules:
            [
                CreateRule("R1", openingId: null, roomId: "R1", zoneId: "Z1", fixedFraction: 0.4),
                CreateRule("R2", openingId: null, roomId: "R2", zoneId: "Z2", fixedFraction: 0.6)
            ],
            HourlyContexts:
            [
                CreateContext(0, roomId: "R1", zoneId: "Z1"),
                CreateContext(1, roomId: "R2", zoneId: "Z2")
            ],
            DisclosureOverride: null,
            Source: "UnitTest"));

        Assert.True(result.RoomOpeningFractionProfilesByRoomId.ContainsKey("R1"));
        Assert.True(result.ZoneOpeningFractionProfilesByZoneId.ContainsKey("Z1"));
    }

    private static IReadOnlyList<NaturalVentilationHourlyControlContext> CreateContexts(int count) =>
        Enumerable.Range(0, count)
            .Select(hour => CreateContext(hour))
            .ToArray();

    private static NaturalVentilationHourlyControlContext CreateContext(
        int hour,
        string roomId = "R1",
        string zoneId = "Z1") =>
        new(
            HourIndex: hour,
            IndoorTemperatureCelsius: 24.0,
            OutdoorTemperatureCelsius: 18.0,
            WindSpeedMetersPerSecond: 2.0,
            OccupancyFraction: 1.0,
            ScheduleFraction: 1.0,
            IsNightHour: hour % 24 is >= 22 or <= 5,
            RoomId: roomId,
            ZoneId: zoneId,
            Diagnostics: []);

    private static NaturalVentilationOpeningControlRule CreateRule(
        string id,
        string? openingId,
        double fixedFraction,
        string roomId = "R1",
        string zoneId = "Z1") =>
        new(
            RuleId: id,
            OpeningId: openingId,
            RoomId: roomId,
            ZoneId: zoneId,
            ControlMode: NaturalVentilationControlMode.FixedFraction,
            NightVentilationMode: NaturalVentilationNightVentilationMode.Disabled,
            FixedOpeningFraction: fixedFraction,
            MinimumOpeningFraction: null,
            MaximumOpeningFraction: null,
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
