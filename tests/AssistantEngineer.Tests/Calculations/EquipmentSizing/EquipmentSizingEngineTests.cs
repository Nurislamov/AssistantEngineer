using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;

namespace AssistantEngineer.Tests.Calculations.EquipmentSizing;

public class EquipmentSizingEngineTests
{
    private readonly EquipmentSizingEngine _engine = new();

    [Fact]
    public void Calculate_AppliesCoolingSafetyFactor()
    {
        var result = _engine.Calculate(new EquipmentSizingInput(
            TargetId: 1,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 5000,
            SafetyFactor: 1.1,
            Candidates:
            [
                Candidate(1, coolingW: 6000)
            ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(5500, result.Value.RequiredCoolingCapacityWithReserveW, precision: 6);
    }

    [Fact]
    public void Calculate_AcceptsCandidateWithSufficientCapacity()
    {
        var result = _engine.Calculate(new EquipmentSizingInput(
            TargetId: 1,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 5000,
            SafetyFactor: 1.1,
            Candidates:
            [
                Candidate(1, coolingW: 6000)
            ]));

        Assert.True(result.IsSuccess);
        var accepted = Assert.Single(result.Value.RecommendedEquipment);
        Assert.Equal(500, accepted.CoolingMarginW, precision: 6);
        Assert.Equal(9.090909, accepted.CoolingMarginPercent!.Value, precision: 6);
        Assert.Empty(result.Value.RejectedEquipment);
    }

    [Fact]
    public void Calculate_RejectsCandidateWithInsufficientCapacity()
    {
        var result = _engine.Calculate(new EquipmentSizingInput(
            TargetId: 1,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 5000,
            SafetyFactor: 1.1,
            Candidates:
            [
                Candidate(1, coolingW: 5000)
            ]));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.RecommendedEquipment);
        var rejected = Assert.Single(result.Value.RejectedEquipment);
        Assert.Contains("insufficient cooling capacity", rejected.Reasons);
    }

    [Fact]
    public void Calculate_RejectsInactiveCandidate()
    {
        var result = _engine.Calculate(new EquipmentSizingInput(
            TargetId: 1,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 5000,
            SafetyFactor: 1.1,
            Candidates:
            [
                Candidate(1, coolingW: 6000, isActive: false)
            ]));

        Assert.True(result.IsSuccess);
        var rejected = Assert.Single(result.Value.RejectedEquipment);
        Assert.Contains("inactive equipment", rejected.Reasons);
    }

    [Fact]
    public void Calculate_NoEquipmentFoundAddsDiagnostic()
    {
        var result = _engine.Calculate(new EquipmentSizingInput(
            TargetId: 1,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 5000,
            SafetyFactor: 1.1,
            Candidates: []));

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "EquipmentSizing.NoEquipmentFound");
    }

    [Fact]
    public void Calculate_SelectsBestMatchBySmallestPositiveMargin()
    {
        var result = _engine.Calculate(new EquipmentSizingInput(
            TargetId: 1,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 5000,
            SafetyFactor: 1.1,
            Candidates:
            [
                Candidate(1, coolingW: 8000),
                Candidate(2, coolingW: 6000),
                Candidate(3, coolingW: 7000)
            ]));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.BestMatch);
        Assert.Equal(2, result.Value.BestMatch!.EquipmentId);
    }

    [Fact]
    public void Calculate_DefaultSafetyFactorIsDocumentedByDiagnostic()
    {
        var result = _engine.Calculate(new EquipmentSizingInput(
            TargetId: 1,
            TargetType: EquipmentSizingTargetType.Room,
            RequiredHeatingLoadW: 0,
            RequiredCoolingLoadW: 5000,
            SafetyFactor: null,
            Candidates:
            [
                Candidate(1, coolingW: 6000)
            ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(1.1, result.Value.SafetyFactor, precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "EquipmentSizing.DefaultSafetyFactorUsed");
    }

    private static EquipmentSizingCandidateInput Candidate(
        int id,
        double? heatingW = null,
        double? coolingW = null,
        bool isActive = true,
        string equipmentType = "DX") =>
        new(
            id,
            $"Equipment {id}",
            $"Model {id}",
            equipmentType,
            heatingW,
            coolingW,
            isActive);
}
