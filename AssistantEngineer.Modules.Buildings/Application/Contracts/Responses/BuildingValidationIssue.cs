using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed record BuildingValidationIssue(
    string Code,
    BuildingCalculationReadinessSeverity Severity,
    string Location,
    string Message,
    bool CanAutoFix = false);