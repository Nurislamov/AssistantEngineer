using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370GroundBoundaryApplicationIntegrationTraceabilityTests
{
    [Fact]
    public void StageManifestAndDoc_ExistAndPreserveClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "Iso13370GroundBoundaryApplicationIntegrationManifest.json");
        var docPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "ground",
            "Iso13370GroundBoundaryApplicationIntegration.md");

        Assert.True(File.Exists(manifestPath), $"Manifest file was not found: {manifestPath}");
        Assert.True(File.Exists(docPath), $"Documentation file was not found: {docPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-GROUND-002", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-application-integration-anchor", root.GetProperty("status").GetString());

        var dependencies = root.GetProperty("dependsOn").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("AE-GROUND-001", dependencies);

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("Compatibility behavior preserved by default.", claimBoundary);
        Assert.Contains("No full ISO 13370 compliance claim.", claimBoundary);
        Assert.Contains("No StandardReference equivalence claim.", claimBoundary);
        Assert.Contains("No EnergyPlus comparison workflow claim.", claimBoundary);
        Assert.Contains("No ASHRAE 140 / BESTEST-style validation anchor claim.", claimBoundary);
        Assert.DoesNotContain("ExternalReferenceCovered", claimBoundary, StringComparer.OrdinalIgnoreCase);

        var docText = File.ReadAllText(docPath);
        Assert.Contains("AE-GROUND-002", docText);
        Assert.Contains("Compatibility behavior preserved by default.", docText);
        Assert.DoesNotContain("ISO 13370 validated", docText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalReferenceCovered", docText, StringComparison.OrdinalIgnoreCase);
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "full ISO 13370 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(docText, "EnergyPlus comparison workflow");
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
