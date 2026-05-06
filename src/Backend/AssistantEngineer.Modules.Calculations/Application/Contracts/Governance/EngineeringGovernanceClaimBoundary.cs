namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public sealed record EngineeringGovernanceClaimBoundary(
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> RequiredNonClaims);
