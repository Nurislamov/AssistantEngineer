namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportValidationIssue(
    string Code,
    string Message,
    string Severity,
    string Path);
