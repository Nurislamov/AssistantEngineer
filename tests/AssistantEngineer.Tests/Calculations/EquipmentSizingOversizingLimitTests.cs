using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;

namespace AssistantEngineer.Tests.Calculations;

public class EquipmentSizingOversizingLimitTests
{
    [Fact]
    public void MaximumOversizingPercentRejectsHeatingCandidateThatIsTooLarge()
    {
        var engine = new EquipmentSizingEngine();

        var result = engine.Calculate(new EquipmentSizingInput(
            TargetId: 401,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 10_000,
            RequiredCoolingLoadW: 0,
            SafetyFactor: 1.0,
            Candidates:
            [
                new EquipmentSizingCandidateInput(
                    EquipmentId: 1,
                    Name: "Too large boiler",
                    Model: "XL-1",
                    EquipmentType: "Boiler",
                    HeatingCapacityW: 20_000,
                    CoolingCapacityW: null),

                new EquipmentSizingCandidateInput(
                    EquipmentId: 2,
                    Name: "Right sized boiler",
                    Model: "M-1",
                    EquipmentType: "Boiler",
                    HeatingCapacityW: 11_000,
                    CoolingCapacityW: null)
            ],
            EquipmentType: "Boiler",
            DiagnosticsContext: "equipment-sizing-oversizing",
            MaximumOversizingPercent: 20));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Single(result.Value.RecommendedEquipment);
        Assert.Equal(2, result.Value.BestMatch!.EquipmentId);
        Assert.Equal(1_000, result.Value.BestMatch.HeatingMarginW, precision: 6);
        Assert.Equal(10, result.Value.BestMatch.HeatingMarginPercent!.Value, precision: 6);

        var rejected = Assert.Single(result.Value.RejectedEquipment);
        Assert.Equal(1, rejected.EquipmentId);
        Assert.Contains("excessive heating oversizing", rejected.Reasons);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Info &&
            diagnostic.Code == "EquipmentSizing.MaximumOversizingLimitApplied" &&
            diagnostic.Context == "equipment-sizing-oversizing");
    }

    [Fact]
    public void MaximumOversizingPercentRejectsCoolingCandidateThatIsTooLarge()
    {
        var engine = new EquipmentSizingEngine();

        var result = engine.Calculate(new EquipmentSizingInput(
            TargetId: 402,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 8_000,
            SafetyFactor: 1.0,
            Candidates:
            [
                new EquipmentSizingCandidateInput(
                    EquipmentId: 1,
                    Name: "Too large chiller",
                    Model: "XL-C",
                    EquipmentType: "Chiller",
                    HeatingCapacityW: null,
                    CoolingCapacityW: 13_000),

                new EquipmentSizingCandidateInput(
                    EquipmentId: 2,
                    Name: "Right sized chiller",
                    Model: "M-C",
                    EquipmentType: "Chiller",
                    HeatingCapacityW: null,
                    CoolingCapacityW: 8_800)
            ],
            EquipmentType: "Chiller",
            MaximumOversizingPercent: 15));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Single(result.Value.RecommendedEquipment);
        Assert.Equal(2, result.Value.BestMatch!.EquipmentId);

        var rejected = Assert.Single(result.Value.RejectedEquipment);
        Assert.Equal(1, rejected.EquipmentId);
        Assert.Contains("excessive cooling oversizing", rejected.Reasons);
    }

    [Fact]
    public void MissingMaximumOversizingPercentKeepsMatrixCapacityOnlySelection()
    {
        var engine = new EquipmentSizingEngine();

        var result = engine.Calculate(new EquipmentSizingInput(
            TargetId: 403,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 10_000,
            RequiredCoolingLoadW: 0,
            SafetyFactor: 1.0,
            Candidates:
            [
                new EquipmentSizingCandidateInput(
                    EquipmentId: 1,
                    Name: "Large boiler",
                    Model: "XL-1",
                    EquipmentType: "Boiler",
                    HeatingCapacityW: 20_000,
                    CoolingCapacityW: null)
            ],
            EquipmentType: "Boiler"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Single(result.Value.RecommendedEquipment);
        Assert.Empty(result.Value.RejectedEquipment);
        Assert.Equal(1, result.Value.BestMatch!.EquipmentId);

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "EquipmentSizing.MaximumOversizingLimitApplied");
    }

    [Fact]
    public void InvalidMaximumOversizingPercentFailsValidation()
    {
        var engine = new EquipmentSizingEngine();

        var result = engine.Calculate(new EquipmentSizingInput(
            TargetId: 404,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 10_000,
            RequiredCoolingLoadW: 0,
            SafetyFactor: 1.0,
            Candidates: [],
            MaximumOversizingPercent: 0));

        Assert.True(result.IsFailure);
        Assert.Contains("EquipmentSizing.InvalidMaximumOversizingPercent", result.Error);
    }
}

