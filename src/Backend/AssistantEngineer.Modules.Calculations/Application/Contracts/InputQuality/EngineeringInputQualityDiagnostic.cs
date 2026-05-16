namespace AssistantEngineer.Modules.Calculations.Application.Contracts.InputQuality;

public sealed record EngineeringInputQualityDiagnostic(
    string Code,
    EngineeringInputQualitySeverity Severity,
    string Message,
    string Category,
    string? Field = null,
    string? Unit = null,
    string? Recommendation = null,
    string? Source = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
