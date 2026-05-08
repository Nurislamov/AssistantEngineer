using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterTraceabilityTests
{
    [Fact]
    public void StageManifest_ExistsAndDeclaresStageAndClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso12831DomesticHotWaterDemandStageManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-DHW-001", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-engineering-anchor", root.GetProperty("status").GetString());

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("ISO12831-3-inspired domestic hot water engineering calculator.", claimBoundary);
        Assert.Contains("No full ISO 12831-3 compliance claim.", claimBoundary);
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
            "domestic-hot-water",
            "Iso12831DomesticHotWaterDemandCalculator.md");

        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");

        var docText = File.ReadAllText(docPath);
        Assert.DoesNotContain("ISO 12831-3 validated", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalReferenceCovered", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full ISO 12831-3 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus comparison workflow");

        var fixturePaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "residential-people-based-basic.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "office-area-based-daytime.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "hotel-unit-based-morning-evening.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "custom-volume-flat-profile.json"),
            Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "domestic-hot-water", "iso12831", "zero-occupants-zero-demand.json")
        };

        foreach (var path in fixturePaths)
        {
            Assert.True(File.Exists(path), $"Fixture file was not found: {path}");
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("ExternalReferenceCovered", text, StringComparison.OrdinalIgnoreCase);
            AssertTokenAppearsOnlyAsNegatedClaim(text, "StandardReference equivalence");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "EnergyPlus comparison workflow");
            AssertTokenAppearsOnlyAsNegatedClaim(text, "full ISO 12831-3 compliance");
        }
    }

    [Fact]
    public void ApplicationIntegrationManifestAndDisclosureFiles_ArePresentAndHonest()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso12831DomesticHotWaterApplicationIntegrationManifest.json");
        var integrationDocPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "domestic-hot-water",
            "Iso12831DomesticHotWaterApplicationIntegration.md");
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
        var statusPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "engineering-core-v1",
            "status.sample.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");
        Assert.True(File.Exists(integrationDocPath), $"Integration doc was not found: {integrationDocPath}");
        Assert.True(File.Exists(validationDocPath), $"validation doc was not found: {validationDocPath}");
        Assert.True(File.Exists(scopeDocPath), $"Scope doc was not found: {scopeDocPath}");
        Assert.True(File.Exists(statusPath), $"Status sample was not found: {statusPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-DHW-002", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-application-integration-anchor", root.GetProperty("status").GetString());
        Assert.Contains(
            "AE-DHW-001",
            root.GetProperty("dependsOn").EnumerateArray().Select(item => item.GetString()));

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("Compatibility behavior preserved by default.", claimBoundary);
        Assert.Contains("No full ISO 12831-3 compliance claim.", claimBoundary);
        Assert.DoesNotContain("ExternalReferenceCovered", claimBoundary, StringComparer.OrdinalIgnoreCase);

        var integrationDoc = File.ReadAllText(integrationDocPath);
        Assert.Contains("Compatibility behavior preserved by default.", integrationDoc);
        AssertTokenAppearsOnlyAsNegatedClaim(integrationDoc, "full ISO 12831-3 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(integrationDoc, "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(integrationDoc, "EnergyPlus comparison workflow");

        var validationDoc = File.ReadAllText(validationDocPath);
        Assert.Contains("compatibility path remains default", validationDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("opt-in", validationDoc, StringComparison.OrdinalIgnoreCase);

        var scopeDoc = File.ReadAllText(scopeDocPath);
        Assert.Contains("compatibility path remains default", scopeDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("opt-in", scopeDoc, StringComparison.OrdinalIgnoreCase);

        var statusText = File.ReadAllText(statusPath);
        Assert.Contains("ISO12831-3-inspired DHW path is opt-in", statusText, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertTokenAppearsOnlyAsNegatedClaim(string text, string token)
    {
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
