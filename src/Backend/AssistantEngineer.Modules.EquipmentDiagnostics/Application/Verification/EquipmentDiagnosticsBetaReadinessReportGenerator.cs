namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class EquipmentDiagnosticsBetaReadinessReportGenerator
{
    private static readonly string[] KnownLimitations =
    [
        "Closed beta only; this is not a production or public release.",
        "No production deployment exists yet.",
        "No real domain, VPS, or bot-provider credential exists yet.",
        "Telegram transport and chat identifier discovery are disabled by default.",
        "No database or audit persistence exists.",
        "No external monitoring stack exists.",
        "No AI, RAG, or vector search exists.",
        "The runtime catalog is the only source for final diagnostic answers.",
        "Manual codebook, staging, and preview data are not final diagnosis.",
        "Manual-backed coverage is partial and is not full vendor manual coverage."
    ];

    public EquipmentDiagnosticsBetaReadinessReport Generate(EquipmentDiagnosticsBetaReadinessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var root = Path.GetFullPath(input.RepositoryRoot);
        var sections = new[]
        {
            Section(root, "Runtime diagnostic catalog readiness",
                Required(root, "Runtime knowledge catalog", "src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Knowledge/equipment-diagnostics.schema.json")),
            Section(root, "Deterministic bot flow readiness",
                Required(root, "Deterministic bot service", "src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Bot/EquipmentDiagnosticBotService.cs"),
                Required(root, "Field scenario pack", "docs/equipment-diagnostics/bot-scenarios/README.md")),
            Section(root, "Bot API endpoint readiness",
                Required(root, "Thin API controller", "src/Backend/AssistantEngineer.Api/Controllers/Equipment/EquipmentDiagnosticsController.cs"),
                Required(root, "Bot API example", "docs/equipment-diagnostics/examples/bot-diagnostic-request.example.json")),
            Section(root, "Frontend beta UI readiness",
                Required(root, "Frontend diagnostic panel", "src/Frontend/src/widgets/equipment-diagnostics/ui/EquipmentDiagnosticBotPanel.tsx"),
                Required(root, "Frontend diagnostic panel tests", "src/Frontend/src/widgets/equipment-diagnostics/ui/EquipmentDiagnosticBotPanel.test.tsx")),
            Section(root, "Telegram adapter readiness",
                Required(root, "Telegram adapter documentation", "docs/equipment-diagnostics/telegram-adapter.md")),
            Section(root, "Telegram webhook transport readiness",
                Required(root, "Webhook deployment guide", "docs/equipment-diagnostics/telegram-webhook-deployment.md"),
                ConfigDisabled(root, "Telegram transport disabled by default", "TELEGRAM_IS_ENABLED:-false")),
            Section(root, "Telegram access policy readiness",
                Required(root, "Telegram operations checklist", "docs/equipment-diagnostics/telegram-operations-checklist.md"),
                ConfigDisabled(root, "Chat identifier discovery disabled by default", "TELEGRAM_ENABLE_CHAT_ID_DISCOVERY:-false")),
            Section(root, "Deployment scaffold readiness",
                Required(root, "Provider-neutral deployment scaffold", "deploy/docker-compose.yml"),
                Required(root, "Deployment release checklist", "docs/deployment/production-release-checklist.md")),
            Section(root, "Deployment CI dry-run readiness",
                Required(root, "Deployment dry-run workflow", ".github/workflows/deployment-dry-run.yml"),
                Required(root, "Deployment dry-run script", "scripts/deployment/run-ci-deployment-dry-run.ps1"),
                OptionalArtifact(root, "Branch readiness report", input.BranchReadinessReportPath, "artifacts/verification/branch-readiness/branch-readiness-report.json")),
            Section(root, "Health/readiness/operational diagnostics readiness",
                Required(root, "Health endpoint tests", "tests/AssistantEngineer.Tests/Api/ApiHealthEndpointsIntegrationTests.cs"),
                Required(root, "Operational diagnostics service", "src/Backend/AssistantEngineer.Api/Services/OperationalDiagnostics/OperationalDiagnosticsService.cs")),
            Section(root, "Correlation/logging readiness",
                Required(root, "Correlation integration tests", "tests/AssistantEngineer.Tests/Api/ApiCorrelationIdIntegrationTests.cs")),
            Section(root, "Incident/log review readiness",
                Required(root, "Incident runbooks", "docs/operations/incidents/README.md"),
                Required(root, "Sanitized log collection", "scripts/operations/collect-sanitized-logs.ps1"),
                Required(root, "Offline log redaction", "scripts/operations/redact-log-file.ps1")),
            Section(root, "Manual/codebook/staging evidence readiness",
                OptionalArtifact(root, "Codebook coverage report", input.CodebookCoverageReportPath, "artifacts/verification/equipment-diagnostics/codebook-coverage-report.json"),
                OptionalArtifact(root, "Staging preview", input.StagingPreviewPath, "artifacts/verification/equipment-diagnostics/staging-candidate-preview.json")),
            Section(root, "Security/no-secret readiness",
                Required(root, "Placeholder-only environment example", "deploy/.env.example"),
                NoConfiguredCredentialValues(root),
                NoForbiddenFiles(root)),
            new EquipmentDiagnosticsBetaReadinessSection(
                "Known limitations",
                EquipmentDiagnosticsBetaReadinessStatus.Warning,
                KnownLimitations.Select(limit => new EquipmentDiagnosticsBetaReadinessCheck(
                    limit,
                    EquipmentDiagnosticsBetaReadinessStatus.Warning,
                    limit)).ToArray())
        };

        var blockers = sections.Sum(section => section.Checks.Count(check => check.Status == EquipmentDiagnosticsBetaReadinessStatus.Blocker));
        var warnings = sections.Sum(section => section.Checks.Count(check => check.Status == EquipmentDiagnosticsBetaReadinessStatus.Warning));
        var overall = blockers > 0
            ? EquipmentDiagnosticsBetaReadinessStatus.Blocker
            : warnings > 0
                ? EquipmentDiagnosticsBetaReadinessStatus.Warning
                : EquipmentDiagnosticsBetaReadinessStatus.Pass;

        return new EquipmentDiagnosticsBetaReadinessReport(
            input.GeneratedAtUtc ?? DateTimeOffset.UtcNow,
            input.RepositoryBaseRef,
            input.Branch,
            input.Head,
            overall,
            blockers,
            warnings,
            sections,
            KnownLimitations);
    }

    private static EquipmentDiagnosticsBetaReadinessSection Section(
        string root,
        string name,
        params EquipmentDiagnosticsBetaReadinessCheck[] checks)
    {
        _ = root;
        var status = checks.Any(check => check.Status == EquipmentDiagnosticsBetaReadinessStatus.Blocker)
            ? EquipmentDiagnosticsBetaReadinessStatus.Blocker
            : checks.Any(check => check.Status == EquipmentDiagnosticsBetaReadinessStatus.Warning)
                ? EquipmentDiagnosticsBetaReadinessStatus.Warning
                : checks.All(check => check.Status == EquipmentDiagnosticsBetaReadinessStatus.NotApplicable)
                    ? EquipmentDiagnosticsBetaReadinessStatus.NotApplicable
                    : EquipmentDiagnosticsBetaReadinessStatus.Pass;
        return new EquipmentDiagnosticsBetaReadinessSection(name, status, checks);
    }

    private static EquipmentDiagnosticsBetaReadinessCheck Required(string root, string name, string relativePath) =>
        File.Exists(Combine(root, relativePath))
            ? new(name, EquipmentDiagnosticsBetaReadinessStatus.Pass, "Required repository contract is present.", relativePath)
            : new(name, EquipmentDiagnosticsBetaReadinessStatus.Blocker, "Required repository contract is missing.", relativePath);

    private static EquipmentDiagnosticsBetaReadinessCheck ConfigDisabled(string root, string name, string expectedFragment)
    {
        const string path = "deploy/docker-compose.yml";
        var content = File.Exists(Combine(root, path)) ? File.ReadAllText(Combine(root, path)) : string.Empty;
        return content.Contains(expectedFragment, StringComparison.Ordinal)
            ? new(name, EquipmentDiagnosticsBetaReadinessStatus.Pass, "Safe disabled-by-default configuration is present.", path)
            : new(name, EquipmentDiagnosticsBetaReadinessStatus.Blocker, "Safe disabled-by-default configuration is missing.", path);
    }

    private static EquipmentDiagnosticsBetaReadinessCheck OptionalArtifact(
        string root,
        string name,
        string? explicitPath,
        string defaultRelativePath)
    {
        var relative = string.IsNullOrWhiteSpace(explicitPath) ? defaultRelativePath : NormalizeRelative(root, explicitPath);
        return File.Exists(Combine(root, relative))
            ? new(name, EquipmentDiagnosticsBetaReadinessStatus.Pass, "Ignored local verification artifact is available.", relative)
            : new(name, EquipmentDiagnosticsBetaReadinessStatus.Warning, "Ignored local verification artifact has not been generated in this workspace.", relative);
    }

    private static EquipmentDiagnosticsBetaReadinessCheck NoForbiddenFiles(string root)
    {
        var forbidden = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredWorkspacePath(root, path))
            .Where(path => Path.GetExtension(path) is ".pdf" or ".log")
            .Select(path => NormalizeRelative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();
        return forbidden is null
            ? new("No committed manual or log dump candidates", EquipmentDiagnosticsBetaReadinessStatus.Pass, "No PDF or log dump files were found.")
            : new("No committed manual or log dump candidates", EquipmentDiagnosticsBetaReadinessStatus.Blocker, $"Forbidden file candidate exists: {forbidden}", forbidden);
    }

    private static EquipmentDiagnosticsBetaReadinessCheck NoConfiguredCredentialValues(string root)
    {
        var files = new[]
        {
            "deploy/.env.example",
            "src/Backend/AssistantEngineer.Api/appsettings.json",
            "src/Backend/AssistantEngineer.Api/appsettings.Development.json",
            "src/Backend/AssistantEngineer.Api/appsettings.Testing.json"
        };
        foreach (var relativePath in files.Where(path => File.Exists(Combine(root, path))))
        {
            foreach (var line in File.ReadLines(Combine(root, relativePath)))
            {
                if (!line.Contains("BotToken", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("WebhookSecret", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var separator = line.IndexOfAny(['=', ':']);
                var value = separator >= 0
                    ? line[(separator + 1)..].Trim().Trim(',', '"', '\'')
                    : string.Empty;
                if (!string.IsNullOrWhiteSpace(value) &&
                    !string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) &&
                    !value.StartsWith("${", StringComparison.Ordinal))
                {
                    return new(
                        "No configured credential values",
                        EquipmentDiagnosticsBetaReadinessStatus.Blocker,
                        "A stable repository configuration contains a non-placeholder credential value.",
                        relativePath);
                }
            }
        }

        return new(
            "No configured credential values",
            EquipmentDiagnosticsBetaReadinessStatus.Pass,
            "Stable repository configuration contains no configured credential values.");
    }

    private static string Combine(string root, string relativePath) =>
        Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string NormalizeRelative(string root, string path)
    {
        var fullPath = Path.IsPathRooted(path) ? Path.GetFullPath(path) : Combine(root, path);
        return Path.GetRelativePath(root, fullPath).Replace('\\', '/');
    }

    private static bool IsIgnoredWorkspacePath(string root, string path)
    {
        var relative = NormalizeRelative(root, path);
        return relative.StartsWith(".git/", StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith("TestResults/", StringComparison.OrdinalIgnoreCase) ||
            relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
            relative.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase);
    }
}
