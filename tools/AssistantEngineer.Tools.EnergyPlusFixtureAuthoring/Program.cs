using System.Text;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tools.EnergyPlusFixtureAuthoring;

internal static partial class Program
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
                "new-fixture" => NewFixture(FixtureAuthoringOptions.Parse(args.Skip(1).ToArray())),
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
        Console.WriteLine("AssistantEngineer EnergyPlus fixture authoring tool");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  new-fixture --case-id <EP-SMOKE-004> --name <name> [--stage Smoke] [--purpose <purpose>] [--weather-source <source>] [--force]");
        Console.WriteLine();
        Console.WriteLine("PowerShell wrapper:");
        Console.WriteLine("  .\\scripts\\engineering-core\\new-energyplus-validation-fixture.ps1 -CaseId EP-SMOKE-004 -Name \"Fixture name\"");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static int NewFixture(FixtureAuthoringOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CaseId))
            throw new ArgumentException("CaseId is required. Use --case-id EP-SMOKE-004.");

        if (string.IsNullOrWhiteSpace(options.Name))
            throw new ArgumentException("Name is required. Use --name \"Fixture name\".");

        if (!CaseIdPattern().IsMatch(options.CaseId))
            throw new ArgumentException("CaseId must use uppercase letters, digits and hyphens only. Example: EP-SMOKE-004.");

        var templateDirectory = "docs/validation/fixtures/_template";
        var fixtureDirectory = Path.Combine("tests/fixtures/validation/energyplus", options.CaseId);
        var docsDirectory = Path.Combine("docs/validation/fixtures", options.CaseId);

        EnsureDirectory(templateDirectory, "Template directory");

        if (Directory.Exists(fixtureDirectory) && !options.Force)
            throw new InvalidOperationException($"Fixture directory already exists: {fixtureDirectory}. Use --force to overwrite.");

        Directory.CreateDirectory(fixtureDirectory);
        Directory.CreateDirectory(docsDirectory);

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CASE_ID"] = options.CaseId,
            ["CASE_NAME"] = options.Name,
            ["STAGE"] = options.Stage,
            ["PURPOSE"] = options.Purpose,
            ["WEATHER_SOURCE"] = options.WeatherSource,
            ["GEOMETRY_DESCRIPTION"] = "Describe the fixture geometry.",
            ["ENVELOPE_DESCRIPTION"] = "Describe the fixture envelope.",
            ["WEATHER_PROFILE"] = "synthetic",
            ["INTERNAL_GAINS_DESCRIPTION"] = "Describe internal gains.",
            ["VENTILATION_DESCRIPTION"] = "Describe ventilation and infiltration.",
            ["HVAC_CONTROL_DESCRIPTION"] = "Describe ideal loads control.",
            ["EXPECTED_BEHAVIOR_1"] = "Describe expected engineering behavior.",
            ["EXPECTED_BEHAVIOR_2"] = "Describe expected directional response.",
            ["EXPECTED_BEHAVIOR_3"] = "Describe expected non-claim boundary.",
            ["CALCULATION_SCOPE"] = options.Name,
            ["PRIMARY_METRIC_FORMULA"] = "Describe primary formula.",
            ["ENERGYPLUS_VERSION"] = "TODO",
            ["OPERATING_SYSTEM"] = "TODO",
            ["RUN_DATE_UTC"] = "TODO",
            ["OUTPUT_VARIABLE_1"] = "TODO",
            ["OUTPUT_VARIABLE_2"] = "TODO",
            ["UNIT_CONVERSION_1"] = "TODO"
        };

        ExpandTemplate(
            Path.Combine(templateDirectory, "case-metadata.template.json"),
            Path.Combine(fixtureDirectory, "case-metadata.json"),
            tokens,
            options.Force);

        ExpandTemplate(
            Path.Combine(templateDirectory, "assistantengineer-input.template.json"),
            Path.Combine(fixtureDirectory, "assistantengineer-input.json"),
            tokens,
            options.Force);

        ExpandTemplate(
            Path.Combine(templateDirectory, "reference-output.placeholder.template.json"),
            Path.Combine(fixtureDirectory, "reference-output.placeholder.json"),
            tokens,
            options.Force);

        ExpandTemplate(
            Path.Combine(templateDirectory, "comparison-tolerances.template.json"),
            Path.Combine(fixtureDirectory, "comparison-tolerances.json"),
            tokens,
            options.Force);

        ExpandTemplate(
            Path.Combine(templateDirectory, "README.template.md"),
            Path.Combine(docsDirectory, "README.md"),
            tokens,
            options.Force);

        WriteSuccess("Validation fixture scaffold created:");
        Console.WriteLine($"- {fixtureDirectory.Replace("\\", "/")}");
        Console.WriteLine($"- {docsDirectory.Replace("\\", "/")}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Edit generated JSON values and tolerances.");
        Console.WriteLine("2. Add the case to docs/validation/EnergyPlusValidationCaseRegistry.json.");
        Console.WriteLine("3. Run .\\scripts\\engineering-core\\compare-energyplus-validation-fixtures.ps1");
        Console.WriteLine("4. Run .\\scripts\\engineering-core\\generate-energyplus-validation-fixture-catalog.ps1");

        return 0;
    }

    private static void ExpandTemplate(
        string templatePath,
        string destinationPath,
        IReadOnlyDictionary<string, string> tokens,
        bool force)
    {
        EnsureFile(templatePath, "Template file");

        if (File.Exists(destinationPath) && !force)
            throw new InvalidOperationException($"Destination already exists: {destinationPath}. Use --force to overwrite.");

        var content = File.ReadAllText(templatePath);

        foreach (var token in tokens)
            content = content.Replace("{{" + token.Key + "}}", token.Value, StringComparison.Ordinal);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.WriteAllText(destinationPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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

    private static void EnsureFile(string path, string description)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{description} not found: {path}", path);
    }

    private static void EnsureDirectory(string path, string description)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"{description} not found: {path}");
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    [GeneratedRegex("^[A-Z0-9]+(-[A-Z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex CaseIdPattern();

    private sealed record FixtureAuthoringOptions(
        string CaseId,
        string Name,
        string Stage,
        string Purpose,
        string WeatherSource,
        bool Force)
    {
        public static FixtureAuthoringOptions Parse(IReadOnlyList<string> args)
        {
            return new FixtureAuthoringOptions(
                CaseId: ReadRequired(args, "--case-id", "-CaseId"),
                Name: ReadRequired(args, "--name", "-Name"),
                Stage: ReadOptional(args, "Smoke", "--stage", "-Stage"),
                Purpose: ReadOptional(args, "Prepare comparative validation fixture structure", "--purpose", "-Purpose"),
                WeatherSource: ReadOptional(args, "Synthetic weather fixture.", "--weather-source", "-WeatherSource"),
                Force: Has(args, "--force") || Has(args, "-Force"));
        }

        private static string ReadRequired(IReadOnlyList<string> args, params string[] names)
        {
            var value = ReadOptional(args, "", names);

            if (string.IsNullOrWhiteSpace(value))
                return "";

            return value;
        }

        private static string ReadOptional(IReadOnlyList<string> args, string fallback, params string[] names)
        {
            for (var index = 0; index < args.Count - 1; index++)
            {
                if (names.Any(name => string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase)))
                    return args[index + 1];
            }

            return fallback;
        }

        private static bool Has(IReadOnlyList<string> args, string name) =>
            args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }
}
