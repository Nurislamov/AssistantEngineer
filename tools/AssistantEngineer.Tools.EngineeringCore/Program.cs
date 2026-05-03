using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.EngineeringCore;

internal static class Program
{
    private const string StableGeneratedAtUtc = "2026-01-01 00:00:00 UTC";

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
                "generate-calculation-module-inventory" => GenerateCalculationModuleInventory(repoRoot),
                "verify-calculation-module-deepening" => VerifyCalculationModuleDeepening(repoRoot),
                "verify-calculation-module-balance-invariants" => VerifyCalculationModuleBalanceInvariants(repoRoot),
                "verify-calculation-module-diagnostics-consistency" => VerifyCalculationModuleDiagnosticsConsistency(repoRoot),
                "verify-calculation-module-deepening-all" => VerifyCalculationModuleDeepeningAll(repoRoot),
                _ => UnknownCommand(command)
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
        Console.WriteLine("AssistantEngineer Engineering Core tools");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  generate-calculation-module-inventory");
        Console.WriteLine("  verify-calculation-module-deepening");
        Console.WriteLine("  verify-calculation-module-balance-invariants");
        Console.WriteLine("  verify-calculation-module-diagnostics-consistency");
        Console.WriteLine("  verify-calculation-module-deepening-all");
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.Error.WriteLine();
        PrintHelp();
        return 1;
    }

    private static int GenerateCalculationModuleInventory(string repoRoot)
    {
        var sourceRoot = "src/Backend/AssistantEngineer.Modules.Calculations";
        var testsRoot = "tests/AssistantEngineer.Tests";
        var outputJsonPath = "docs/reports/calculations/CalculationModuleInventory.json";
        var outputMarkdownPath = "docs/reports/calculations/CalculationModuleInventory.md";

        EnsureDirectoryExists(sourceRoot, "Calculations module source root");
        EnsureDirectoryExists(testsRoot, "Tests root");

        var serviceFiles = EnumerateFiles(Path.Combine(sourceRoot, "Application", "Services"), "*.cs");
        var contractFiles = EnumerateFiles(Path.Combine(sourceRoot, "Application", "Contracts"), "*.cs");
        var abstractionFiles = EnumerateFiles(Path.Combine(sourceRoot, "Application", "Abstractions"), "*.cs");
        var calculationTests = EnumerateFiles(Path.Combine(testsRoot, "Calculations"), "*.cs");
        var parityTests = EnumerateFiles(Path.Combine(testsRoot, "Parity", "EnergyCalculationParity"), "*.cs");

        var keyEngines = new Dictionary<string, KeyEngine>(StringComparer.Ordinal)
        {
            ["RoomLoadCalculationEngine"] = NewKeyEngine(
                "RoomLoadCalculationEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/RoomLoads/RoomLoadCalculationEngine.cs",
                "Room load orchestration",
                "Combines room-level heating/cooling load components."),

            ["LoadAggregationEngine"] = NewKeyEngine(
                "LoadAggregationEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Aggregation/LoadAggregationEngine.cs",
                "Aggregation",
                "Aggregates room/floor/building load results."),

            ["AnnualEnergyBalanceEngine"] = NewKeyEngine(
                "AnnualEnergyBalanceEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/AnnualEnergy/AnnualEnergyBalanceEngine.cs",
                "Annual energy",
                "Calculates annual energy balance from hourly/monthly inputs."),

            ["HourlySimulationToAnnualEnergyInputMapper"] = NewKeyEngine(
                "HourlySimulationToAnnualEnergyInputMapper",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/AnnualEnergy/HourlySimulationToAnnualEnergyInputMapper.cs",
                "Annual energy input mapping",
                "Maps hourly simulation records into annual energy input."),

            ["SystemEnergyEngine"] = NewKeyEngine(
                "SystemEnergyEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SystemEnergy/SystemEnergyEngine.cs",
                "System energy",
                "Keeps useful, final and primary energy distinct."),

            ["EquipmentSizingEngine"] = NewKeyEngine(
                "EquipmentSizingEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/EquipmentSizing/EquipmentSizingEngine.cs",
                "Equipment sizing",
                "Applies capacity margin/sizing rules."),

            ["TransmissionHeatTransferEngine"] = NewKeyEngine(
                "TransmissionHeatTransferEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Transmission/TransmissionHeatTransferEngine.cs",
                "Envelope transmission",
                "Calculates transmission heat transfer."),

            ["VentilationAndInfiltrationLoadEngine"] = NewKeyEngine(
                "VentilationAndInfiltrationLoadEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/VentilationAndInfiltrationLoadEngine.cs",
                "Ventilation",
                "Calculates sensible ventilation/infiltration loads."),

            ["InternalGainEngine"] = NewKeyEngine(
                "InternalGainEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/InternalGains/InternalGainEngine.cs",
                "Internal gains",
                "Calculates sensible internal gain components."),

            ["WindowSolarGainEngine"] = NewKeyEngine(
                "WindowSolarGainEngine",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SolarGains/WindowSolarGainEngine.cs",
                "Window solar gains",
                "Calculates simplified SHGC/window solar gains."),

            ["AnnualWeatherSolarProfileBuilder"] = NewKeyEngine(
                "AnnualWeatherSolarProfileBuilder",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/WeatherSolar/AnnualWeatherSolarProfileBuilder.cs",
                "Weather/solar profile",
                "Builds annual solar/weather profile context."),

            ["EnergyCalculationPipelineService"] = NewKeyEngine(
                "EnergyCalculationPipelineService",
                "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Pipeline/EnergyCalculationPipelineService.cs",
                "Application pipeline",
                "Coordinates the real application calculation path.")
        };

        var missingKeyEngines = keyEngines
            .Where(pair => !pair.Value.Exists)
            .Select(pair => pair.Key)
            .ToArray();

        var deepeningAxes = new[]
        {
            "Input normalization and units policy.",
            "Scenario fixtures for room, floor, building and annual-energy paths.",
            "Diagnostics consistency across all calculation engines.",
            "Cross-engine balance invariants: component sum, aggregation sum, useful/final/primary energy separation.",
            "Method strategy isolation: simplified, ISO-inspired and future external validation paths must stay explicit.",
            "No silent fallback: simplifications and adapters must be visible as diagnostics."
        };

        var requiredNonClaims = new[]
        {
            "Does not claim exact EnergyPlus numerical parity.",
            "Does not claim ASHRAE 140 validation coverage.",
            "Does not claim full ISO 52016 node/matrix solver parity.",
            "Does not claim full ISO 13370 implementation.",
            "Does not claim full EN 15316 system-chain implementation."
        };

        var inventory = new Dictionary<string, object?>
        {
            ["inventoryName"] = "Calculation Module Deepening Inventory",
            ["version"] = "v1",
            ["status"] = "DeepeningBaseline",
            ["generatedAtUtc"] = StableGeneratedAtUtc,
            ["sourceRoot"] = sourceRoot,
            ["testsRoot"] = testsRoot,
            ["totals"] = new Dictionary<string, object?>
            {
                ["serviceFiles"] = serviceFiles.Length,
                ["contractFiles"] = contractFiles.Length,
                ["abstractionFiles"] = abstractionFiles.Length,
                ["calculationTests"] = calculationTests.Length,
                ["parityTests"] = parityTests.Length,
                ["keyEngines"] = keyEngines.Count,
                ["missingKeyEngines"] = missingKeyEngines.Length
            },
            ["keyEngines"] = keyEngines,
            ["missingKeyEngines"] = missingKeyEngines,
            ["requiredDocuments"] = new Dictionary<string, object?>
            {
                ["CalculationModuleDeepeningPlan"] = new Dictionary<string, object?>
                {
                    ["path"] = "docs/calculations/CalculationModuleDeepeningPlan.md",
                    ["exists"] = File.Exists("docs/calculations/CalculationModuleDeepeningPlan.md")
                },
                ["CalculationModuleBoundaryPolicy"] = new Dictionary<string, object?>
                {
                    ["path"] = "docs/calculations/CalculationModuleBoundaryPolicy.md",
                    ["exists"] = File.Exists("docs/calculations/CalculationModuleBoundaryPolicy.md")
                }
            },
            ["deepeningAxes"] = deepeningAxes,
            ["requiredNonClaims"] = requiredNonClaims
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(outputMarkdownPath)!);

        var json = JsonSerializer.Serialize(
            inventory,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        File.WriteAllText(outputJsonPath, json + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var markdown = BuildCalculationModuleInventoryMarkdown(
            sourceRoot,
            testsRoot,
            serviceFiles.Length,
            contractFiles.Length,
            abstractionFiles.Length,
            calculationTests.Length,
            parityTests.Length,
            keyEngines,
            missingKeyEngines,
            deepeningAxes,
            requiredNonClaims);

        File.WriteAllText(outputMarkdownPath, markdown, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        WriteSuccess("Calculation module inventory generated:");
        Console.WriteLine($"- {outputJsonPath}");
        Console.WriteLine($"- {outputMarkdownPath}");
        Console.WriteLine($"Key engines: {keyEngines.Count}");
        Console.WriteLine($"Missing key engines: {missingKeyEngines.Length}");

        return missingKeyEngines.Length == 0 ? 0 : 1;
    }

    private static int VerifyCalculationModuleDeepening(string repoRoot)
    {
        WriteStep("Generate calculation module inventory");
        var generateCode = GenerateCalculationModuleInventory(repoRoot);
        if (generateCode != 0)
            return generateCode;

        WriteStep("Run calculation module deepening guard tests");
        return RunDotnetTest("CalculationModuleDeepeningGuardTests");
    }

    private static int VerifyCalculationModuleBalanceInvariants(string repoRoot)
    {
        EnsureFileExists(
            "docs/calculations/CalculationModuleBalanceInvariants.md",
            "Required balance invariant document");

        WriteStep("Run calculation module balance invariant tests");
        return RunDotnetTest("CalculationModuleBalanceInvariantTests");
    }

    private static int VerifyCalculationModuleDiagnosticsConsistency(string repoRoot)
    {
        EnsureFileExists(
            "docs/calculations/CalculationModuleDiagnosticsConsistency.md",
            "Required diagnostics consistency document");

        WriteStep("Run calculation module diagnostics consistency tests");
        return RunDotnetTest("CalculationModuleDiagnosticsConsistencyTests");
    }

    private static int VerifyCalculationModuleDeepeningAll(string repoRoot)
    {
        var steps = new Func<string, int>[]
        {
            VerifyCalculationModuleDeepening,
            VerifyCalculationModuleBalanceInvariants,
            VerifyCalculationModuleDiagnosticsConsistency
        };

        foreach (var step in steps)
        {
            var code = step(repoRoot);
            if (code != 0)
                return code;
        }

        WriteSuccess("Calculation module deepening verification completed successfully.");
        return 0;
    }

    private static int RunDotnetTest(string filter)
    {
        var arguments = $"test .\\AssistantEngineer.sln --filter \"{filter}\"";
        return RunProcess("dotnet", arguments);
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                Console.WriteLine(args.Data);
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                Console.Error.WriteLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return process.ExitCode;
    }

    private static string BuildCalculationModuleInventoryMarkdown(
        string sourceRoot,
        string testsRoot,
        int serviceFileCount,
        int contractFileCount,
        int abstractionFileCount,
        int calculationTestCount,
        int parityTestCount,
        IReadOnlyDictionary<string, KeyEngine> keyEngines,
        IReadOnlyCollection<string> missingKeyEngines,
        IReadOnlyCollection<string> deepeningAxes,
        IReadOnlyCollection<string> requiredNonClaims)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Calculation Module Deepening Inventory");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Inventory | Calculation Module Deepening Inventory |");
        builder.AppendLine("| Version | v1 |");
        builder.AppendLine("| Status | DeepeningBaseline |");
        builder.AppendLine($"| Source root | {sourceRoot} |");
        builder.AppendLine($"| Tests root | {testsRoot} |");
        builder.AppendLine($"| Service files | {serviceFileCount} |");
        builder.AppendLine($"| Contract files | {contractFileCount} |");
        builder.AppendLine($"| Abstraction files | {abstractionFileCount} |");
        builder.AppendLine($"| Calculation tests | {calculationTestCount} |");
        builder.AppendLine($"| Parity tests | {parityTestCount} |");
        builder.AppendLine($"| Key engines | {keyEngines.Count} |");
        builder.AppendLine($"| Missing key engines | {missingKeyEngines.Count} |");
        builder.AppendLine();
        builder.AppendLine("## Key engines");
        builder.AppendLine();
        builder.AppendLine("| Engine | Layer | Exists | Path |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var engine in keyEngines.Values)
        {
            builder.AppendLine($"| {engine.Name} | {engine.Layer} | {engine.Exists} | `{engine.Path}` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Missing key engines");
        builder.AppendLine();

        if (missingKeyEngines.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var missing in missingKeyEngines)
                builder.AppendLine($"- {missing}");
        }

        builder.AppendLine();
        builder.AppendLine("## Deepening axes");
        builder.AppendLine();

        foreach (var axis in deepeningAxes)
            builder.AppendLine($"- {axis}");

        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();

        foreach (var nonClaim in requiredNonClaims)
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("This inventory is a calculation-module deepening baseline.");
        builder.AppendLine();
        builder.AppendLine("It does not add new physics by itself.");
        builder.AppendLine();
        builder.AppendLine("It defines which calculation engines and guard rails must remain visible before deeper formula changes are made.");

        return builder.ToString();
    }

    private static KeyEngine NewKeyEngine(string name, string path, string layer, string purpose) =>
        new(
            name,
            path,
            layer,
            purpose,
            File.Exists(path));

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

    private static void EnsureDirectoryExists(string path, string description)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"{description} not found: {path}");
    }

    private static void EnsureFileExists(string path, string description)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{description} not found: {path}", path);
    }

    private static string[] EnumerateFiles(string path, string pattern) =>
        Directory.Exists(path)
            ? Directory.GetFiles(path, pattern, SearchOption.AllDirectories)
            : [];

    private static void WriteStep(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"==> {message}");
        Console.ResetColor();
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private sealed record KeyEngine(
        string Name,
        string Path,
        string Layer,
        string Purpose,
        bool Exists);
}

