using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Api;

public class ApiHardeningDocumentationTests
{
    [Fact]
    public void ApiHardeningDocumentationExistsAndDescribesBaselineControls()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "api-hardening.md");

        Assert.True(File.Exists(path), $"API hardening documentation must exist: {path}");
        var content = File.ReadAllText(path);

        Assert.Contains("P2-01", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GET /health", content, StringComparison.Ordinal);
        Assert.Contains("GET /ready", content, StringComparison.Ordinal);
        Assert.Contains("Rate limiting", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CORS", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("default", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deny-by-default", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiHardeningDocumentationContainsRequiredLimitationsAndNonClaims()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "api",
            "api-hardening.md");

        var content = File.ReadAllText(path);

        Assert.Contains("not a full production security program", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not distributed", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("structured audit logging is future work", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("distributed/durable idempotency remains future work", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tenant/user isolation hardening is future work", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("distributed rate limiting for multi-node deployment is future work", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a compliance certificate", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not external validation evidence", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not provide full standard compliance claim", content, StringComparison.OrdinalIgnoreCase);
    }
}
