using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316SystemEnergyApplicationIntegrationTraceabilityTests
{
    [Fact]
    public void IntegrationManifest_ExistsAndDeclaresClaimBoundary()
    {
        var manifestPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "En15316SystemEnergyApplicationIntegrationManifest.json");

        Assert.True(File.Exists(manifestPath), $"Manifest was not found: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal("AE-EN15316-002", root.GetProperty("stageId").GetString());
        Assert.Equal("internal-application-integration-anchor", root.GetProperty("status").GetString());
        Assert.Contains(
            "AE-EN15316-001",
            root.GetProperty("dependsOn").EnumerateArray().Select(item => item.GetString()));

        var claimBoundary = root.GetProperty("claimBoundary").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("Compatibility SystemEnergyEngine behavior preserved by default.", claimBoundary);
        Assert.Contains("No full EN 15316 compliance claim.", claimBoundary);
        Assert.DoesNotContain("ExternalReferenceCovered", claimBoundary, StringComparer.OrdinalIgnoreCase);

        var implementationFiles = root.GetProperty("implementationFiles")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        Assert.Contains(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SystemEnergy/SystemEnergyUsefulEnergyHandoffBuilder.cs",
            implementationFiles);
        Assert.Contains(
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Pipeline/EnergyCalculationPipelineService.cs",
            implementationFiles);
    }

    [Fact]
    public void IntegrationDocAndDisclosureFiles_ArePresentAndHonest()
    {
        var integrationDocPath = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "system-energy",
            "En15316SystemEnergyApplicationIntegration.md");
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

        Assert.True(File.Exists(integrationDocPath), $"Integration doc was not found: {integrationDocPath}");
        Assert.True(File.Exists(validationDocPath), $"validation doc was not found: {validationDocPath}");
        Assert.True(File.Exists(scopeDocPath), $"Scope doc was not found: {scopeDocPath}");
        Assert.True(File.Exists(statusPath), $"Status sample was not found: {statusPath}");

        var integrationDoc = File.ReadAllText(integrationDocPath);
        Assert.Contains("Compatibility SystemEnergyEngine behavior preserved by default.", integrationDoc);
        Assert.Contains("opt-in", integrationDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("handoff", integrationDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Standard-Based Calculation", integrationDoc, StringComparison.Ordinal);
        AssertTokenAppearsOnlyAsNegatedClaim(integrationDoc, "full EN 15316 compliance");
        AssertTokenAppearsOnlyAsNegatedClaim(integrationDoc, "StandardReference equivalence");
        AssertTokenAppearsOnlyAsNegatedClaim(integrationDoc, "EnergyPlus comparison workflow");

        var validationDoc = File.ReadAllText(validationDocPath);
        Assert.Contains("compatibility path remains default", validationDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("opt-in", validationDoc, StringComparison.OrdinalIgnoreCase);

        var scopeDoc = File.ReadAllText(scopeDocPath);
        Assert.Contains("compatibility", scopeDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("opt-in", scopeDoc, StringComparison.OrdinalIgnoreCase);

        var statusText = File.ReadAllText(statusPath);
        Assert.Contains("SystemEnergyEngine compatibility path remains default", statusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EN15316-inspired modular chain is opt-in", statusText, StringComparison.OrdinalIgnoreCase);
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
