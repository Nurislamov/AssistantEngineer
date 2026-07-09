namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportValidationResult(
    bool IsAccepted,
    IReadOnlyList<GreeAliceRegistryImportValidationIssue> Issues)
{
    public static GreeAliceRegistryImportValidationResult Accepted { get; } = new(true, []);
}
