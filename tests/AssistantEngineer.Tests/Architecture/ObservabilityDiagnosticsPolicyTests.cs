using System.Reflection;
using System.Text.Json;
using AssistantEngineer.SharedKernel.Diagnostics;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ObservabilityDiagnosticsPolicyTests
{
    [Fact]
    public void PolicyAndRegistryFilesExist()
    {
        Assert.True(File.Exists(PolicyPath), $"Missing observability policy document: {PolicyPath}");
        Assert.True(File.Exists(RegistryPath), $"Missing observability registry JSON: {RegistryPath}");
        Assert.True(File.Exists(SchemaPath), $"Missing observability registry schema descriptor: {SchemaPath}");
    }

    [Fact]
    public void PolicyContainsRequiredSections()
    {
        var content = File.ReadAllText(PolicyPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Identifier model",
            "## Structured logging rule",
            "## Diagnostic event severity model",
            "## Event taxonomy",
            "## Required event metadata",
            "## Timing/duration policy",
            "## Privacy/security policy",
            "## Future OpenTelemetry integration"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PolicyContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(PolicyPath);

        Assert.Contains("No production monitoring certification claim.", content, StringComparison.Ordinal);
        Assert.Contains("No globally exactly-once distributed execution claim.", content, StringComparison.Ordinal);
        Assert.Contains("No exact EnergyPlus equivalence claim.", content, StringComparison.Ordinal);
        Assert.Contains("No ASHRAE 140 compliance claim.", content, StringComparison.Ordinal);
        Assert.Contains("No full ISO/EN compliance claim.", content, StringComparison.Ordinal);
        Assert.Contains("No certified/certification claim.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistryEventsAreWellFormedAndUseAllowedValues()
    {
        using var registry = JsonDocument.Parse(File.ReadAllText(RegistryPath));
        Assert.Equal(JsonValueKind.Array, registry.RootElement.ValueKind);

        var allowedCategories = new HashSet<string>(StringComparer.Ordinal)
        {
            "Workflow",
            "Job",
            "Calculation",
            "InputQuality",
            "Validation",
            "ArtifactStorage",
            "Persistence",
            "Reporting",
            "Governance"
        };

        var allowedLevels = new HashSet<string>(StringComparer.Ordinal)
        {
            "Trace",
            "Debug",
            "Information",
            "Warning",
            "Error",
            "Critical"
        };

        var eventCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in registry.RootElement.EnumerateArray())
        {
            Assert.True(item.TryGetProperty("eventCode", out var eventCodeElement));
            Assert.True(item.TryGetProperty("category", out var categoryElement));
            Assert.True(item.TryGetProperty("defaultLogLevel", out var levelElement));
            Assert.True(item.TryGetProperty("messageTemplate", out _));
            Assert.True(item.TryGetProperty("requiredProperties", out var requiredPropertiesElement));
            Assert.True(item.TryGetProperty("sensitivePayloadAllowed", out var sensitiveElement));
            Assert.True(item.TryGetProperty("description", out _));

            var eventCode = eventCodeElement.GetString() ?? string.Empty;
            var category = categoryElement.GetString() ?? string.Empty;
            var level = levelElement.GetString() ?? string.Empty;

            Assert.True(eventCodes.Add(eventCode), $"Duplicate event code found: {eventCode}");
            Assert.Contains(category, allowedCategories);
            Assert.Contains(level, allowedLevels);
            Assert.Equal(JsonValueKind.Array, requiredPropertiesElement.ValueKind);
            Assert.False(sensitiveElement.GetBoolean());
        }

        var requiredEventCodes = new[]
        {
            "OBS-WF-001","OBS-WF-002","OBS-WF-003","OBS-WF-004",
            "OBS-JOB-001","OBS-JOB-002","OBS-JOB-003","OBS-JOB-004","OBS-JOB-005","OBS-JOB-006",
            "OBS-CALC-001","OBS-CALC-002","OBS-CALC-003","OBS-CALC-004",
            "OBS-IQ-001","OBS-IQ-002","OBS-IQ-003",
            "OBS-ART-001","OBS-ART-002","OBS-ART-003","OBS-ART-004","OBS-ART-005",
            "OBS-PER-001","OBS-PER-002","OBS-PER-003","OBS-PER-004",
            "OBS-VAL-001","OBS-VAL-002",
            "OBS-GOV-001","OBS-GOV-002"
        };

        foreach (var code in requiredEventCodes)
        {
            Assert.Contains(code, eventCodes);
        }
    }

    [Fact]
    public void SchemaDescriptorContainsRequiredFieldsAndAllowedSets()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(SchemaPath));
        var root = schema.RootElement;
        var requiredFields = root.GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("eventCode", requiredFields);
        Assert.Contains("category", requiredFields);
        Assert.Contains("defaultLogLevel", requiredFields);
        Assert.Contains("messageTemplate", requiredFields);
        Assert.Contains("requiredProperties", requiredFields);
        Assert.Contains("sensitivePayloadAllowed", requiredFields);
        Assert.Contains("description", requiredFields);

        var categories = root.GetProperty("allowedCategories").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        Assert.Contains("Workflow", categories);
        Assert.Contains("ArtifactStorage", categories);
        Assert.Contains("Governance", categories);

        var levels = root.GetProperty("allowedDefaultLogLevels").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        Assert.Contains("Information", levels);
        Assert.Contains("Error", levels);
    }

    [Fact]
    public void SourceObservabilityEventCodesContainRegistryValues()
    {
        var sourceCodes = typeof(ObservabilityEventCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => field.GetRawConstantValue() as string)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        using var registry = JsonDocument.Parse(File.ReadAllText(RegistryPath));
        var registryCodes = registry.RootElement
            .EnumerateArray()
            .Select(item => item.GetProperty("eventCode").GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var code in registryCodes)
        {
            Assert.Contains(code, sourceCodes);
        }
    }

    [Fact]
    public void CrossDocumentsReferenceObservabilityPolicy()
    {
        var inputQualityDoc = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "input-quality-checks.md"));
        var traceDoc = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "calculation-trace-explainability.md"));
        var artifactDoc = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "engineering-artifact-storage.md"));
        var persistenceDoc = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "postgresql-durable-persistence-hardening.md"));

        Assert.Contains("observability-diagnostics-policy.md", inputQualityDoc, StringComparison.Ordinal);
        Assert.Contains("observability-diagnostics-policy.md", traceDoc, StringComparison.Ordinal);
        Assert.Contains("observability-diagnostics-policy.md", artifactDoc, StringComparison.Ordinal);
        Assert.Contains("observability-diagnostics-policy.md", persistenceDoc, StringComparison.Ordinal);
    }

    private static string PolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "observability-diagnostics-policy.md");

    private static string RegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "observability-diagnostic-events.json");

    private static string SchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "observability-diagnostic-events.schema.json");
}
