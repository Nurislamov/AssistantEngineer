namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public sealed record FormulaAuditFeature(
    string CalculationId,
    string Name,
    string Formula,
    string Units,
    string SourcePrinciple,
    string ImplementationArea,
    string Diagnostics,
    string Tests,
    FormulaAuditStatus Status,
    FormulaAuditPriority Priority,
    string Limitations);

public enum FormulaAuditStatus
{
    ClosedV1,
    Partial,
    OutOfScopeV1,
    PlannedValidation
}

public enum FormulaAuditPriority
{
    P0,
    P1,
    P2,
    P3
}