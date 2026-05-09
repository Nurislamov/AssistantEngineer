using AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services.ExternalComparison;

internal sealed class ExternalComparisonCaseValidator
{
    private static readonly string[] UnsupportedClaims =
    [
        "EnergyPlus " + "validated",
        "ASHRAE 140 " + "validated",
        "BESTEST " + "passed"
    ];

    public Result Validate(ExternalComparisonCase comparisonCase)
    {
        if (string.IsNullOrWhiteSpace(comparisonCase.CaseId))
            return Result.Validation("External comparison case id is required.");

        if (string.IsNullOrWhiteSpace(comparisonCase.Workflow))
            return Result.Validation($"External comparison case '{comparisonCase.CaseId}' workflow is required.");

        if (string.IsNullOrWhiteSpace(comparisonCase.ModelInputPath))
            return Result.Validation($"External comparison case '{comparisonCase.CaseId}' model input path is required.");

        if (ContainsUnsupportedClaim(comparisonCase.ClaimBoundary) ||
            comparisonCase.Notes.Any(ContainsUnsupportedClaim))
        {
            return Result.Validation(
                $"External comparison case '{comparisonCase.CaseId}' contains unsupported validation/compliance claims.");
        }

        var requiresProvenance = comparisonCase.Status is ExternalComparisonStatus.ExternalOutputImported
            or ExternalComparisonStatus.PassedTolerance;
        if (requiresProvenance && !HasProvenance(comparisonCase.Provenance))
        {
            return Result.Validation(
                $"External comparison case '{comparisonCase.CaseId}' requires provenance for status '{comparisonCase.Status}'.");
        }

        var requiresExpectedOutput = comparisonCase.Status is ExternalComparisonStatus.Compared
            or ExternalComparisonStatus.PassedTolerance
            or ExternalComparisonStatus.FailedTolerance;
        if (requiresExpectedOutput && !HasExpectedOutput(comparisonCase.ExpectedOutput))
        {
            return Result.Validation(
                $"External comparison case '{comparisonCase.CaseId}' requires expected output for status '{comparisonCase.Status}'.");
        }

        if (comparisonCase.Status == ExternalComparisonStatus.PassedTolerance && comparisonCase.Tolerance is null)
        {
            return Result.Validation(
                $"External comparison case '{comparisonCase.CaseId}' requires tolerance for status '{comparisonCase.Status}'.");
        }

        return Result.Success();
    }

    private static bool HasProvenance(ExternalComparisonProvenance? provenance) =>
        provenance is not null &&
        !string.IsNullOrWhiteSpace(provenance.SourceTool) &&
        !string.IsNullOrWhiteSpace(provenance.ArtifactPath);

    private static bool HasExpectedOutput(ExternalComparisonExpectedOutput? expectedOutput) =>
        expectedOutput is not null &&
        !string.IsNullOrWhiteSpace(expectedOutput.OutputPath);

    private static bool ContainsUnsupportedClaim(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return UnsupportedClaims.Any(marker => content.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
