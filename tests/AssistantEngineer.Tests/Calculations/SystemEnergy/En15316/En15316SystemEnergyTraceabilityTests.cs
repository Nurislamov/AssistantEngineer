using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316SystemEnergyTraceabilityTests
{
    [Fact]
    public void StageManifest_ExistsAndDeclaresStageAndClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "En15316SystemEnergyChainStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-EN15316-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-anchor", root.GetProperty("status").GetString());

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("EN15316-inspired modular system energy engineering calculator.", claimBoundary);
        Assert.Contains("No full EN 15316 compliance claim.", claimBoundary);
        Assert.Contains("No StandardReference equivalence claim.", claimBoundary);
        Assert.Contains("No EnergyPlus comparison workflow claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor claim.", claimBoundary);
        Assert.Contains("No external certification claim.", claimBoundary);
        Assert.DoesNotContain("ExternalReferenceCovered", claimBoundary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocsAndFixtures_ExistAndDoNotContainUnsupportedValidationClaims()
    {
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "system-energy",
            "En15316SystemEnergyChainCalculator.md");
        var circuitDocPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "En15316SystemEnergyCircuitModel.md");

        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");
        Assert.True(File.Exists(circuitDocPath), $"Documentation file was not found: {circuitDocPath}");

        var docText = File.ReadAllText(docPath);
        var circuitDocText = File.ReadAllText(circuitDocPath);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full EN 15316 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus comparison workflow");
        Assert.DoesNotContain("EN 15316 validated", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(circuitDocText, "full EN15316 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(circuitDocText, "external validation");
        Assert.Contains("handoff", circuitDocText, StringComparison.OrdinalIgnoreCase);

        var fixturePaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "boiler-heating-emission-distribution-generation.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "condensing-boiler-heating-with-recovered-losses.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "heat-pump-heating-electricity-primary.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "chiller-cooling-electricity-primary.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "dhw-storage-distribution-generation-chain.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "boiler-simple-circuit.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "heat-pump-simple-circuit.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "distribution-losses-enabled.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "system-energy", "en15316", "zero-demand.json")
        };

        foreach (var path in fixturePaths)
        {
            Assert.True(File.Exists(path), $"Fixture file was not found: {path}");
            var text = File.ReadAllText(path);
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full EN 15316 compliance");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "StandardReference equivalence");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "EnergyPlus comparison workflow");
            Assert.DoesNotContain("ExternalReferenceCovered", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DisclosureFiles_ReflectDefaultCompatibilityAndOptInIntegrationStatus()
    {
        var validationDocPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "ExternalReferenceValidationVerification.md");
        var scopeDocPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1Scope.md");

        Assert.True(File.Exists(validationDocPath), $"validation doc was not found: {validationDocPath}");
        Assert.True(File.Exists(scopeDocPath), $"Scope doc was not found: {scopeDocPath}");

        var validationDoc = File.ReadAllText(validationDocPath);
        var scopeDoc = File.ReadAllText(scopeDocPath);

        Assert.Contains("SystemEnergyEngine", validationDoc);
        Assert.Contains("remains default", validationDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("opt-in", validationDoc, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("EN15316-inspired", scopeDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("opt-in", scopeDoc, StringComparison.OrdinalIgnoreCase);

        AssertTokenAppearsOnlyAsNegatedClaim(validationDoc, "full EN 15316 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(scopeDoc, "full EN 15316 compliance");
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
                line.Contains("must not", StringComparison.OrdinalIgnoreCase),
                $"Token '{token}' appears without negation in line: {line}");
        }
    }
}
