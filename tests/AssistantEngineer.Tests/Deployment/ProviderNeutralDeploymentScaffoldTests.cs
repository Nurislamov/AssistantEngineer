using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.Deployment;

public sealed class ProviderNeutralDeploymentScaffoldTests
{
    private static readonly string DeployRoot = Path.Combine(TestPaths.RepoRoot, "deploy");

    [Fact]
    public void RequiredScaffoldFilesExist()
    {
        foreach (var path in RequiredFiles())
        {
            Assert.True(File.Exists(path), $"Missing provider-neutral deployment scaffold file: {path}");
        }
    }

    [Fact]
    public void DockerfilesAreMultiStageAndDoNotBakeSecretsOrManualArtifacts()
    {
        var backend = Read("docker", "backend", "Dockerfile");
        var frontend = Read("docker", "frontend", "Dockerfile");

        Assert.True(Regex.Matches(backend, "^FROM ", RegexOptions.Multiline).Count >= 2);
        Assert.True(Regex.Matches(frontend, "^FROM ", RegexOptions.Multiline).Count >= 2);
        Assert.Contains("mcr.microsoft.com/dotnet/sdk:10.0", backend, StringComparison.Ordinal);
        Assert.Contains("mcr.microsoft.com/dotnet/aspnet:10.0", backend, StringComparison.Ordinal);
        Assert.Contains("USER $APP_UID", backend, StringComparison.Ordinal);
        Assert.Contains("EXPOSE 8080", backend, StringComparison.Ordinal);
        Assert.Contains("npm ci", frontend, StringComparison.Ordinal);
        Assert.Contains("VITE_API_BASE_URL", frontend, StringComparison.Ordinal);

        Assert.All(new[] { backend, frontend }, content =>
        {
            Assert.DoesNotContain("BotToken=", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("WebhookSecret=", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(".pdf", content, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ComposeIsProviderNeutralAndAddsNoDatabaseService()
    {
        var compose = Read("docker-compose.yml");

        Assert.Contains("assistantengineer-api:", compose, StringComparison.Ordinal);
        Assert.Contains("assistantengineer-frontend:", compose, StringComparison.Ordinal);
        Assert.Contains("reverse-proxy:", compose, StringComparison.Ordinal);
        Assert.Contains("env_file:", compose, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_IS_ENABLED:-false", compose, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_ENABLE_CHAT_ID_DISCOVERY:-false", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("postgres:", compose, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mysql:", compose, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sqlserver:", compose, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnvironmentExampleIsIgnoredAndContainsOnlySafeDefaults()
    {
        var example = Read(".env.example");
        var ignore = Read(".gitignore");

        Assert.Contains(".env", ignore, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_IS_ENABLED=false", example, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false", example, StringComparison.Ordinal);
        Assert.Contains("AllowedChatIds", example, StringComparison.Ordinal);
        Assert.Contains("DeniedChatIds", example, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", example);
        Assert.DoesNotContain("example.com", example, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TelegramDefaultsRemainDisabledInApplicationAndDeploymentExamples()
    {
        var appSettings = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "appsettings.json"));
        var example = Read(".env.example");

        Assert.Contains("\"IsEnabled\":  false", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"EnableChatIdDiscovery\":  false", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"BotToken\":  null", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"WebhookSecret\":  null", appSettings, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_IS_ENABLED=false", example, StringComparison.Ordinal);
        Assert.Contains("TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false", example, StringComparison.Ordinal);
    }

    [Fact]
    public void ReverseProxySupportsApiAndTelegramWebhookRouting()
    {
        var caddy = Read("reverse-proxy", "Caddyfile.example");

        Assert.Contains("example.com", caddy, StringComparison.Ordinal);
        Assert.Contains("handle /api/*", caddy, StringComparison.Ordinal);
        Assert.Contains("/api/v1/equipment-diagnostics/telegram/webhook", caddy, StringComparison.Ordinal);
        Assert.DoesNotContain("getUpdates", caddy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentDocsStateBoundariesAndFutureRequirements()
    {
        var docs = string.Join(Environment.NewLine,
            Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, "docs", "deployment"), "*.md")
                .Select(File.ReadAllText));

        Assert.Contains("provider-neutral", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a production deployment", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never commit", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("domain", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HTTPS", docs, StringComparison.Ordinal);
        Assert.Contains("no database service", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DockerIgnoreExcludesGeneratedSecretsAndManualFiles()
    {
        var ignore = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".dockerignore"));

        foreach (var fragment in new[] { "bin/", "obj/", "node_modules/", "dist/", "artifacts/", ".git/", ".github/", "TestResults/", "coverage/", "*.pdf", ".env" })
        {
            Assert.Contains(fragment, ignore, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DeploymentScriptsAreInventoriedAndContainNoSecrets()
    {
        using var inventory = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "docs", "architecture", "scripts-tools-inventory.json")));
        var paths = inventory.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("path").GetString())
            .ToHashSet(StringComparer.Ordinal);
        var scriptsRoot = Path.Combine(TestPaths.RepoRoot, "scripts", "deployment");

        foreach (var script in Directory.GetFiles(scriptsRoot, "*.ps1"))
        {
            var relative = Path.GetRelativePath(TestPaths.RepoRoot, script).Replace('\\', '/');
            var content = File.ReadAllText(script);
            Assert.Contains(relative, paths);
            Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", content);
        }
    }

    private static IEnumerable<string> RequiredFiles() =>
    [
        Path.Combine(DeployRoot, "docker", "backend", "Dockerfile"),
        Path.Combine(DeployRoot, "docker", "frontend", "Dockerfile"),
        Path.Combine(DeployRoot, "docker-compose.yml"),
        Path.Combine(DeployRoot, ".env.example"),
        Path.Combine(DeployRoot, ".gitignore"),
        Path.Combine(DeployRoot, "reverse-proxy", "Caddyfile.example")
    ];

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([DeployRoot, .. parts]));
}
