using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ProductionSaasReadinessGovernanceTests
{
    [Fact]
    public void SecurityDocumentsExist()
    {
        Assert.True(File.Exists(InventoryMarkdownPath), $"Missing production SaaS inventory markdown: {InventoryMarkdownPath}");
        Assert.True(File.Exists(InventoryJsonPath), $"Missing production SaaS inventory JSON: {InventoryJsonPath}");
        Assert.True(File.Exists(InventorySchemaPath), $"Missing production SaaS inventory schema: {InventorySchemaPath}");
        Assert.True(File.Exists(SecurityBoundaryPolicyPath), $"Missing security boundary policy markdown: {SecurityBoundaryPolicyPath}");
    }

    [Fact]
    public void InventoryMarkdownContainsRequiredSections()
    {
        var content = File.ReadAllText(InventoryMarkdownPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Current state inventory",
            "## Target production model",
            "## Security boundary rules",
            "## Migration principles",
            "## P5 roadmap"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SecurityBoundaryPolicyContainsRequiredSections()
    {
        var content = File.ReadAllText(SecurityBoundaryPolicyPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Principal model",
            "## Tenant boundary model",
            "## Project ownership rule",
            "## Controller authorization rule",
            "## Development-only endpoint rule",
            "## API key handling rule",
            "## Secret/logging rule",
            "## Audit event rule",
            "## Rate limiting rule",
            "## Testing rule",
            "## Non-claims"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RequiredNonClaimsArePresent()
    {
        var inventory = File.ReadAllText(InventoryMarkdownPath);
        var policy = File.ReadAllText(SecurityBoundaryPolicyPath);

        var requiredPhrases = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No external identity provider integration claim",
            "No certified/certification claim"
        };

        foreach (var phrase in requiredPhrases)
        {
            Assert.Contains(phrase, inventory, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(phrase, policy, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void InventoryJsonParsesAndContainsExpectedShape()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("version", out _));
        Assert.True(root.TryGetProperty("lastReviewedDate", out _));
        Assert.True(root.TryGetProperty("areas", out var areas));
        Assert.True(root.TryGetProperty("p5Roadmap", out var roadmap));
        Assert.True(root.TryGetProperty("nonClaims", out var nonClaims));

        Assert.Equal(JsonValueKind.Array, areas.ValueKind);
        Assert.Equal(JsonValueKind.Array, roadmap.ValueKind);
        Assert.Equal(JsonValueKind.Array, nonClaims.ValueKind);
        Assert.True(areas.GetArrayLength() > 0, "Inventory areas must not be empty.");

        var areaNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var area in areas.EnumerateArray())
        {
            var requiredAreaFields = new[] { "area", "currentStatus", "evidence", "risk", "recommendedAction" };
            foreach (var field in requiredAreaFields)
            {
                Assert.True(area.TryGetProperty(field, out _), $"Inventory area entry is missing field '{field}'.");
            }

            var name = area.GetProperty("area").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(name), "Inventory area name must not be empty.");
            Assert.True(areaNames.Add(name), $"Duplicate inventory area name: {name}");
        }

        var roadmapItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in roadmap.EnumerateArray())
        {
            var requiredRoadmapFields = new[] { "item", "title", "status", "risk" };
            foreach (var field in requiredRoadmapFields)
            {
                Assert.True(item.TryGetProperty(field, out _), $"Roadmap entry is missing field '{field}'.");
            }

            var id = item.GetProperty("item").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(id), "Roadmap item id must not be empty.");
            Assert.True(roadmapItems.Add(id), $"Duplicate roadmap item id: {id}");
        }

        var requiredRoadmapItems = new[]
        {
            "P5-01",
            "P5-02",
            "P5-03",
            "P5-04",
            "P5-05",
            "P5-06",
            "P5-07",
            "P5-08",
            "P5-09"
        };

        foreach (var requiredItem in requiredRoadmapItems)
        {
            Assert.Contains(requiredItem, roadmapItems);
        }
    }

    [Fact]
    public void InventorySchemaContainsRequiredTopLevelFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventorySchemaPath));
        var root = document.RootElement;

        var requiredFields = root
            .GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("version", requiredFields);
        Assert.Contains("lastReviewedDate", requiredFields);
        Assert.Contains("areas", requiredFields);
        Assert.Contains("p5Roadmap", requiredFields);
        Assert.Contains("nonClaims", requiredFields);
    }

    [Fact]
    public void DevelopmentDemoEndpointRemainsEnvironmentGated()
    {
        var controllerText = File.ReadAllText(DevelopmentDemoDataControllerPath);

        Assert.Contains("environment is null || !environment.IsDevelopment()", controllerText, StringComparison.Ordinal);
        Assert.Contains("return NotFound();", controllerText, StringComparison.Ordinal);
        Assert.Contains("development/demo-data", controllerText, StringComparison.Ordinal);
    }

    [Fact]
    public void LoggingTemplatesDoNotExposeSensitivePlaceholders()
    {
        var sourceRoot = Path.Combine(TestPaths.RepoRoot, "src", "Backend");
        var suspicious = new List<string>();

        var sensitiveMarkers = new[]
        {
            "{ApiKey",
            "{Token",
            "{Password",
            "{Secret",
            "{Authentication__ApiKey__Key"
        };

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(TestPaths.RepoRoot, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(part =>
                    part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                if (!line.Contains("Log", StringComparison.Ordinal))
                    continue;

                if (sensitiveMarkers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                {
                    suspicious.Add($"{relative}:{index + 1}");
                }
            }
        }

        Assert.True(
            suspicious.Count == 0,
            "Sensitive logging placeholders were found:\n" + string.Join('\n', suspicious));
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.schema.json");

    private static string SecurityBoundaryPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-boundary-policy.md");

    private static string DevelopmentDemoDataControllerPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Controllers",
            "ReferenceData",
            "DevelopmentDemoDataController.cs");
}
