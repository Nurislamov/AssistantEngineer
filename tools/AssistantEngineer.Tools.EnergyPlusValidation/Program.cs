using System.Globalization;

namespace AssistantEngineer.Tools.EnergyPlusValidation;

internal static class Program
{
    public static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

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
            var toolArgs = args.Skip(1).ToArray();

            return command switch
            {
                "compare-fixtures" => EnergyPlusValidationToolRunner.CompareFixtures(HasSwitch(toolArgs, "require-real-references")),
                "assert-smoke001-real-fixture-ready" => EnergyPlusValidationToolRunner.AssertSmoke001RealFixtureReady(HasSwitch(toolArgs, "require-real-fixture")),
                "generate-fixture-catalog" => EnergyPlusValidationToolRunner.GenerateFixtureCatalog(),
                "generate-comparison-summary" => EnergyPlusValidationToolRunner.GenerateComparisonSummary(),
                "generate-validation-readiness" => EnergyPlusValidationToolRunner.GenerateValidationReadiness(),
                "generate-smoke001-comparison-readiness" => EnergyPlusValidationToolRunner.GenerateSmoke001ComparisonReadiness(),
                "generate-validation-evidence" => EnergyPlusValidationToolRunner.GenerateValidationEvidence(),
                "regenerate-validation-artifacts" => EnergyPlusValidationToolRunner.RegenerateValidationArtifacts(),
                "verify-validation" => EnergyPlusValidationToolRunner.VerifyValidation(),
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
        Console.WriteLine("AssistantEngineer EnergyPlus validation tools");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  compare-fixtures [--require-real-references]");
        Console.WriteLine("  assert-smoke001-real-fixture-ready [--require-real-fixture]");
        Console.WriteLine("  generate-fixture-catalog");
        Console.WriteLine("  generate-comparison-summary");
        Console.WriteLine("  generate-validation-readiness");
        Console.WriteLine("  generate-smoke001-comparison-readiness");
        Console.WriteLine("  generate-validation-evidence");
        Console.WriteLine("  regenerate-validation-artifacts");
        Console.WriteLine("  verify-validation");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static bool HasSwitch(IReadOnlyCollection<string> args, string name)
    {
        var normalized = name.TrimStart('-');

        return args.Any(arg =>
            string.Equals(arg.TrimStart('-'), normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg.TrimStart('-'), ToPowerShellSwitchName(normalized), StringComparison.OrdinalIgnoreCase));
    }

    private static string ToPowerShellSwitchName(string kebab)
    {
        var builder = new System.Text.StringBuilder();
        var nextUpper = true;

        foreach (var character in kebab)
        {
            if (character == '-')
            {
                nextUpper = true;
                continue;
            }

            builder.Append(nextUpper ? char.ToUpperInvariant(character) : character);
            nextUpper = false;
        }

        return builder.ToString();
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
