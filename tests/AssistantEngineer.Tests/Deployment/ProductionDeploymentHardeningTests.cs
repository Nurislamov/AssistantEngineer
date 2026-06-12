using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Deployment;

public sealed class ProductionDeploymentHardeningTests
{
    private static readonly string DeployRoot = Path.Combine(TestPaths.RepoRoot, "deploy");
    private static readonly string DeploymentScriptsRoot =
        Path.Combine(TestPaths.RepoRoot, "scripts", "deployment");
    private static readonly string DeploymentDocsRoot =
        Path.Combine(TestPaths.RepoRoot, "docs", "deployment");

    [Fact]
    public void DeploymentValidatorsExistAndAreInventoried()
    {
        var expectedScripts = new[]
        {
            "scripts/deployment/validate-production-env.ps1",
            "scripts/deployment/validate-deployment-scaffold.ps1"
        };
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json")));
        var inventoriedPaths = inventory.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("path").GetString())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var relativePath in expectedScripts)
        {
            Assert.True(File.Exists(Path.Combine(
                TestPaths.RepoRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar))));
            Assert.Contains(relativePath, inventoriedPaths);
        }
    }

    [Fact]
    public void SmokeScriptDoesNotPrintTokensOrSecrets()
    {
        var smoke = ReadScript("smoke-production-stack.ps1");

        Assert.DoesNotContain("Write-Host $BotToken", smoke, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host $WebhookSecret", smoke, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", smoke);
        Assert.Contains("Telegram webhook disabled by default", smoke, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvironmentExampleKeepsSafeEmptyDefaults()
    {
        var example = ReadDeploy(".env.example");

        Assert.Contains("TELEGRAM_IS_ENABLED=false", example, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false", example, StringComparison.Ordinal);
        Assert.Contains("AssistantEngineer__EquipmentDiagnostics__Telegram__BotToken=", example, StringComparison.Ordinal);
        Assert.Contains("AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret=", example, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", example);
    }

    [Fact]
    public void ComposeKeepsExpectedSecretFreeServicesAndNoDatabase()
    {
        var compose = ReadDeploy("docker-compose.yml");

        foreach (var service in new[] { "assistantengineer-api:", "assistantengineer-frontend:", "reverse-proxy:" })
        {
            Assert.Contains(service, compose, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", compose);
        Assert.DoesNotMatch(@"(?im)^\s*(postgres|mysql|mariadb|sqlserver|mssql|mongodb|redis|database|db)\s*:", compose);
    }

    [Fact]
    public void ReverseProxyUsesOnlyApprovedPlaceholderDomainsAndRoutesApi()
    {
        var proxy = ReadDeploy("reverse-proxy", "Caddyfile.example");
        var domains = Regex.Matches(proxy, @"(?i)\b(?:[a-z0-9-]+\.)+[a-z]{2,}\b")
            .Select(match => match.Value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(domains);
        Assert.All(domains, domain => Assert.Contains(domain, new[] { "example.com", "api.example.com" }));
        Assert.Contains("handle /api/*", proxy, StringComparison.Ordinal);
        Assert.Contains("reverse_proxy assistantengineer-api:8080", proxy, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionReleaseRollbackAndOperationsNotesExist()
    {
        foreach (var file in new[]
        {
            "production-release-checklist.md",
            "rollback-checklist.md",
            "logging-monitoring-backup-notes.md"
        })
        {
            Assert.True(File.Exists(Path.Combine(DeploymentDocsRoot, file)), $"Missing deployment document: {file}");
        }
    }

    [Fact]
    public void DeploymentDocsStateSecurityAndOperationalNonClaims()
    {
        var docs = string.Join(Environment.NewLine,
            Directory.GetFiles(DeploymentDocsRoot, "*.md").Select(File.ReadAllText));

        Assert.Contains("never commit secrets", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public HTTPS", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BotFather token only at the final", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Telegram webhook transport and chat ID discovery disabled", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No monitoring system", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No database service", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit log", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentScaffoldAndScriptsContainNoTokenLikeValuesOrPdfFiles()
    {
        var files = Directory.GetFiles(DeployRoot, "*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(DeploymentScriptsRoot, "*", SearchOption.AllDirectories))
            .ToArray();

        Assert.DoesNotContain(files, file => string.Equals(Path.GetExtension(file), ".pdf", StringComparison.OrdinalIgnoreCase));
        Assert.All(files, file =>
            Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", File.ReadAllText(file)));
    }

    [Fact]
    public void DeploymentScaffoldAddsNoProviderSpecificInfrastructureOrLongPollingService()
    {
        var paths = Directory.GetFiles(DeployRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(DeployRoot, path).Replace('\\', '/'))
            .ToArray();
        var deploymentText = string.Join(Environment.NewLine,
            Directory.GetFiles(DeployRoot, "*", SearchOption.AllDirectories)
                .Where(path => Path.GetExtension(path) is not ".env")
                .Select(File.ReadAllText));

        Assert.DoesNotContain(paths, path =>
            path.EndsWith(".tf", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".bicep", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("cloudformation", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("getUpdates", deploymentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BackgroundService", deploymentText, StringComparison.Ordinal);
    }

    private static string ReadDeploy(params string[] parts) =>
        File.ReadAllText(Path.Combine([DeployRoot, .. parts]));

    private static string ReadScript(string file) =>
        File.ReadAllText(Path.Combine(DeploymentScriptsRoot, file));
}
