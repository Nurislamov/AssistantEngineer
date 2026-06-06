using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
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

            var input = EquipmentDiagnosticsVerificationInputLoader.Load(repoRoot);
            var report = new EquipmentDiagnosticsVerificationService().Verify(input);
            var selectedReport = SelectCommandReport(report, options.Command);

            Console.WriteLine(JsonSerializer.Serialize(selectedReport, ReportJsonOptions));
            return selectedReport.HasBlockingIssues ? 1 : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int VerifyBranch(
        string repoRoot,
        EquipmentDiagnosticsVerificationOptions options)
    {
        var (currentBranch, files) = BranchReadinessGitCollector.Collect(repoRoot, options.BaseRef);
        var equipmentInput = EquipmentDiagnosticsVerificationInputLoader.Load(repoRoot);
        var equipmentReport = new EquipmentDiagnosticsVerificationService().Verify(equipmentInput);
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
        Console.WriteLine("  verify-branch");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repo-root <path>");
        Console.WriteLine("  --base-ref <git-ref>              Default: origin/master");
        Console.WriteLine("  --scope <scope>                    Default: EquipmentDiagnostics");
        Console.WriteLine("  --output-directory <path>");
        Console.WriteLine("  --skip-command-checks              Skip restore/build/test (for focused development only)");
    }
}
