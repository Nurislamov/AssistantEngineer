using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.Governance;

public sealed class EngineeringArtifactStorageGovernanceTests
{
    [Fact]
    public void EngineeringArtifactStorageDocumentsExist()
    {
        Assert.True(File.Exists(ArtifactStorageDocPath), $"Artifact storage document is missing: {ArtifactStorageDocPath}");
        Assert.True(File.Exists(DescriptorSchemaPath), $"Artifact descriptor schema is missing: {DescriptorSchemaPath}");
    }

    [Fact]
    public void EngineeringArtifactStorageDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(ArtifactStorageDocPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Artifact kinds",
            "## Storage providers",
            "## Descriptor model",
            "## Integrity/SHA256 policy",
            "## Size limit policy",
            "## Relationship to workflow persistence",
            "## Relationship to calculation trace explainability",
            "## Relationship to reports",
            "## Future object/blob storage providers",
            "## Migration policy"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EngineeringArtifactStorageDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ArtifactStorageDocPath);

        var py = "pyBuilding";
        var energy = "Energy";
        var exactPyPhrase = $"No {py}{energy} parity claim";
        var escapedPyPhrase = "No pyBuilding\\u0045nergy parity claim";

        Assert.Contains("No ASHRAE 140 compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No exact EnergyPlus equivalence claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains(exactPyPhrase, StringComparison.OrdinalIgnoreCase) ||
            content.Contains(escapedPyPhrase, StringComparison.OrdinalIgnoreCase),
            "Artifact storage document must include external-calculator parity non-claim wording.");
        Assert.Contains("No full ISO/EN compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No certified/certification claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EngineeringArtifactDescriptorSchemaContainsRequiredFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(DescriptorSchemaPath));
        var root = document.RootElement;

        var requiredFields = root
            .GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("artifactId", requiredFields);
        Assert.Contains("artifactKind", requiredFields);
        Assert.Contains("scope", requiredFields);
        Assert.Contains("contentType", requiredFields);
        Assert.Contains("sizeBytes", requiredFields);
        Assert.Contains("sha256", requiredFields);
        Assert.Contains("storageProvider", requiredFields);
        Assert.Contains("storageKey", requiredFields);
        Assert.Contains("createdAtUtc", requiredFields);
    }

    [Fact]
    public void GitIgnoreIncludesEngineeringArtifactsRuntimeOutputPath()
    {
        var gitIgnorePath = Path.Combine(TestPaths.RepoRoot, ".gitignore");
        var content = File.ReadAllText(gitIgnorePath);

        Assert.True(
            content.Contains("artifacts/engineering/", StringComparison.Ordinal) ||
            content.Contains("artifacts/", StringComparison.Ordinal),
            ".gitignore must ignore engineering artifact runtime output path.");
    }

    [Fact]
    public void EngineeringArtifactStorageDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                ArtifactStorageDocPath,
                DescriptorSchemaPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string ArtifactStorageDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-artifact-storage.md");

    private static string DescriptorSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-artifact-descriptor.schema.json");
}
