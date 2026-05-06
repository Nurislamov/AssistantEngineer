namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public sealed record EngineeringGovernanceManifestReference(
    string StageId,
    string ManifestPath,
    bool Exists,
    bool IsRequired,
    bool IsStrictRequired);
