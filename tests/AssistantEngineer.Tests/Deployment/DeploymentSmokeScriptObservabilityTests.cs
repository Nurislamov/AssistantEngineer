namespace AssistantEngineer.Tests.Deployment;

public sealed class DeploymentSmokeScriptObservabilityTests
{
    [Fact]
    public void SmokeScriptChecksExistingHealthAndReadinessWithoutSecrets()
    {
        var script = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "scripts", "deployment", "smoke-production-stack.ps1"));

        Assert.Contains("$ApiBaseUrl/health", script, StringComparison.Ordinal);
        Assert.Contains("$ApiBaseUrl/ready", script, StringComparison.Ordinal);
        Assert.Contains("ExplicitlyExpectTelegramEnabled", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $BotToken", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host $WebhookSecret", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", script);
    }
}
