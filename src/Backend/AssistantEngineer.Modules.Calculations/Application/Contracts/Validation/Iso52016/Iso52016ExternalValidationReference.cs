namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

public sealed record Iso52016ExternalValidationReference(
    string DerivationDocument,
    string DerivationKind,
    string SourceDescription,
    string? MethodologySourceName = null,
    string? MethodologySourceUrl = null,
    string? MethodologySourceCommit = null,
    IReadOnlyList<string>? MethodologyNotes = null);
