using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.Governance;

public sealed class InputQualityGovernanceTests
{
    [Fact]
    public void InputQualityDocumentationAndRegistryFilesExist()
    {
        Assert.True(File.Exists(InputQualityChecksPath), $"Input quality checks markdown is missing: {InputQualityChecksPath}");
        Assert.True(File.Exists(DiagnosticRegistryPath), $"Input quality diagnostic registry is missing: {DiagnosticRegistryPath}");
        Assert.True(File.Exists(DiagnosticRegistrySchemaPath), $"Input quality diagnostic registry schema is missing: {DiagnosticRegistrySchemaPath}");
    }

    [Fact]
    public void InputQualityChecksMarkdownContainsRequiredSections()
    {
        var content = File.ReadAllText(InputQualityChecksPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Difference between validation and input quality",
            "## Severity model",
            "## Diagnostic categories",
            "## Diagnostic code list",
            "## Calculation readiness interpretation",
            "## Relationship to assumptions registry",
            "## Relationship to units governance",
            "## Future UI usage"
        };

        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);
    }

    [Fact]
    public void InputQualityChecksMarkdownContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(InputQualityChecksPath);

        var py = "pyBuilding";
        var energy = "Energy";
        var exactPyPhrase = $"No {py}{energy} parity claim";
        var escapedPyPhrase = "No pyBuilding\\u0045nergy parity claim";

        Assert.Contains("No ASHRAE 140 compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No exact EnergyPlus equivalence claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains(exactPyPhrase, StringComparison.OrdinalIgnoreCase) ||
            content.Contains(escapedPyPhrase, StringComparison.OrdinalIgnoreCase),
            "Input quality checks document must include external-calculator parity non-claim wording.");
        Assert.Contains("No full ISO/EN compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No certified/certification claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InputQualityChecksMarkdownReferencesAssumptionsAndUnitsGovernance()
    {
        var content = File.ReadAllText(InputQualityChecksPath);

        Assert.Contains("docs/engineering/engineering-assumptions-registry.md", content, StringComparison.Ordinal);
        Assert.Contains("docs/engineering/units-governance.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void InputQualityDiagnosticRegistryParsesAndContainsRequiredStructure()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(DiagnosticRegistryPath));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("diagnostics", out var diagnostics));
        Assert.Equal(JsonValueKind.Array, diagnostics.ValueKind);

        var requiredFields = new[]
        {
            "code",
            "category",
            "severity",
            "messageTemplate",
            "recommendation",
            "blocking"
        };

        var allowedSeverities = new HashSet<string>(StringComparer.Ordinal)
        {
            "Info",
            "Warning",
            "Error",
            "Blocking"
        };

        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var diagnostic in diagnostics.EnumerateArray())
        {
            foreach (var field in requiredFields)
                Assert.True(diagnostic.TryGetProperty(field, out _), $"Diagnostic registry entry is missing field: {field}");

            var code = diagnostic.GetProperty("code").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(code), "Diagnostic code must not be empty.");
            Assert.True(codes.Add(code), $"Duplicate diagnostic code found: {code}");

            var severity = diagnostic.GetProperty("severity").GetString() ?? string.Empty;
            Assert.Contains(severity, allowedSeverities);
        }

        var requiredCodes = new[]
        {
            "IQ-BLD-001",
            "IQ-BLD-010",
            "IQ-BLD-011",
            "IQ-BLD-020",
            "IQ-ROOM-001",
            "IQ-ROOM-010",
            "IQ-ROOM-011",
            "IQ-ROOM-012",
            "IQ-ROOM-020",
            "IQ-ROOM-030",
            "IQ-ROOM-040",
            "IQ-ROOM-041",
            "IQ-ROOM-050",
            "IQ-ROOM-051",
            "IQ-ROOM-060",
            "IQ-ROOM-070",
            "IQ-ASSUMP-001",
            "IQ-UNITS-001"
        };

        foreach (var requiredCode in requiredCodes)
            Assert.Contains(requiredCode, codes);
    }

    [Fact]
    public void InputQualityDiagnosticRegistrySchemaContainsRequiredKeys()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(DiagnosticRegistrySchemaPath));
        var root = schema.RootElement;

        var requiredFields = root
            .GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("code", requiredFields);
        Assert.Contains("category", requiredFields);
        Assert.Contains("severity", requiredFields);
        Assert.Contains("messageTemplate", requiredFields);
        Assert.Contains("recommendation", requiredFields);
        Assert.Contains("blocking", requiredFields);

        var allowedSeverities = root
            .GetProperty("allowedSeverities")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("Info", allowedSeverities);
        Assert.Contains("Warning", allowedSeverities);
        Assert.Contains("Error", allowedSeverities);
        Assert.Contains("Blocking", allowedSeverities);
    }

    [Fact]
    public void InputQualityGovernanceDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                InputQualityChecksPath,
                DiagnosticRegistryPath,
                DiagnosticRegistrySchemaPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string InputQualityChecksPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "input-quality-checks.md");

    private static string DiagnosticRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "input-quality-diagnostic-registry.json");

    private static string DiagnosticRegistrySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "input-quality-diagnostic-registry.schema.json");
}
