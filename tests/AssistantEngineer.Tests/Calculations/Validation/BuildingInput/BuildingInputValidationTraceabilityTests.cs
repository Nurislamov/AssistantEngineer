using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Validation.BuildingInput;

public sealed class BuildingInputValidationTraceabilityTests
{
    [Fact]
    public void DocsManifestAndFixtures_Exist()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "validation",
            "BuildingInputValidationAndCorrectionFramework.md");
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "BuildingInputValidationFrameworkManifest.json");
        var fixturesPath = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "fixtures",
            "building-input-validation");

        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");
        Assert.True(File.Exists(manifestPath), $"Manifest file was not found: {manifestPath}");
        Assert.True(Directory.Exists(fixturesPath), $"Fixture directory was not found: {fixturesPath}");
        Assert.NotEmpty(Directory.GetFiles(fixturesPath, "*.json", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void Manifest_DeclaresStageAndClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "BuildingInputValidationFrameworkManifest.json");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-BUI-VALIDATION-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-governance-anchor", root.GetProperty("status").GetString());

        var claimBoundary = root.GetProperty("claimBoundary")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("Building input validation and correction framework.", claimBoundary);
        Assert.Contains("No automatic production data mutation.", claimBoundary);
        AssertTokenAppearsOnlyAsNegatedClaim(string.Join('\n', claimBoundary), "full ISO/EN compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(string.Join('\n', claimBoundary), "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(string.Join('\n', claimBoundary), "EnergyPlus comparison workflow");
        AssertTokenAppearsOnlyAsNegatedClaim(string.Join('\n', claimBoundary), "ASHRAE 140 / BESTEST-style validation anchor");
    }

    [Fact]
    public void DisclosureFiles_DoNotContainUnsupportedValidationOrCertificationClaims()
    {
        var paths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "validation", "BuildingInputValidationAndCorrectionFramework.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "BuildingInputValidationFrameworkManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "ExternalReferenceValidationVerification.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "EngineeringCoreV1Scope.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "status.sample.json")
        };

        foreach (var path in paths)
        {
            var text = File.ReadAllText(path);
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full ISO compliance");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full EN compliance");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "StandardReference equivalence");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "EnergyPlus comparison workflow");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "ASHRAE 140 / BESTEST-style validated");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "ExternalReferenceCovered");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "certified");
        }
    }

    private static void AssertTokenAppearsOnlyAsNegatedClaim(string text, string token)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(token, StringComparison.OrdinalIgnoreCase))
                continue;

            Assert.True(
                line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
                line.TrimStart().StartsWith("- `", StringComparison.Ordinal),
                $"Token '{token}' appears without negation in line: {line}");
        }
    }
}
