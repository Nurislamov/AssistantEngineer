using System.Text.Json;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;
using AssistantEngineer.Modules.Benchmarks.Application.Services.ExternalComparison;

namespace AssistantEngineer.Tests.Validation.ExternalComparison;

public sealed class ExternalComparisonWorkflowFoundationTests
{
    private readonly ExternalComparisonCaseValidator _validator = new();
    private readonly EnergyPlusComparisonResultBuilder _resultBuilder = new();

    [Fact]
    public void Registry_LoadsCases()
    {
        var registry = LoadRegistry();
        Assert.NotNull(registry);
        Assert.NotEmpty(registry.Cases);
    }

    [Fact]
    public void CaseWithoutExternalOutput_IsNotMarkedPassed()
    {
        var fixtureDefinedCase = LoadRegistry()
            .Cases
            .First(item => item.Status == ExternalComparisonStatus.FixtureDefined);

        var result = _resultBuilder.Build(fixtureDefinedCase, importedExternalOutput: null);

        Assert.NotEqual(ExternalComparisonStatus.PassedTolerance, result.Status);
        Assert.NotEqual(true, result.PassedTolerance);
    }

    [Fact]
    public void PassedStatus_RequiresExpectedOutputAndComparisonMetadata()
    {
        var invalidCase = new ExternalComparisonCase
        {
            CaseId = "EC-TEST-INVALID-PASS",
            Name = "invalid pass case",
            Workflow = "EnergyPlus comparison workflow",
            ValidationAnchor = "internal engineering anchor",
            ModelInputPath = "tests/fixtures/external-comparison/energyplus/ep-smoke-foundation.case.json",
            Status = ExternalComparisonStatus.PassedTolerance,
            ClaimBoundary = "not full validation; not compliance claim"
        };

        var validation = _validator.Validate(invalidCase);

        Assert.True(validation.IsFailure);
    }

    [Fact]
    public void Provenance_IsRequiredForImportedOrPassedStatus()
    {
        var importedCase = new ExternalComparisonCase
        {
            CaseId = "EC-TEST-IMPORTED",
            Name = "imported without provenance",
            Workflow = "EnergyPlus comparison workflow",
            ValidationAnchor = "internal engineering anchor",
            ModelInputPath = "tests/fixtures/external-comparison/energyplus/ep-smoke-foundation.case.json",
            Status = ExternalComparisonStatus.ExternalOutputImported,
            ClaimBoundary = "not full validation; not compliance claim"
        };

        var validation = _validator.Validate(importedCase);

        Assert.True(validation.IsFailure);
    }

    [Fact]
    public void UnsupportedClaims_AreRejected()
    {
        var unsupportedEnergyPlusValidated = "EnergyPlus " + "validated";
        var unsupportedClaimCase = new ExternalComparisonCase
        {
            CaseId = "EC-TEST-UNSUPPORTED-CLAIM",
            Name = "unsupported claim case",
            Workflow = "EnergyPlus comparison workflow",
            ValidationAnchor = "internal engineering anchor",
            ModelInputPath = "tests/fixtures/external-comparison/energyplus/ep-smoke-foundation.case.json",
            Status = ExternalComparisonStatus.Planned,
            ClaimBoundary = unsupportedEnergyPlusValidated
        };

        var validation = _validator.Validate(unsupportedClaimCase);

        Assert.True(validation.IsFailure);
    }

    [Fact]
    public void Documentation_UsesComparisonWorkflowWording()
    {
        var unsupportedEnergyPlusValidated = "EnergyPlus " + "validated";
        var unsupportedAshraeValidated = "ASHRAE 140 " + "validated";
        var unsupportedBestestPassed = "BESTEST " + "passed";
        var docPaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "ExternalComparisonWorkflow.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "EnergyPlusComparisonWorkflow.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "Ashrae140BestestStyleAnchors.md"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "external-comparison", "energyplus", "README.md"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "external-comparison", "ashrae140-style", "README.md")
        };

        foreach (var path in docPaths)
        {
            Assert.True(File.Exists(path), $"Required external comparison documentation file is missing: {path}");
            var content = File.ReadAllText(path);
            var normalized = content.ToLowerInvariant();

            Assert.Contains("comparison", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("not full validation", content, StringComparison.OrdinalIgnoreCase);

            Assert.False(
                normalized.Contains(unsupportedEnergyPlusValidated.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("not " + unsupportedEnergyPlusValidated.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("no " + unsupportedEnergyPlusValidated.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase),
                $"Positive unsupported claim detected in {path}: {unsupportedEnergyPlusValidated}");

            Assert.False(
                normalized.Contains(unsupportedAshraeValidated.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("not " + unsupportedAshraeValidated.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("no " + unsupportedAshraeValidated.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase),
                $"Positive unsupported claim detected in {path}: {unsupportedAshraeValidated}");

            Assert.False(
                normalized.Contains(unsupportedBestestPassed.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("not " + unsupportedBestestPassed.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("no " + unsupportedBestestPassed.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                !normalized.Contains("do not claim " + unsupportedBestestPassed.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase),
                $"Positive unsupported claim detected in {path}: {unsupportedBestestPassed}");
        }
    }

    private static ExternalComparisonCaseRegistry LoadRegistry()
    {
        var path = Path.Combine(TestPaths.RepoRoot, "docs", "validation", "ExternalComparisonCaseRegistry.json");
        var registry = JsonSerializer.Deserialize<ExternalComparisonCaseRegistry>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        Assert.NotNull(registry);
        return registry!;
    }
}
