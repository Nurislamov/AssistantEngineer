namespace AssistantEngineer.Tools.EngineeringCoreEvidence;

internal static class Program
{
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

            var command = args[0];

            return command switch
            {
                "generate-release-evidence" => EngineeringCoreEvidenceToolRunner.GenerateReleaseEvidence(ReadOption(args, "--output-path") ?? "docs/reports/EngineeringCoreV1ReleaseEvidence.md"),
                "generate-export-disclosure-checklist" => EngineeringCoreEvidenceToolRunner.GenerateExportDisclosureChecklist(ReadOption(args, "--output-path") ?? "docs/reports/engineering-core-v1/ExportDisclosureChecklist.md"),
                "generate-traceability-matrix" => EngineeringCoreEvidenceToolRunner.GenerateTraceabilityMatrix(ReadOption(args, "--output-directory") ?? "docs/traceability"),
                "generate-all-evidence" => EngineeringCoreEvidenceToolRunner.GenerateAllEvidence(),
                _ => Unknown(command)
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

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer Engineering Core evidence tools");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  generate-release-evidence [--output-path <path>]");
        Console.WriteLine("  generate-export-disclosure-checklist [--output-path <path>]");
        Console.WriteLine("  generate-traceability-matrix [--output-directory <path>]");
        Console.WriteLine("  generate-all-evidence");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static string? ReadOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                return args[index + 1];
        }

        return null;
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
}
