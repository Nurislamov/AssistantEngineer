using System.Text.Json;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

public sealed record Iso52016ExternalValidationFixture(
    string Id,
    Iso52016ExternalValidationFixtureSourceKind SourceKind,
    Iso52016ExternalValidationCalculationPath CalculationPath,
    IReadOnlyList<string> ClaimBoundary,
    Iso52016ExternalValidationReference? Reference,
    JsonElement Input,
    Iso52016ExternalValidationExpectedResult Expected,
    Iso52016ExternalValidationTolerance Tolerance);
