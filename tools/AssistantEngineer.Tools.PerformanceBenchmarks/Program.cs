using System.Text.Json;

namespace AssistantEngineer.Tools.PerformanceBenchmarks;

internal static class Program
{
    private static readonly IReadOnlyList<ScenarioDescriptor> Scenarios =
    [
        new(
            Key: "small-building",
            Name: "Small Building",
            Description: "Single-building, low-complexity baseline with limited zones/rooms.",
            FocusArea: "End-to-end calculation request overhead and allocation baseline.",
            InputProfileHint: "Representative small project fixture (low zone count)."),
        new(
            Key: "medium-building",
            Name: "Medium Building",
            Description: "Mid-scale project profile with moderate room/envelope complexity.",
            FocusArea: "Throughput and scaling behavior from small to medium input size.",
            InputProfileHint: "Representative medium project fixture."),
        new(
            Key: "large-building",
            Name: "Large Building",
            Description: "High-complexity project profile approaching production-size envelope and zoning.",
            FocusArea: "Peak memory and worst-case latency envelope.",
            InputProfileHint: "Representative large project fixture with many rooms/zones."),
        new(
            Key: "hourly-8760",
            Name: "8760 Hourly Simulation",
            Description: "Annual true-hourly path with 8760 records where configured.",
            FocusArea: "Annual-hourly execution cost and profile-expansion overhead.",
            InputProfileHint: "Fixture with complete annual hourly weather/profile data."),
        new(
            Key: "multi-zone",
            Name: "Multi-Zone Case",
            Description: "Scenario emphasizing adjacent zones and multi-zone interactions.",
            FocusArea: "Solver/scenario orchestration cost with zone-coupled paths.",
            InputProfileHint: "Fixture with multiple conditioned/unconditioned zones."),
        new(
            Key: "report-generation",
            Name: "Report Generation",
            Description: "Engineering report assembly/export from completed scenario data.",
            FocusArea: "Serialization and report-section composition overhead.",
            InputProfileHint: "Scenario result fixture with diagnostics and trace summary."),
        new(
            Key: "workflow-snapshot",
            Name: "Workflow Snapshot Creation",
            Description: "Engineering workflow state/snapshot construction lane.",
            FocusArea: "Snapshot mapper/builder overhead under realistic workflow data.",
            InputProfileHint: "Workflow state fixture with project/building/zones diagnostics.")
    ];

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                PrintHelp();
                return 0;
            }

            var repoRoot = FindRepositoryRoot();
            Directory.SetCurrentDirectory(repoRoot);

            return args[0] switch
            {
                "list-scenarios" => ListScenarios(),
                "run-smoke-baseline" => RunSmokeBaseline(args.Skip(1).ToArray()),
                _ => UnknownCommand(args[0])
            };
        }
        catch (Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(exception.Message);
            Console.ResetColor();
            return 1;
        }
    }

    private static int ListScenarios()
    {
        Console.WriteLine("AssistantEngineer performance benchmark campaign scenarios:");
        foreach (var scenario in Scenarios)
        {
            Console.WriteLine($"- {scenario.Key}: {scenario.Name}");
            Console.WriteLine($"  {scenario.Description}");
        }

        Console.WriteLine();
        Console.WriteLine("These scenario entries are planning skeletons, not performance claims.");
        return 0;
    }

    private static int RunSmokeBaseline(string[] args)
    {
        var outputDirectory = ResolveOutputDirectory(args);
        Directory.CreateDirectory(outputDirectory);

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var machine = Environment.MachineName;
        var runtime = Environment.Version.ToString();

        var baseline = new
        {
            campaignName = "AssistantEngineer Performance Benchmark Skeleton",
            status = "SkeletonOnlyNoMeasurements",
            generatedAtUtc = generatedAtUtc,
            environment = new
            {
                machine,
                os = Environment.OSVersion.ToString(),
                dotnetRuntime = runtime
            },
            scenarios = Scenarios.Select(item => new
            {
                item.Key,
                item.Name,
                item.Description,
                item.FocusArea,
                item.InputProfileHint,
                measurementStatus = "NotExecutedInSkeleton"
            }),
            nonClaims = new[]
            {
                "No performance optimization claim is made by this skeleton output.",
                "No benchmark result claim is made without measured data.",
                "No calculation physics changes are executed by this tool."
            }
        };

        var jsonPath = Path.Combine(outputDirectory, "performance-benchmark-skeleton-baseline.json");
        var markdownPath = Path.Combine(outputDirectory, "performance-benchmark-skeleton-baseline.md");

        var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(jsonPath, json + Environment.NewLine);

        var markdown = BuildMarkdownBaseline(generatedAtUtc, machine, runtime);
        File.WriteAllText(markdownPath, markdown);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Performance benchmark skeleton baseline generated.");
        Console.ResetColor();
        Console.WriteLine($"- {Path.GetRelativePath(Directory.GetCurrentDirectory(), jsonPath)}");
        Console.WriteLine($"- {Path.GetRelativePath(Directory.GetCurrentDirectory(), markdownPath)}");

        return 0;
    }

    private static string ResolveOutputDirectory(string[] args)
    {
        const string defaultPath = ".\\artifacts\\performance\\benchmark-campaign";

        if (args.Length == 0)
            return Path.GetFullPath(defaultPath);

        if (args.Length == 2 && string.Equals(args[0], "--output-directory", StringComparison.OrdinalIgnoreCase))
            return Path.GetFullPath(args[1]);

        throw new InvalidOperationException("run-smoke-baseline supports only optional '--output-directory <path>' argument.");
    }

    private static string BuildMarkdownBaseline(DateTimeOffset generatedAtUtc, string machine, string runtime)
    {
        var lines = new List<string>
        {
            "# Performance Benchmark Skeleton Baseline",
            string.Empty,
            $"Generated (UTC): {generatedAtUtc:O}",
            $"Machine: {machine}",
            $".NET runtime: {runtime}",
            string.Empty,
            "## Status",
            "- Skeleton only.",
            "- No measurements collected.",
            "- No performance claims.",
            string.Empty,
            "## Planned Scenarios"
        };

        lines.AddRange(Scenarios.Select(item => $"- `{item.Key}`: {item.Name} - {item.FocusArea}"));

        lines.Add(string.Empty);
        lines.Add("## Non-claims");
        lines.Add("- No optimization claim is made by this output.");
        lines.Add("- No fake benchmark claim is made without measured data.");
        lines.Add("- Calculation behavior/physics is untouched.");

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.Error.WriteLine();
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer performance benchmark campaign tool");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  list-scenarios");
        Console.WriteLine("  run-smoke-baseline [--output-directory <path>]");
        Console.WriteLine();
        Console.WriteLine("This tool is opt-in and not part of normal CI test flow.");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    private sealed record ScenarioDescriptor(
        string Key,
        string Name,
        string Description,
        string FocusArea,
        string InputProfileHint);
}
