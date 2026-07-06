using System.Diagnostics;
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
            "scripts/deployment/validate-deployment-scaffold.ps1",
            "scripts/deployment/generate-production-secret-values.ps1"
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
        Assert.Contains(
            "ASSISTANTENGINEER_DATAPROTECTION_KEYS_PATH=/home/app/.aspnet/DataProtection-Keys",
            example,
            StringComparison.Ordinal);
        Assert.Contains("ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PATH=", example, StringComparison.Ordinal);
        Assert.Contains("ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PASSWORD=", example, StringComparison.Ordinal);
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

        Assert.Contains("../artifacts/operations:/app/artifacts/operations", compose, StringComparison.Ordinal);
        Assert.Contains(
            "assistantengineer_dataprotection_keys:/home/app/.aspnet/DataProtection-Keys",
            compose,
            StringComparison.Ordinal);
        Assert.Contains("assistantengineer_dataprotection_keys:", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("api_operations:", compose, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", compose);
        Assert.DoesNotMatch(@"(?im)^\s*(postgres|mysql|mariadb|sqlserver|mssql|mongodb|redis|database|db)\s*:", compose);
    }

    [Fact]
    public void BackendImagePreparesWritableDataProtectionVolumeMount()
    {
        var dockerfile = ReadDeploy("docker", "backend", "Dockerfile");

        Assert.Contains("mkdir -p /app/artifacts/operations", dockerfile, StringComparison.Ordinal);
        Assert.Contains("/home/app/.aspnet/DataProtection-Keys", dockerfile, StringComparison.Ordinal);
        Assert.Contains("chown -R $APP_UID:$APP_UID /app/artifacts /home/app/.aspnet", dockerfile, StringComparison.Ordinal);
        Assert.Contains("USER $APP_UID", dockerfile, StringComparison.Ordinal);
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
        Assert.Contains("docker compose config", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not paste full", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production-secret-rotation-runbook.md", docs, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSecretRotationRunbookUsesPlaceholdersAndSafeComposeGuidance()
    {
        var runbook = File.ReadAllText(Path.Combine(DeploymentDocsRoot, "production-secret-rotation-runbook.md"));

        Assert.Contains("What Leaked", runbook, StringComparison.Ordinal);
        Assert.Contains("What Not To Paste", runbook, StringComparison.Ordinal);
        Assert.Contains("Safe Verification Commands", runbook, StringComparison.Ordinal);
        Assert.Contains("PostgreSQL password", runbook, StringComparison.Ordinal);
        Assert.Contains("API key", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("webhook secret", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BotFather", runbook, StringComparison.Ordinal);
        Assert.Contains("config --quiet", runbook, StringComparison.Ordinal);
        Assert.Contains("Do not run `docker compose --env-file deploy/.env -f deploy/docker-compose.yml config`", runbook, StringComparison.Ordinal);
        Assert.Contains("<new-api-key>", runbook, StringComparison.Ordinal);
        Assert.Contains("<new-webhook-secret>", runbook, StringComparison.Ordinal);
        Assert.Contains("<new-postgres-password>", runbook, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", runbook);
        Assert.DoesNotMatch(@"(?i)(password|secret|api[-_ ]?key)\s*=\s*[A-Za-z0-9_-]{16,}", runbook);
    }

    [Fact]
    public void ProductionSecretGeneratorDoesNotReadOrEditEnvByDefault()
    {
        var script = ReadScript("generate-production-secret-values.ps1");

        Assert.Contains("RandomNumberGenerator", script, StringComparison.Ordinal);
        Assert.Contains("artifacts/operations/secret-rotation", script, StringComparison.Ordinal);
        Assert.Contains("Do not commit", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("did not read deploy/.env", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("did not edit deploy/.env", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Get-Content deploy/.env", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Set-Content deploy/.env", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Add-Content deploy/.env", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", script);
    }

    [Fact]
    public void SecretRuntimeAndManualBinaryFilesRemainUntracked()
    {
        var tracked = GitLsFiles();

        Assert.DoesNotContain("deploy/.env", tracked);
        Assert.DoesNotContain("artifacts/operations/equipment-diagnostics-manual-bindings.json", tracked);
        Assert.DoesNotContain(tracked, path =>
            path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));

        var rootIgnore = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".gitignore"));
        var deployIgnore = ReadDeploy(".gitignore");
        Assert.Contains("/artifacts/operations/", rootIgnore, StringComparison.Ordinal);
        Assert.Contains("/artifacts/operations/secret-rotation/", rootIgnore, StringComparison.Ordinal);
        Assert.Contains(".env", deployIgnore, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvironmentBackupsAreIgnoredAndOperatorRunbookMovesThemOutsideRepository()
    {
        var deployIgnore = ReadDeploy(".gitignore");
        var runbook = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "operations",
            "production-compose-hygiene.md"));

        Assert.Contains(".env.before-*", deployIgnore, StringComparison.Ordinal);
        Assert.Contains("/opt/assistantengineer-runtime-backups/env", runbook, StringComparison.Ordinal);
        Assert.Contains(
            "mv /opt/assistantengineer/deploy/.env.before-* /opt/assistantengineer-runtime-backups/env/",
            runbook,
            StringComparison.Ordinal);
        Assert.Contains("confirm each matched file is a backup copy", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("is not the active", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rm /opt/assistantengineer/deploy/.env.before-", runbook, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionOrphanRunbookRequiresInspectionAndNoAutomaticRemoval()
    {
        var runbook = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "operations",
            "production-compose-hygiene.md"));

        Assert.Contains(
            "docker compose --env-file .env -f docker-compose.yml ps",
            runbook,
            StringComparison.Ordinal);
        Assert.Contains("docker ps --filter \"name=postgres\"", runbook, StringComparison.Ordinal);
        Assert.Contains("docker inspect assistantengineer-postgres-1", runbook, StringComparison.Ordinal);
        Assert.Contains("docker volume ls", runbook, StringComparison.Ordinal);
        Assert.Contains("production data-bearing infrastructure", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not add or run", runbook, StringComparison.Ordinal);
        Assert.Contains("`--remove-orphans`", runbook, StringComparison.Ordinal);

        var productionScripts = Directory.GetFiles(
                DeploymentScriptsRoot,
                "*",
                SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(
                Path.Combine(TestPaths.RepoRoot, "scripts", "operations"),
                "*",
                SearchOption.AllDirectories));
        Assert.All(productionScripts, path =>
            Assert.DoesNotContain("--remove-orphans", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase));
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

    private static IReadOnlySet<string> GitLsFiles()
    {
        var start = new ProcessStartInfo("git", "ls-files -z")
        {
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start git.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, stderr);
        return stdout
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);
    }
}
