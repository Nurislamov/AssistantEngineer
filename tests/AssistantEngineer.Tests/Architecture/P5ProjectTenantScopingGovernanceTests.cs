using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ProjectTenantScopingGovernanceTests
{
    [Fact]
    public void ProjectTenantScopingDocumentExists()
    {
        Assert.True(File.Exists(ProjectTenantScopingPath), $"Missing project tenant scoping document: {ProjectTenantScopingPath}");
    }

    [Fact]
    public void ProjectTenantScopingDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(ProjectTenantScopingPath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Resource ownership hierarchy",
            "## Project ownership model",
            "## Building scoping model",
            "## Workflow scoping model",
            "## Legacy unscoped resource migration",
            "## Access policy rules",
            "## Schema status",
            "## What is intentionally not enforced yet",
            "## Next steps P5-03/P5-04"
        };

        foreach (var section in requiredSections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProjectTenantScopingDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ProjectTenantScopingPath);
        var nonClaims = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No route authorization enforcement claim",
            "No external identity provider integration claim",
            "No certified/certification claim"
        };

        foreach (var claim in nonClaims)
        {
            Assert.Contains(claim, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SecurityBoundaryPolicyReferencesProjectTenantScopingModel()
    {
        var content = File.ReadAllText(SecurityBoundaryPolicyPath);

        Assert.Contains("project-tenant-scoping-model.md", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InventoryJsonContainsP5_02RoadmapEntry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(SecurityInventoryJsonPath));
        var roadmap = document.RootElement.GetProperty("p5Roadmap").EnumerateArray().ToArray();

        var p5Item = roadmap.SingleOrDefault(item =>
            string.Equals(item.GetProperty("item").GetString(), "P5-02", StringComparison.Ordinal));

        Assert.True(p5Item.ValueKind != JsonValueKind.Undefined, "P5-02 roadmap entry is missing.");

        var status = p5Item.GetProperty("status").GetString() ?? string.Empty;
        Assert.True(
            string.Equals(status, "Implemented", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase),
            $"Unexpected P5-02 status: '{status}'.");
    }

    [Fact]
    public void ProjectTenantAccessPolicyExists()
    {
        Assert.True(File.Exists(ProjectTenantAccessPolicyPath), $"Missing access policy source file: {ProjectTenantAccessPolicyPath}");
    }

    [Fact]
    public void ApiControllersRemainWithoutAuthorizeAttributesInP5_02()
    {
        var controllerRoot = Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Controllers");
        var files = Directory.GetFiles(controllerRoot, "*Controller.cs", SearchOption.AllDirectories);

        var violations = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("[Authorize", StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)}:{i + 1}");
                }
            }
        }

        Assert.True(violations.Count == 0, "P5-02 must not roll out controller authorization yet:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void CriticalPublicRouteShapesRemainUnchanged()
    {
        var routes = CollectRoutes();

        Assert.Contains("api/v{version:apiVersion}/projects", routes);
        Assert.Contains("api/v{version:apiVersion}/engineering-workflow", routes);
        Assert.Contains("api/v{version:apiVersion}/buildings", routes);
    }

    private static HashSet<string> CollectRoutes()
    {
        var controllerRoot = Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Controllers");
        var files = Directory.GetFiles(controllerRoot, "*Controller.cs", SearchOption.AllDirectories);

        var routes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            foreach (var line in File.ReadAllLines(file))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("[Route(\"", StringComparison.Ordinal))
                {
                    continue;
                }

                var start = trimmed.IndexOf('"', StringComparison.Ordinal);
                var end = trimmed.LastIndexOf('"');
                if (start < 0 || end <= start)
                {
                    continue;
                }

                routes.Add(trimmed[(start + 1)..end]);
            }
        }

        return routes;
    }

    private static string ProjectTenantScopingPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "project-tenant-scoping-model.md");

    private static string SecurityBoundaryPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-boundary-policy.md");

    private static string SecurityInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");

    private static string ProjectTenantAccessPolicyPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Identity",
            "Application",
            "Services",
            "Access",
            "ProjectTenantAccessPolicy.cs");
}
