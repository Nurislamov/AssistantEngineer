namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;

public enum BuildingInputValidationReadinessStatus
{
    Ready = 0,
    ReadyWithWarnings = 1,
    BlockedByErrors = 2,
    BlockedByCriticalErrors = 3,
    NotEvaluated = 4
}
