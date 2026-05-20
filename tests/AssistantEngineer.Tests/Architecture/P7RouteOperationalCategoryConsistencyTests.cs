using System.Text.RegularExpressions;
using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed partial class P7RouteOperationalCategoryConsistencyTests
{
    [Fact]
    public void InventoryRoutesContainRateLimitAndAuditCategories()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = inventory.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();
        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            Assert.False(string.IsNullOrWhiteSpace(endpoint.GetProperty("rateLimitCategory").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(endpoint.GetProperty("auditCategory").GetString()));
        }
    }

    [Fact]
    public void InventoryRateLimitCategoriesAlignWithResolverConstantsAndDocs()
    {
        var constantsContent = File.ReadAllText(RateLimitConstantsPath);
        var allowedCategories = RateLimitConstantRegex().Matches(constantsContent)
            .Select(match => match.Groups["value"].Value)
            .ToHashSet(StringComparer.Ordinal);

        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var usedCategories = inventory.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Select(endpoint => endpoint.GetProperty("rateLimitCategory").GetString() ?? string.Empty)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var category in usedCategories)
            Assert.Contains(category, allowedCategories);

        var rateLimitingDoc = File.ReadAllText(RateLimitingDocPath);
        foreach (var category in usedCategories)
            Assert.Contains(category, rateLimitingDoc, StringComparison.Ordinal);
    }

    [Fact]
    public void InventoryAuditCategoriesAreDocumented()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var usedAuditCategories = inventory.RootElement.GetProperty("endpoints")
            .EnumerateArray()
            .Select(endpoint => endpoint.GetProperty("auditCategory").GetString() ?? string.Empty)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .ToHashSet(StringComparer.Ordinal);

        var auditDoc = File.ReadAllText(AuditDocPath);
        var allowedExtra = new HashSet<string>(StringComparer.Ordinal) { "AuditDeferred" };
        foreach (var category in usedAuditCategories)
        {
            if (allowedExtra.Contains(category))
                continue;

            Assert.Contains(category, auditDoc, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ExecutionCategoriesOnReadMethodsMustBeDocumented()
    {
        using var inventory = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var endpoints = inventory.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        foreach (var endpoint in endpoints)
        {
            var rateLimitCategory = endpoint.GetProperty("rateLimitCategory").GetString() ?? string.Empty;
            var method = endpoint.GetProperty("httpMethod").GetString() ?? string.Empty;
            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(rateLimitCategory, "WorkflowExecute", StringComparison.Ordinal) &&
                !string.Equals(rateLimitCategory, "CalculationRun", StringComparison.Ordinal))
            {
                continue;
            }

            var knownLimitations = endpoint.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
            var targetPolicy = endpoint.GetProperty("targetPolicy").GetString() ?? string.Empty;

            Assert.True(
                targetPolicy.Contains("WorkflowsExecute", StringComparison.OrdinalIgnoreCase) ||
                knownLimitations.Any(item => item.Contains("execution", StringComparison.OrdinalIgnoreCase)),
                $"GET endpoint with execution category must explain execution semantics: {endpoint.GetProperty("controller").GetString()} {endpoint.GetProperty("routePattern").GetString()}");
        }
    }

    private static string InventoryJsonPath =>
        GovernancePathHelper.SecurityDocPath("api-endpoint-protection-inventory.json");

    private static string RateLimitingDocPath =>
        GovernancePathHelper.SecurityDocPath("rate-limiting-foundation.md");

    private static string AuditDocPath =>
        GovernancePathHelper.SecurityDocPath("audit-log-foundation.md");

    private static string RateLimitConstantsPath =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Security", "RateLimiting", "EndpointRateLimitCategories.cs");

    [GeneratedRegex("public const string \\w+ = \"(?<value>[^\"]+)\";", RegexOptions.CultureInvariant)]
    private static partial Regex RateLimitConstantRegex();
}
