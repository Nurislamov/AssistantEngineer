namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

public sealed record EngineeringReportValue(
    string Key,
    string Label,
    object? Value,
    EngineeringReportUnit? Unit = null,
    int DisplayPrecision = 2,
    string? Source = null,
    IReadOnlyList<string>? Tags = null);

