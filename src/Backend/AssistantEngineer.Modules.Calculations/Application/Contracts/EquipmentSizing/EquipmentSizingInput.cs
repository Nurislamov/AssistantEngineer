namespace AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;

public sealed record EquipmentSizingInput(
    int TargetId,
    EquipmentSizingTargetType TargetType,
    double RequiredHeatingLoadW,
    double RequiredCoolingLoadW,
    double? SafetyFactor,
    IReadOnlyList<EquipmentSizingCandidateInput> Candidates,
    string? EquipmentType = null,
    string? DiagnosticsContext = null,
    double? HeatingSafetyFactor = null,
    double? CoolingSafetyFactor = null,
    double? MaximumOversizingPercent = null);

