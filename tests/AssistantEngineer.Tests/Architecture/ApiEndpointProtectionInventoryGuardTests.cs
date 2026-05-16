using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ApiEndpointProtectionInventoryGuardTests
{
    [Fact]
    public void EndpointInventoryFilesExistAndParse()
    {
        Assert.True(File.Exists(InventoryMarkdownPath), $"Missing endpoint inventory markdown: {InventoryMarkdownPath}");
        Assert.True(File.Exists(InventoryJsonPath), $"Missing endpoint inventory JSON: {InventoryJsonPath}");
        Assert.True(File.Exists(InventorySchemaPath), $"Missing endpoint inventory schema: {InventorySchemaPath}");

        using var _ = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
    }

    [Fact]
    public void EndpointInventoryEntriesHaveRequiredFieldsAndAllowedStatus()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var endpoints = document.RootElement.GetProperty("endpoints");
        Assert.Equal(JsonValueKind.Array, endpoints.ValueKind);

        var allowedStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "PublicAllowed",
            "DevelopmentOnly",
            "AuthPlanned",
            "AuthPilot",
            "Protected",
            "UnknownNeedsAudit"
        };

        foreach (var endpoint in endpoints.EnumerateArray())
        {
            var requiredFields = new[]
            {
                "controller",
                "routePattern",
                "currentAuthStatus",
                "targetPolicy",
                "rolloutStage",
                "risk",
                "notes"
            };

            foreach (var field in requiredFields)
            {
                Assert.True(endpoint.TryGetProperty(field, out _), $"Inventory endpoint is missing field '{field}'.");
            }

            var status = endpoint.GetProperty("currentAuthStatus").GetString() ?? string.Empty;
            var targetPolicy = endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty;
            var risk = endpoint.GetProperty("risk").GetString() ?? string.Empty;
            var notes = endpoint.GetProperty("notes").GetString() ?? string.Empty;

            Assert.Contains(status, allowedStatuses);
            Assert.False(string.IsNullOrWhiteSpace(targetPolicy), "targetPolicy must not be empty.");

            if (string.Equals(status, "UnknownNeedsAudit", StringComparison.Ordinal))
            {
                Assert.False(string.IsNullOrWhiteSpace(risk), "UnknownNeedsAudit entry must contain risk.");
                Assert.False(string.IsNullOrWhiteSpace(notes), "UnknownNeedsAudit entry must contain notes.");
            }
        }
    }

    [Fact]
    public void EndpointInventoryContainsRequiredControllerCategories()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var controllers = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Select(item => item.GetProperty("controller").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(controllers, controller => controller.Contains("Projects", StringComparison.Ordinal));
        Assert.Contains(controllers, controller => controller.Contains("Buildings", StringComparison.Ordinal));
        Assert.Contains(controllers, controller => controller.Contains("Calculations", StringComparison.Ordinal) || controller.Contains("LoadCalculations", StringComparison.Ordinal));
        Assert.Contains(controllers, controller => controller.Contains("EngineeringWorkflow", StringComparison.Ordinal));
        Assert.Contains(controllers, controller =>
            controller.Contains("Reports", StringComparison.Ordinal) ||
            controller.Contains("Reporting", StringComparison.Ordinal));
        Assert.Contains(controllers, controller =>
            controller.Contains("ReferenceData", StringComparison.Ordinal) ||
            controller.Contains("Development", StringComparison.Ordinal) ||
            controller.Contains("StandardTables", StringComparison.Ordinal));
    }

    [Fact]
    public void ApiControllersAreRepresentedInInventoryOrAllowlist()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(InventoryJsonPath));
        var inventoryControllers = document.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Select(item => item.GetProperty("controller").GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.Ordinal);

        var allowlist = ParseAllowlistLines(ControllerAllowlistPath)
            .ToHashSet(StringComparer.Ordinal);

        var controllerFiles = Directory.GetFiles(ControllersRootPath, "*Controller.cs", SearchOption.AllDirectories);
        var missing = controllerFiles
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !inventoryControllers.Contains(name) && !allowlist.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Controllers missing in endpoint inventory and allowlist:\n" + string.Join('\n', missing));
    }

    private static IReadOnlyList<string> ParseAllowlistLines(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
            .ToArray();
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-endpoint-protection-inventory.schema.json");

    private static string ControllersRootPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Controllers");

    private static string ControllerAllowlistPath =>
        Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "security", "api-endpoint-inventory-controller-allowlist.txt");
}
