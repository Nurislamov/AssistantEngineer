using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Deployment;

public sealed class DeploymentCicdDryRunTests
{
    private static readonly string WorkflowPath = Path.Combine(
        TestPaths.RepoRoot, ".github", "workflows", "deployment-dry-run.yml");
    private static readonly string LocalRunnerPath = Path.Combine(
        TestPaths.RepoRoot, "scripts", "deployment", "run-ci-deployment-dry-run.ps1");
    private static readonly string DryRunDocsPath = Path.Combine(
        TestPaths.RepoRoot, "docs", "deployment", "ci-deployment-dry-run.md");

    [Fact]
    public void DeploymentDryRunWorkflowExistsAndRunsRequiredValidation()
    {
        Assert.True(File.Exists(WorkflowPath));
        var workflow = File.ReadAllText(WorkflowPath);

        Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
        Assert.Contains("validate-deployment-scaffold.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("validate-production-env.ps1 -EnvPath deploy/.env.example -AllowPlaceholders", workflow, StringComparison.Ordinal);
        Assert.Contains("docker compose --env-file deploy/.env.example -f deploy/docker-compose.yml config --quiet", workflow, StringComparison.Ordinal);
        Assert.Contains("assistantengineer-api:ci", workflow, StringComparison.Ordinal);
        Assert.Contains("assistantengineer-frontend:ci", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void DeploymentDryRunWorkflowCannotDeployPushOrUseRemoteShell()
    {
        var workflow = File.ReadAllText(WorkflowPath);

        Assert.DoesNotMatch(@"(?im)^\s*environment\s*:\s*production\s*$", workflow);
        Assert.DoesNotMatch(@"(?im)^\s*(run\s*:\s*)?(ssh|scp)\b", workflow);
        Assert.DoesNotContain("docker push", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker login", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("login-action", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("appleboy/", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secrets.", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentDryRunWorkflowContainsNoRealTokenDomainOrPdf()
    {
        var workflow = File.ReadAllText(WorkflowPath);

        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", workflow);
        Assert.DoesNotContain("BotToken", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WebhookSecret", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".pdf", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Regex.Matches(workflow, @"(?i)\b(?:[a-z0-9-]+\.)+(?:com|net|org|io|dev|cloud)\b"));
    }

    [Fact]
    public void LocalDryRunRunnerExistsIsInventoriedAndHasExplicitDockerControls()
    {
        Assert.True(File.Exists(LocalRunnerPath));
        var runner = File.ReadAllText(LocalRunnerPath);
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json")));
        var inventoriedPaths = inventory.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("path").GetString())
            .ToArray();

        Assert.Contains("scripts/deployment/run-ci-deployment-dry-run.ps1", inventoriedPaths);
        Assert.Contains("[switch]$RequireDocker", runner, StringComparison.Ordinal);
        Assert.Contains("[switch]$BuildImages", runner, StringComparison.Ordinal);
        Assert.Contains("validate-deployment-scaffold.ps1", runner, StringComparison.Ordinal);
        Assert.Contains("validate-production-env.ps1", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("docker push", runner, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker login", runner, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DryRunDocumentationStatesBoundariesAndRemainingProductionWork()
    {
        Assert.True(File.Exists(DryRunDocsPath));
        var docs = File.ReadAllText(DryRunDocsPath);

        Assert.Contains("does not perform a production deployment", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not push images", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uses no real secrets", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("provider-specific infrastructure", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("keeps Telegram webhook transport and chat ID discovery disabled", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("choose a VPS provider", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("configure DNS and public HTTPS", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentDefaultsRemainDisabledAndSecretFree()
    {
        var example = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "deploy", ".env.example"));

        Assert.Contains("TELEGRAM_IS_ENABLED=false", example, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false", example, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", example);
    }
}
