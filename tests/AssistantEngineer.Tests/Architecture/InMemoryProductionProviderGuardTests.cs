using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class InMemoryProductionProviderGuardTests
{
    [Fact]
    public void ProductionInMemoryProvidersRequireExplicitNonDurableDocumentation()
    {
        using var production = JsonDocument.Parse(File.ReadAllText(AppSettingsPath));
        var inMemoryProviderPaths = ResolveInMemoryProviderPaths(production.RootElement);

        if (inMemoryProviderPaths.Count == 0)
        {
            return;
        }

        var securityInventory = File.ReadAllText(SecurityInventoryMarkdownPath);
        var persistenceHardening = File.ReadAllText(PostgreSqlHardeningPath);
        var guardrailDoc = File.ReadAllText(SecurityGuardrailsPath);

        Assert.Contains("in-memory provider is not durable", securityInventory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No claim that in-memory provider is durable", persistenceHardening, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("InMemory", guardrailDoc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DevelopmentAppsettingsMayUseInMemoryOrSqliteForCompatibility()
    {
        using var development = JsonDocument.Parse(File.ReadAllText(AppSettingsDevelopmentPath));

        if (development.RootElement.TryGetProperty("EngineeringWorkflowPersistence", out var workflow) &&
            workflow.TryGetProperty("Provider", out var provider))
        {
            var value = provider.GetString() ?? string.Empty;
            Assert.True(
                string.Equals(value, "SQLite", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "InMemory", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "PostgreSQL", StringComparison.OrdinalIgnoreCase),
                $"Unexpected development workflow provider: {value}");
        }
    }

    private static List<string> ResolveInMemoryProviderPaths(JsonElement root)
    {
        var paths = new List<string>();

        AddIfInMemory(root, paths, "EngineeringWorkflowPersistence", "Provider");
        AddIfInMemory(root, paths, "EngineeringArtifacts", "Provider");

        if (root.TryGetProperty("Identity", out var identity) &&
            identity.ValueKind == JsonValueKind.Object &&
            identity.TryGetProperty("AuditLog", out var auditLog))
        {
            AddIfInMemory(auditLog, paths, "Provider", null, prefix: "Identity.AuditLog");
        }

        return paths;
    }

    private static void AddIfInMemory(
        JsonElement element,
        ICollection<string> output,
        string sectionOrKey,
        string? nestedKey,
        string? prefix = null)
    {
        if (nestedKey is null)
        {
            if (element.TryGetProperty(sectionOrKey, out var providerElement) &&
                providerElement.ValueKind == JsonValueKind.String &&
                string.Equals(providerElement.GetString(), "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                output.Add($"{prefix ?? string.Empty}.{sectionOrKey}".Trim('.'));
            }

            return;
        }

        if (!element.TryGetProperty(sectionOrKey, out var section) || section.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (!section.TryGetProperty(nestedKey, out var nested) || nested.ValueKind != JsonValueKind.String)
        {
            return;
        }

        if (string.Equals(nested.GetString(), "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            output.Add($"{sectionOrKey}.{nestedKey}");
        }
    }

    private static string AppSettingsPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.json");

    private static string AppSettingsDevelopmentPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.Development.json");

    private static string SecurityInventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string PostgreSqlHardeningPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "postgresql-durable-persistence-hardening.md");

    private static string SecurityGuardrailsPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-regression-guardrails.md");
}
