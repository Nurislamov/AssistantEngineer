using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class SecurityRegressionGuardrailsDocumentTests
{
    [Fact]
    public void GuardrailDocumentsExist()
    {
        Assert.True(File.Exists(GuardrailsMarkdownPath), $"Missing document: {GuardrailsMarkdownPath}");
        Assert.True(File.Exists(GuardrailsRegistryPath), $"Missing registry: {GuardrailsRegistryPath}");
        Assert.True(File.Exists(GuardrailsSchemaPath), $"Missing schema: {GuardrailsSchemaPath}");
    }

    [Fact]
    public void GuardrailsMarkdownContainsRequiredSections()
    {
        var content = File.ReadAllText(GuardrailsMarkdownPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Guardrail categories",
            "## Enforcement model",
            "## Current guardrails",
            "## Future guardrails"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GuardrailsMarkdownContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(GuardrailsMarkdownPath);
        var requiredPhrases = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No external identity provider integration claim",
            "No certified/certification claim",
            "No claim that all API endpoints are protected yet"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GuardrailsMarkdownListsCurrentGuardrailTestNames()
    {
        var content = File.ReadAllText(GuardrailsMarkdownPath);
        var requiredTestNames = new[]
        {
            "ApiEndpointProtectionInventoryGuardTests",
            "DevelopmentEndpointSecurityGuardTests",
            "SecretLoggingSecurityGuardTests",
            "ApiAuthenticationDefaultsGuardTests",
            "ApiRateLimitingDefaultsGuardTests",
            "AuditLogSecurityGuardTests",
            "InMemoryProductionProviderGuardTests",
            "SecurityFalseClaimsGuardTests",
            "FrontendSecretsGuardTests"
        };

        foreach (var testName in requiredTestNames)
        {
            Assert.Contains(testName, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GuardrailsRegistryContainsRequiredEntriesAndAllowedValues()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsRegistryPath));
        var root = document.RootElement;
        var guardrails = root.GetProperty("guardrails");

        Assert.Equal(JsonValueKind.Array, guardrails.ValueKind);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var allowedStatus = new HashSet<string>(StringComparer.Ordinal)
        {
            "Active",
            "Planned",
            "DocumentedOnly",
            "Deprecated"
        };
        var allowedEnforcement = new HashSet<string>(StringComparer.Ordinal)
        {
            "AutomatedTest",
            "ManualReview",
            "DocumentedOnly"
        };

        foreach (var item in guardrails.EnumerateArray())
        {
            var id = item.GetProperty("guardrailId").GetString() ?? string.Empty;
            var status = item.GetProperty("status").GetString() ?? string.Empty;
            var enforcement = item.GetProperty("enforcement").GetString() ?? string.Empty;

            Assert.True(ids.Add(id), $"Duplicate guardrailId found: {id}");
            Assert.Contains(status, allowedStatus);
            Assert.Contains(enforcement, allowedEnforcement);
        }

        var requiredIds = new[]
        {
            "SEC-GUARD-ROUTE-INVENTORY",
            "SEC-GUARD-DEV-ENDPOINT",
            "SEC-GUARD-SECRET-LOGGING",
            "SEC-GUARD-AUTH-DEFAULTS",
            "SEC-GUARD-RATE-LIMIT-DEFAULTS",
            "SEC-GUARD-AUDIT-SANITIZATION",
            "SEC-GUARD-INMEMORY-PRODUCTION",
            "SEC-GUARD-FALSE-CLAIMS",
            "SEC-GUARD-FRONTEND-SECRETS"
        };

        foreach (var requiredId in requiredIds)
        {
            Assert.Contains(requiredId, ids);
        }
    }

    [Fact]
    public void GuardrailsSchemaContainsRequiredFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GuardrailsSchemaPath));
        var requiredTopLevelFields = document.RootElement
            .GetProperty("requiredTopLevelFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        var requiredGuardrailFields = document.RootElement
            .GetProperty("guardrailRequiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("version", requiredTopLevelFields);
        Assert.Contains("lastReviewedDate", requiredTopLevelFields);
        Assert.Contains("guardrails", requiredTopLevelFields);
        Assert.Contains("nonClaims", requiredTopLevelFields);

        Assert.Contains("guardrailId", requiredGuardrailFields);
        Assert.Contains("title", requiredGuardrailFields);
        Assert.Contains("status", requiredGuardrailFields);
        Assert.Contains("enforcement", requiredGuardrailFields);
        Assert.Contains("risk", requiredGuardrailFields);
        Assert.Contains("notes", requiredGuardrailFields);
    }

    private static string GuardrailsMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");

    private static string GuardrailsRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.json");

    private static string GuardrailsSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.schema.json");
}
