namespace AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;

public sealed record EngineeringCoreV1DiagnosticsCatalogResponse(
    string CatalogName,
    string Version,
    string Status,
    EngineeringCoreV1DiagnosticsRules Rules,
    IReadOnlyList<EngineeringCoreV1DiagnosticCatalogItem> Diagnostics);

public sealed record EngineeringCoreV1DiagnosticsRules(
    string Error,
    string Warning,
    string Info,
    string SuccessRule);

public sealed record EngineeringCoreV1DiagnosticCatalogItem(
    string Code,
    string Severity,
    string Category,
    string UserMessage,
    string UserAction,
    string ClosedV1Gate);
