using System.Text.Json;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5ApiAuthenticationBoundaryGovernanceTests
{
    [Fact]
    public void ApiAuthenticationBoundaryDocumentExists()
    {
        Assert.True(File.Exists(ApiAuthenticationBoundaryDocPath), $"Missing document: {ApiAuthenticationBoundaryDocPath}");
    }

    [Fact]
    public void ApiAuthenticationBoundaryDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(ApiAuthenticationBoundaryDocPath);
        var sections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Authentication model",
            "## API key policy",
            "## JWT/OIDC future policy",
            "## Development compatibility",
            "## Failure behavior",
            "## Observability",
            "## P5-04 readiness"
        };

        foreach (var section in sections)
        {
            Assert.Contains(section, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ApiAuthenticationBoundaryDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ApiAuthenticationBoundaryDocPath);
        var required = new[]
        {
            "No production security certification claim",
            "No SOC 2 / ISO 27001 compliance claim",
            "No full multi-tenant isolation claim yet",
            "No external identity provider integration claim",
            "No certified/certification claim",
            "No claim that API keys alone are complete user authentication"
        };

        foreach (var phrase in required)
        {
            Assert.Contains(phrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SecurityBoundaryPolicyReferencesApiAuthenticationBoundary()
    {
        var content = File.ReadAllText(SecurityBoundaryPolicyPath);
        Assert.Contains("api-authentication-boundary.md", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InventoryJsonMarksP5_03AsImplementedOrInProgress()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(SecurityInventoryJsonPath));
        var roadmap = document.RootElement.GetProperty("p5Roadmap").EnumerateArray().ToArray();
        var p5_03 = roadmap.SingleOrDefault(item =>
            string.Equals(item.GetProperty("item").GetString(), "P5-03", StringComparison.Ordinal));

        Assert.True(p5_03.ValueKind != JsonValueKind.Undefined, "P5-03 roadmap item is missing.");

        var status = p5_03.GetProperty("status").GetString() ?? string.Empty;
        Assert.True(
            string.Equals(status, "Implemented", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "InProgress", StringComparison.OrdinalIgnoreCase),
            $"Unexpected P5-03 status: '{status}'.");
    }

    [Fact]
    public void Appsettings_DoNotContainPlainAllowedApiKeysArrays()
    {
        var files = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "appsettings.json"),
            Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "appsettings.Development.json")
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("AllowedApiKeys", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ApiKeys", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SourceLoggingTemplates_DoNotUseRawCredentialPlaceholders()
    {
        var sourceRoot = Path.Combine(TestPaths.RepoRoot, "src", "Backend");
        var sensitiveMarkers = new[]
        {
            "{ApiKey",
            "{Token",
            "{Password",
            "{Secret",
            "{BearerToken"
        };

        var violations = new List<string>();
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
                {
                    continue;
                }

                if (sensitiveMarkers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                {
                    violations.Add($"{relative}:{index + 1}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Potential sensitive credential logging placeholders found:\n" + string.Join('\n', violations));
    }

    private static string ApiAuthenticationBoundaryDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-authentication-boundary.md");

    private static string SecurityBoundaryPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-boundary-policy.md");

    private static string SecurityInventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.json");
}
