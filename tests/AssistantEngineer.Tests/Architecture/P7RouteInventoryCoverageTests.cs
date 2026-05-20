using System.Text.Json;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P7RouteInventoryCoverageTests
{
    [Fact]
    public void InventoryArtifactsExistAndParse()
    {
        GovernanceDocumentTestHelper.AssertFilesExist(
            InventoryJsonPath,
            InventoryMarkdownPath,
            InventorySchemaPath,
            ClassificationModelPath,
            IgnoreListPath);

        _ = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        _ = GovernanceJsonTestHelper.Parse(IgnoreListPath);
    }

    [Fact]
    public void InventoryEntriesContainRequiredClassificationFields()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        Assert.NotEmpty(endpoints);

        var requiredFields = new[]
        {
            "controller",
            "routePattern",
            "currentAuthStatus",
            "targetPolicy",
            "rolloutStage",
            "risk",
            "notes",
            "route",
            "httpMethod",
            "action",
            "endpointGroup",
            "protectionStage",
            "permission",
            "tenantScope",
            "rateLimitCategory",
            "auditCategory",
            "releaseBoundaryClaim",
            "knownLimitations"
        };

        foreach (var endpoint in endpoints)
        {
            foreach (var field in requiredFields)
                Assert.True(endpoint.TryGetProperty(field, out _), $"Missing inventory field '{field}'.");

            Assert.False(string.IsNullOrWhiteSpace(endpoint.GetProperty("route").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(endpoint.GetProperty("httpMethod").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(endpoint.GetProperty("controller").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(endpoint.GetProperty("endpointGroup").GetString()));

            var knownLimitations = endpoint.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
            Assert.NotEmpty(knownLimitations);

            var endpointGroup = endpoint.GetProperty("endpointGroup").GetString() ?? string.Empty;
            if (string.Equals(endpointGroup, "UnknownNeedsClassification", StringComparison.Ordinal))
            {
                Assert.Contains(knownLimitations, item =>
                    item.Contains("classification", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void DiscoveredControllerRoutesAreRepresentedOrIgnored()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var ignores = RouteInventoryTestHelper.LoadIgnoreEntries(IgnoreListPath);
        foreach (var ignore in ignores)
            Assert.False(string.IsNullOrWhiteSpace(ignore.Reason), "Every route-inventory ignore entry must include a reason.");

        var discovered = RouteInventoryTestHelper.DiscoverControllerEndpoints(ControllersRootPath);
        Assert.NotEmpty(discovered);

        var missing = new List<string>();
        foreach (var endpoint in discovered)
        {
            if (RouteInventoryTestHelper.IsIgnored(endpoint, ignores))
                continue;

            if (RouteInventoryTestHelper.IsEndpointRepresentedInInventory(inventory.RootElement, endpoint))
                continue;

            missing.Add($"{endpoint.Controller} {endpoint.HttpMethod} {endpoint.RouteTemplate} ({endpoint.SourcePath}:{endpoint.SourceLine})");
        }

        Assert.True(missing.Count == 0,
            "Discovered routes missing from inventory/ignore list:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void InventoryDocsReferenceClassificationModel()
    {
        var content = File.ReadAllText(InventoryMarkdownPath);
        Assert.Contains("api-endpoint-classification-model.md", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.json");

    private static string InventoryMarkdownPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.md");

    private static string InventorySchemaPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.schema.json");

    private static string ClassificationModelPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-classification-model.md");

    private static string ControllersRootPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Controllers");

    private static string IgnoreListPath =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "security", "route-inventory-ignore-list.json");
}
