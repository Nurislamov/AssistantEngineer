namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class BranchReadinessVerificationService
{
    private static readonly string[] UnsafeTextFragments =
    [
        "bypass",
        "disable protection",
        "disable protections",
        "force run",
        "short protection",
        "ignore protection"
    ];

    private static readonly string[] ForbiddenPathFragments =
    [
        "src/Backend/AssistantEngineer.Modules.Calculations/",
        "src/Backend/AssistantEngineer.Modules.Equipment/",
        "src/Backend/AssistantEngineer.Infrastructure/Persistence/",
        "/Migrations/",
        "DbContext",
        "ISO52016",
        "Iso52016",
        "EnergyCalculation",
        "Telegram",
        "/Rag/",
        "/RAG/",
        "VectorSearch",
        "Embedding"
    ];

    private static readonly string[] AllowedPathPrefixes =
    [
        "docs/equipment-diagnostics/",
        "src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/",
        "tests/AssistantEngineer.Tests/EquipmentDiagnostics/",
        "tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification/",
        "tools/AssistantEngineer.Tools.BranchReadinessVerification/",
        "scripts/equipment-diagnostics/",
        "scripts/dev/",
        "src/Frontend/src/entities/equipment-diagnostics/",
        "src/Frontend/src/pages/equipment-diagnostics/",
        "src/Frontend/src/widgets/equipment-diagnostics/"
    ];

    private static readonly string[] AllowedExactPaths =
    [
        "AssistantEngineer.sln",
        "tests/AssistantEngineer.Tests/Api/ApiIntegrationTests.cs",
        "tests/AssistantEngineer.Tests/Api/EquipmentDiagnosticBotApiIntegrationTests.cs",
        "tests/AssistantEngineer.Tests/AssistantEngineer.Tests.csproj",
        "src/Backend/AssistantEngineer.Api/Controllers/Equipment/EquipmentDiagnosticsController.cs",
        "src/Frontend/src/app/router/AppRouter.tsx",
        "src/Frontend/src/app/router/paths.ts",
        "src/Frontend/src/shared/api/apiRoutes.ts",
        "src/Frontend/src/widgets/app-sidebar/ui/AppSidebar.tsx",
        "docs/security/api-endpoint-protection-inventory.json",
        "docs/security/api-endpoint-protection-inventory.md",
        "docs/architecture/module-boundary-matrix.json",
        "docs/architecture/scripts-tools-inventory.json",
        "docs/architecture/scripts-tools-inventory.md",
        ".github/workflows/equipment-diagnostics-branch-readiness.yml",
        ".gitignore"
    ];

    public BranchReadinessReport Verify(BranchReadinessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!string.Equals(input.Scope, "EquipmentDiagnostics", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unsupported branch readiness scope '{input.Scope}'. Only EquipmentDiagnostics is currently defined.");
        }

        var changedFiles = input.Files
            .Select(Classify)
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToArray();
        var issues = changedFiles
            .Where(file => file.ScopeClassification == BranchReadinessScopeClassification.Forbidden)
            .Select(file => Error(
                "ForbiddenChangedPath",
                file.Path,
                file.ScopeReason))
            .Concat(changedFiles
                .Where(file => file.ScopeClassification == BranchReadinessScopeClassification.Suspicious)
                .Select(file => Warning(
                    "SuspiciousChangedPath",
                    file.Path,
                    file.ScopeReason)))
            .Concat(ScanUnsafeWording(input.Files))
            .OrderBy(issue => issue.Severity)
            .ThenBy(issue => issue.Path, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal)
            .ToArray();
        var summary = BuildSummary(changedFiles);
        var hasBlockers = issues.Any(issue => issue.Severity == EquipmentDiagnosticsVerificationSeverity.Error) ||
            input.Commands.Any(command => !command.Passed) ||
            input.EquipmentDiagnosticsReport.HasBlockingIssues;
        var nextActions = BuildNextActions(issues, input.Commands, input.EquipmentDiagnosticsReport);

        return new BranchReadinessReport(
            Status: hasBlockers ? "Fail" : "Pass",
            CurrentBranch: input.CurrentBranch,
            BaseRef: input.BaseRef,
            Scope: input.Scope,
            ChangedFilesSummary: summary,
            ChangedFiles: changedFiles,
            Issues: issues,
            EquipmentDiagnostics: input.EquipmentDiagnosticsReport,
            Commands: input.Commands.OrderBy(command => command.Name, StringComparer.Ordinal).ToArray(),
            NextActions: nextActions);
    }

    private static BranchReadinessChangedFile Classify(BranchReadinessFileInput file)
    {
        var path = NormalizePath(file.Path);
        var (classification, reason) = ClassifyPath(path);

        return new BranchReadinessChangedFile(
            Path: path,
            ChangeType: file.ChangeType,
            IsBranchChange: file.IsBranchChange,
            IsStaged: file.IsStaged,
            IsUnstaged: file.IsUnstaged,
            IsUntracked: file.IsUntracked,
            ScopeClassification: classification,
            ScopeReason: reason);
    }

    private static (BranchReadinessScopeClassification Classification, string Reason) ClassifyPath(string path)
    {
        if (path.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("TestResults/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
        {
            return (
                BranchReadinessScopeClassification.GeneratedIgnoredCandidate,
                "Generated verification/build artifacts must remain ignored and uncommitted.");
        }

        var forbidden = ForbiddenPathFragments.FirstOrDefault(fragment =>
            path.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        if (forbidden is not null)
        {
            return (
                BranchReadinessScopeClassification.Forbidden,
                $"Path matches forbidden EquipmentDiagnostics scope fragment '{forbidden}'.");
        }

        if (AllowedExactPaths.Contains(path, StringComparer.OrdinalIgnoreCase) ||
            AllowedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return (
                BranchReadinessScopeClassification.Allowed,
                "Path is allowed by the EquipmentDiagnostics branch scope policy.");
        }

        return (
            BranchReadinessScopeClassification.Suspicious,
            "Path is outside the explicit EquipmentDiagnostics allow-set and requires review.");
    }

    private static IReadOnlyList<BranchReadinessIssue> ScanUnsafeWording(
        IReadOnlyList<BranchReadinessFileInput> files)
    {
        var issues = new List<BranchReadinessIssue>();

        foreach (var file in files
                     .Where(file => file.Content is not null)
                     .OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var path = NormalizePath(file.Path);
            if (!IsTextPath(path))
            {
                continue;
            }

            var lines = file.Content!.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                foreach (var fragment in UnsafeTextFragments.Where(fragment =>
                             line.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                {
                    if (IsExplicitSafeContext(path, line))
                    {
                        continue;
                    }

                    issues.Add(Error(
                        "UnsafeChangedWording",
                        $"{path}:{index + 1}",
                        $"Changed content contains unsafe diagnostic wording fragment '{fragment}'."));
                }
            }
        }

        return issues;
    }

    private static bool IsExplicitSafeContext(string path, string line)
    {
        if (path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/Application/Verification/", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/entities/equipment-diagnostics/api/equipmentDiagnosticBotClient.ts", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var safeContextFragments = new[]
        {
            "do not",
            "must not",
            "should not",
            "prohibited",
            "forbidden",
            "denylist",
            "unsafe wording",
            "should fail",
            "blocked"
        };

        return safeContextFragments.Any(fragment =>
            line.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTextPath(string path) =>
        new[] { ".cs", ".json", ".md", ".ps1", ".yml", ".yaml", ".txt" }
            .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static BranchReadinessChangedFileSummary BuildSummary(
        IReadOnlyList<BranchReadinessChangedFile> files) =>
        new(
            Total: files.Count,
            BranchChanges: files.Count(file => file.IsBranchChange),
            Staged: files.Count(file => file.IsStaged),
            Unstaged: files.Count(file => file.IsUnstaged),
            Untracked: files.Count(file => file.IsUntracked),
            Added: files.Count(file => file.ChangeType == "Added"),
            Modified: files.Count(file => file.ChangeType == "Modified"),
            Deleted: files.Count(file => file.ChangeType == "Deleted"),
            Allowed: files.Count(file => file.ScopeClassification == BranchReadinessScopeClassification.Allowed),
            Suspicious: files.Count(file => file.ScopeClassification == BranchReadinessScopeClassification.Suspicious),
            Forbidden: files.Count(file => file.ScopeClassification == BranchReadinessScopeClassification.Forbidden),
            GeneratedIgnoredCandidates: files.Count(file =>
                file.ScopeClassification == BranchReadinessScopeClassification.GeneratedIgnoredCandidate));

    private static IReadOnlyList<string> BuildNextActions(
        IReadOnlyList<BranchReadinessIssue> issues,
        IReadOnlyList<BranchReadinessCommandResult> commands,
        EquipmentDiagnosticsVerificationReport equipmentReport)
    {
        var actions = new List<string>();

        if (issues.Any(issue => issue.Code == "ForbiddenChangedPath"))
        {
            actions.Add("Remove forbidden scope changes or move them to a separately reviewed branch.");
        }

        if (issues.Any(issue => issue.Code == "UnsafeChangedWording"))
        {
            actions.Add("Remove unsafe diagnostic wording or document an explicit validation-only safe context.");
        }

        if (commands.Any(command => !command.Passed))
        {
            actions.Add("Fix failed build/test commands and rerun branch readiness verification.");
        }

        if (equipmentReport.HasBlockingIssues)
        {
            actions.Add("Resolve EquipmentDiagnostics runtime, staging, or docs example blockers.");
        }

        if (actions.Count == 0)
        {
            actions.Add("Open or update the reviewed PR and use its diff/checks for final review.");
        }

        return actions.OrderBy(action => action, StringComparer.Ordinal).ToArray();
    }

    private static BranchReadinessIssue Error(string code, string path, string message) =>
        new(code, path, message, EquipmentDiagnosticsVerificationSeverity.Error);

    private static BranchReadinessIssue Warning(string code, string path, string message) =>
        new(code, path, message, EquipmentDiagnosticsVerificationSeverity.Warning);

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
