namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public sealed record EngineeringGovernanceStageDependency(
    string StageId,
    bool IsExternalReference = false,
    string? Reason = null);
