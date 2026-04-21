using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed record BuildingCalculationReadinessIssue(
    BuildingCalculationReadinessSeverity Severity,
    string Path,
    string Message);