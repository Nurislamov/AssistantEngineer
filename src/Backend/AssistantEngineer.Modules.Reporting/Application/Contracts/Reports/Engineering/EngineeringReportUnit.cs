namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

public sealed record EngineeringReportUnit(
    string Symbol,
    string? QuantityKind = null,
    string? DisplayName = null);

