using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class Program
{
    private static readonly JsonSerializerOptions ReportJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions InputJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static int Main(string[] args)
    {
        try
        {
            var options = EquipmentDiagnosticsVerificationOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            var repoRoot = ResolveRepositoryRoot(options.RepositoryRoot);
            if (options.Command == "verify-branch")
            {
                return VerifyBranch(repoRoot, options);
            }

            if (options.Command == "prepare-pr-body")
            {
                var outputPath = BranchReadinessPrBodyWriter.Write(repoRoot, options.ReportPath, options.OutputPath);
                Console.WriteLine("PASS");
                Console.WriteLine($"PR body: {Path.GetRelativePath(repoRoot, outputPath).Replace('\\', '/')}");
                return 0;
            }

            if (options.Command == "beta-readiness")
            {
                return GenerateBetaReadiness(repoRoot, options);
            }

            if (options.Command == "goal-run-report")
            {
                return ValidateGoalRunReport(repoRoot, options);
            }

            if (options.Command == "verify-knowledge")
            {
                return VerifyErrorKnowledge(repoRoot);
            }

            var input = EquipmentDiagnosticsVerificationInputLoader.Load(repoRoot);
            var report = new EquipmentDiagnosticsVerificationService().Verify(input);
            if (options.Command == "codebook-coverage")
            {
                var paths = CodebookCoverageReportWriter.Write(repoRoot, report.CodebookCoverage);
                StagingPreviewReportWriter.Write(repoRoot, report.StagingPreview);
                PrintCoverageSummary(repoRoot, paths.JsonPath, report.CodebookCoverage);
                return report.CodebookCoverage.Passed ? 0 : 1;
            }
            if (options.Command == "preview-staging-candidates")
            {
                var paths = StagingPreviewReportWriter.Write(repoRoot, report.StagingPreview);
                Console.WriteLine("PASS");
                Console.WriteLine($"Staging preview: {Path.GetRelativePath(repoRoot, paths.JsonPath).Replace('\\', '/')}");
                Console.WriteLine($"Ready for staging candidates: {report.StagingPreview.CandidateCount}");
                return 0;
            }
            var selectedReport = SelectCommandReport(report, options.Command);

            Console.WriteLine(JsonSerializer.Serialize(selectedReport, ReportJsonOptions));
            return selectedReport.HasBlockingIssues ? 1 : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(FormatExpectedError(exception));
            return 1;
        }
    }

    private static int ValidateGoalRunReport(
        string repoRoot,
        EquipmentDiagnosticsVerificationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.InputPath))
        {
            throw new InvalidOperationException("goal-run-report requires --input <path>.");
        }

        var inputPath = Path.GetFullPath(options.InputPath, repoRoot);
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Goal-run report input was not found.", inputPath);
        }

        var report = JsonSerializer.Deserialize<AssistantEngineerGoalRunReport>(
            File.ReadAllText(inputPath),
            InputJsonOptions) ?? throw new InvalidOperationException("Goal-run report JSON contains no report.");
        var result = new AssistantEngineerGoalRunReportValidator().Validate(report);
        Console.WriteLine(result.Status);
        Console.WriteLine($"Blockers: {result.Blockers.Count}");
        Console.WriteLine($"Warnings: {result.Warnings.Count}");
        foreach (var blocker in result.Blockers)
        {
            Console.WriteLine($"Blocker: {blocker}");
        }

        return result.IsReady ? 0 : 1;
    }

    private static int VerifyErrorKnowledge(string repoRoot)
    {
        var directory = Path.Combine(
            repoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge");
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Error knowledge directory was not found: {directory}");
        }

        var sources = Directory
            .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new ErrorKnowledgeJsonSource(
                Path.GetRelativePath(repoRoot, path).Replace('\\', '/'),
                File.ReadAllText(path)))
            .ToArray();
        var result = new ErrorKnowledgeJsonValidator().Validate(sources);

        Console.WriteLine(result.IsValid ? "PASS" : "FAIL");
        Console.WriteLine($"Files: {sources.Length}; entries: {result.Entries.Count}; issues: {result.Issues.Count}");
        foreach (var issue in result.Issues)
        {
            Console.WriteLine($"{issue.Path}: {issue.Problem}");
        }

        return result.IsValid ? 0 : 1;
    }

    private static int GenerateBetaReadiness(
        string repoRoot,
        EquipmentDiagnosticsVerificationOptions options)
    {
        var (branch, _) = BranchReadinessGitCollector.Collect(repoRoot, options.BaseRef);
        var report = new EquipmentDiagnosticsBetaReadinessReportGenerator().Generate(
            new EquipmentDiagnosticsBetaReadinessInput(
                RepositoryRoot: repoRoot,
                RepositoryBaseRef: options.BaseRef,
                Branch: branch,
                Head: BranchReadinessGitCollector.GetHead(repoRoot),
                BranchReadinessReportPath: options.ReportPath));
        var paths = BetaReadinessReportWriter.Write(repoRoot, options.OutputPath, options.MarkdownOutputPath, report);
        Console.WriteLine($"Overall status: {report.OverallStatus}");
        Console.WriteLine($"Blockers: {report.BlockerCount}");
        Console.WriteLine($"Warnings: {report.WarningCount}");
        Console.WriteLine($"Beta report: {Path.GetRelativePath(repoRoot, paths.JsonPath).Replace('\\', '/')}");
        Console.WriteLine($"Beta summary: {Path.GetRelativePath(repoRoot, paths.MarkdownPath).Replace('\\', '/')}");
        return report.BlockerCount == 0 ? 0 : 1;
    }

    private static string FormatExpectedError(Exception exception)
    {
        var root = exception;
        while (root.InnerException is not null)
        {
            root = root.InnerException;
        }

        return root switch
        {
            FileNotFoundException missingFile =>
                $"Required file was not found: {missingFile.FileName ?? missingFile.Message}",
            DirectoryNotFoundException missingDirectory =>
                $"Required directory was not found: {missingDirectory.Message}",
            _ => root.Message
        };
    }

    private static int VerifyBranch(
        string repoRoot,
        EquipmentDiagnosticsVerificationOptions options)
    {
        var (currentBranch, files) = BranchReadinessGitCollector.Collect(repoRoot, options.BaseRef);
        var equipmentInput = EquipmentDiagnosticsVerificationInputLoader.Load(repoRoot);
        var equipmentReport = new EquipmentDiagnosticsVerificationService().Verify(equipmentInput);
        CodebookCoverageReportWriter.Write(repoRoot, equipmentReport.CodebookCoverage);
        StagingPreviewReportWriter.Write(repoRoot, equipmentReport.StagingPreview);
        var commands = options.SkipCommandChecks
            ? Array.Empty<BranchReadinessCommandResult>()
            : BranchReadinessCommandRunner.RunRequiredChecks(repoRoot);
        var report = new BranchReadinessVerificationService().Verify(new BranchReadinessInput(
            CurrentBranch: currentBranch,
            BaseRef: options.BaseRef,
            Scope: options.Scope,
            Files: files,
            EquipmentDiagnosticsReport: equipmentReport,
            Commands: commands));
        var paths = BranchReadinessReportWriter.Write(repoRoot, options.OutputDirectory, report);

        Console.WriteLine(report.Passed ? "PASS" : "FAIL");
        Console.WriteLine($"Report: {Path.GetRelativePath(repoRoot, paths.JsonPath).Replace('\\', '/')}");
        Console.WriteLine($"Blockers: {report.BlockersCount}; warnings: {report.WarningsCount}; changed files: {report.ChangedFilesSummary.Total}");
        foreach (var command in report.Commands.Where(command => !command.Passed))
        {
            Console.WriteLine($"Failed command: {command.Command}");
        }

        return report.Passed ? 0 : 1;
    }

    private static void PrintCoverageSummary(
        string repoRoot,
        string jsonPath,
        EquipmentDiagnosticsCodebookCoverageReport report)
    {
        Console.WriteLine(report.Passed ? "PASS" : "FAIL");
        Console.WriteLine($"Blockers: {report.BlockerCount}; warnings: {report.WarningCount}");
        Console.WriteLine($"Coverage report: {Path.GetRelativePath(repoRoot, jsonPath).Replace('\\', '/')}");
        Console.WriteLine($"Ready for staging candidates: {report.Summary.ReadyForStagingCandidateCount}");
        Console.WriteLine($"Conflicts: {report.Summary.ConflictCount}");
    }

    private static EquipmentDiagnosticsVerificationReport SelectCommandReport(
        EquipmentDiagnosticsVerificationReport report,
        string command)
    {
        var sectionNames = command switch
        {
            "validate-staging" => new[] { "staging-candidates", "staging-examples" },
            "validate-runtime-catalog" => new[] { "runtime-catalog" },
            "validate-doc-examples" => new[] { "docs-examples" },
            "full-report" => report.Sections.Select(section => section.Name).ToArray(),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'. Use --help for available commands.")
        };
        var sections = report.Sections
            .Where(section => sectionNames.Contains(section.Name, StringComparer.Ordinal))
            .ToArray();
        var hasBlockingIssues = sections
            .Where(section => section.Name != "staging-examples")
            .Any(section => section.HasBlockingIssues);

        return report with
        {
            Sections = sections,
            IsReleaseReady = !hasBlockingIssues,
            HasBlockingIssues = hasBlockingIssues
        };
    }

    private static string ResolveRepositoryRoot(string? explicitRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            return Path.GetFullPath(explicitRoot);
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer EquipmentDiagnostics verification");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  validate-staging");
        Console.WriteLine("  validate-runtime-catalog");
        Console.WriteLine("  validate-doc-examples");
        Console.WriteLine("  full-report");
        Console.WriteLine("  codebook-coverage");
        Console.WriteLine("  preview-staging-candidates");
        Console.WriteLine("  verify-branch");
        Console.WriteLine("  prepare-pr-body");
        Console.WriteLine("  beta-readiness");
        Console.WriteLine("  goal-run-report");
        Console.WriteLine("  verify-knowledge");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repo-root <path>");
        Console.WriteLine("  --base-ref <git-ref>              Default: origin/master");
        Console.WriteLine("  --scope <scope>                    Default: EquipmentDiagnostics");
        Console.WriteLine("  --output-directory <path>");
        Console.WriteLine("  --report <path>                    Branch readiness JSON report");
        Console.WriteLine("  --output <path>                    Generated PR body Markdown");
        Console.WriteLine("  --markdown-output <path>           Generated beta readiness Markdown summary");
        Console.WriteLine("  --input <path>                     Goal-run report JSON input");
        Console.WriteLine("  --skip-command-checks              Skip restore/build/test (for focused development only)");
    }
}
