namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public enum EngineeringGovernanceStageStatus
{
    Unknown = 0,
    ClosedInternalGate = 1,
    InternalGovernanceAnchor = 2,
    InternalValidationAnchor = 3,
    InternalApplicationIntegrationAnchor = 4,
    InternalReleaseReadinessGate = 5,
    InternalStatusDisclosureAnchor = 6
}
