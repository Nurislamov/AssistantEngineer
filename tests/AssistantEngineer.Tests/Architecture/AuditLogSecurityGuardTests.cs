using System.Text.Json;
using AssistantEngineer.Modules.Identity.Application.Services.Audit;

namespace AssistantEngineer.Tests.Architecture;

public sealed class AuditLogSecurityGuardTests
{
    [Fact]
    public void AuditMetadataSanitizerRemovesSensitiveKeys()
    {
        var sanitizer = new AuditMetadataSanitizer();
        var metadata = new Dictionary<string, string>
        {
            ["apiKey"] = "value",
            ["token"] = "value",
            ["password"] = "value",
            ["secret"] = "value",
            ["authorization"] = "value",
            ["cookie"] = "value",
            ["safeKey"] = "safe-value"
        };

        var sanitized = sanitizer.Sanitize(metadata, maxValueLength: 512);

        Assert.DoesNotContain("apiKey", sanitized.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", sanitized.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", sanitized.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", sanitized.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorization", sanitized.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("cookie", sanitized.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("safeKey", sanitized.Keys);
    }

    [Fact]
    public void AuditEventRegistryDisablesPayloadByDefault()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(AuditEventRegistryPath));
        foreach (var eventEntry in document.RootElement.EnumerateArray())
        {
            Assert.True(eventEntry.TryGetProperty("allowPayload", out var allowPayload));
            Assert.False(allowPayload.GetBoolean());
        }
    }

    [Fact]
    public void AuditLogFoundationDeclaresNoSecretsOrFullPayloads()
    {
        var content = File.ReadAllText(AuditLogFoundationPath);

        Assert.Contains("Never store API keys, tokens, passwords, secrets", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Never store full request/response payloads", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string AuditEventRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "audit-event-registry.json");

    private static string AuditLogFoundationPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "audit-log-foundation.md");
}
