using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Calculations.Governance;

public sealed class EngineeringClaimBoundaryScannerTests
{
    private readonly EngineeringClaimBoundaryScanner _scanner = new();

    [Fact]
    public void NegatedClaims_Pass()
    {
        var file = WriteTempFile(
            "negated.md",
            "No StandardReference equivalence claim.\nNo EnergyPlus comparison workflow claim.\nNo external certification claim.");

        var result = _scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: [file]);

        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void PositiveForbiddenClaims_Fail()
    {
        var file = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "governance",
            "claim-scanner-inputs",
            "positive-forbidden-sample.md");

        var result = _scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: [file]);

        Assert.True(result.ErrorCount > 0);
    }

    [Fact]
    public void Scanner_ReportsLineNumber()
    {
        var file = WriteTempFile(
            "line-check.md",
            "No StandardReference equivalence claim.\nStandardReference equivalence\n");

        var result = _scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: [file]);

        var diagnostic = Assert.Single(result.Diagnostics, item => item.Token == "StandardReference equivalence");
        Assert.Equal(2, diagnostic.LineNumber);
    }

    [Fact]
    public void RepositoryScan_PassesForDocsAndFixtures()
    {
        var result = _scanner.ScanRepository(TestPaths.RepoRoot);

        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void ExternalReferenceCovered_IsAllowedOnlyInNegativeContext()
    {
        var negativeFile = WriteTempFile(
            "external-equivalence-negative.md",
            "No ExternalReferenceCovered claim.");
        var positiveFile = WriteTempFile(
            "external-equivalence-positive.md",
            "ExternalReferenceCovered");

        var negativeResult = _scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: [negativeFile]);
        var positiveResult = _scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: [positiveFile]);

        Assert.Equal(0, negativeResult.ErrorCount);
        Assert.True(positiveResult.ErrorCount > 0);
    }

    [Fact]
    public void ForbiddenOverclaimPhrases_AreAllowedInNonClaimsContext()
    {
        var ashraeValidated = "ASHRAE 140 " + "validated";
        var file = WriteTempFile(
            "non-claims-context.md",
            $"""
            ## Required non-claims
            This document does not claim:
            - exact EnergyPlus numerical equivalence;
            - {ashraeValidated};
            - full ISO compliance;
            - certified;
            """);

        var result = _scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: [file]);

        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void ForbiddenOverclaimPhrases_FailOutsideNonClaimsContext()
    {
        var ashraeValidated = "ASHRAE 140 " + "validated";
        var file = WriteTempFile(
            "positive-overclaims.md",
            $"""
            This release is exact EnergyPlus numerical equivalence.
            {ashraeValidated}.
            full ISO compliance.
            certified.
            """);

        var result = _scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: [file]);

        Assert.True(result.ErrorCount >= 4);
    }

    private static string WriteTempFile(string fileName, string content)
    {
        var directory = Path.Combine(TestPaths.RepoRoot, "artifacts", "generated", "governance-tests");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }
}
