using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5RateLimitingGovernanceTests
{
    [Fact]
    public void RateLimitingDocumentsExist()
    {
        Assert.True(File.Exists(RateLimitingFoundationPath), $"Missing rate limiting foundation document: {RateLimitingFoundationPath}");
        Assert.True(File.Exists(RateLimitingPolicyRegistryPath), $"Missing rate limiting policy registry JSON: {RateLimitingPolicyRegistryPath}");
        Assert.True(File.Exists(RateLimitingPolicyRegistrySchemaPath), $"Missing rate limiting policy registry schema: {RateLimitingPolicyRegistrySchemaPath}");
    }

    [Fact]
    public void FoundationDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(RateLimitingFoundationPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Partition key model",
            "## Endpoint category model",
            "## Default compatibility mode",
            "## Recommended initial limits",
            "## Failure behavior",
            "## Observability and audit",
            "## Future distributed model"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void FoundationDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(RateLimitingFoundationPath);
        var requiredPhrases = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full distributed rate limiting claim",
            "No full multi-tenant isolation claim yet",
            "No external Redis/distributed limiter integration claim",
            "No certified/certification claim"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PolicyRegistryContainsExpectedPartitionPriorityAndCategories()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RateLimitingPolicyRegistryPath));
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Array, root.GetProperty("partitionPriority").ValueKind);
        var partitionPriority = root.GetProperty("partitionPriority")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("OrganizationId", partitionPriority);
        Assert.Contains("UserId", partitionPriority);
        Assert.Contains("ApiKeyFingerprint", partitionPriority);
        Assert.Contains("IpAddress", partitionPriority);
        Assert.Contains("AnonymousUnknown", partitionPriority);

        var categories = root.GetProperty("endpointCategories");
        Assert.Equal(JsonValueKind.Array, categories.ValueKind);

        var categoryNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in categories.EnumerateArray())
        {
            Assert.True(entry.TryGetProperty("category", out var categoryElement));
            Assert.True(entry.TryGetProperty("anonymousLimitPerMinute", out _));
            Assert.True(entry.TryGetProperty("authenticatedUserLimitPerMinute", out _));
            Assert.True(entry.TryGetProperty("organizationLimitPerMinute", out _));
            Assert.True(entry.TryGetProperty("enabledByDefault", out _));
            Assert.True(entry.TryGetProperty("notes", out _));

            var category = categoryElement.GetString() ?? string.Empty;
            Assert.True(categoryNames.Add(category), $"Duplicate endpoint category found: {category}");
        }

        var requiredCategories = new[]
        {
            "PublicRead",
            "ReferenceData",
            "ProjectRead",
            "ProjectWrite",
            "BuildingRead",
            "BuildingWrite",
            "WorkflowRead",
            "WorkflowExecute",
            "CalculationRun",
            "ReportGenerate",
            "ArtifactRead",
            "ArtifactWrite",
            "Administration"
        };

        foreach (var requiredCategory in requiredCategories)
        {
            Assert.Contains(requiredCategory, categoryNames);
        }
    }

    [Fact]
    public void RegistrySchemaContainsRequiredFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RateLimitingPolicyRegistrySchemaPath));
        var requiredTopLevelFields = document.RootElement.GetProperty("requiredTopLevelFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        var endpointFields = document.RootElement.GetProperty("endpointCategoryRequiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("version", requiredTopLevelFields);
        Assert.Contains("lastReviewedDate", requiredTopLevelFields);
        Assert.Contains("defaultMode", requiredTopLevelFields);
        Assert.Contains("partitionPriority", requiredTopLevelFields);
        Assert.Contains("endpointCategories", requiredTopLevelFields);
        Assert.Contains("nonClaims", requiredTopLevelFields);

        Assert.Contains("category", endpointFields);
        Assert.Contains("anonymousLimitPerMinute", endpointFields);
        Assert.Contains("authenticatedUserLimitPerMinute", endpointFields);
        Assert.Contains("organizationLimitPerMinute", endpointFields);
        Assert.Contains("enabledByDefault", endpointFields);
        Assert.Contains("notes", endpointFields);
    }

    [Fact]
    public void AppSettingsKeepApiRateLimitingDisabledByDefaultForCompatibility()
    {
        using var production = JsonDocument.Parse(File.ReadAllText(AppSettingsPath));
        using var development = JsonDocument.Parse(File.ReadAllText(AppSettingsDevelopmentPath));

        Assert.False(production.RootElement.GetProperty("ApiRateLimiting").GetProperty("Enabled").GetBoolean());
        Assert.False(development.RootElement.GetProperty("ApiRateLimiting").GetProperty("Enabled").GetBoolean());
    }

    [Fact]
    public void SecurityDocumentsReferenceRateLimitingFoundation()
    {
        var authenticationBoundary = File.ReadAllText(ApiAuthenticationBoundaryPath);
        var authorizationRollout = File.ReadAllText(AuthorizationPolicyRolloutPath);
        var auditFoundation = File.ReadAllText(AuditLogFoundationPath);
        var securityBoundary = File.ReadAllText(SecurityBoundaryPolicyPath);

        Assert.Contains("rate-limiting-foundation.md", authenticationBoundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rate-limiting-foundation.md", authorizationRollout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rate-limiting-foundation.md", auditFoundation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rate-limiting-foundation.md", securityBoundary, StringComparison.OrdinalIgnoreCase);
    }

    private static string RateLimitingFoundationPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "rate-limiting-foundation.md");

    private static string RateLimitingPolicyRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "rate-limiting-policy-registry.json");

    private static string RateLimitingPolicyRegistrySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "rate-limiting-policy-registry.schema.json");

    private static string ApiAuthenticationBoundaryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-authentication-boundary.md");

    private static string AuthorizationPolicyRolloutPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "authorization-policy-rollout.md");

    private static string AuditLogFoundationPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "audit-log-foundation.md");

    private static string SecurityBoundaryPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-boundary-policy.md");

    private static string AppSettingsPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.json");

    private static string AppSettingsDevelopmentPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.Development.json");
}
