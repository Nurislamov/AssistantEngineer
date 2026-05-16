using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5AuditLogGovernanceTests
{
    [Fact]
    public void AuditLogDocumentsExist()
    {
        Assert.True(File.Exists(AuditLogFoundationPath), $"Missing audit log foundation document: {AuditLogFoundationPath}");
        Assert.True(File.Exists(AuditEventRegistryPath), $"Missing audit event registry JSON: {AuditEventRegistryPath}");
        Assert.True(File.Exists(AuditEventRegistrySchemaPath), $"Missing audit event registry schema: {AuditEventRegistrySchemaPath}");
    }

    [Fact]
    public void AuditLogFoundationContainsRequiredSections()
    {
        var content = File.ReadAllText(AuditLogFoundationPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Audit event model",
            "## Event categories",
            "## Outcome model",
            "## Principal/resource identifiers",
            "## Metadata sanitization policy",
            "## Secret handling policy",
            "## Append-only policy",
            "## Durable storage status",
            "## Relationship to authentication boundary",
            "## Relationship to authorization policy",
            "## Relationship to observability",
            "## Future work"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AuditLogFoundationContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(AuditLogFoundationPath);
        var nonClaims = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No external identity provider integration claim",
            "No certified/certification claim",
            "No SIEM integration claim"
        };

        foreach (var phrase in nonClaims)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AuditRegistryIsWellFormedAndContainsRequiredEventTypes()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(AuditEventRegistryPath));
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);

        var allowedCategories = new HashSet<string>(StringComparer.Ordinal)
        {
            "Authentication",
            "Authorization",
            "Project",
            "Building",
            "Workflow",
            "Calculation",
            "Report",
            "Artifact",
            "Administration",
            "Security",
            "System"
        };

        var allowedOutcomes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Succeeded",
            "Failed",
            "Denied",
            "Skipped",
            "Unknown"
        };

        var eventTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            Assert.True(item.TryGetProperty("eventType", out var eventTypeElement));
            Assert.True(item.TryGetProperty("category", out var categoryElement));
            Assert.True(item.TryGetProperty("defaultOutcome", out var outcomeElement));
            Assert.True(item.TryGetProperty("messageTemplate", out _));
            Assert.True(item.TryGetProperty("requiredIdentifiers", out var requiredIdentifiers));
            Assert.True(item.TryGetProperty("allowPayload", out var allowPayload));
            Assert.True(item.TryGetProperty("description", out _));

            var eventType = eventTypeElement.GetString() ?? string.Empty;
            var category = categoryElement.GetString() ?? string.Empty;
            var outcome = outcomeElement.GetString() ?? string.Empty;

            Assert.True(eventTypes.Add(eventType), $"Duplicate audit event type detected: {eventType}");
            Assert.Contains(category, allowedCategories);
            Assert.Contains(outcome, allowedOutcomes);
            Assert.Equal(JsonValueKind.Array, requiredIdentifiers.ValueKind);
            Assert.False(allowPayload.GetBoolean());
        }

        var requiredEventTypes = new[]
        {
            "AUD-AUTH-001",
            "AUD-AUTH-002",
            "AUD-AUTHZ-001",
            "AUD-AUTHZ-002",
            "AUD-WF-001",
            "AUD-WF-002",
            "AUD-WF-003",
            "AUD-CALC-001",
            "AUD-CALC-002",
            "AUD-CALC-003",
            "AUD-ART-001",
            "AUD-ART-002",
            "AUD-ART-003"
        };

        foreach (var eventType in requiredEventTypes)
        {
            Assert.Contains(eventType, eventTypes);
        }
    }

    [Fact]
    public void AuditRegistrySchemaContainsRequiredDescriptorFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(AuditEventRegistrySchemaPath));
        var requiredFields = document.RootElement
            .GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("eventType", requiredFields);
        Assert.Contains("category", requiredFields);
        Assert.Contains("defaultOutcome", requiredFields);
        Assert.Contains("messageTemplate", requiredFields);
        Assert.Contains("requiredIdentifiers", requiredFields);
        Assert.Contains("allowPayload", requiredFields);
        Assert.Contains("description", requiredFields);
    }

    [Fact]
    public void SecurityDocumentsReferenceAuditFoundation()
    {
        var securityBoundary = File.ReadAllText(SecurityBoundaryPolicyPath);
        var authenticationBoundary = File.ReadAllText(ApiAuthenticationBoundaryPath);
        var authorizationRollout = File.ReadAllText(AuthorizationPolicyRolloutPath);

        Assert.Contains("audit-log-foundation.md", securityBoundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit-log-foundation.md", authenticationBoundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit-log-foundation.md", authorizationRollout, StringComparison.OrdinalIgnoreCase);
    }

    private static string AuditLogFoundationPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "audit-log-foundation.md");

    private static string AuditEventRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "audit-event-registry.json");

    private static string AuditEventRegistrySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "audit-event-registry.schema.json");

    private static string SecurityBoundaryPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-boundary-policy.md");

    private static string ApiAuthenticationBoundaryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-authentication-boundary.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");
}
